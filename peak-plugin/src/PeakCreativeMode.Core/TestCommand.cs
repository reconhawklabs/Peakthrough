using System;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public sealed class TestCommand {
  public string Id,Op;public V3 Position;public float Amount,Yaw,Pitch;public int Size;public ushort ItemId=33;
  private static float Number(JObject o,string key,float min,float max){var t=o[key];if(t==null||(t.Type!=JTokenType.Integer&&t.Type!=JTokenType.Float))throw new ArgumentException(key);float value=(float)t;if(float.IsNaN(value)||float.IsInfinity(value)||value<min||value>max)throw new ArgumentException(key);return value;}
  public static TestCommand Parse(JObject o){try{if(o["id"]?.Type!=JTokenType.String||o["op"]?.Type!=JTokenType.String)return null;var c=new TestCommand{Id=(string)o["id"],Op=(string)o["op"]};if(c.Id.Length==0||c.Id.Length>64)return null;switch(c.Op){case "lightfire":float segment=Number(o,"segment",1,4);if(segment!=(int)segment)return null;c.Size=(int)segment;break;case "nativegive":if(o["itemId"]!=null)c.ItemId=(ushort)Number(o,"itemId",0,65534);break;case "openluggage":float count=Number(o,"count",1,3);if(count!=(int)count)return null;c.Size=(int)count;break;case "climb":case "campfires":case "spawndump":case "die":case "handstate":case "observe":case "relay":case "restore":case "disconnect":case "connect":break;case "terraform":float size=Number(o,"size",4,8);if(size!=(int)size)return null;c.Size=(int)size;goto case "warp";case "throwat":case "explode":case "warp":case "clip":case "lightprobe":c.Position=new V3(Number(o,"x",-30000000,30000000),Number(o,"y",-2000,2000),Number(o,"z",-30000000,30000000));break;case "look":c.Yaw=Number(o,"yaw",-360,360);c.Pitch=Number(o,"pitch",-85,85);break;case "injury":case "hunger":c.Amount=Number(o,"amount",0,1);break;default:return null;}return c;}catch{return null;}}
 }
}
