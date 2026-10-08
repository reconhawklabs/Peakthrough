using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 /// Reliable state/events stay lossless; only superseded motion/debug snapshots are replaced.
 public sealed class BridgeMessageQueue {
  private readonly object _lock=new object();private readonly Queue<JObject> _control=new Queue<JObject>(),_world=new Queue<JObject>();
  private readonly LinkedList<JObject> _state=new LinkedList<JObject>();private readonly Dictionary<string,LinkedListNode<JObject>> _latest=new Dictionary<string,LinkedListNode<JObject>>();private bool _worldNext;
  private static string Key(JObject m)=>((string)m["t"])=="entity_update"?"entity:"+m["id"]:((string)m["t"]=="debug_state"?"debug":null);
  public int Count{get{lock(_lock)return _control.Count+_world.Count+_state.Count;}}
  public void Enqueue(JObject m){lock(_lock){string t=(string)m["t"],key=Key(m);
   if(t=="entity_remove"){var entity="entity:"+m["id"];if(_latest.TryGetValue(entity,out var old)){_state.Remove(old);_latest.Remove(entity);}}
   if(key!=null){if(_latest.TryGetValue(key,out var node))node.Value=m;else _latest[key]=_state.AddLast(m);}
   else if(t=="chunk"||t=="block")_world.Enqueue(m);else _control.Enqueue(m);
  }}
  private JObject Peek()=>_control.Count>0?_control.Peek():_world.Count>0&&(_worldNext||_state.Count==0)?_world.Peek():_state.First?.Value;
  public bool TryPeek(out JObject m){lock(_lock){m=Peek();return m!=null;}}
  public bool TryDequeue(out JObject m){lock(_lock){m=Peek();if(m==null)return false;
   if(_control.Count>0)_control.Dequeue();else if(_world.Count>0&&(_worldNext||_state.Count==0)){_world.Dequeue();_worldNext=false;}else{_state.RemoveFirst();_latest.Remove(Key(m));_worldNext=true;}return true;
  }}
  public void Clear(){lock(_lock){_control.Clear();_world.Clear();_state.Clear();_latest.Clear();_worldNext=false;}}
 }
}
