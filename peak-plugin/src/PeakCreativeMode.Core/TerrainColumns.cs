using System;
namespace PeakCreativeMode.Core
{
 public static class TerrainColumns
 {
  public static int Surface(float y)=>(int)Math.Floor(y/Coords.BLOCK_SIZE-.001f);
 }
}
