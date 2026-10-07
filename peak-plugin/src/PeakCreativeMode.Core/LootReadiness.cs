using System;
namespace PeakCreativeMode.Core {
 public static class LootReadiness {
  public static bool Ready(V3 unityFeet,V3 mcPose){double dx=unityFeet.X-mcPose.X,dz=unityFeet.Z+mcPose.Z,dy=unityFeet.Y-mcPose.Y;return dx*dx+dz*dz<=25&&Math.Abs(dy)<=5;}
 }
}
