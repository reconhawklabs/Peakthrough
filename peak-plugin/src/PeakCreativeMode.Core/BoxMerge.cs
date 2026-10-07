using System.Collections.Generic;
namespace PeakCreativeMode.Core {
 public static class BoxMerge {
  public static List<ClipBox> Merge(IEnumerable<ClipBox> input){var boxes=new List<ClipBox>(input);bool changed;do{changed=false;for(int i=0;i<boxes.Count&&!changed;i++)for(int j=i+1;j<boxes.Count;j++){var a=boxes[i];var b=boxes[j];bool x=a.Min.Y==b.Min.Y&&a.Max.Y==b.Max.Y&&a.Min.Z==b.Min.Z&&a.Max.Z==b.Max.Z&&(a.Max.X==b.Min.X||b.Max.X==a.Min.X);bool y=a.Min.X==b.Min.X&&a.Max.X==b.Max.X&&a.Min.Z==b.Min.Z&&a.Max.Z==b.Max.Z&&(a.Max.Y==b.Min.Y||b.Max.Y==a.Min.Y);bool z=a.Min.X==b.Min.X&&a.Max.X==b.Max.X&&a.Min.Y==b.Min.Y&&a.Max.Y==b.Max.Y&&(a.Max.Z==b.Min.Z||b.Max.Z==a.Min.Z);if(!x&&!y&&!z)continue;boxes[i]=new ClipBox(new V3(System.Math.Min(a.Min.X,b.Min.X),System.Math.Min(a.Min.Y,b.Min.Y),System.Math.Min(a.Min.Z,b.Min.Z)),new V3(System.Math.Max(a.Max.X,b.Max.X),System.Math.Max(a.Max.Y,b.Max.Y),System.Math.Max(a.Max.Z,b.Max.Z)));boxes.RemoveAt(j);changed=true;break;}}while(changed);return boxes;}
 }
}
