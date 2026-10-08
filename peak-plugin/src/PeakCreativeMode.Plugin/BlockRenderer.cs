using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.Rendering;
using GridPos=PeakCreativeMode.Core.BlockPos;
namespace PeakCreativeMode.Plugin
{
    internal sealed class BlockRenderer:MonoBehaviour
    {
        public static BlockRenderer Instance {get;private set;}
        public readonly BlockGrid Grid=new BlockGrid();
        public bool Ready {get;private set;}
        public string AssetStatus {get;private set;}="loading";
        public int ChunkCount=>_chunks.Count;
        public int QuadCount {get;private set;}
        public static readonly string[] Picker={"minecraft:stone","minecraft:cobblestone","minecraft:oak_planks","minecraft:dirt","minecraft:bricks","minecraft:sandstone","minecraft:oak_log","minecraft:white_wool"};
        private readonly Dictionary<GridPos,GameObject> _chunks=new Dictionary<GridPos,GameObject>();
        private readonly Dictionary<string,Dictionary<string,string>> _faces=new Dictionary<string,Dictionary<string,string>>();
        private readonly Dictionary<string,Rect> _uv=new Dictionary<string,Rect>();
        private Material _material;
        private AssetModels _models;private readonly Dictionary<string,List<ModelQuad>> _special=new Dictionary<string,List<ModelQuad>>();
        private Texture2D _atlas;private readonly Dictionary<string,float> _textureUsed=new Dictionary<string,float>();
        private Rect Texture(string id){if(_uv.TryGetValue(id,out var rect)){_textureUsed[id]=Time.unscaledTime;return rect;}
            int slot=_uv.Count;
            if(slot>=256){string oldest=_textureUsed.OrderBy(p=>p.Value).First().Key;rect=_uv[oldest];slot=Mathf.RoundToInt(rect.x*288-1)/18+16*(Mathf.RoundToInt(rect.y*288-1)/18);_uv.Remove(oldest);_textureUsed.Remove(oldest);foreach(var chunk in _chunks.Keys)Grid.Dirty.Add(chunk);}
            var source=new Texture2D(2,2,TextureFormat.RGBA32,false);if(!source.LoadImage(File.ReadAllBytes(_models.TexturePath(id))))throw new InvalidDataException("Cannot load "+id);
            int ox=slot%16*18+1,oy=slot/16*18+1;
            // Fit arbitrary resolution textures to a nearest sampled 16px atlas tile.
            for(int y=-1;y<=16;y++)for(int x=-1;x<=16;x++){int sx=Mathf.Clamp(x,0,15)*source.width/16,sy=source.height-Mathf.Min(source.height,source.width)+Mathf.Clamp(y,0,15)*Mathf.Min(source.height,source.width)/16;_atlas.SetPixel(ox+x,oy+y,source.GetPixel(sx,sy));}
            Destroy(source);_atlas.Apply(false,false);rect=new Rect(ox/288f,oy/288f,16/288f,16/288f);_uv[id]=rect;_textureUsed[id]=Time.unscaledTime;return rect;
        }

