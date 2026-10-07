using System;
namespace PeakCreativeMode.Core {
 public readonly struct EffectPlan {
  public readonly float FallDamageScale,JumpMultiplier,SpeedMultiplier,InjuryReliefPerSecond,InstantInjuryRelief;
  public readonly bool ExtraDrag;
  internal EffectPlan(float fall=1,float jump=1,float speed=1,float healing=0,float instant=0,bool drag=false){FallDamageScale=fall;JumpMultiplier=jump;SpeedMultiplier=speed;InjuryReliefPerSecond=healing;InstantInjuryRelief=instant;ExtraDrag=drag;}
  public static EffectPlan For(string id,int amplifier){int strength=Math.Max(0,Math.Min(4,amplifier))+1;switch(id){case "minecraft:slow_falling":return new EffectPlan(fall:0,drag:true);case "minecraft:jump_boost":return new EffectPlan(jump:1+.35f*strength);case "minecraft:speed":return new EffectPlan(speed:1+.2f*strength);case "minecraft:regeneration":return new EffectPlan(healing:.01f*strength);case "minecraft:absorption":return new EffectPlan(instant:.1f);default:return new EffectPlan(fall:1);}}
  public static float Duration(double seconds)=>double.IsFinite(seconds)&&seconds>0?(float)Math.Min(86400,seconds):0;
 }
}
