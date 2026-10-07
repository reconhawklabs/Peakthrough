using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class SpawnHintSampler:MonoBehaviour {
  public static JObject Diagnostics{get;private set;}=new JObject();
  private float _lastHints;private float _nextHints,_nextEnv;private int _phase;
  private void Update(){var bridge=BridgeBehaviour.Instance;var ch=Character.localCharacter;if(!PhotonNetwork.IsMasterClient||!WorldDiagnostics.Playing(ch)||bridge?.Client.Status!=BridgeStatus.Connected)return;var feet=WorldDiagnostics.Feet(ch);bool night=DayNightManager.instance!=null&&DayNightManager.instance.isDay<.5f;
   if(Time.unscaledTime>=_nextEnv){_nextEnv=Time.unscaledTime+1;var fires=new JArray();foreach(var fire in UnityEngine.Object.FindObjectsByType<Campfire>(FindObjectsSortMode.None))if(fire.state==Campfire.FireState.Lit)fires.Add(new JArray(fire.transform.position.x,fire.transform.position.y,fire.transform.position.z));var map=UnityEngine.Object.FindAnyObjectByType<MapHandler>();bridge.Client.Send(new JObject{["t"]="env",["time"]=DayNightManager.instance?.timeOfDay??12,["night"]=night,["segment"]=map!=null?MapHandler.CurrentSegmentNumber.ToString():"Beach",["biome"]=map!=null?map.GetCurrentBiome().ToString():"Shore",["campfires"]=fires,["mobs"]=Plugin.Keys.MobsEnabled.Value,["deposits"]=Plugin.Keys.BlockDeposits.Value,["griefing"]=Plugin.Keys.MobsGriefing.Value,["sunBurn"]=Plugin.Keys.MobsSunBurn.Value,["hostileCap"]=Plugin.Keys.HostileCap.Value,["passiveCap"]=Plugin.Keys.PassiveCap.Value,["runCap"]=Plugin.Keys.RunCap.Value}.ToString(Formatting.None));}
   if(Time.unscaledTime<_nextHints)return;_nextHints=Time.unscaledTime+2.1f;var cells=new List<(V3 pos,bool dark,bool open)>();float threshold=Plugin.Keys.OpennessThreshold.Value;
   if(Plugin.Keys.MobsEnabled.Value)for(int r=16;r<=40;r+=8)for(int i=0;i<8;i++){float angle=(i+_phase*.25f)*Mathf.PI/4;var origin=feet+new Vector3(Mathf.Cos(angle)*r,24,Mathf.Sin(angle)*r);var hits=Physics.RaycastAll(origin,Vector3.down,48,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var hit in hits){if(!NativeTerrainClipper.StaticTerrain(hit.collider))continue;var p=hit.point;if(hit.normal.y<.7071f||p.y<.5f||Vector3.Distance(p,feet)>47||Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.8f,.3f,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore))break;var sample=p+Vector3.up;bool sun=LightingProbe.SunOccluded(sample);float? alpha=LightingProbe.Openness(sample);bool open=alpha.HasValue?alpha.Value>=threshold:!sun;cells.Add((new V3(p.x,p.y,p.z),night||!open||sun,open));break;}}
   _phase=(_phase+1)%4;BlockDepositSampler.Collect(feet,cells,night,threshold);string message=SpawnHints.Encode(cells);Diagnostics=new JObject{["hints"]=JObject.Parse(message),["interval"]=Time.unscaledTime-_lastHints,["night"]=night,["threshold"]=threshold};_lastHints=Time.unscaledTime;bridge.Client.Send(message);
  }
 }
}