        private GameObject _root;
        private int _layer;
        private bool _warnedMessage;
        private void Awake()
        {
            Instance=this;
            try
            {
                _layer=LayerMask.NameToLayer("Terrain");if(_layer<0)throw new InvalidOperationException("Missing Terrain layer");
                _models=new AssetModels(Plugin.Keys.AssetCache.Value);
                _atlas=new Texture2D(288,288,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
                _atlas.Apply(false,false);var shader=Plugin.Keys.LitBlocks.Value?(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard")):Shader.Find("Universal Render Pipeline/Unlit");
                shader=shader??Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Texture");
                if(shader==null)
                {
                    string names=string.Join(", ",Resources.FindObjectsOfTypeAll<Shader>().Select(x=>x.name));
                    throw new InvalidOperationException("No supported world shader loaded. Available: "+names);
                }
                _material=new Material(shader);_material.SetFloat("_Metallic",0);_material.SetFloat("_Smoothness",0);_material.SetFloat("_AlphaClip",1);_material.EnableKeyword("_ALPHATEST_ON");
                if(_material.HasProperty("_BaseMap"))_material.SetTexture("_BaseMap",_atlas);
                if(_material.HasProperty("_MainTex"))_material.SetTexture("_MainTex",_atlas);
                if(_material.HasProperty("_BaseColor"))_material.SetColor("_BaseColor",Color.white);
                Plugin.Log.LogInfo("[blocks] shader "+shader.name);
                _root=new GameObject("PeakMinecraftBlocks");DontDestroyOnLoad(_root);
                Ready=true;AssetStatus="26.3 block cache ready";
                Plugin.Log.LogInfo($"[blocks] 256 texture LRU slots, Terrain layer {_layer}; one non-trigger MeshCollider per 16³ chunk");
            }
            catch(Exception e){AssetStatus="Export assets and set Assets.CachePath: "+e.Message;Plugin.Log.LogError("[blocks] "+AssetStatus);}
        }
        public void Receive(JObject msg)
        {
            AverageFrameMs=Mathf.Lerp(AverageFrameMs,Time.unscaledDeltaTime*1000,.02f);
            if(!Ready)return;
            if(!Grid.Apply(msg)&&!_warnedMessage){_warnedMessage=true;Plugin.Log.LogWarning("[blocks] malformed block/chunk dropped");}
        }
        private static void DisposeChunk(GameObject go){var mesh=go.GetComponent<MeshFilter>().sharedMesh;var collision=go.GetComponent<MeshCollider>().sharedMesh;if(collision!=mesh)Destroy(collision);Destroy(mesh);Destroy(go);}
        private List<ModelQuad> Special(string state){if(state.StartsWith("minecraft:water")||state.StartsWith("minecraft:lava"))return new List<ModelQuad>();if(!_special.TryGetValue(state,out var result)){try{result=_models.Quads(state);foreach(var q in result)q.Collision&=BlockModelGeometry.HasCollision(state)&&!BlockModelGeometry.ClimbCell(state);}catch(Exception e){Plugin.Log.LogWarning("[blocks] model "+state+": "+e.Message);result=_models.Quads("minecraft:stone");}_special[state]=result;}return result;}

        public void Clear()
        {
            foreach(var go in _chunks.Values){DisposeChunk(go);}
            _chunks.Clear();Grid.Clear();QuadCount=0;
        }
        public float LastRebuildMs {get;private set;}
        public float AverageFrameMs {get;private set;}
        private void Update()
        {
            AverageFrameMs=Mathf.Lerp(AverageFrameMs,Time.unscaledDeltaTime*1000,.02f);
            if(!Ready)return;
            var bridge=BridgeBehaviour.Instance;bool connected=bridge!=null&&bridge.Client.Status==BridgeStatus.Connected;
            if(!connected&&(_chunks.Count>0||Grid.Count>0))Clear();
            var ch=Character.localCharacter;
            _root.SetActive(connected&&ch!=null&&!ch.inAirport);
            if(!connected)return;
            var watch=System.Diagnostics.Stopwatch.StartNew();
            for(int i=0;i<Plugin.Keys.ChunksPerFrame.Value&&Grid.Dirty.Count>0&&watch.Elapsed.TotalMilliseconds<2;i++)
            {
                GridPos chunk=Grid.Dirty.First();Grid.Dirty.Remove(chunk);Rebuild(chunk);
            }
            LastRebuildMs=(float)watch.Elapsed.TotalMilliseconds;
        }
        private void Rebuild(GridPos chunk)
        {
            var faces=ChunkMesher.Build(Grid,chunk,Special);
            if(_chunks.TryGetValue(chunk,out var old))
            {
                var mesh=old.GetComponent<MeshFilter>().sharedMesh;QuadCount-=mesh.vertexCount/4;DisposeChunk(old);_chunks.Remove(chunk);
            }
            if(faces.Count==0)return;
            var collisionIndices=new List<int>();
            var vertices=new Vector3[faces.Count*4];var uv=new Vector2[vertices.Length];var triangles=new int[faces.Count*6];
            for(int j=0;j<faces.Count;j++)
            {
                var q=faces[j];Rect rect=Texture(q.Texture??"minecraft:block/stone");
                for(int v=0;v<4;v++)
                {
                    var p=q.Vertices[v];vertices[j*4+v]=new Vector3(p.X,p.Y,p.Z);
                    uv[j*4+v]=q.UV!=null?new Vector2(rect.x+q.UV[v][0]*rect.width,rect.y+q.UV[v][1]*rect.height):new Vector2(rect.x+((v==2||v==3)?rect.width:0),rect.y+((v==1||v==2)?rect.height:0));
                }
                int a=j*4,b=j*6;triangles[b]=a;triangles[b+1]=a+1;triangles[b+2]=a+2;triangles[b+3]=a;triangles[b+4]=a+2;triangles[b+5]=a+3;if(q.Collision)for(int index=0;index<6;index++)collisionIndices.Add(triangles[b+index]);
            }
            var m=new Mesh{name="Minecraft chunk mesh",indexFormat=IndexFormat.UInt32,vertices=vertices,uv=uv,triangles=triangles};m.RecalculateNormals();m.RecalculateBounds();
            var go=new GameObject($"MC chunk {chunk.X},{chunk.Y},{chunk.Z}");go.layer=_layer;go.transform.SetParent(_root.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=m;go.AddComponent<MeshRenderer>().sharedMaterial=_material;
            var collisionVertices=new List<Vector3>(vertices);
            for(int y=0;y<16;y++)for(int z=0;z<16;z++)for(int x=0;x<16;x++){var pos=new GridPos(chunk.X*16+x,chunk.Y*16+y,chunk.Z*16+z);if(!BlockModelGeometry.ClimbCell(Grid.Get(pos)))continue;var min=Coords.McBlockMinToUnity(pos.X,pos.Y,pos.Z);foreach(var q in _models.Quads("minecraft:stone")){int start=collisionVertices.Count;foreach(var v in q.Vertices)collisionVertices.Add(new Vector3(min.X+v.X,min.Y+v.Y,min.Z+v.Z));collisionIndices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}}
            var collider=go.AddComponent<MeshCollider>();var collisionMesh=new Mesh{name="Minecraft solid collision",indexFormat=IndexFormat.UInt32,vertices=collisionVertices.ToArray(),triangles=collisionIndices.ToArray()};collisionMesh.RecalculateBounds();collider.sharedMesh=collisionMesh;collider.convex=false;collider.isTrigger=false;
            _chunks.Add(chunk,go);QuadCount+=faces.Count;
            Plugin.Log.LogInfo($"[blocks] chunk {chunk.X},{chunk.Y},{chunk.Z}: {faces.Count} quads, Terrain MeshCollider non-trigger");
        }
        private void OnDestroy(){Clear();if(_root!=null)Destroy(_root);if(_material!=null)Destroy(_material);if(_atlas!=null)Destroy(_atlas);}
    }
}
