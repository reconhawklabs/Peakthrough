using System;using System.Collections.Generic;using HarmonyLib;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using Photon.Pun;using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal static class ImpactLink {
  private static readonly Dictionary<int,float> Thrown=new Dictionary<int,float>();private static readonly Dictionary<int,float> Exploded=new Dictionary<int,float>();
  private static readonly System.Reflection.FieldInfo LastThrown=AccessTools.Field(typeof(Item),"lastThrownCharacter");
  private static bool Ready=>PhotonNetwork.IsMasterClient&&WorldDiagnostics.Playing(Character.localCharacter)&&BridgeBehaviour.Instance?.Client.Status==PeakCreativeMode.Core.BridgeStatus.Connected;
  private static bool Claim(Dictionary<int,float> seen,int id){float now=Time.unscaledTime;if(seen.TryGetValue(id,out float at)&&now-at<.25f)return false;if(seen.Count>1024)seen.Clear();seen[id]=now;return true;}
  private static void Send(JObject target){BridgeBehaviour.Instance.Client.Send(new JObject{["t"]="action",["kind"]="impact",["target"]=target}.ToString(Formatting.None));Plugin.Log.LogInfo("[impact] "+target.ToString(Formatting.None));}
  public static void Explosion(AOE aoe){if(!Ready||aoe==null||aoe.range<=0||aoe.knockback<=0||!Claim(Exploded,aoe.GetInstanceID()))return;var p=aoe.transform.position;Send(new JObject{["source"]="explosion",["x"]=p.x,["y"]=p.y,["z"]=-p.z,["radius"]=Mathf.Min(32,aoe.range),["power"]=20});}
  public static void Hit(MobHitbox box,Collider collider){if(!WorldDiagnostics.Playing(Character.localCharacter)||BridgeBehaviour.Instance?.Client.Status!=PeakCreativeMode.Core.BridgeStatus.Connected||!Item.TryGetItemFromCollider(collider,out Item item)||item==null||(Character)LastThrown.GetValue(item)!=Character.localCharacter||item.rig==null)return;float speed=item.rig.linearVelocity.magnitude;if(speed<6||!Claim(Thrown,item.GetInstanceID()))return;var target=new JObject{["source"]="thrown",["entityId"]=box.EntityId,["mass"]=Mathf.Clamp(item.rig.mass,.001f,100),["speed"]=Mathf.Min(100,speed)};if(box.Part!=null)target["part"]=box.Part;Send(target);}
 }
 [HarmonyPatch(typeof(AOE),nameof(AOE.Explode))]internal static class ExplosionImpactPatch {private static void Postfix(AOE __instance)=>ImpactLink.Explosion(__instance);}
}
