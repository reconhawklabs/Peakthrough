using System;
namespace PeakCreativeMode.Core
{
    public static class TowerPlacement
    {
        public static float JumpSpeed(float current,float minimum)
            => minimum>0 ? Math.Max(current,minimum) : current;
        public static bool FeetClear(float feet,float top) => feet>=top+.02f;
        public static bool Pending(float now,float until) => now<=until;
    }
}
