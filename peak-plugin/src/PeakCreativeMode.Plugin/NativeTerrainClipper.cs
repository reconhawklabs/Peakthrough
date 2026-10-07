using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
 internal sealed class NativeTerrainClipper:MonoBehaviour
 {
  private sealed class RenderEntry{public MeshFilter Filter;public Mesh Original,Copy;public NativeTerrainMesh Data;}
  private sealed class ColliderEntry{public MeshCollider Collider;public Mesh Original,Copy;public NativeTerrainMesh Data;}
  private readonly Dictionary<MeshFilter,RenderEntry> _renders=new Dictionary<MeshFilter,RenderEntry>();
  private readonly Dictionary<MeshCollider,ColliderEntry> _colliders=new Dictionary<MeshCollider,ColliderEntry>();
  private readonly Dictionary<Mesh,NativeTerrainMesh> _data=new Dictionary<Mesh,NativeTerrainMesh>();private readonly List<ClipBox> _boxes=new List<ClipBox>();
  public static NativeTerrainClipper Instance{get;private set;}public string Status{get;private set;}="original terrain";public JObject Probe{get;private set;}=new JObject();
  public int Count=>_boxes.Count;public long CachePayloadBytes=>_data.Values.Sum(d=>d.PayloadBytes);private bool _warnedSlow;public float LastClipMs{get;private set;}
  private void Awake(){Instance=this;}
  private static Bounds Bounds(ClipBox box)=>new Bounds(new Vector3((box.Min.X+box.Max.X)/2,(box.Min.Y+box.Max.Y)/2,(box.Min.Z+box.Max.Z)/2),new Vector3(box.Max.X-box.Min.X,box.Max.Y-box.Min.Y,box.Max.Z-box.Min.Z));
  private static Bounds OriginalBounds(Mesh mesh,Transform transform){var b=mesh.bounds;var result=new Bounds(transform.TransformPoint(b.center),Vector3.zero);for(int i=0;i<8;i++)result.Encapsulate(transform.TransformPoint(b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1))));return result;}
  internal static bool StaticTerrain(Collider collider)=>collider!=null&&collider.enabled&&collider.gameObject.activeInHierarchy&&!collider.isTrigger&&(HelperFunctions.terrainMapMask.value&(1<<collider.gameObject.layer))!=0&&collider.transform.root.name!="PeakMinecraftBlocks"&&!collider.CompareTag("Water")&&collider.GetComponentInParent<Rigidbody>()==null&&collider.GetComponentInParent<Character>()==null;
  private NativeTerrainMesh Data(Mesh mesh){if(!_data.TryGetValue(mesh,out var data)){if(_data.Keys.Sum(m=>m.vertexCount)+mesh.vertexCount>2400000)throw new InvalidOperationException("Terrain mesh budget reached");data=NativeTerrainMesh.Read(mesh);_data[mesh]=data;}return data;}
  private void Render(MeshFilter filter)
  {
   if(filter==null||filter.sharedMesh==null||_renders.ContainsKey(filter))return;var renderer=filter.GetComponent<MeshRenderer>();if(renderer==null)return;
   if(_renders.Count>=2048)throw new InvalidOperationException("Terrain renderer budget reached");
   if(renderer.isPartOfStaticBatch||renderer.additionalVertexStreams!=null)throw new InvalidOperationException("Batched/additional-stream terrain needs another clipping path: "+filter.name);
   _renders[filter]=new RenderEntry{Filter=filter,Original=filter.sharedMesh,Data=Data(filter.sharedMesh)};
  }
  public bool Preflight(ClipBox box,out string error)
  {
   error=null;try{
    foreach(var collider in Physics.OverlapBox(Bounds(box).center,Bounds(box).extents,Quaternion.identity,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore))
    {
     if(!StaticTerrain(collider))continue;if(!(collider is MeshCollider mc))throw new InvalidOperationException("Unsupported original collider "+collider.GetType().Name+": "+collider.name);
     if(mc.sharedMesh==null)continue;
     if(!_colliders.ContainsKey(mc)&&_colliders.Count>=1024)throw new InvalidOperationException("Terrain collider budget reached");
     if(!_colliders.ContainsKey(mc))_colliders[mc]=new ColliderEntry{Collider=mc,Original=mc.sharedMesh,Data=Data(mc.sharedMesh)};
     Render(mc.GetComponent<MeshFilter>());var lod=mc.GetComponentInParent<LODGroup>();if(lod!=null)foreach(var level in lod.GetLODs())foreach(var r in level.renderers)if(r!=null)Render(r.GetComponent<MeshFilter>());
    }
    return true;
   }catch(Exception e){error=e.Message;Status="Cannot clip: "+error;Plugin.Log.LogWarning("[terraform] "+Status);return false;}
  }
  public bool Apply(ClipBox box)
  {
   if(!Preflight(box,out _))return false;var boxes=BoxMerge.Merge(new List<ClipBox>(_boxes){box});var renderCopies=new Dictionary<RenderEntry,Mesh>();var colliderCopies=new Dictionary<ColliderEntry,Mesh>();var watch=System.Diagnostics.Stopwatch.StartNew();int changed=0;var oldRenders=_renders.Values.ToDictionary(e=>e,e=>e.Copy);var oldColliders=_colliders.Values.ToDictionary(e=>e,e=>e.Copy);
   try{
    foreach(var entry in _renders.Values)if(entry.Filter!=null&&OriginalBounds(entry.Original,entry.Filter.transform).Intersects(Bounds(box))){var relevant=boxes.Where(b=>OriginalBounds(entry.Original,entry.Filter.transform).Intersects(Bounds(b))).ToArray();var copy=entry.Data.Cut(entry.Filter.transform,relevant,out int n);renderCopies[entry]=copy;changed+=n;}
    foreach(var entry in _colliders.Values)if(entry.Collider!=null&&OriginalBounds(entry.Original,entry.Collider.transform).Intersects(Bounds(box))){Mesh copy=null;var filter=entry.Collider.GetComponent<MeshFilter>();if(filter!=null&&_renders.TryGetValue(filter,out var render)&&render.Original==entry.Original)copy=renderCopies.TryGetValue(render,out var rendered)?rendered:render.Copy;else copy=entry.Data.Cut(entry.Collider.transform,boxes.Where(b=>OriginalBounds(entry.Original,entry.Collider.transform).Intersects(Bounds(b))).ToArray(),out _);colliderCopies[entry]=copy;}
    var previous=new HashSet<Mesh>();foreach(var pair in renderCopies){if(pair.Key.Copy!=null)previous.Add(pair.Key.Copy);pair.Key.Copy=pair.Value;pair.Key.Filter.sharedMesh=pair.Value??pair.Key.Original;}
    foreach(var pair in colliderCopies){if(pair.Key.Copy!=null)previous.Add(pair.Key.Copy);pair.Key.Copy=pair.Value;pair.Key.Collider.sharedMesh=null;pair.Key.Collider.sharedMesh=pair.Value??pair.Key.Original;}
    foreach(var mesh in previous)Destroy(mesh);_boxes.Add(box);Physics.SyncTransforms();LastClipMs=(float)watch.Elapsed.TotalMilliseconds;Status=$"clipped {_renders.Count} renderers/{_colliders.Count} colliders, {changed} affected triangles, {LastClipMs:F1}ms";Plugin.Log.LogInfo("[terraform] "+Status+" source cache bytes="+CachePayloadBytes);if(LastClipMs>20&&!_warnedSlow){_warnedSlow=true;Plugin.Log.LogWarning("[dig] clip exceeded20ms: "+LastClipMs);}return true;
   }catch(Exception e){foreach(var pair in oldRenders){pair.Key.Copy=pair.Value;if(pair.Key.Filter!=null)pair.Key.Filter.sharedMesh=pair.Value??pair.Key.Original;}foreach(var pair in oldColliders){pair.Key.Copy=pair.Value;if(pair.Key.Collider!=null){pair.Key.Collider.sharedMesh=null;pair.Key.Collider.sharedMesh=pair.Value??pair.Key.Original;}}Physics.SyncTransforms();var copies=new HashSet<Mesh>(renderCopies.Values.Concat(colliderCopies.Values));foreach(var mesh in copies)if(mesh!=null)Destroy(mesh);Status="Clip failed: "+e.Message;Plugin.Log.LogWarning("[terraform] "+Status);return false;}
  }
  private bool _applying;private int _generation;
  public bool BeginApply(ClipBox box,Action<bool> complete){if(_applying||!Preflight(box,out _))return false;_applying=true;StartCoroutine(ApplyRoutine(box,complete,_generation));return true;}
  private System.Collections.IEnumerator ApplyRoutine(ClipBox box,Action<bool> complete,int generation){
   var boxes=BoxMerge.Merge(new List<ClipBox>(_boxes){box});var renderCopies=new Dictionary<RenderEntry,Mesh>();var colliderCopies=new Dictionary<ColliderEntry,Mesh>();var copies=new HashSet<Mesh>();var oldRenders=_renders.Values.ToDictionary(e=>e,e=>e.Copy);var oldColliders=_colliders.Values.ToDictionary(e=>e,e=>e.Copy);int changed=0;var total=System.Diagnostics.Stopwatch.StartNew();float maxSlice=0;string failure=null;
   foreach(var entry in _renders.Values.ToArray())if(entry.Filter!=null&&OriginalBounds(entry.Original,entry.Filter.transform).Intersects(Bounds(box))){
    var relevant=boxes.Where(b=>OriginalBounds(entry.Original,entry.Filter.transform).Intersects(Bounds(b))).ToArray();var job=entry.Data.CutAsync(entry.Filter.transform,relevant,(mesh,n)=>{renderCopies[entry]=mesh;if(mesh!=null)copies.Add(mesh);changed+=n;});
    while(true){var slice=System.Diagnostics.Stopwatch.StartNew();bool next=false;try{next=job.MoveNext();}catch(Exception e){failure=e.Message;}maxSlice=Math.Max(maxSlice,(float)slice.Elapsed.TotalMilliseconds);if(slice.Elapsed.TotalMilliseconds>20)Plugin.Log.LogWarning("[dig timing] mesh "+entry.Original.name+" slice "+slice.Elapsed.TotalMilliseconds);if(failure!=null||!next)break;yield return job.Current;if(generation!=_generation){failure="scene changed";break;}}
    if(failure!=null)break;yield return null;
   }
   if(failure==null)foreach(var entry in _colliders.Values.ToArray())if(entry.Collider!=null&&OriginalBounds(entry.Original,entry.Collider.transform).Intersects(Bounds(box))){
    var filter=entry.Collider.GetComponent<MeshFilter>();if(filter!=null&&_renders.TryGetValue(filter,out var render)&&render.Original==entry.Original)colliderCopies[entry]=renderCopies.TryGetValue(render,out var mesh)?mesh:render.Copy;
    else {var relevant=boxes.Where(b=>OriginalBounds(entry.Original,entry.Collider.transform).Intersects(Bounds(b))).ToArray();var job=entry.Data.CutAsync(entry.Collider.transform,relevant,(mesh,n)=>{colliderCopies[entry]=mesh;if(mesh!=null)copies.Add(mesh);});while(true){var slice=System.Diagnostics.Stopwatch.StartNew();bool next=false;try{next=job.MoveNext();}catch(Exception e){failure=e.Message;}maxSlice=Math.Max(maxSlice,(float)slice.Elapsed.TotalMilliseconds);if(slice.Elapsed.TotalMilliseconds>20)Plugin.Log.LogWarning("[dig timing] mesh "+entry.Original.name+" slice "+slice.Elapsed.TotalMilliseconds);if(failure!=null||!next)break;yield return job.Current;if(generation!=_generation){failure="scene changed";break;}}}
    if(failure!=null)break;yield return null;
   }
   if(generation!=_generation)failure="scene changed";
   if(failure==null)foreach(var pair in renderCopies){try{if(pair.Key.Filter!=null){pair.Key.Copy=pair.Value;pair.Key.Filter.sharedMesh=pair.Value??pair.Key.Original;}}catch(Exception e){failure=e.Message;}if(failure!=null)break;yield return null;if(generation!=_generation){failure="scene changed";break;}}
   if(failure==null)foreach(var pair in colliderCopies){var slice=System.Diagnostics.Stopwatch.StartNew();try{if(pair.Key.Collider!=null){pair.Key.Copy=pair.Value;pair.Key.Collider.sharedMesh=null;pair.Key.Collider.sharedMesh=pair.Value??pair.Key.Original;}}catch(Exception e){failure=e.Message;}maxSlice=Math.Max(maxSlice,(float)slice.Elapsed.TotalMilliseconds);if(slice.Elapsed.TotalMilliseconds>20)Plugin.Log.LogWarning("[dig timing] collider "+pair.Key.Original.name+" slice "+slice.Elapsed.TotalMilliseconds);if(failure!=null)break;yield return null;if(generation!=_generation){failure="scene changed";break;}}
   if(failure!=null){if(generation==_generation){foreach(var pair in oldRenders){pair.Key.Copy=pair.Value;if(pair.Key.Filter!=null)pair.Key.Filter.sharedMesh=pair.Value??pair.Key.Original;}foreach(var pair in oldColliders){pair.Key.Copy=pair.Value;if(pair.Key.Collider!=null){pair.Key.Collider.sharedMesh=null;pair.Key.Collider.sharedMesh=pair.Value??pair.Key.Original;}}}foreach(var mesh in copies)if(mesh!=null)Destroy(mesh);Status="Clip failed: "+failure;}
   else{var previous=new HashSet<Mesh>();foreach(var entry in renderCopies.Keys)if(oldRenders[entry]!=null)previous.Add(oldRenders[entry]);foreach(var entry in colliderCopies.Keys)if(oldColliders[entry]!=null)previous.Add(oldColliders[entry]);foreach(var mesh in previous)Destroy(mesh);_boxes.Add(box);Physics.SyncTransforms();LastClipMs=maxSlice;Status=$"clipped {_renders.Count} renderers/{_colliders.Count} colliders, {changed} triangles, max frame slice {maxSlice:F1}ms, total {total.Elapsed.TotalMilliseconds:F1}ms";Plugin.Log.LogInfo("[dig] "+Status+" source cache bytes="+CachePayloadBytes);if(maxSlice>20&&!_warnedSlow){_warnedSlow=true;Plugin.Log.LogWarning("[dig] clip slice exceeded20ms: "+maxSlice);}}
   _applying=false;complete(failure==null);
  }
  private static JObject Surface(ClipBox box)
  {
   var bounds=Bounds(box);var hits=Physics.RaycastAll(bounds.center+Vector3.up*(bounds.extents.y+1),Vector3.down,bounds.size.y+2,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
   foreach(var hit in hits)if(StaticTerrain(hit.collider)&&hit.point.y>=box.Min.Y-.001f&&hit.point.y<=box.Max.Y+.001f)return new JObject{["name"]=hit.collider.name,["y"]=hit.point.y};return null;
  }
  public void Prototype(V3 minimum)
  {
   var box=new ClipBox(minimum,minimum+new V3(4,4,4));var feet=WorldDiagnostics.Feet(Character.localCharacter);if(Vector3.Distance(feet,Bounds(box).center)>16)throw new ArgumentException("Prototype must be within16 units");
   var before=Surface(box);bool result=Apply(box);Probe=new JObject{["before"]=before,["after"]=Surface(box),["applied"]=result,["status"]=Status};
  }
  public void Restore()
  {
   _generation++;bool exact=true;var copies=new HashSet<Mesh>();foreach(var entry in _renders.Values){if(entry.Filter!=null){entry.Filter.sharedMesh=entry.Original;exact&=entry.Filter.sharedMesh==entry.Original;}if(entry.Copy!=null)copies.Add(entry.Copy);}
   foreach(var entry in _colliders.Values){if(entry.Collider!=null){entry.Collider.sharedMesh=null;entry.Collider.sharedMesh=entry.Original;exact&=entry.Collider.sharedMesh==entry.Original;}if(entry.Copy!=null)copies.Add(entry.Copy);}
   foreach(var mesh in copies)Destroy(mesh);int count=_boxes.Count;_renders.Clear();_colliders.Clear();_data.Clear();_boxes.Clear();Physics.SyncTransforms();Probe["restoredExact"]=exact;Status="original terrain restored";if(count>0)Plugin.Log.LogInfo("[terraform] Restored original mesh references exactly="+exact);
  }
  private void Update(){if(_boxes.Count>0&&(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(Character.localCharacter)))Restore();}
  private void OnDestroy(){Restore();Instance=null;}
 }
}
