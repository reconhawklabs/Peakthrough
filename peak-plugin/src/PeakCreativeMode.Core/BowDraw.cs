using System;
namespace PeakCreativeMode.Core {
 public static class BowDraw {
  public static int Stage(bool usingItem,float ticks){if(!usingItem)return -1;if(float.IsNaN(ticks)||float.IsInfinity(ticks)||ticks<0)ticks=0;return ticks>=18?2:ticks>=13?1:0;}
  public static float FovScale(float zoom)=>1-.1f*Math.Max(0,Math.Min(1,float.IsNaN(zoom)||float.IsInfinity(zoom)?0:zoom));
 }
}
