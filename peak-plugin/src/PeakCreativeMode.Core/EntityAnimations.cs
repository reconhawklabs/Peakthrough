using System;
using System.Collections.Generic;
namespace PeakCreativeMode.Core {
 public readonly struct EntityAnimationPart {public readonly string Part;public readonly int Sign;public readonly bool Wing;public EntityAnimationPart(string part,int sign,bool wing=false){Part=part;Sign=sign;Wing=wing;}}
 public static class EntityAnimations {
  private static readonly IReadOnlyList<EntityAnimationPart> Biped=Array.AsReadOnly(new[]{new EntityAnimationPart("right_leg",1),new EntityAnimationPart("left_leg",-1)});
  private static readonly IReadOnlyList<EntityAnimationPart> Quad=Array.AsReadOnly(new[]{new EntityAnimationPart("right_hind_leg",1),new EntityAnimationPart("left_hind_leg",-1),new EntityAnimationPart("right_front_leg",-1),new EntityAnimationPart("left_front_leg",1)});
  private static readonly IReadOnlyList<EntityAnimationPart> Spider=Array.AsReadOnly(new[]{new EntityAnimationPart("right_hind_leg",1),new EntityAnimationPart("left_hind_leg",-1),new EntityAnimationPart("right_middle_hind_leg",-1),new EntityAnimationPart("left_middle_hind_leg",1),new EntityAnimationPart("right_middle_front_leg",1),new EntityAnimationPart("left_middle_front_leg",-1),new EntityAnimationPart("right_front_leg",-1),new EntityAnimationPart("left_front_leg",1)});
  private static readonly IReadOnlyList<EntityAnimationPart> Chicken=Array.AsReadOnly(new[]{new EntityAnimationPart("right_leg",1),new EntityAnimationPart("left_leg",-1),new EntityAnimationPart("right_wing",1,true),new EntityAnimationPart("left_wing",-1,true)});
  public static IReadOnlyList<EntityAnimationPart> For(string type){switch(type){case "minecraft:zombie":case "minecraft:skeleton":return Biped;case "minecraft:cow":case "minecraft:pig":case "minecraft:creeper":return Quad;case "minecraft:spider":return Spider;case "minecraft:chicken":return Chicken;default:return Array.Empty<EntityAnimationPart>();}}
 }
}
