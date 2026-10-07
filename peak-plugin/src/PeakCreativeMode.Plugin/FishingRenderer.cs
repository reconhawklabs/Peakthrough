using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class FishingRenderer:MonoBehaviour {
  private sealed class Bobber {public GameObject Root;public string OwnerUuid;public LineRenderer Line;public EntityMotion Motion=new EntityMotion();public V3 Owner;public float Received;public bool Ready,Biting;}
  public static FishingRenderer Instance {get;private set;}
  private readonly Dictionary<int,Bobber> _bobbers=new Dictionary<int,Bobber>();private Material _sprite,_line;private Texture2D _texture;private bool _warned;
  private void Awake(){Instance=this;}
  public void Clear(){foreach(var b in _bobbers.Values)Destroy(b.Root);_bobbers.Clear();}
  public void Receive(JObject msg){if(msg["id"]?.Type!=JTokenType.Integer)return;int id=(int)msg["id"];string t=(string)msg["t"];if(t=="entity_remove"){if(_bobbers.TryGetValue(id,out var old)){Destroy(old.Root);_bobbers.Remove(id);}return;}
   if(t=="entity_add"&&(string)msg["type"]=="minecraft:fishing_bobber"&&!_bobbers.ContainsKey(id))try{
    if(_sprite==null){_texture=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};_texture.LoadImage(File.ReadAllBytes(Path.Combine(Plugin.Keys.AssetCache.Value,"assets/minecraft/textures/entity/fishing/fishing_hook.png")));_sprite=new Material(Shader.Find("Universal Render Pipeline/Unlit"));_sprite.SetTexture("_BaseMap",_texture);_sprite.SetFloat("_Cull",0);_sprite.SetFloat("_AlphaClip",1);_sprite.EnableKeyword("_ALPHATEST_ON");_line=new Material(Shader.Find("Universal Render Pipeline/Unlit"));_line.SetColor("_BaseColor",new Color(.08f,.06f,.04f));}
    var root=new GameObject("MC native bobber "+id);DontDestroyOnLoad(root);var icon=GameObject.CreatePrimitive(PrimitiveType.Quad);Destroy(icon.GetComponent<Collider>());icon.transform.SetParent(root.transform,false);icon.transform.localScale=Vector3.one*.25f;icon.GetComponent<MeshRenderer>().sharedMaterial=_sprite;var lr=root.AddComponent<LineRenderer>();lr.sharedMaterial=_line;lr.startWidth=lr.endWidth=.008f;lr.positionCount=17;lr.useWorldSpace=true;root.SetActive(false);_bobbers[id]=new Bobber{Root=root,Line=lr};
   }catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[fishing] Export native bobber texture: "+e.Message);}}
   if(t=="entity_update"&&_bobbers.TryGetValue(id,out var b)&&b.Motion.Apply(msg)){var owner=msg["pose"]?["ownerPos"] as JArray;if(owner==null||owner.Count!=3)return;var validation=new EntityMotion();if(!validation.Apply(new JObject{["pos"]=owner.DeepClone()}))return;b.OwnerUuid=(string)msg["pose"]?["owner"];b.Owner=validation.Position;b.Biting=(bool?)msg["pose"]?["biting"]??false;b.Received=Time.unscaledTime;b.Ready=true;}
  }
  private void Update(){bool connected=BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected;if(!connected){Clear();return;}bool visible=WorldDiagnostics.Playing(Character.localCharacter);var cam=MainCamera.instance?.transform??Camera.main?.transform;
   foreach(var b in _bobbers.Values){b.Root.SetActive(visible&&b.Ready);if(!visible||!b.Ready)continue;var p=b.Motion.Interpolate((Time.unscaledTime-b.Received)/.05f);var end=Coords.McToUnity(p.X,p.Y,p.Z);var start=Coords.McToUnity(b.Owner.X,b.Owner.Y,b.Owner.Z);b.Root.transform.position=new Vector3(end.X,end.Y,end.Z);if(cam!=null)b.Root.transform.GetChild(0).rotation=cam.rotation;for(int i=0;i<17;i++){var point=FishingLine.Point(start,end,i/16f);b.Line.SetPosition(i,new Vector3(point.X,point.Y,point.Z));}}
  }
  private void OnGUI(){if(!WorldDiagnostics.Playing(Character.localCharacter))return;foreach(var b in _bobbers.Values)if(b.Ready&&b.Biting&&b.OwnerUuid==BridgeBehaviour.Instance?.PlayerUuid){GUI.Box(new Rect(Screen.width/2-100,Screen.height/2+35,200,28),"Bite! RMB to reel in");break;}}
  private void OnDestroy(){Clear();if(_sprite!=null)Destroy(_sprite);if(_line!=null)Destroy(_line);if(_texture!=null)Destroy(_texture);}
 }
}
