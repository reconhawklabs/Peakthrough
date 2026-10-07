using System.Collections;
using HarmonyLib;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
    // PEAK still authorizes the jump and charges its normal stamina before this hook.
    [HarmonyPatch(typeof(Character),"OnJump")]
    internal static class TowerJump
    {
        private static bool Eligible(Character ch)
        {
            var mode=InputMode.Instance;
            var cam=MainCamera.instance!=null?MainCamera.instance.transform:Camera.main?.transform;
            return ch==Character.localCharacter&&WorldDiagnostics.Playing(ch)&&UnifiedHotbar.McInput&&!mode.PickerOpen&&mode.Selected!=null
                &&BlockRenderer.Instance?.Ready==true&&BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected
                &&cam!=null&&cam.forward.y<-.65f&&Plugin.Keys.TowerJumpSpeed.Value>0;
        }
        private static void Postfix(Character __instance)
        {
            if(__instance==Character.localCharacter&&WorldDiagnostics.Playing(__instance)&&(Eligible(__instance)||EffectLink.Current.JumpMultiplier>1))InputMode.Instance.StartCoroutine(Assist(__instance));
        }
        private static IEnumerator Assist(Character ch)
        {
            // Allow PEAK's queued native impulse to reach the rigidbodies first.
            yield return new WaitForFixedUpdate();
            if(ch==null||!WorldDiagnostics.Playing(ch)||!(Eligible(ch)||EffectLink.Current.JumpMultiplier>1)||ch.refs.ragdoll?.partList==null)yield break;
            float start=WorldDiagnostics.Feet(ch).y;
            foreach(var part in ch.refs.ragdoll.partList)
            {
                var rig=part.Rig;if(rig==null||rig.isKinematic)continue;
                var velocity=rig.linearVelocity;
                if(velocity.y>0)velocity.y*=EffectLink.Current.JumpMultiplier;
                if(Eligible(ch))velocity.y=TowerPlacement.JumpSpeed(velocity.y,Plugin.Keys.TowerJumpSpeed.Value);
                rig.linearVelocity=velocity;
            }
            float peak=start;float until=Time.time+1.5f;
            while(ch!=null&&Time.time<until){peak=Mathf.Max(peak,WorldDiagnostics.Feet(ch).y);yield return null;}
            Plugin.Log.LogInfo($"[building] tower jump feet rise {peak-start:F3}, minimum speed {Plugin.Keys.TowerJumpSpeed.Value:F1}");
        }
    }
}
