using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;
using Display=PeakCreativeMode.Core.Display;
namespace PeakCreativeMode.Plugin {
 internal sealed class DropLease:MonoBehaviour{public string Item;private void OnDestroy(){HeldItemRenderer.Instance?.ReleaseDrop(Item);}}
 internal sealed class HeldItemRenderer:MonoBehaviour {
  public static HeldItemRenderer Instance;
  private sealed class Cached{public Mesh Mesh;public Material[] Materials;public Display Display;public bool Block;public float Used;public int Leases;}
  private sealed class View{public Character Character;public string Item;public GameObject Object;public Vector3 BasePosition;public Quaternion BaseRotation;}
  private readonly Dictionary<string,Cached> _cache=new Dictionary<string,Cached>();private readonly Dictionary<int,View> _views=new Dictionary<int,View>();private readonly Dictionary<string,AvatarHeld> _remote=new Dictionary<string,AvatarHeld>();private readonly Dictionary<string,float> _remoteDrawStart=new Dictionary<string,float>();private readonly ItemSwing _swing=new ItemSwing();private float _localDrawStart;public bool Drawing {get;private set;}public float DrawTicks=>Drawing?(Time.unscaledTime-_localDrawStart)*20:0;private ItemModels _models;private AssetModels _blocks;private bool _warned;
  private void Awake(){Instance=this;_models=new ItemModels(p=>{var path=Path.Combine(Plugin.Keys.AssetCache.Value,"assets","minecraft",p);return File.Exists(path)?File.ReadAllText(path):null;});_blocks=new AssetModels(Plugin.Keys.AssetCache.Value);}
  public void Receive(JObject message){if(AvatarHeld.TryParse(message,out var h)){if(h.Using&&(!_remote.TryGetValue(h.Player,out var old)||!old.Using))_remoteDrawStart[h.Player]=Time.unscaledTime;_remote[h.Player]=h;}}
  public void Clear(){foreach(var v in _views.Values)if(v.Object!=null)Destroy(v.Object);_views.Clear();_remote.Clear();_remoteDrawStart.Clear();Drawing=false;_swing.Reset();}
  private static Vector3 V(V3 p)=>new Vector3(p.X,p.Y,p.Z);
  private Material Material(string texture){var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));var tex=texture==null?null:LoadTexture(texture);material.SetTexture("_BaseMap",tex);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");return material;}
  private Texture2D LoadTexture(string id){var path=_blocks.TexturePath(id);var t=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};t.LoadImage(File.ReadAllBytes(path));return t;}
  private Cached Build(string id){string baseId=id.Split('#')[0];int stage=id.IndexOf('#')>=0?int.Parse(id.Substring(id.IndexOf('#')+1)):-1;var definition=_models.Resolve(baseId,stage>=0,stage==2?20:stage==1?13:0);var vertices=new List<Vector3>();var uv=new List<Vector2>();var indexGroups=new List<int[]>();var materials=new List<Material>();
   if(definition?.BlockModel!=null){foreach(var group in _blocks.ItemQuads(definition.BlockModel).GroupBy(q=>q.Texture)){var indices=new List<int>();foreach(var q in group){int a=vertices.Count;for(int i=0;i<4;i++){vertices.Add(V(q.Vertices[i])-Vector3.one*.5f);uv.Add(new Vector2(q.UV[i][0],q.UV[i][1]));}indices.AddRange(new[]{a,a+1,a+2,a,a+2,a+3});}indexGroups.Add(indices.ToArray());materials.Add(Material(group.Key));}}
   else {var texture=definition?.Layer0??baseId.Replace("minecraft:","minecraft:item/");var material=Material(texture);var t=(Texture2D)material.GetTexture("_BaseMap");var pixels=t.GetPixels();var quads=ItemMesh.Extrude(t.width,t.height,(x,y)=>pixels[(t.height-1-y)*t.width+x].a>.1f);var indices=new List<int>();foreach(var q in quads){int a=vertices.Count;vertices.AddRange(new[]{V(q.A),V(q.B),V(q.C),V(q.D)});uv.AddRange(new[]{new Vector2(q.U0,q.V0),new Vector2(q.U1,q.V0),new Vector2(q.U1,q.V1),new Vector2(q.U0,q.V1)});indices.AddRange(new[]{a,a+2,a+1,a,a+3,a+2});}indexGroups.Add(indices.ToArray());materials.Add(material);}
   var mesh=new Mesh{name="Held "+id,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=indexGroups.Count;for(int i=0;i<indexGroups.Count;i++)mesh.SetTriangles(indexGroups[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return new Cached{Mesh=mesh,Materials=materials.ToArray(),Display=definition?.ThirdPersonRight??Display.Identity,Block=definition?.BlockModel!=null,Used=Time.unscaledTime};
  }
  private Cached Get(string item){if(!_cache.TryGetValue(item,out var cached)){if(_cache.Count>=64){var old=_cache.Where(pair=>pair.Value.Leases==0&&!_views.Values.Any(v=>v.Item==pair.Key)).OrderBy(pair=>pair.Value.Used).FirstOrDefault();if(old.Key!=null){Dispose(old.Value);_cache.Remove(old.Key);}else throw new InvalidOperationException("All item models are in use");}cached=Build(item);_cache[item]=cached;}cached.Used=Time.unscaledTime;return cached;}
  public GameObject CreateDrop(string item){var cached=Get(item);var go=new GameObject("MC drop "+item);go.AddComponent<MeshFilter>().sharedMesh=cached.Mesh;go.AddComponent<MeshRenderer>().sharedMaterials=cached.Materials;go.transform.localScale=Vector3.one*(cached.Block?.25f:.4f);cached.Leases++;go.AddComponent<DropLease>().Item=item;return go;}
  public GameObject CreateEquipment(string item){var cached=Get(item);var go=CreateDrop(item);var d=cached.Display;go.transform.localPosition=new Vector3(d.Translation.X/16,-.75f+d.Translation.Y/16,-d.Translation.Z/16);go.transform.localRotation=Quaternion.Euler(-d.Rotation.X,-d.Rotation.Y,d.Rotation.Z);go.transform.localScale=V(d.Scale);return go;}
  internal void ReleaseDrop(string item){if(_cache.TryGetValue(item,out var cached)&&cached.Leases>0)cached.Leases--;}
  public void Show(Character ch,string item,int drawStage=-1){if(item=="minecraft:bow"&&drawStage>=0)item+="#"+drawStage;if(ch==null||item==null){Hide(ch);return;}int key=ch.GetInstanceID();if(_views.TryGetValue(key,out var view)&&view.Item==item&&view.Object!=null)return;Hide(ch);
   try{var cached=Get(item);
    var hand=((Rigidbody)HarmonyLib.AccessTools.Method(typeof(Character),"GetBodypartRig").Invoke(ch,new object[]{BodypartType.Hand_R})).transform;var go=new GameObject("MC held "+item);go.transform.SetParent(hand,false);go.AddComponent<MeshFilter>().sharedMesh=cached.Mesh;go.AddComponent<MeshRenderer>().sharedMaterials=cached.Materials;
    var d=ch==Character.localCharacter?Display.Identity:cached.Display;go.transform.localPosition=new Vector3(d.Translation.X/16,d.Translation.Y/16,-d.Translation.Z/16);go.transform.localRotation=Quaternion.Euler(-d.Rotation.X,-d.Rotation.Y,d.Rotation.Z);var desired=V(d.Scale)*(cached.Block?.4f:.65f)*Plugin.Keys.HeldItemScale.Value;var parentScale=hand.lossyScale;go.transform.localScale=new Vector3(desired.x/Mathf.Max(.001f,Mathf.Abs(parentScale.x)),desired.y/Mathf.Max(.001f,Mathf.Abs(parentScale.y)),desired.z/Mathf.Max(.001f,Mathf.Abs(parentScale.z)));
    // Default layer is visible to PEAK's first-person camera, confirmed by live inspection.
    go.layer=0;_views[key]=new View{Character=ch,Item=item,Object=go,BasePosition=go.transform.localPosition,BaseRotation=go.transform.localRotation};
   }catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[held] "+e.Message);}}
  }
  public float SwingArc=>_swing.Arc;
  public void Swing()=>_swing.Start();
  private void AnimateSwing(Character ch){
   _swing.Advance(Time.unscaledDeltaTime);
   if(!_views.TryGetValue(ch.GetInstanceID(),out var view)||view.Object==null){_swing.Reset();return;}
   ApplyArc(view,_swing.Arc,MainCamera.instance!=null?MainCamera.instance.transform:Camera.main?.transform);
  }
  private static void ApplyArc(View view,float arc,Transform cam){
   var t=view.Object.transform;var hand=t.parent;
   t.localPosition=view.BasePosition;t.localRotation=view.BaseRotation;
   if(arc<=0)return;
   if(cam==null)return;
   t.position+=(-cam.right*.25f-cam.up*.20f+cam.forward*.10f)*arc;
   t.rotation=Quaternion.AngleAxis(-65*arc,cam.right)*Quaternion.AngleAxis(-25*arc,cam.up)*hand.rotation*view.BaseRotation;
  }
  public void Hide(Character ch){if(ch==null)return;if(ch==Character.localCharacter)_swing.Reset();int id=ch.GetInstanceID();if(_views.TryGetValue(id,out var view)){if(view.Object!=null)Destroy(view.Object);_views.Remove(id);}}
  private static string Uuid(string seed,string identity){using(var md5=MD5.Create()){var b=md5.ComputeHash(Encoding.UTF8.GetBytes("peak:"+seed+"\0"+identity));b[6]=(byte)((b[6]&15)|48);b[8]=(byte)((b[8]&63)|128);string hex=BitConverter.ToString(b).Replace("-","").ToLowerInvariant();return hex.Substring(0,8)+"-"+hex.Substring(8,4)+"-"+hex.Substring(12,4)+"-"+hex.Substring(16,4)+"-"+hex.Substring(20);}}
  private void LateUpdate(){if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(Character.localCharacter)){Clear();return;}var local=Character.localCharacter;var inv=ItemUI.Instance.State;var slot=inv.Slots[inv.Selected];bool drawing=slot.Item=="minecraft:bow"&&BridgeBehaviour.Instance.LastHeld?["using"]?.Type==JTokenType.Boolean&&(bool)BridgeBehaviour.Instance.LastHeld["using"];if(drawing&&!Drawing)_localDrawStart=Time.unscaledTime;Drawing=drawing;if((UnifiedHotbar.Enabled&&UnifiedHotbar.McInput||!Plugin.Keys.UnifiedHotbar.Value&&InputMode.Instance.McMode)&&!inv.Stowed&&slot.Peak==null&&slot.Item!=null)Show(local,slot.Item,BowDraw.Stage(Drawing,DrawTicks));else Hide(local);
   AnimateSwing(local);
   foreach(var ch in PlayerHandler.GetAllPlayerCharacters()){if(ch==local||ch==null)continue;var actor=ch.photonView.Owner;string identity=actor.IsMasterClient?actor.UserId:RelayAuthority.PlayerId(actor.UserId,actor.ActorNumber);if(identity==null)continue;var uuid=Uuid(BridgeBehaviour.Instance.MapSeed,identity);if(_remote.TryGetValue(uuid,out var held)&&held.Item!=null&&held.Peak==null)Show(ch,held.Item,BowDraw.Stage(held.Using,(Time.unscaledTime-(_remoteDrawStart.TryGetValue(uuid,out float start)?start:Time.unscaledTime))*20));else Hide(ch);if(_views.TryGetValue(ch.GetInstanceID(),out var remoteView))ApplyArc(remoteView,held.Item!=null?(float)Math.Sin(held.Swing*Math.PI):0,ch.transform);}
   foreach(var entry in _views.ToArray())if(entry.Value.Character==null){if(entry.Value.Object!=null)Destroy(entry.Value.Object);_views.Remove(entry.Key);}
  }
  private static void Dispose(Cached c){Destroy(c.Mesh);foreach(var m in c.Materials){Destroy(m.GetTexture("_BaseMap"));Destroy(m);}}
  private void OnDestroy(){Instance=null;Clear();foreach(var cached in _cache.Values)Dispose(cached);_cache.Clear();}
 }
}
