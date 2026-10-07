using System;using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public static class HazardPose {public static bool TryRadius(JToken t,out float radius){radius=0;if(t==null||(t.Type!=JTokenType.Integer&&t.Type!=JTokenType.Float))return false;double n=(double)t;if(double.IsNaN(n)||double.IsInfinity(n)||n<0||n>64)return false;radius=(float)n;return true;}}
 public static class MeleeTarget {public static bool CanHit(string part,float distance){if(float.IsNaN(distance)||float.IsInfinity(distance)||distance<0)return false;if(part!=null&&Array.IndexOf(DragonPose.PartNames,part)<0)return false;return distance<=(part==null?4.5f:8f);}}
}
