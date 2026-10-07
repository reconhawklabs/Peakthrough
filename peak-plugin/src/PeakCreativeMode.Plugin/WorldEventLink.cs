using System;
using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PeakCreativeMode.Plugin {
 internal sealed class WorldEventLink:MonoBehaviour {
  public static WorldEventLink Instance;private string _scene;private bool _sent,_scanLit=true;private readonly Dictionary<int,Campfire> _fires=new Dictionary<int,Campfire>();private readonly HashSet<int> _sentFires=new HashSet<int>();
  private void Awake(){Instance=this;}
  public void Replay(){_sent=false;_scanLit=true;_sentFires.Clear();}
  public void Fire(Campfire fire){if(PhotonNetwork.IsMasterClient&&fire!=null)_fires[fire.GetInstanceID()]=fire;}
  private static JArray Pos(Vector3 v)=>new JArray(v.x,v.y,v.z);
  private static JToken BowPoint(Transform fire){var point=fire.position+fire.right*1.5f;var hits=Physics.RaycastAll(point+Vector3.up*4,Vector3.down,12,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var hit in hits)if(NativeTerrainClipper.StaticTerrain(hit.collider))return Pos(hit.point);return JValue.CreateNull();}
  private void Update(){string scene=SceneManager.GetActiveScene().name;if(scene!=_scene){_scene=scene;_sent=false;_scanLit=true;_fires.Clear();_sentFires.Clear();}var bridge=BridgeBehaviour.Instance;var ch=Character.localCharacter;if(!PhotonNetwork.IsMasterClient||!WorldDiagnostics.Playing(ch)||bridge?.Client.Status!=BridgeStatus.Connected)return;var map=UnityEngine.Object.FindAnyObjectByType<MapHandler>();var feet=WorldDiagnostics.Feet(ch);var debug=bridge.LastDebugState;if(map==null||debug==null||!LootReadiness.Ready(new V3(feet.x,feet.y,feet.z),new V3((float)debug["x"],(float)debug["y"],(float)debug["z"]))||!Traverse.Create(map).Field("hasSpawnedInitialSpawners").GetValue<bool>()||!ch.data.isGrounded)return;
   if(!_sent){var point=feet+ch.transform.forward*3;var hits=Physics.RaycastAll(point+Vector3.up*4,Vector3.down,8,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));bool found=false;foreach(var hit in hits)if(NativeTerrainClipper.StaticTerrain(hit.collider)){point=hit.point;found=true;break;}if(!found)return;var fires=new JArray();for(int i=0;i<map.segments.Length;i++){var root=MapHandler.GetCampfireRoot(i);if(root==null)continue;var fire=root.GetComponentInChildren<Campfire>(true);fires.Add(new JObject{["segment"]=fire!=null?(int)fire.advanceToSegment:i,["pos"]=Pos(fire!=null?fire.transform.position:root.transform.position),["bowPos"]=BowPoint(fire!=null?fire.transform:root.transform)});}var config=new JObject{["triggerSegment"]=(int)(Segment)Enum.Parse(typeof(Segment),Plugin.Keys.DragonTriggerSegment.Value),["maxHealth"]=Plugin.Keys.DragonMaxHealth.Value,["orbit"]=Plugin.Keys.DragonOrbit.Value};_sent=bridge.Client.Send(new JObject{["t"]="world_event",["kind"]="run_start",["beach"]=Pos(point),["campfires"]=fires,["starterTool"]=Plugin.Keys.StarterTool.Value,["dragon"]=config}.ToString(Formatting.None));if(!_sent)return;Plugin.Log.LogInfo("[world] Sent run_start after native initial spawns at "+point);}
   if(_scanLit){_scanLit=false;for(int i=0;i<map.segments.Length;i++){var root=MapHandler.GetCampfireRoot(i);var fire=root!=null?root.GetComponentInChildren<Campfire>(true):null;if(fire!=null&&fire.state==Campfire.FireState.Lit)Fire(fire);}}
   foreach(var pair in _fires){var fire=pair.Value;if(fire==null||_sentFires.Contains(pair.Key)||(fire.transform.position-feet).sqrMagnitude>64*64)continue;if(bridge.Client.Send(new JObject{["t"]="world_event",["kind"]="campfire_lit",["segment"]=(int)fire.advanceToSegment,["pos"]=Pos(fire.transform.position)}.ToString(Formatting.None))){_sentFires.Add(pair.Key);Plugin.Log.LogInfo("[world] Campfire lit for "+fire.advanceToSegment);}}
  }
 }
 [HarmonyPatch(typeof(Campfire),"Light_Rpc")]
 internal static class CampfireDragonTrigger {private static void Postfix(Campfire __instance){WorldEventLink.Instance?.Fire(__instance);}}
}
