namespace PeakCreativeMode.Core
{
 public enum TerrainMaterial{Stone,Dirt,Sandstone}
 public static class MineGrid
 {
  public const int Cell=4;
  /// Origin (Unity space, min corner) of the grid-aligned 4³ cell containing a point just inside the surface.
  public static V3 CellOrigin(V3 insidePoint)=>new V3(Floor(insidePoint.X),Floor(insidePoint.Y),Floor(insidePoint.Z));
  private static float Floor(float v)=>(float)System.Math.Floor(v/Cell)*Cell;
  public static bool CanDig(string heldItem,TerrainMaterial m){if(heldItem==null)return false;if(heldItem.EndsWith("_pickaxe"))return true;return heldItem.EndsWith("_shovel")&&m==TerrainMaterial.Dirt;}
 }
 /// At most one request per Interval, dedupes cells already requested, at most MaxPending in flight.
 public sealed class ConversionThrottle
 {
  public const float Interval=.4f;public const int MaxPending=3;
  private readonly System.Collections.Generic.HashSet<V3Key> _requested=new System.Collections.Generic.HashSet<V3Key>();private float _last=-999;private int _pending;
  public bool TryRequest(V3 origin,float now){var k=new V3Key(origin);if(_requested.Contains(k)||_pending>=MaxPending||now-_last<Interval)return false;_requested.Add(k);_last=now;_pending++;return true;}
  public void Completed(){if(_pending>0)_pending--;}
  public void Reset(){_requested.Clear();_pending=0;_last=-999;}
  private readonly struct V3Key:System.IEquatable<V3Key>{private readonly int X,Y,Z;public V3Key(V3 v){X=(int)v.X;Y=(int)v.Y;Z=(int)v.Z;}public bool Equals(V3Key o)=>X==o.X&&Y==o.Y&&Z==o.Z;public override bool Equals(object o)=>o is V3Key k&&Equals(k);public override int GetHashCode()=>System.HashCode.Combine(X,Y,Z);}
 }
}
