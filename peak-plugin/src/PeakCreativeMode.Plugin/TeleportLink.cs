using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal static class TeleportLink {
  public static void Receive(JObject o){var ch=Character.localCharacter;if(!WorldDiagnostics.Playing(ch))return;var current=WorldDiagnostics.Feet(ch);if(!TeleportTarget.TryParse(o["pos"],new V3(current.x,current.y,current.z),out var target))return;var feet=new Vector3(target.X,target.Y,target.Z);if(Physics.CheckCapsule(feet+Vector3.up*.35f,feet+Vector3.up*1.55f,.3f,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore)){Plugin.Log.LogInfo("[pearl] Refused occupied destination "+feet);return;}var center=WarpTarget.Center(target,new V3(ch.Center.x,ch.Center.y,ch.Center.z),new V3(current.x,current.y,current.z));ch.photonView.RPC(nameof(Character.WarpPlayerRPC),RpcTarget.All,new Vector3(center.X,center.Y,center.Z),true);Plugin.Log.LogInfo($"[pearl] PEAK feet {current} -> {feet}");}
 }
}
