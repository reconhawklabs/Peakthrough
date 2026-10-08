using System;
namespace PeakCreativeMode.Core {
 public static class Knockback {
  public static float SourceScale(string source,float scale,float creeper){if(!float.IsFinite(scale)||scale<=0)return 0;if(source!="creeper"&&source!="tnt")return scale;if(!float.IsFinite(creeper)||creeper<0)return 0;return scale*Math.Min(6,creeper);}
  // A three-quarter-second equivalent blast, capped at 30 m/s by ToUnity's 40-unit bound.
  public static V3 CreeperVelocity(double[] mc,float scale){var force=ToUnity(mc,scale);return new V3(force.X*.75f,force.Y*.75f,force.Z*.75f);}
  public static V3 ToUnity(double[] mc,float scale){
   if(mc==null||mc.Length!=3||!float.IsFinite(scale)||scale<=0)return default;
   double max=0;foreach(var n in mc){if(!double.IsFinite(n))return default;max=Math.Max(max,Math.Abs(n));}
   if(max==0)return default;
   double x=mc[0]/max,y=mc[1]/max,z=-mc[2]/max;
   double norm=Math.Sqrt(x*x+y*y+z*z),magnitude=Math.Min(40,max*norm*scale);
   return new V3((float)(x/norm*magnitude),(float)(y/norm*magnitude),(float)(z/norm*magnitude));
  }
 }
}
