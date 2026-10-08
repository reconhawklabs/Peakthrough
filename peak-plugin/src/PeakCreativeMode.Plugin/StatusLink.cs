using Newtonsoft.Json.Linq;
using HarmonyLib;
using UnityEngine;
using PeakCreativeMode.Core;
namespace PeakCreativeMode.Plugin
{
 internal static class StatusLink
 {
  public static void Eat(JObject o){var ch=Character.localCharacter;if(!WorldDiagnostics.Playing(ch))return;var token=o["nutrition"];if(token==null||(token.Type!=JTokenType.Integer&&token.Type!=JTokenType.Float))return;float nutrition=(float)token;if(nutrition>100)return;float relief=StatusAmounts.Hunger(nutrition,Plugin.Keys.HungerMultiplier.Value);if(relief<=0)return;var aff=ch.refs.afflictions;float before=aff.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger);aff.SubtractStatus(CharacterAfflictions.STATUSTYPE.Hunger,relief);Plugin.Log.LogInfo($"[status] native nutrition {nutrition} -> PEAK Hunger relief {relief:F3}, before {before:F3}, after {aff.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Hunger):F3}");}
  private static readonly System.Reflection.MethodInfo AddForce=AccessTools.Method(typeof(Character),"AddForce",new[]{typeof(Vector3),typeof(float),typeof(float)}),Fall=AccessTools.Method(typeof(Character),"Fall",new[]{typeof(float),typeof(float)});
  public static void Receive(JObject o)
  {
   EmbeddedArrows.Instance?.Receive(o);
   var ch=Character.localCharacter;if(!WorldDiagnostics.Playing(ch))return;var token=o["amount"];if(token==null||(token.Type!=JTokenType.Float&&token.Type!=JTokenType.Integer))return;
   float amount=StatusAmounts.Injury((float)token,Plugin.Keys.MobInjuryMultiplier.Value);
   if(o["knock"] is JArray a&&a.Count==3){double[] mc=new double[3];bool valid=true;for(int i=0;i<3;i++){if(a[i].Type!=JTokenType.Float&&a[i].Type!=JTokenType.Integer){valid=false;break;}mc[i]=(double)a[i];}if(valid){var v=Knockback.ToUnity(mc,Knockback.SourceScale((string)o["source"],Plugin.Keys.KnockbackMultiplier.Value,Plugin.Keys.CreeperKnockbackMultiplier.Value));var force=new Vector3(v.X,v.Y,v.Z);if(force.sqrMagnitude>0){if((string)o["source"]=="creeper"||(string)o["source"]=="tnt"){
      var impulse=Knockback.CreeperVelocity(mc,Knockback.SourceScale("creeper",Plugin.Keys.KnockbackMultiplier.Value,Plugin.Keys.CreeperKnockbackMultiplier.Value));var velocity=new Vector3(impulse.X,impulse.Y,impulse.Z);
      Fall?.Invoke(ch,new object[]{1f,0f});
      // Bodypart.AddForce queues Force even for Impulse. Apply the verified native Rigidbody impulse directly.
      foreach(var part in ch.refs.ragdoll.partList)if(part.Rig!=null&&!part.Rig.isKinematic)part.Rig.AddForce(velocity*part.Rig.mass,ForceMode.Impulse);
      Plugin.Log.LogInfo($"[status] {o["source"]} blast velocity {velocity}");
     }else{AddForce?.Invoke(ch,new object[]{force,1f,1f});if(force.magnitude>=12)Fall?.Invoke(ch,new object[]{.5f,0f});}Plugin.Log.LogInfo($"[status] native knock -> PEAK force {force}");}}}
   if(amount<=0)return;
   ch.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury,amount);Plugin.Log.LogInfo($"[status] MC {o["source"]} damage -> PEAK Injury +{amount:F3}");
  }
 }
}
