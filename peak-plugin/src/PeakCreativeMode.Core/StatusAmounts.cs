using System;
namespace PeakCreativeMode.Core
{
 public static class StatusAmounts
 {
  public static float Injury(float amount,float multiplier)=>float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0?0:Math.Max(0,Math.Min(1,amount*multiplier));
  public static float Hunger(float nutrition,float multiplier)=>Injury(nutrition,multiplier);
 }
}
