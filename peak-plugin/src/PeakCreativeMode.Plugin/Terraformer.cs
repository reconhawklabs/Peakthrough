using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace PeakCreativeMode.Plugin {
 /// Native confirmations gate every visual/physics cut behind finished block colliders.
 internal sealed class Terraformer:MonoBehaviour {
  public static Terraformer Instance{get;private set;}
  private readonly TerraformQueue _queue=new TerraformQueue();private float _requestUntil,_showUntil;private string _scene;
  public string Status{get;private set;}="F6: convert nearby terrain";public int Confirmed=>_queue.Applied.Count;
  private bool _debugConversion;private bool _clipping;private int _generation;
  private readonly ConversionThrottle _throttle=new ConversionThrottle();private readonly Dictionary<string,float> _quickRequests=new Dictionary<string,float>();
  private static string Key(V3 v)=>$"{v.X}:{v.Y}:{v.Z}";
  public static TerrainMaterial TerrainKind(){switch(Material()){case "minecraft:dirt":return TerrainMaterial.Dirt;case "minecraft:sandstone":return TerrainMaterial.Sandstone;default:return TerrainMaterial.Stone;}}
  public bool ReadyForMining(BlockPos pos){var min=Coords.McBlockMinToUnity(pos.X,pos.Y,pos.Z);var point=min+new V3(.5f,.5f,.5f);foreach(var volume in _queue.Pending.Values)if(volume.Box.StrictlyContains(point))return false;return true;}
  public bool RequestQuick(V3 minimum)=>Request(minimum,4,true);
  private void Awake(){Instance=this;}
  public void Clear(){_debugConversion=false;_generation++;_queue.Clear();_throttle.Reset();_quickRequests.Clear();_requestUntil=0;NativeTerrainClipper.Instance?.Restore();}
  public void Receive(JObject msg){_scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;if(!(msg["terraform"] is JArray records)||records.Count>256)return;foreach(var token in records){var v=token is JObject o?TerraformVolume.Parse(o):null;if(v!=null)_queue.Add(v);}_requestUntil=0;}
  private void Tell(string text){Status=text;_showUntil=Time.unscaledTime+8;Plugin.Log.LogInfo("[terraform] "+text);}
  private static string Material(){var map=UnityEngine.Object.FindAnyObjectByType<MapHandler>();int segment=map!=null?Traverse.Create(map).Field("currentSegment").GetValue<int>():-1;var biome=map!=null&&map.segments!=null&&segment>=0&&segment<map.segments.Length?map.GetCurrentBiome():Biome.BiomeType.Alpine;switch(biome){case Biome.BiomeType.Shore:case Biome.BiomeType.Mesa:case Biome.BiomeType.Temple:return "minecraft:sandstone";case Biome.BiomeType.Tropics:case Biome.BiomeType.Roots:case Biome.BiomeType.Grasslands:case Biome.BiomeType.Swamp:return "minecraft:dirt";default:return "minecraft:stone";}}
  public bool Request(V3 minimum,int size)=>Request(minimum,size,false);
  private bool Request(V3 minimum,int size,bool quick){
   var bridge=BridgeBehaviour.Instance;var ch=Character.localCharacter;if(!WorldDiagnostics.Playing(ch)||bridge?.Client.Status!=BridgeStatus.Connected||BlockRenderer.Instance?.Ready!=true||size<4||size>8)return false;
   if(!quick&&_requestUntil>Time.unscaledTime){Tell("Converting terrain; please wait");return false;}if(_queue.Applied.Count+_queue.Pending.Count>=256){Tell("This run reached the 256-region limit");return false;}
   minimum=new V3(Mathf.Floor(minimum.X),Mathf.Floor(minimum.Y),Mathf.Floor(minimum.Z));var box=new ClipBox(minimum,minimum+new V3(size,size,size));var center=new Vector3(minimum.X+size*.5f,minimum.Y+size*.5f,minimum.Z+size*.5f);
   if(Vector3.Distance(WorldDiagnostics.Feet(ch),center)>15){Tell("Choose terrain within reach");return false;}if(!NativeTerrainClipper.Instance.Preflight(box,out string error)){Tell(error);return false;}
   if(quick&&!_throttle.TryRequest(minimum,Time.unscaledTime))return false;
   var solid=new JArray();for(int x=0;x<size;x++)for(int z=0;z<size;z++){
    var origin=new Vector3(minimum.X+x+.5f,minimum.Y+size+48,minimum.Z+z+.5f);float top=float.NegativeInfinity;
    foreach(var hit in Physics.RaycastAll(origin,Vector3.down,size+96,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore))if(NativeTerrainClipper.StaticTerrain(hit.collider))top=Mathf.Max(top,hit.point.y);
    for(int y=0;y<size;y++)if(minimum.Y+y+.5f<=top)solid.Add(new JArray(x,y,z));
   }
   bool sent=bridge.Client.Send(new JObject{["t"]="terrain",["convert"]=true,["quick"]=quick,["origin"]=new JArray(minimum.X,minimum.Y,minimum.Z),["size"]=new JArray(size,size,size),["material"]=Material(),["solid"]=solid,["water"]=new JArray()}.ToString(Formatting.None));
   if(sent){if(!quick)_debugConversion=true;if(quick)_quickRequests[Key(minimum)]=Time.unscaledTime;else _requestUntil=Time.unscaledTime+15;Tell($"Converting {size}³ terrain…");}else if(quick)_throttle.Completed();return sent;
  }
  private void Target(){var ch=Character.localCharacter;var camera=MainCamera.instance!=null?MainCamera.instance.transform:Camera.main?.transform;if(camera==null)return;var hits=Physics.RaycastAll(camera.position,camera.forward,4.5f,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var hit in hits){if(hit.collider.GetComponentInParent<Character>()==ch)continue;if(!NativeTerrainClipper.StaticTerrain(hit.collider)){Tell("Aim at original terrain");return;}int size=Plugin.Keys.TerraformSize.Value;var centre=hit.point-hit.normal*(size*.5f-1);Request(new V3(centre.x-size*.5f,Mathf.Floor(WorldDiagnostics.Feet(ch).y)-1,centre.z-size*.5f),size);return;}Tell("Aim at terrain within 4.5 units");}
  private void Update(){var bridge=BridgeBehaviour.Instance;var ch=Character.localCharacter;string scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;if(_scene!=scene){NativeTerrainClipper.Instance?.Restore();_generation++;_queue.ResetGeometry();_scene=scene;}if(bridge?.Client.Status!=BridgeStatus.Connected){if(_queue.Pending.Count>0||_queue.Applied.Count>0)Clear();return;}if(!WorldDiagnostics.Playing(ch)){if(_queue.Applied.Count>0){NativeTerrainClipper.Instance?.Restore();_queue.ResetGeometry();}return;}
   if(_requestUntil>0&&Time.unscaledTime>_requestUntil){_requestUntil=0;Tell("Minecraft did not confirm the conversion; try reconnecting");}
   foreach(var pending in _quickRequests.ToArray())if(Time.unscaledTime-pending.Value>15){_quickRequests.Remove(pending.Key);_throttle.Completed();Tell("Minecraft did not confirm this dig cell; reconnect to retry");}
   if((Plugin.Keys.NativeTerrainDigging.Value||_debugConversion)&&!_clipping)foreach(var pair in _queue.Pending.ToArray()){if(!pair.Value.Ready(BlockRenderer.Instance.Grid))continue;_clipping=true;int generation=_generation;if(!NativeTerrainClipper.Instance.BeginApply(pair.Value.Box,ok=>{_clipping=false;if(generation!=_generation)return;if(ok){_queue.Applied.Add(pair.Key);Tell("Terrain ready — mine with a pickaxe");}else{_queue.Failed.Add(pair.Key);Tell(NativeTerrainClipper.Instance.Status);}_queue.Pending.Remove(pair.Key);string key=Key(pair.Value.Box.Min);if(_quickRequests.TryGetValue(key,out float start)){_quickRequests.Remove(key);_throttle.Completed();Plugin.Log.LogInfo($"[dig] cell {key} request-to-clip {(Time.unscaledTime-start)*1000:F1}ms");}})){_clipping=false;_queue.Failed.Add(pair.Key);_queue.Pending.Remove(pair.Key);}break;}

   var kb=Keyboard.current;if(kb!=null&&kb[Plugin.Keys.Terraform.Value].wasPressedThisFrame&&(UnifiedHotbar.Enabled||InputMode.Instance?.McMode==true)&&InputMode.Instance?.PickerOpen!=true&&ItemUI.Instance?.Open!=true&&!(GUIManager.instance?.windowBlockingInput??false)&&!(GUIManager.instance?.wheelActive??false))Target();
  }
  private void OnGUI(){if(WorldDiagnostics.Playing(Character.localCharacter)&&Time.unscaledTime<_showUntil)GUI.Box(new Rect(Screen.width/2-300,Screen.height-120,600,35),Status);}
  private void OnDestroy(){Clear();Instance=null;}
 }
}
