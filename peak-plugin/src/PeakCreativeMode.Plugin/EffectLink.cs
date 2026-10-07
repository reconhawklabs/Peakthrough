using HarmonyLib;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class EffectLink:MonoBehaviour {
  public static EffectLink Instance;private readonly EffectState _state=new EffectState();private Character _character;
  public static EffectPlan Current=>Instance!=null&&BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected?Instance._state.At(Time.unscaledTime):EffectPlan.For(null,0);
  public static JObject Diagnostics {get{var p=Current;return new JObject{["fallScale"]=p.FallDamageScale,["jump"]=p.JumpMultiplier,["speed"]=p.SpeedMultiplier,["healingPerSecond"]=p.InjuryReliefPerSecond,["drag"]=p.ExtraDrag};}}
  private void Awake(){Instance=this;}
  public void Clear()=>_state.Clear();
  public void Receive(JObject o){var ch=Character.localCharacter;if(!WorldDiagnostics.Playing(ch)||o["id"]?.Type!=JTokenType.String||!(o["seconds"]?.Type==JTokenType.Float||o["seconds"]?.Type==JTokenType.Integer)||o["amplifier"]?.Type!=JTokenType.Integer)return;double amp=(double)o["amplifier"];if(amp<0||amp>4)return;float relief=_state.Apply((string)o["id"],(double)o["seconds"],(int)amp,Time.unscaledTime);_character=ch;if(relief>0)ch.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury,relief);Plugin.Log.LogInfo($"[effect] native {o["id"]}, {o["seconds"]}s, amplifier {amp}");}
  private void Update(){var ch=Character.localCharacter;if(ch!=_character||!WorldDiagnostics.Playing(ch)||BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected){_state.Clear();_character=ch;return;}float relief=Current.InjuryReliefPerSecond*Time.unscaledDeltaTime;if(relief>0)ch.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Injury,relief);}
  private void FixedUpdate(){var ch=Character.localCharacter;if(WorldDiagnostics.Playing(ch)&&Current.ExtraDrag)ch.refs.movement.ApplyParasolDrag(.96f,1f);}
 }
 [HarmonyPatch(typeof(CharacterMovement),"GetMovementForce")]
 internal static class EffectMovement {
  private static void Postfix(Character ___character,ref float __result){if(___character==Character.localCharacter&&WorldDiagnostics.Playing(___character))__result*=EffectLink.Current.SpeedMultiplier;}
 }
 [HarmonyPatch(typeof(CharacterMovement),"CheckFallDamage")]
 internal static class EffectFall {
  private static bool Prefix(Character ___character)=>___character!=Character.localCharacter||EffectLink.Current.FallDamageScale>0;
 }
}
