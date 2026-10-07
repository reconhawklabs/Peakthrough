using HarmonyLib;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class BowFov:MonoBehaviour {
  private static float _zoom;private void Update(){if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(Character.localCharacter)){_zoom=0;return;}_zoom=Mathf.Lerp(_zoom,HeldItemRenderer.Instance?.Drawing==true?1:0,1-Mathf.Exp(-10*Time.unscaledDeltaTime));}
  [HarmonyPatch(typeof(MainCameraMovement),"GetFov")]
  private static class Zoom {private static void Postfix(ref float __result){if(WorldDiagnostics.Playing(Character.localCharacter))__result*=BowDraw.FovScale(_zoom);}}
 }
}
