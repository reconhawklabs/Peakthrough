using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace PeakCreativeMode.Plugin
{
 internal sealed class MobHitbox:MonoBehaviour {public int EntityId;public string Part;private void OnTriggerEnter(Collider other)=>ImpactLink.Hit(this,other);private void OnCollisionEnter(Collision other)=>ImpactLink.Hit(this,other.collider);}
 internal sealed class EntityRenderer:MonoBehaviour
 {
  private sealed class Actor {public GameObject Root;public readonly List<Mesh> Meshes=new List<Mesh>();public string Type;public GameObject Equipment;public string EquipmentItem;public Transform ModelRoot;public readonly EntityMotion Motion=new EntityMotion();public float Received;public bool Ready;public float GroundOffset;public float HazardRadius=3;public DragonDeathVisual Death;public float? GroundY;public readonly Dictionary<string,Transform> Hitboxes=new Dictionary<string,Transform>();public readonly Dictionary<string,Transform> Parts=new Dictionary<string,Transform>();public readonly Dictionary<string,Quaternion> Rest=new Dictionary<string,Quaternion>();}
  public static EntityRenderer Instance {get;private set;}
  private readonly Dictionary<int,Actor> _actors=new Dictionary<int,Actor>();
  private readonly Dictionary<string,EntityModels> _models=new Dictionary<string,EntityModels>();
  private readonly Dictionary<string,Material> _materials=new Dictionary<string,Material>();
  private readonly List<Mesh> _meshes=new List<Mesh>();
  private bool _warned;
  private readonly RaycastHit[] _groundHits=new RaycastHit[16];
  public JArray Diagnostics {get{var rows=new JArray();foreach(var pair in _actors){var a=pair.Value;if(!a.Ready)continue;rows.Add(new JObject{["id"]=pair.Key,["type"]=a.Type,["grounded"]=a.Motion.Grounded,["nativeY"]=a.Motion.Position.Y,["renderY"]=a.Root.transform.position.y,["held"]=a.Motion.Equipment.Item,["equipmentVisible"]=a.Equipment!=null&&a.Equipment.activeInHierarchy,["offset"]=a.GroundOffset,["groundY"]=a.GroundY.HasValue?new JValue(a.GroundY.Value):JValue.CreateNull()});}return rows;}}
  private float? Ground(Vector3 feet){float? surface=null;float distance=float.MaxValue;int count=Physics.RaycastNonAlloc(feet+Vector3.up*.2f,Vector3.down,_groundHits,1.45f,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++){var hit=_groundHits[i];if(hit.normal.y<.35f||hit.distance>=distance||hit.collider.GetComponentInParent<Character>()!=null||hit.collider.GetComponentInParent<Rigidbody>()!=null||hit.collider.CompareTag("Water"))continue;distance=hit.distance;surface=hit.point.y;}return surface;}
  public int Count=>_actors.Count;
  private void Awake(){Instance=this;}
  public void Clear(){foreach(var a in _actors.Values)Destroy(a.Root);_actors.Clear();foreach(var m in _meshes)Destroy(m);_meshes.Clear();}
  public void Receive(JObject o)
  {
   if(o["id"]?.Type!=JTokenType.Integer||(long)o["id"]<=0||(long)o["id"]>int.MaxValue)return;int id=(int)o["id"];string t=(string)o["t"];
   if(t=="entity_remove"){if(_actors.TryGetValue(id,out var old)){Destroy(old.Root);foreach(var mesh in old.Meshes){Destroy(mesh);_meshes.Remove(mesh);}_actors.Remove(id);}return;}
   if(t=="entity_add")
   {
    string type=(string)o["type"];if(type=="minecraft:tnt"){if(!_actors.ContainsKey(id)){var a=new Actor{Root=new GameObject("MC primed TNT "+id),Type=type};DontDestroyOnLoad(a.Root);var visual=HeldItemRenderer.Instance.CreateDrop("minecraft:tnt");visual.transform.SetParent(a.Root.transform,false);visual.transform.localScale=Vector3.one*.98f;visual.transform.localPosition=Vector3.up*.49f;a.ModelRoot=visual.transform;a.Root.SetActive(false);_actors[id]=a;}return;}if(type=="minecraft:dragon_fireball"||type=="minecraft:area_effect_cloud"){if(!_actors.ContainsKey(id))AddHazard(id,type);return;}if(EntityAnimations.For(type).Count==0&&type!="minecraft:arrow"&&type!="minecraft:ender_dragon")return;if(_actors.ContainsKey(id))return;
    try
    {
     var model=Model(type);
     var a=new Actor{Root=new GameObject("MC "+type+" "+id),Type=type};DontDestroyOnLoad(a.Root);a.Root.SetActive(false);if(type!="minecraft:arrow"&&type!="minecraft:ender_dragon"){var shape=a.Root.AddComponent<BoxCollider>();shape.isTrigger=true;shape.center=new Vector3(0,type=="minecraft:cow"?.7f:.9f,0);shape.size=type=="minecraft:cow"?new Vector3(.9f,1.4f,.9f):new Vector3(.6f,1.8f,.6f);if(EntitySize.TryParse(o["size"],out float w,out float h)){shape.center=Vector3.up*(h*.5f);shape.size=new Vector3(w,h,w);}a.Root.AddComponent<MobHitbox>().EntityId=id;}
     var modelRoot=new GameObject("baked model");a.ModelRoot=modelRoot.transform;modelRoot.transform.SetParent(a.Root.transform,false);modelRoot.transform.localPosition=type=="minecraft:arrow"?Vector3.zero:Vector3.up*1.5f;Build(model.Root,modelRoot.transform,a,_materials[type]);if(type=="minecraft:ender_dragon"){foreach(var part in DragonPose.PartNames){var box=new GameObject("dragon hit "+part);box.transform.SetParent(a.Root.transform,false);box.AddComponent<BoxCollider>().isTrigger=true;var hit=box.AddComponent<MobHitbox>();hit.EntityId=id;hit.Part=part;a.Hitboxes[part]=box.transform;}}_actors[id]=a;Plugin.Log.LogInfo($"[entities] native model {type} #{id}, {a.Parts.Count} parts");
    }
    catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[entities] Run Minecraft client to export entity models: "+e.Message);}}
   }
   if(t=="entity_update"&&_actors.TryGetValue(id,out var actor)&&actor.Motion.Apply(o)){actor.Received=Time.unscaledTime;actor.Ready=true;if(HazardPose.TryRadius(o["pose"]?["radius"],out float radius))actor.HazardRadius=radius;}
  }
  private EntityModels Model(string type){
     if(!_models.TryGetValue(type,out var model))
     {
      string root=Plugin.Keys.AssetCache.Value;model=EntityModels.Parse(File.ReadAllText(Path.Combine(root,"entity-models",type.Substring(10)+".json")));
      string path=Path.GetFullPath(Path.Combine(root,model.Texture));if(!path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Entity texture path");
      var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};if(!texture.LoadImage(File.ReadAllBytes(path)))throw new InvalidDataException("Entity texture");
      var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",texture);material.SetFloat("_Cull",0);material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");_models[type]=model;_materials[type]=material;
     }
return model;
  }
  public GameObject CreateAttachedArrow(){var model=Model("minecraft:arrow");var a=new Actor{Root=new GameObject("MC embedded arrow")};Build(model.Root,a.Root.transform,a,_materials["minecraft:arrow"]);return a.Root;}
  public void ReleaseAttachedArrow(GameObject arrow){if(arrow==null)return;foreach(var filter in arrow.GetComponentsInChildren<MeshFilter>()){_meshes.Remove(filter.sharedMesh);Destroy(filter.sharedMesh);}Destroy(arrow);}
  private void AddHazard(int id,string type)
  {
   var a=new Actor{Root=new GameObject("MC "+type+" "+id),Type=type};DontDestroyOnLoad(a.Root);a.Root.SetActive(false);
   var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetFloat("_Cull",0);material.SetFloat("_AlphaClip",1);material.EnableKeyword("_ALPHATEST_ON");material.SetColor("_BaseColor",type=="minecraft:dragon_fireball"?Color.white:new Color(.65f,.2f,.9f,.7f));
   string path=Path.Combine(Plugin.Keys.AssetCache.Value,"assets/minecraft/textures/entity/enderdragon/dragon_fireball.png");if(File.Exists(path)){var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};texture.LoadImage(File.ReadAllBytes(path));material.SetTexture("_BaseMap",texture);}if(!_materials.ContainsKey(type))_materials[type]=material;else{Destroy(material.GetTexture("_BaseMap"));Destroy(material);material=_materials[type];}
   int count=type=="minecraft:dragon_fireball"?1:36;for(int i=0;i<count;i++){var go=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(go.GetComponent<Collider>());go.transform.SetParent(a.Root.transform,false);go.GetComponent<MeshRenderer>().sharedMaterial=material;a.Parts[i.ToString()]=go.transform;}_actors[id]=a;
  }
  private static Quaternion Rotation(V3 r)=>Quaternion.AngleAxis(-r.Z*Mathf.Rad2Deg,Vector3.forward)*Quaternion.AngleAxis(-r.Y*Mathf.Rad2Deg,Vector3.up)*Quaternion.AngleAxis(r.X*Mathf.Rad2Deg,Vector3.right);
  private void Build(ModelPartData p,Transform parent,Actor a,Material material)
  {
   var go=new GameObject(p.Name);go.transform.SetParent(parent,false);var pivot=Coords.McModelToUnity(p.Pivot.X,p.Pivot.Y,p.Pivot.Z);go.transform.localPosition=new Vector3(pivot.X,pivot.Y,pivot.Z);go.transform.localRotation=Rotation(p.Rotation);go.transform.localScale=new Vector3(p.Scale.X,p.Scale.Y,p.Scale.Z);a.Parts[p.Name]=go.transform;a.Rest[p.Name]=go.transform.localRotation;
   if(p.Faces.Count>0)
   {
    var vertices=new Vector3[p.Faces.Count*4];var uv=new Vector2[vertices.Length];var indices=new int[p.Faces.Count*6];
    for(int i=0;i<p.Faces.Count;i++){for(int v=0;v<4;v++){var f=p.Faces[i][v];var point=Coords.McModelToUnity(f[0],f[1],f[2]);vertices[i*4+v]=new Vector3(point.X,point.Y,point.Z);uv[i*4+v]=new Vector2(f[3],1-f[4]);}int k=i*4,j=i*6;indices[j]=k;indices[j+1]=k+1;indices[j+2]=k+2;indices[j+3]=k;indices[j+4]=k+2;indices[j+5]=k+3;}
    var mesh=new Mesh{name="baked Minecraft "+p.Name,indexFormat=IndexFormat.UInt32,vertices=vertices,uv=uv,triangles=indices};mesh.RecalculateNormals();mesh.RecalculateBounds();_meshes.Add(mesh);a.Meshes.Add(mesh);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
   }
   foreach(var child in p.Children)Build(child,go.transform,a,material);
  }
  private void Animate(Actor a,string part,float x,float y=0)
  {if(a.Parts.TryGetValue(part,out var transform))transform.localRotation=a.Rest[part]*Quaternion.Euler(x,y,0);}
  private void DragonWing(Actor a,string part,float x,float y,float z){if(a.Parts.TryGetValue(part,out var p))p.localRotation=Rotation(new V3(x,y,z));}
  private void Update()
  {
   bool active=BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected&&WorldDiagnostics.Playing(Character.localCharacter);if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected){Clear();return;}
   foreach(var a in _actors.Values)
   {
    a.Root.SetActive(active&&a.Ready);if(!active||!a.Ready)continue;
    float t=(Time.unscaledTime-a.Received)/.05f;var p=a.Motion.Interpolate(t);var v=Coords.McToUnity(p.X,p.Y,p.Z);var position=new Vector3(v.X,v.Y,v.Z);
    if(a.Type!="minecraft:tnt"&&a.Type!="minecraft:arrow"&&a.Type!="minecraft:ender_dragon"&&a.Type!="minecraft:dragon_fireball"&&a.Type!="minecraft:area_effect_cloud"){a.GroundY=Ground(position);a.GroundOffset=EntityGroundFit.Offset(a.Motion.Grounded,v.Y,a.GroundY,a.GroundOffset,Time.unscaledDeltaTime);position.y+=a.GroundOffset;}
    a.Root.transform.position=position;float yaw=EntityMotion.Angle(a.Motion.OldYaw,a.Motion.BodyYaw,t);a.Root.transform.rotation=a.Type=="minecraft:arrow"?Quaternion.Euler(0,Coords.McYawToUnity(yaw)-90,0)*Quaternion.AngleAxis(-a.Motion.Pitch,Vector3.forward):Quaternion.Euler(0,a.Type=="minecraft:ender_dragon"?Coords.DragonYawToUnity(yaw):Coords.McYawToUnity(yaw),0);
    if(a.Type=="minecraft:dragon_fireball"||a.Type=="minecraft:area_effect_cloud"){int i=0;var cam=MainCamera.instance?.transform;foreach(var point in a.Parts.Values){float angle=i*2.399963f+Time.time*.3f;float radius=a.HazardRadius*Mathf.Sqrt((i+.5f)/36);point.localPosition=a.Type=="minecraft:dragon_fireball"?Vector3.zero:new Vector3(Mathf.Cos(angle)*radius,.2f+Mathf.Sin(Time.time*2+i)*.15f,Mathf.Sin(angle)*radius);point.localScale=Vector3.one*(a.Type=="minecraft:dragon_fireball"?1.5f:.45f);if(cam!=null)point.rotation=cam.rotation;i++;}continue;}
    if(a.Type=="minecraft:arrow"||a.Type=="minecraft:tnt")continue;
    if(a.Type=="minecraft:ender_dragon"){var dragon=a.Motion.Dragon;if(dragon!=null){if(dragon.DeathTime>0){if(a.Death==null)a.Death=a.Root.AddComponent<DragonDeathVisual>();a.Death.NativeTick(dragon.DeathTime);}float f=dragon.Flap*Mathf.PI*2;float bounce=Mathf.Sin(f-1)+1;bounce=(bounce*bounce+bounce*2)*.05f;a.ModelRoot.localPosition=new Vector3(0,3.501f-bounce,2);Animate(a,"jaw",(Mathf.Sin(f)+1)*.2f*Mathf.Rad2Deg);DragonWing(a,"left_wing",.125f-Mathf.Cos(f)*.2f,-.25f,-(Mathf.Sin(f)+.125f)*.8f);DragonWing(a,"right_wing",.125f-Mathf.Cos(f)*.2f,.25f,(Mathf.Sin(f)+.125f)*.8f);DragonWing(a,"left_wing_tip",0,0,(Mathf.Sin(f+2)+.5f)*.75f);DragonWing(a,"right_wing_tip",0,0,-(Mathf.Sin(f+2)+.5f)*.75f);foreach(var part in dragon.Parts)if(a.Hitboxes.TryGetValue(part.Part,out var box)){var offset=Coords.McToUnity(part.Offset.X,part.Offset.Y,part.Offset.Z);box.position=position+new Vector3(offset.X,offset.Y,offset.Z);box.rotation=Quaternion.identity;var shape=box.GetComponent<BoxCollider>();shape.size=new Vector3(part.Width,part.Height,part.Width);shape.center=Vector3.up*part.Height*.5f;}}continue;}

    string held=a.Motion.Equipment.Item;if(held=="minecraft:bow"&&a.Motion.Equipment.Using)held+="#2";if(held!=a.EquipmentItem){if(a.Equipment!=null)Destroy(a.Equipment);a.Equipment=null;a.EquipmentItem=null;if(held!=null&&a.Parts.TryGetValue("right_arm",out var hand)){try{a.Equipment=HeldItemRenderer.Instance.CreateEquipment(held);a.Equipment.transform.SetParent(hand,false);a.EquipmentItem=held;}catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[equipment] "+e.Message);}}}}
    Animate(a,"head",a.Motion.Pitch,EntityMotion.Angle(yaw,a.Motion.HeadYaw,1)-yaw);
    float swing=Mathf.Cos(a.Motion.Swing*.6662f)*a.Motion.SwingAmount*80;
    foreach(var rule in EntityAnimations.For(a.Type)){if(rule.Wing&&a.Parts.TryGetValue(rule.Part,out var wing))wing.localRotation=a.Rest[rule.Part]*Quaternion.Euler(0,0,Mathf.Sin(Time.time*12)*25*rule.Sign);else Animate(a,rule.Part,swing*rule.Sign);}
    if(a.Type=="minecraft:zombie"||a.Type=="minecraft:skeleton"){Animate(a,"right_arm",a.Type=="minecraft:zombie"?-90+swing*.15f:-70);Animate(a,"left_arm",a.Type=="minecraft:zombie"?-90-swing*.15f:-70);}
   }
  }
  private void OnDestroy(){Clear();foreach(var m in _materials.Values){Destroy(m.GetTexture("_BaseMap"));Destroy(m);}}
 }
}
