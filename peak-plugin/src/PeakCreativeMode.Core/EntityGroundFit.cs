using System;
namespace PeakCreativeMode.Core {
 /** Render-only contact correction; airborne motion retains its takeoff offset. */
 public static class EntityGroundFit {
  public static float Offset(bool grounded,float nativeY,float? surfaceY,float previous,float dt){
   if(!float.IsFinite(previous))previous=0;
   if(!grounded||!float.IsFinite(dt)||dt<=0)return previous;
   float target=0;
   if(float.IsFinite(nativeY)&&surfaceY.HasValue&&float.IsFinite(surfaceY.Value)){
    float gap=surfaceY.Value-nativeY;if(gap>=-1.25f&&gap<=.2f)target=gap;
   }
   return previous+(target-previous)*(1-(float)Math.Exp(-18*dt));
  }
 }
}
