using System;
namespace PeakCreativeMode.Core {
 public static class BlockTargets {
  public static BlockPos Adjacent(BlockPos hit,string face){switch(face){case "up":return hit.Offset(0,1,0);case "down":return hit.Offset(0,-1,0);case "east":return hit.Offset(1,0,0);case "west":return hit.Offset(-1,0,0);case "north":return hit.Offset(0,0,-1);case "south":return hit.Offset(0,0,1);default:throw new ArgumentException("Native block face");}}
 }
}
