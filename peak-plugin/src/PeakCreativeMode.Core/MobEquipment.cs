using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
namespace PeakCreativeMode.Core {
 public readonly struct MobEquipment {
  public readonly string Item;public readonly bool Using;
  private MobEquipment(string item,bool usingItem){Item=item;Using=usingItem;}
  public static bool TryParse(JToken token,out MobEquipment equipment){equipment=default;if(token==null||token.Type==JTokenType.Null)return true;if(!(token is JObject o))return false;if(o["using"]!=null&&o["using"].Type!=JTokenType.Boolean)return false;var id=o["item"];if(id!=null&&id.Type!=JTokenType.Null&&(id.Type!=JTokenType.String||!Regex.IsMatch((string)id,@"^minecraft:[a-z0-9_]+$")))return false;equipment=new MobEquipment((string)id,o["using"]?.Value<bool>()??false);return true;}
 }
}
