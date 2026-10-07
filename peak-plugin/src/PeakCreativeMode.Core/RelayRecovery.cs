using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 // Photon delivers reliable packets in order; retain a bounded window of retired connections.
 public sealed class RetiredRelayTokens
 {
  private readonly HashSet<string> _tokens=new HashSet<string>();
  private readonly Queue<string> _order=new Queue<string>();
  public int Count=>_tokens.Count;
  public bool Contains(string token)=>_tokens.Contains(token);
  public void Retire(string token)
  {
   if(string.IsNullOrEmpty(token)||!_tokens.Add(token))return;
   _order.Enqueue(token);
   while(_order.Count>16)_tokens.Remove(_order.Dequeue());
  }
 }
 // A partial snapshot is unsafe to apply. Overflow requires a fresh welcome and full snapshot.
 public sealed class RelayInbox
 {
  private readonly Queue<JObject> _messages=new Queue<JObject>();
  private readonly int _capacity;
  public bool NeedsReconnect{get;private set;}
  public RelayInbox(int capacity=10000){if(capacity<1)throw new System.ArgumentOutOfRangeException(nameof(capacity));_capacity=capacity;}
  public bool TryEnqueue(JObject message)
  {
   if(NeedsReconnect)return false;
   if(_messages.Count>=_capacity){_messages.Clear();NeedsReconnect=true;return false;}
   _messages.Enqueue(message);return true;
  }
  public bool TryReceive(out JObject message){if(_messages.Count==0){message=null;return false;}message=_messages.Dequeue();return true;}
  public void Clear()=>_messages.Clear();
 }
}
