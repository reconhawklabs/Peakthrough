namespace PeakCreativeMode.Core {
 public static class WarpTarget {
  public static V3 Center(V3 requestedFeet,V3 currentCenter,V3 currentFeet)=>new V3(requestedFeet.X+currentCenter.X-currentFeet.X,requestedFeet.Y+currentCenter.Y-currentFeet.Y,requestedFeet.Z+currentCenter.Z-currentFeet.Z);
 }
}
