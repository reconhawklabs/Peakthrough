using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Protocol=PeakCreativeMode.Core.Protocol;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
 internal sealed class PhotonRelay:MonoBehaviour,IOnEventCallback
 {
  public const byte RequestEvent=190,ResponseEvent=191;
  public static PhotonRelay Instance{get;private set;}public PhotonGuestBridge Guest;
  private sealed class Peer
  {
   public string Token;public RelayFrames Inbound=new RelayFrames();public readonly RetiredRelayTokens Retired=new RetiredRelayTokens();
   public BridgeClient Client;public long Sequence;public BridgeStatus SentStatus=(BridgeStatus)(-1);public readonly Queue<string> Out=new Queue<string>();public int Bytes;public float NextHand;
  }
  private readonly Dictionary<int,Peer> _peers=new Dictionary<int,Peer>();private float _flush;private string _seed;private bool _warned;
  private void Awake(){Instance=this;PhotonNetwork.AddCallbackTarget(this);}
  public static bool Send(byte code,int actor,object payload)=>PhotonNetwork.RaiseEvent(code,payload,new RaiseEventOptions{TargetActors=new[]{actor},Receivers=PhotonNetwork.OfflineMode&&PhotonRelayCheck.Active?ReceiverGroup.All:ReceiverGroup.Others},SendOptions.SendReliable);
  public void OnEvent(EventData data)
  {
   if(data.Code==ResponseEvent){Guest?.Receive(data.Sender,data.CustomData);return;}
   if(data.Code!=RequestEvent||!RelayAuthority.Request(data.Sender,PhotonNetwork.LocalPlayer?.ActorNumber??0,PhotonNetwork.IsMasterClient,PhotonNetwork.CurrentRoom?.GetPlayer(data.Sender)!=null))return;
   try
   {
    string token=RelayFrames.PacketToken(data.CustomData);if(token==null)return;
    if(!_peers.TryGetValue(data.Sender,out var peer)){if(_peers.Count>=3)return;peer=new Peer();_peers[data.Sender]=peer;}
    if(peer.Retired.Contains(token))return;
    if(peer.Inbound.Token!=null&&peer.Inbound.Token!=token){peer.Retired.Retire(peer.Inbound.Token);peer.Inbound.Reset(token);}
    string batch=peer.Inbound.Accept(data.CustomData);if(batch==null)return;
    foreach(string line in batch.Split('\n'))
    {
     if(line.Length>65536)continue;var message=Protocol.Parse(line);if(message==null)continue;string type=(string)message["t"];
     if(type=="hello")
     {
      if(peer.Token==token&&peer.Client!=null)continue;
      peer.Client?.Stop();peer.Out.Clear();peer.Bytes=0;peer.Sequence=0;peer.Token=token;
      var player=PhotonNetwork.CurrentRoom.GetPlayer(data.Sender);
      message["playerId"]=RelayAuthority.PlayerId(player.UserId,data.Sender);message["mapSeed"]=RunIdentity.Current();message["biomes"]=new JArray(RunIdentity.Biomes());
      string hello=message.ToString(Formatting.None);peer.Client=new BridgeClient("127.0.0.1",Plugin.Keys.BridgePort.Value,()=>hello,lineLog=>Plugin.Log.LogInfo("[relay actor "+data.Sender+"] "+lineLog));peer.Client.Start();peer.SentStatus=(BridgeStatus)(-1);
     }
     else if(peer.Token==token&&type=="hand"){if((string)message["op"]=="death"||Time.unscaledTime>=peer.NextHand){peer.NextHand=Time.unscaledTime+.25f;PeakItemBridge.HandleHand(data.Sender,message);}}
     else if(peer.Token==token&&(type=="pose"||type=="terrain"||type=="action"))peer.Client?.Send(line);
    }
   }catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[relay] Invalid peer frame: "+e.Message);}}
  }
  private void Update()
  {
   if(!PhotonNetwork.InRoom||!PhotonNetwork.IsMasterClient||(PhotonNetwork.OfflineMode&&!PhotonRelayCheck.Active)){ClearPeers();return;}
   string seed=RunIdentity.Current();if(_seed!=null&&_seed!=seed)ClearPeers();_seed=seed;
   foreach(int actor in new List<int>(_peers.Keys))if((PhotonNetwork.CurrentRoom.GetPlayer(actor)==null||PhotonNetwork.CurrentRoom.GetPlayer(actor).IsInactive)){_peers[actor].Client?.Stop();_peers.Remove(actor);}
   foreach(var pair in _peers)
   {
    var peer=pair.Value;if(peer.Client==null)continue;
    if(peer.Client.Status!=peer.SentStatus){peer.SentStatus=peer.Client.Status;Send(ResponseEvent,pair.Key,new object[]{1,peer.Token,"status",(int)peer.SentStatus});if(peer.SentStatus!=BridgeStatus.Connected){peer.Out.Clear();peer.Bytes=0;}}
    while(peer.Bytes<262144&&peer.Client.TryReceive(out var message))
    {
     if(peer.Client.Status!=BridgeStatus.Connected&&(string)message["t"]!="error")continue;
     string line=message.ToString(Formatting.None);if(peer.Bytes+line.Length>1048576){peer.Client.Stop();peer.Out.Clear();peer.Bytes=0;break;}peer.Out.Enqueue(line);peer.Bytes+=line.Length;
    }
   }
   if(Time.unscaledTime<_flush)return;_flush=Time.unscaledTime+.05f;
   foreach(var pair in _peers)
   {
    var peer=pair.Value;var batch=new StringBuilder();while(peer.Out.Count>0&&batch.Length+peer.Out.Peek().Length+1<=65536){string line=peer.Out.Dequeue();peer.Bytes-=line.Length;batch.Append(line).Append('\n');}
    if(batch.Length==0&&peer.Out.Count>0){string line=peer.Out.Dequeue();peer.Bytes-=line.Length;batch.Append(line).Append('\n');}
    if(batch.Length>0)foreach(var packet in RelayFrames.Split(peer.Token,peer.Sequence++,batch.ToString()))Send(ResponseEvent,pair.Key,packet);
   }
  }
  private void ClearPeers(){foreach(var peer in _peers.Values)peer.Client?.Stop();_peers.Clear();}
  private void OnDestroy(){PhotonNetwork.RemoveCallbackTarget(this);ClearPeers();Guest?.Stop();Instance=null;}
 }
}
