using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
using ExitGames.Client.Photon;
using Wire=PeakCreativeMode.Core.Protocol;
namespace PeakCreativeMode.Plugin
{
 /// Offline-only SDK dispatch/codec/native integration; enabled solely by the test-helper config.
 internal sealed class PhotonRelayCheck:MonoBehaviour
 {
  public static bool Active{get;private set;}
  private const int Actor=900001;public static string Result="not run";
  private readonly Protocol18 _codec=new Protocol18();
  private PhotonGuestBridge _guest,_saved;private Photon.Realtime.Player _fake;private Photon.Realtime.Room _room;
  private readonly List<JObject> _messages=new List<JObject>();private bool _running;
  public void Begin()
  {
   if(_running)return;
   if(!Plugin.Keys.EnableAutomation.Value||!PhotonNetwork.OfflineMode||!WorldDiagnostics.Playing(Character.localCharacter)){Result="requires enabled offline live host";return;}
   _running=true;Active=true;Result="running";StartCoroutine(Run());
  }
  private void Drain(){_guest.Tick();while(_guest.TryReceive(out var message))_messages.Add(message);}
  private JObject Find(Func<JObject,bool> match)=>_messages.Find(m=>match(m));
  private IEnumerator Wait(Func<bool> done,float timeout=10)
  {float deadline=Time.unscaledTime+timeout;while(!done()){Drain();if(Time.unscaledTime>deadline)throw new Exception("Timed out: "+Result);yield return null;}}
  private bool Inject(byte code,int target,object payload)
  {
   var data=new EventData{Code=code};data.Parameters[(byte)254]=Actor;
   data.Parameters[(byte)245]=_codec.Deserialize(_codec.Serialize(payload));
   PhotonNetwork.NetworkingClient.OnEvent(data);return true;
  }
  private void Action(string kind,JObject target){_messages.Clear();_guest.Send(new JObject{["t"]="action",["kind"]=kind,["target"]=target}.ToString(Newtonsoft.Json.Formatting.None));}
  private IEnumerator Body()
  {
   _room=PhotonNetwork.CurrentRoom;
   if(_room.Players.ContainsKey(Actor))throw new Exception("Diagnostic actor already present");
   _fake=(Photon.Realtime.Player)Activator.CreateInstance(typeof(Photon.Realtime.Player),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{"PEAK relay diagnostic",Actor,false},null);
   _room.Players[Actor]=_fake;_saved=PhotonRelay.Instance.Guest;
   _guest=new PhotonGuestBridge(Wire.Hello("diagnostic","ignored"),Inject);_guest.Start();
   Result="waiting for native welcome";
   yield return Wait(()=>_guest.Status==BridgeStatus.Connected&&Find(m=>(string)m["t"]=="inventory")!=null);
   string uuid=(string)Find(m=>(string)m["t"]=="welcome")?["playerUuid"];
   if(!Guid.TryParse(uuid,out _))throw new Exception("Native UUID missing");
   int x=1000+DateTime.UtcNow.Second*16,z=1000;
   _guest.Send(Wire.Pose(x+.5f,402.4f,-z-.5f,0,80));
   _guest.Send(new JObject{["t"]="terrain",["origin"]=new JArray(0,0,0),["size"]=new JArray(1,1,1),["solid"]=new JArray{new JArray(x,400,-z-1)},["water"]=new JArray()}.ToString(Newtonsoft.Json.Formatting.None));
   Result="granting native stone";Action("creative_give",new JObject{["block"]="minecraft:stone"});
   yield return Wait(()=>Find(m=>(string)m["t"]=="held"&&(string)m["item"]=="minecraft:stone")!=null);
   Result="shared placement";Action("use",new JObject{["x"]=x,["y"]=400,["z"]=z,["face"]="up"});
   yield return Wait(()=>Find(m=>(string)m["t"]=="block"&&(int?)m["x"]==x&&(int?)m["y"]==401&&(string)m["state"]=="minecraft:stone")!=null&&BlockRenderer.Instance.Grid.Get(new BlockPos(x,401,z))=="minecraft:stone");
   yield return Wait(()=>Find(m=>(string)m["t"]=="inventory"&&(int?)m["slots"]?[0]?["count"]==63)!=null);
   Result="mining diagnostic block";Action("creative_give",new JObject{["item"]="minecraft:diamond_pickaxe"});
   yield return Wait(()=>Find(m=>(string)m["t"]=="held"&&(string)m["item"]=="minecraft:diamond_pickaxe")!=null);
   Action("mine_start",new JObject{["x"]=x,["y"]=401,["z"]=z,["face"]="up"});
   yield return Wait(()=>Find(m=>(string)m["t"]=="block"&&(int?)m["x"]==x&&(int?)m["y"]==401&&(string)m["state"]=="minecraft:air")!=null&&BlockRenderer.Instance.Grid.Get(new BlockPos(x,401,z))==BlockGrid.Air);
   // New tokens must remain usable beyond the retired-token replay window.
   for(int attempt=0;attempt<18;attempt++)
   {
    Result="reconnect "+(attempt+1)+" of 18";
    _guest.Stop();_messages.Clear();
    _guest=new PhotonGuestBridge(Wire.Hello("diagnostic","ignored"),Inject);_guest.Start();
    yield return Wait(()=>_guest.Status==BridgeStatus.Connected&&Find(m=>(string)m["t"]=="inventory")!=null);
    var welcome=Find(m=>(string)m["t"]=="welcome");
    if((string)welcome?["playerUuid"]!=uuid||(string)welcome?["mapSeed"]!=BridgeBehaviour.Instance.MapSeed)throw new Exception("Reconnect ownership changed");
    var inventory=Find(m=>(string)m["t"]=="inventory");
    if((string)inventory?["slots"]?[0]?["item"]!="minecraft:diamond_pickaxe")throw new Exception("Private tool lost on reconnect");
   }
   Result="overflow recovery";
   // Deliver a valid oversized burst through the real guest decoder, then demand a fresh handshake.
   var burst=new System.Text.StringBuilder();for(int i=0;i<10001;i++)burst.Append("{\"t\":\"debug_state\"}\n");
   foreach(var packet in RelayFrames.Split(_guest.Token,1000000,burst.ToString()))_guest.Receive(PhotonNetwork.MasterClient.ActorNumber,_codec.Deserialize(_codec.Serialize(packet)));
   if(!_guest.NeedsReconnect||_guest.Status!=BridgeStatus.Offline||_guest.TryReceive(out _))throw new Exception("Overflow retained a partial snapshot");
   _guest.Stop();_messages.Clear();_guest=new PhotonGuestBridge(Wire.Hello("diagnostic","ignored"),Inject);_guest.Start();
   yield return Wait(()=>_guest.Status==BridgeStatus.Connected&&Find(m=>(string)m["t"]=="inventory")!=null);
   if((string)Find(m=>(string)m["t"]=="welcome")?["playerUuid"]!=uuid)throw new Exception("Overflow reconnect changed owner");
   Result="passed: Photon codec/dispatch, private UUID/inventory, host run identity, shared placement/mining, 18 reconnects and overflow recovery";
  }
  private IEnumerator Run()
  {
   var body=Body();var stack=new Stack<IEnumerator>();stack.Push(body);
   while(stack.Count>0)
   {
    bool next=false;object current=null;Exception error=null;
    try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}catch(Exception e){error=e;}
    if(error!=null){Result="failed: "+error.Message;break;}
    if(!next){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
   }
   Cleanup();Plugin.Log.LogInfo("[relay check] "+Result);
  }
  private void Cleanup()
  {
   _guest?.Stop();if(PhotonRelay.Instance!=null)PhotonRelay.Instance.Guest=_saved;
   if(_room!=null&&_room.Players.TryGetValue(Actor,out var p)&&p==_fake)_room.Players.Remove(Actor);
   Active=false;_running=false;_guest=null;_fake=null;_room=null;
  }
  private void OnDestroy(){Cleanup();}
 }
}
