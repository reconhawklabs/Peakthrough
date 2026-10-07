using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 /// Replaces natural spawn candidates before instantiation; never touches an owned item.
 internal sealed class LootReplacer:MonoBehaviour {
  private sealed class Request {public string Seed,Json;public bool Sent;}
  public static LootReplacer Instance;private readonly Dictionary<string,Request> _requests=new Dictionary<string,Request>();private float _next;
  private void Awake(){Instance=this;}
  public void Replay(){foreach(var request in _requests.Values)request.Sent=false;}
  private void Update(){if(Time.unscaledTime<_next)return;_next=Time.unscaledTime+.2f;var bridge=BridgeBehaviour.Instance;if(bridge?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(Character.localCharacter))return;var state=bridge.LastDebugState;if(state==null)return;var feet=WorldDiagnostics.Feet(Character.localCharacter);if(!LootReadiness.Ready(new V3(feet.x,feet.y,feet.z),new V3((float)state["x"],(float)state["y"],(float)state["z"])))return;int n=0;foreach(var request in _requests.Values){if(request.Sent||request.Seed!=bridge.MapSeed)continue;if(!bridge.Client.Send(request.Json))break;request.Sent=true;if(++n>=8)break;}}
  internal static bool Enabled=>Instance!=null&&PhotonNetwork.IsMasterClient&&BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected&&Plugin.Keys.LootReplaceChance.Value>0;
  internal static bool Substitute(Component source,GameObject prefab,Vector3 pos,int index){
   if(prefab==null)return false;var item=prefab.GetComponent<Item>();if(item==null)return false;string seed=RunIdentity.Current();var tracker=source.GetComponent<Peak.SpawnedItemTracker>();string identity=tracker!=null?tracker.SpawnerId.ToString():Hash128.Compute(string.Join("/",source.GetComponentsInParent<Transform>().AsEnumerable().Reverse().Select(t=>t.name))).ToString();string spawn=identity+"/"+LootTable.SpawnKey(Mathf.RoundToInt(pos.x*100),Mathf.RoundToInt(pos.y*100),Mathf.RoundToInt(pos.z*100),index);
   if(!LootTable.ShouldReplace(seed,spawn,prefab.name,Plugin.Keys.LootReplaceChance.Value,Plugin.Keys.LootReplaceProtected.Value))return false;
   int segment=0;var map=MapHandler.Instance;if(map!=null)for(int i=0;i<map.segments.Length;i++){var parent=map.segments[i].segmentParent;if(parent!=null&&source.transform.IsChildOf(parent.transform)){segment=i;break;}}
   return Queue(seed,spawn,LootTable.For(segment,seed,spawn),pos,prefab.name,item.itemID,source.GetType().Name);
  }
  private static bool Queue(string seed,string spawn,Loot loot,Vector3 pos,string original,ushort itemId,string type){string key=seed+"|"+spawn;if(Instance._requests.ContainsKey(key))return true;if(Instance._requests.Count>=8192)return false;var o=new JObject{["t"]="loot_spawn",["key"]=key,["item"]=loot.Item,["count"]=loot.Count,["pos"]=new JArray(pos.x,pos.y,pos.z)};if(loot.Potion!=null)o["components"]=new JObject{["potion"]=loot.Potion};Instance._requests[key]=new Request{Seed=seed,Json=o.ToString(Formatting.None)};SpawnAudit.Record(original,itemId,type+" -> "+loot.Item,pos);return true;}
  internal static bool SubstituteRecord(Peak.SpawnedItemTracker tracker,Peak.SpawnedItemTracker.SpawnRecord record,int index){if(!ItemDatabase.TryGetItem(record.itemId,out var prefab))return false;return Substitute(tracker,prefab.gameObject,record.position,index);}
 }
 [HarmonyPatch]
 internal static class NaturalLootPrefixes {
  private static IEnumerable<MethodBase> TargetMethods(){foreach(var t in new[]{typeof(Spawner),typeof(BerryBush),typeof(BerryVine),typeof(GroundPlaceSpawner)})yield return AccessTools.DeclaredMethod(t,"SpawnItems");}
  private static bool Prefix(Spawner __instance,List<Transform> spawnSpots,ref List<PhotonView> __result){
   if(!LootReplacer.Enabled)return true;
   var candidates=new List<(GameObject prefab,Vector3 pos,Quaternion rot,bool fixedItem)>();
   if(__instance is BerryBush bush||__instance is BerryVine){
    Vector2 range=__instance is BerryBush b?b.possibleBerries:((BerryVine)__instance).possibleBerries;float pow=__instance is BerryBush bb?bb.randomPow:((BerryVine)__instance).randomPow;
    int count=Mathf.Min(spawnSpots.Count,Mathf.RoundToInt(Mathf.Lerp(range.x,range.y,Mathf.Pow(UnityEngine.Random.value,pow))));
    GameObject prefab=__instance is BerryBush?(__instance.spawnMode==Spawner.SpawnMode.SingleItem?__instance.spawnedObjectPrefab:LootData.GetRandomItem(__instance.spawnPool)):__instance.spawns.GetSpawns(1)[0];
    foreach(var spot in Choose(spawnSpots,count)){Vector3 pos=spot.position+(__instance is BerryVine vine?vine.spawnOffsetWorldSpace:Vector3.zero);candidates.Add((prefab,pos,Quaternion.identity,true));}
   }else if(__instance is GroundPlaceSpawner ground){
    int count=UnityEngine.Random.Range(Mathf.FloorToInt(ground.possibleItems.x),Mathf.FloorToInt(ground.possibleItems.y+1));var prefab=ground.spawns.GetSpawns(1)[0];
    foreach(var spot in Choose(spawnSpots,count))if(Physics.Raycast(spot.position,-ground.transform.up,out var hit,100,HelperFunctions.terrainMapMask))candidates.Add((prefab,hit.point,(Quaternion)AccessTools.Method(typeof(HelperFunctions),"GetRandomRotationWithUp").Invoke(null,new object[]{hit.normal}),true));
   }else{
    var prefabs=(List<GameObject>)AccessTools.Method(typeof(Spawner),"GetObjectsToSpawn").Invoke(__instance,new object[]{spawnSpots.Count,__instance.canRepeatSpawns});for(int i=0;i<Math.Min(prefabs.Count,spawnSpots.Count);i++)candidates.Add((prefabs[i],spawnSpots[i].position,spawnSpots[i].rotation,false));
   }
   __result=new List<PhotonView>();for(int i=0;i<candidates.Count;i++){var c=candidates[i];if(c.prefab==null||LootReplacer.Substitute(__instance,c.prefab,c.pos,i))continue;var item=PhotonNetwork.InstantiateItemRoom(c.prefab.name,c.pos,c.rot).GetComponent<Item>();if(item==null)continue;__result.Add(item.GetComponent<PhotonView>());if(__instance.spawnUpTowardsTarget!=null)item.transform.up=(__instance.spawnUpTowardsTarget.position-item.transform.position).normalized;if(c.fixedItem)item.SetKinematicNetworked(true,item.transform.position,item.transform.rotation);else{if(__instance.centerItemsVisually)item.transform.position+=c.pos-item.Center();AccessTools.Method(typeof(Spawner),"OffsetSpawn").Invoke(__instance,new object[]{item});AccessTools.Method(typeof(Spawner),"InitializePhysics").Invoke(__instance,new object[]{item});}}
   return false;
  }
  private static IEnumerable<Transform> Choose(List<Transform> spots,int count){var remaining=new List<Transform>(spots);for(int i=0;i<count&&remaining.Count>0;i++){int k=UnityEngine.Random.Range(0,remaining.Count);var spot=remaining[k];remaining.RemoveAt(k);yield return spot;}}
 }
 [HarmonyPatch(typeof(SingleItemSpawner),"TrySpawnItems")]
 internal static class SingleNaturalLoot {
  private static bool Prefix(SingleItemSpawner __instance,ref List<PhotonView> __result){if(!LootReplacer.Enabled)return true;if(__instance.belowAscentRequirement!=-1&&Ascents.currentAscent>=__instance.belowAscentRequirement)return true;if(__instance.HasSpawnTracking(out var tracker)&&tracker.HasSpawnHistory)return true;if(__instance.playersInRoomRequirement>PhotonNetwork.PlayerList.Length)return true;if(!LootReplacer.Substitute(__instance,__instance.prefab,__instance.transform.position+Vector3.up*.1f,0))return true;__result=new List<PhotonView>();tracker?.TrackSpawnedItems(__result);return false;}
 }
 [HarmonyPatch(typeof(Peak.SpawnedItemTracker),"SpawnAndTrackFromItemHistory")]
 internal static class SavedNaturalLoot {
  private static bool Prefix(Peak.SpawnedItemTracker __instance,ref List<PhotonView> __result){if(!LootReplacer.Enabled)return true;var history=AccessTools.Field(typeof(Peak.SpawnedItemTracker),"_historyFromSave").GetValue(__instance) as List<Peak.SpawnedItemTracker.SpawnRecord>;if(!__instance.HasSpawnHistory||history==null)return true;__result=new List<PhotonView>();for(int i=0;i<history.Count;i++)if(!LootReplacer.SubstituteRecord(__instance,history[i],i)){var view=history[i].Spawn(Vector3.zero);if(view!=null)__result.Add(view);}__instance.TrackSpawnedItems(__result);return false;}
 }
}
