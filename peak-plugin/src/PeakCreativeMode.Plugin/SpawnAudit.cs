using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 [HarmonyPatch]
 internal static class SpawnAudit {
  private static readonly JArray _rows=new JArray();
  public static JArray Rows=>(JArray)_rows.DeepClone();
  private static IEnumerable<MethodBase> TargetMethods(){foreach(var type in new[]{typeof(Spawner),typeof(BerryBush),typeof(BerryVine),typeof(GroundPlaceSpawner)})yield return AccessTools.DeclaredMethod(type,"SpawnItems");yield return AccessTools.Method(typeof(SingleItemSpawner),"TrySpawnItems");yield return AccessTools.Method(typeof(Peak.SpawnedItemTracker),"SpawnAndTrackFromItemHistory");}
  private static void Postfix(object __instance,List<PhotonView> __result){if(!Plugin.Keys.EnableAutomation.Value||__result==null)return;foreach(var view in __result){var item=view?.GetComponent<Item>();if(item==null)continue;Record(item.name,item.itemID,__instance.GetType().Name,item.transform.position);}}
  internal static void Record(string name,ushort id,string type,Vector3 pos){if(!Plugin.Keys.EnableAutomation.Value)return;if(_rows.Count>=4096)_rows.RemoveAt(0);_rows.Add(new JObject{["name"]=name,["itemID"]=id,["spawnerType"]=type,["pos"]=new JArray(pos.x,pos.y,pos.z)});}
 }
}
