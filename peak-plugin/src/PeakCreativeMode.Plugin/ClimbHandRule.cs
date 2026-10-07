using HarmonyLib;
using PeakCreativeMode.Core;
namespace PeakCreativeMode.Plugin {
 internal static class ClimbHandRule {
  internal static bool Blocked(Character ch){if(ch!=Character.localCharacter||!UnifiedHotbar.Enabled)return false;var inv=ItemUI.Instance.State;return ClimbPolicy.Blocks(true,inv.Stowed||UnifiedHotbar.Instance.Backpack?default:inv.Slots[inv.Selected],UnifiedHotbar.Instance.Backpack&&ch.data.currentItem!=null);}
 }
 [HarmonyPatch(typeof(CharacterClimbing),"TryToStartWallClimb")]
 internal static class EmptyHandWallClimb {private static bool Prefix(Character ___character)=>!ClimbHandRule.Blocked(___character);}
 [HarmonyPatch(typeof(ClimbHandle),"IsInteractible")]
 internal static class EmptyHandHandle {private static void Postfix(Character interactor,ref bool __result){if(ClimbHandRule.Blocked(interactor))__result=false;}}
 [HarmonyPatch(typeof(CharacterClimbing),"Update")]
 internal static class ReleaseOccupiedClimb {private static void Prefix(CharacterClimbing __instance,Character ___character){if(ClimbHandRule.Blocked(___character)&&(___character.data.isClimbing||___character.data.isRopeClimbing||___character.data.isVineClimbing||___character.data.currentClimbHandle!=null))__instance.StopAnyClimbing();}}
 [HarmonyPatch(typeof(CharacterClimbing),"StartHang")]
 internal static class EmptyHandHang {private static bool Prefix(Character ___character)=>!ClimbHandRule.Blocked(___character);}
 [HarmonyPatch(typeof(CharacterRopeHandling),"GrabRopeRpc")]
 internal static class EmptyHandRope {private static bool Prefix(Character ___character)=>!ClimbHandRule.Blocked(___character);}
 [HarmonyPatch(typeof(CharacterVineClimbing),"GrabVineRpc")]
 internal static class EmptyHandVine {private static bool Prefix(Character ___character)=>!ClimbHandRule.Blocked(___character);}
}
