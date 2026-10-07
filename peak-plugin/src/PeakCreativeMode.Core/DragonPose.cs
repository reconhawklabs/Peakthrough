using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public sealed class DragonPartPose {public string Part;public V3 Offset;public float Width,Height;}
 public sealed class DragonPose {
  public static readonly string[] PartNames={"head","neck","body","tail1","tail2","tail3","wing1","wing2"};
  public float Flap;public int Phase,DeathTime;public readonly List<DragonPartPose> Parts=new List<DragonPartPose>();
  private static bool Number(JToken t,double min,double max,out float value){value=0;if(t==null||(t.Type!=JTokenType.Integer&&t.Type!=JTokenType.Float))return false;double n=(double)t;if(double.IsNaN(n)||double.IsInfinity(n)||n<min||n>max)return false;value=(float)n;return true;}
  public static bool TryParse(JObject o,out DragonPose pose){pose=null;if(o==null||!Number(o["flap"],-1000000,1000000,out float f)||o["phase"]?.Type!=JTokenType.Integer||!Number(o["phase"],0,10,out float phase))return false;var d=new DragonPose{Flap=f,Phase=(int)phase};if(o["deathTime"]!=null){if(o["deathTime"].Type!=JTokenType.Integer||!Number(o["deathTime"],0,200,out float time))return false;d.DeathTime=(int)time;}if(o["parts"]!=null){if(!(o["parts"] is JArray parts)||parts.Count>8)return false;var names=new HashSet<string>();foreach(var token in parts){if(!(token is JObject p)||p["part"]?.Type!=JTokenType.String)return false;string name=(string)p["part"];if(!PartNames.Contains(name)||!names.Add(name)||!(p["offset"] is JArray a)||a.Count!=3||!Number(a[0],-64,64,out float x)||!Number(a[1],-64,64,out float y)||!Number(a[2],-64,64,out float z)||!EntitySize.TryParse(p["size"],out float w,out float h))return false;d.Parts.Add(new DragonPartPose{Part=name,Offset=new V3(x,y,z),Width=w,Height=h});}}pose=d;return true;}
 }
}
