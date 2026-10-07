using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
 internal sealed class PhotonGuestBridge:IBridgeTransport
 {
  public readonly string Token=Guid.NewGuid().ToString("N");
  private readonly Func<byte,int,object,bool> _send;private readonly string _hello;private readonly Queue<string> _out=new Queue<string>();private readonly RelayInbox _in=new RelayInbox();
  private readonly RelayFrames _frames=new RelayFrames();private int _bytes;private long _sequence;private bool _running;private float _nextHello,_nextFlush;
  public BridgeStatus Status{get;private set;}=BridgeStatus.Offline;public string LastError{get;private set;}
  public PhotonGuestBridge(string hello,Func<byte,int,object,bool> send=null){_send=send??PhotonRelay.Send;_hello=hello;_frames.Reset(Token);}
  public void Start(){_running=true;Status=BridgeStatus.Connecting;PhotonRelay.Instance.Guest=this;}
  public void Stop(){_running=false;Status=BridgeStatus.Offline;_out.Clear();_in.Clear();_bytes=0;if(PhotonRelay.Instance?.Guest==this)PhotonRelay.Instance.Guest=null;}
  public bool Send(string line){if(!_running||Status!=BridgeStatus.Connected||line==null||line.Length>=65536||_bytes+line.Length>262144)return false;_out.Enqueue(line);_bytes+=line.Length;return true;}
  public bool TryReceive(out JObject message)=>_in.TryReceive(out message);
  public bool NeedsReconnect=>_in.NeedsReconnect;
  public void Tick()
  {
   if(!_running||NeedsReconnect||!PhotonNetwork.InRoom||PhotonNetwork.MasterClient==null)return;
   if(Status!=BridgeStatus.Connected&&Status!=BridgeStatus.Rejected&&Time.unscaledTime>=_nextHello){_nextHello=Time.unscaledTime+2;Transmit(_hello+"\n");}
   if(Time.unscaledTime<_nextFlush)return;_nextFlush=Time.unscaledTime+.05f;
   var batch=new StringBuilder();while(_out.Count>0&&batch.Length+_out.Peek().Length+1<=65536){var line=_out.Dequeue();_bytes-=line.Length;batch.Append(line).Append('\n');}
   if(batch.Length>0)Transmit(batch.ToString());
  }
  private void Transmit(string batch){foreach(var packet in RelayFrames.Split(Token,_sequence++,batch))_send(PhotonRelay.RequestEvent,PhotonNetwork.MasterClient.ActorNumber,packet);}
  public void Receive(int sender,object payload)
  {
   if(NeedsReconnect)return;
   int master=PhotonNetwork.MasterClient?.ActorNumber??-1;
   if(payload is object[] status&&status.Length==4&&status[0] is int version&&version==1&&status[1] is string token&&status[2] is string kind&&kind=="status"&&status[3] is int value)
   {
    if(!RelayAuthority.Response(sender,master,token,Token)||value<0||value>3)return;
    if(value!=(int)BridgeStatus.Connected){Status=(BridgeStatus)value;if(Status==BridgeStatus.Rejected)LastError="Host native bridge rejected the connection";_in.Clear();_out.Clear();_bytes=0;}
    return;
   }
   if(!RelayAuthority.Response(sender,master,RelayFrames.PacketToken(payload),Token))return;
   string batch=_frames.Accept(payload);if(batch==null)return;
   foreach(string line in batch.Split('\n'))
   {
    var message=Protocol.Parse(line);if(message==null)continue;
    if((string)message["t"]=="welcome")
    {
     if(message["protocol"]?.Type!=JTokenType.Integer||(int)message["protocol"]!=Protocol.Version){Status=BridgeStatus.Rejected;LastError="protocol mismatch";return;}
     Status=BridgeStatus.Connected;Plugin.Log.LogInfo("[relay] Guest welcome through host actor "+master);
    }
    if((string)message["t"]=="error"){Status=BridgeStatus.Rejected;LastError=(string)message["reason"];}
    if(!_in.TryEnqueue(message)){Status=BridgeStatus.Offline;_out.Clear();_bytes=0;return;}
   }
  }
 }
}
