using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 public sealed class SoundMessage
 {
  public string Id {get;private set;}public V3 Position {get;private set;}public float Volume {get;private set;}public float Pitch {get;private set;}
  private static bool Number(JToken token,out float v){v=0;if(token==null||(token.Type!=JTokenType.Integer&&token.Type!=JTokenType.Float))return false;v=(float)token;return !float.IsNaN(v)&&!float.IsInfinity(v);}
  public static bool TryParse(JObject o,out SoundMessage sound)
  {
   sound=null;if(o["id"]?.Type!=JTokenType.String)return false;string id=(string)o["id"];if(id==null||id.Length>160||!Regex.IsMatch(id,@"^[a-z0-9_]+:[a-z0-9_.-]+$")||!(o["pos"] is JArray p)||p.Count!=3)return false;
   if(!Number(p[0],out float x)||!Number(p[1],out float y)||!Number(p[2],out float z)||Math.Abs(x)>30000000||Math.Abs(y)>4096||Math.Abs(z)>30000000)return false;
   float volume=1,pitch=1;if(o["volume"]!=null&&!Number(o["volume"],out volume)||o["pitch"]!=null&&!Number(o["pitch"],out pitch)||volume<0||volume>16||pitch<=0||pitch>4)return false;
   sound=new SoundMessage{Id=id,Position=new V3(x,y,z),Volume=volume,Pitch=pitch};return true;
  }
 }
}
