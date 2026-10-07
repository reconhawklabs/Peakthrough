using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public readonly struct AvatarHeld {
  public readonly string Player,Item;public readonly PeakToken Peak;public readonly bool Using;public readonly float Swing;
  public AvatarHeld(string player,string item,PeakToken peak,bool usingItem,float swing=0){Swing=swing;Player=player;Item=item;Peak=peak;Using=usingItem;}
  public static bool TryParse(JObject o,out AvatarHeld held){held=default;if(o?["player"]?.Type!=JTokenType.String||!Guid.TryParse((string)o["player"],out _))return false;var item=o["item"];if(item==null||(item.Type!=JTokenType.Null&&item.Type!=JTokenType.String))return false;string id=(string)item;if(id!=null&&(!Regex.IsMatch(id,"^minecraft:[a-z0-9_./]{1,64}$")||id.Contains("..")))return false;if(o["using"]!=null&&o["using"].Type!=JTokenType.Boolean)return false;PeakToken peak=null;if(o["peak"]!=null&&o["peak"].Type!=JTokenType.Null){peak=PeakToken.FromJson(o["peak"]);if(peak==null)return false;}float swing=0;if(o["swing"]!=null){if(o["swing"].Type!=JTokenType.Float&&o["swing"].Type!=JTokenType.Integer)return false;swing=(float)o["swing"];if(!float.IsFinite(swing)||swing<0||swing>1)return false;}held=new AvatarHeld((string)o["player"],id,peak,(bool?)o["using"]??false,swing);return true;}
 }
}
