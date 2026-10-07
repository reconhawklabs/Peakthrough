using System;
namespace PeakCreativeMode.Core {
 public static class FishingLine {
  public static V3 Point(V3 a,V3 b,float t){t=Math.Max(0,Math.Min(1,t));float dx=b.X-a.X,dz=b.Z-a.Z;float sag=Math.Min(1.5f,(float)Math.Sqrt(dx*dx+dz*dz)*.08f);return new V3(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t-4*t*(1-t)*sag,a.Z+(b.Z-a.Z)*t);}
 }
}
