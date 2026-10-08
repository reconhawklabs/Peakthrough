using System;
namespace PeakCreativeMode.Core {
 /// Bounds reliable Photon traffic without losing packets or banking an idle-time burst.
 public sealed class RelayPacer {
  public const int BytesPerSecond=65536,BurstBytes=20480;
  private double _credit=BurstBytes,_last;
  public bool TrySend(int bytes,double now,Func<bool> send){
   if(bytes<1||bytes>BurstBytes||double.IsNaN(now)||double.IsInfinity(now))return false;
   _credit=Math.Min(BurstBytes,_credit+Math.Max(0,now-_last)*BytesPerSecond);_last=Math.Max(_last,now);
   if(bytes>_credit||!send())return false;
   _credit-=bytes;return true;
  }
 }
}
