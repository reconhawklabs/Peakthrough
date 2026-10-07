using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
 internal sealed class MiningOverlay:MonoBehaviour
 {
  public static MiningOverlay Instance {get;private set;}
  private GameObject _cracks,_outline,_surface;private Material _crackMaterial,_lineMaterial;
  private readonly Dictionary<int,Texture2D> _textures=new Dictionary<int,Texture2D>();
  private float _lastProgress;
  private void Awake()
  {
   Instance=this;var shader=Shader.Find("Universal Render Pipeline/Unlit");_crackMaterial=new Material(shader);_crackMaterial.SetFloat("_AlphaClip",1);_crackMaterial.SetFloat("_Cutoff",.1f);_crackMaterial.EnableKeyword("_ALPHATEST_ON");
   _cracks=GameObject.CreatePrimitive(PrimitiveType.Cube);_cracks.name="MC mining cracks";DontDestroyOnLoad(_cracks);Destroy(_cracks.GetComponent<Collider>());_cracks.GetComponent<Renderer>().sharedMaterial=_crackMaterial;_cracks.transform.localScale=Vector3.one*1.005f;_cracks.SetActive(false);
   _surface=GameObject.CreatePrimitive(PrimitiveType.Quad);_surface.name="MC terrain cracks";DontDestroyOnLoad(_surface);Destroy(_surface.GetComponent<Collider>());_surface.GetComponent<Renderer>().sharedMaterial=_crackMaterial;_surface.transform.localScale=Vector3.one*.5f;_surface.SetActive(false);_crackMaterial.SetFloat("_Cull",0);
   _lineMaterial=new Material(shader);_lineMaterial.SetColor("_BaseColor",Color.cyan);_outline=new GameObject("MC block outline");DontDestroyOnLoad(_outline);var line=_outline.AddComponent<LineRenderer>();line.useWorldSpace=false;line.widthMultiplier=.012f;line.sharedMaterial=_lineMaterial;
   int[] points={0,1,3,2,0,4,5,1,5,7,3,7,6,2,6,4};var vertices=new Vector3[points.Length];for(int i=0;i<points.Length;i++){int n=points[i];vertices[i]=new Vector3((n&1)-.5f,((n>>1)&1)-.5f,((n>>2)&1)-.5f)*1.01f;}line.positionCount=vertices.Length;line.SetPositions(vertices);_outline.SetActive(false);
  }
  public void ShowAt(Vector3 point,Vector3 normal){if(!_textures.TryGetValue(0,out var texture)){string path=Path.Combine(Plugin.Keys.AssetCache.Value,"assets","minecraft","textures","block","destroy_stage_0.png");if(!File.Exists(path))return;texture=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};texture.LoadImage(File.ReadAllBytes(path));_textures[0]=texture;}_crackMaterial.SetTexture("_BaseMap",texture);_surface.transform.position=point+normal*.015f;_surface.transform.rotation=Quaternion.LookRotation(normal);_surface.SetActive(true);_lastProgress=Time.unscaledTime;}
  public void Receive(JObject o)
  {
   _surface.SetActive(false);if(o["stage"]?.Type!=JTokenType.Integer)return;int stage=(int)o["stage"];if(stage<0||stage>9){_cracks.SetActive(false);return;}
   if(o["x"]?.Type!=JTokenType.Integer||o["y"]?.Type!=JTokenType.Integer||o["z"]?.Type!=JTokenType.Integer)return;
   var min=Coords.McBlockMinToUnity((int)o["x"],(int)o["y"],(int)o["z"]);_cracks.transform.position=new Vector3(min.X+.5f,min.Y+.5f,min.Z+.5f);
   if(!_textures.TryGetValue(stage,out var texture)){string path=Path.Combine(Plugin.Keys.AssetCache.Value,"assets","minecraft","textures","block","destroy_stage_"+stage+".png");if(!File.Exists(path))return;texture=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};texture.LoadImage(File.ReadAllBytes(path));_textures[stage]=texture;}
   _crackMaterial.SetTexture("_BaseMap",texture);_cracks.SetActive(true);_lastProgress=Time.unscaledTime;
  }
  private void Update()
  {
   bool active=UnifiedHotbar.McInput&&InputMode.Instance?.PickerOpen!=true&&ItemUI.Instance?.Open!=true&&BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected&&WorldDiagnostics.Playing(Character.localCharacter);
   if(!active||Time.unscaledTime-_lastProgress>.4f){_cracks.SetActive(false);_surface.SetActive(false);}
   _outline.SetActive(false);if(!active)return;var cam=MainCamera.instance!=null?MainCamera.instance.transform:Camera.main?.transform;if(cam==null)return;
   if(Physics.Raycast(cam.position,cam.forward,out var hit,4.5f,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore)){var p=hit.point-hit.normal*.01f;var pos=Coords.UnityPointToMcBlock(p.x,p.y,p.z);if(BlockRenderer.Instance.Grid.Get(pos)==BlockGrid.Air)return;var min=Coords.McBlockMinToUnity(pos.X,pos.Y,pos.Z);_outline.transform.position=new Vector3(min.X+.5f,min.Y+.5f,min.Z+.5f);_outline.SetActive(true);}
  }
  private void OnDestroy(){Destroy(_cracks);Destroy(_surface);Destroy(_outline);Destroy(_crackMaterial);Destroy(_lineMaterial);foreach(var t in _textures.Values)Destroy(t);}
 }
}
