using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public sealed class ModelQuad {public string Texture;public V3[] Vertices;public float[][] UV;public bool Collision;}
 public static class BlockModelGeometry {
  public static bool ClimbCell(string state)=>state.Split('[')[0]=="minecraft:ladder"||state.Split('[')[0]=="minecraft:scaffolding";
  public static bool HasCollision(string state){string id=state.Split('[')[0];return !(id.EndsWith("torch")||id.EndsWith("_flower")||id=="minecraft:short_grass"||id=="minecraft:fire");}
  public static bool Matches(JToken when,string state){if(when==null)return true;var o=(JObject)when;if(o["OR"] is JArray or)return or.Any(w=>Matches(w,state));if(o["AND"] is JArray and)return and.All(w=>Matches(w,state));foreach(var p in o.Properties()){int start=state.IndexOf('[');string properties=start<0?"":state.Substring(start+1).TrimEnd(']');if(!p.Value.ToString().Split('|').Any(v=>properties.Split(',').Contains(p.Name+"="+v)))return false;}return true;}

  public static JToken Variant(JObject variants,string state){var properties=new Dictionary<string,string>();int bracket=state.IndexOf('[');if(bracket>=0)foreach(string part in state.Substring(bracket+1).TrimEnd(']').Split(',')){var pair=part.Split('=');if(pair.Length==2)properties[pair[0]]=pair[1];}foreach(var candidate in variants.Properties()){bool match=true;foreach(string part in candidate.Name.Split(',')){if(part.Length==0)continue;var pair=part.Split('=');if(pair.Length!=2||!properties.TryGetValue(pair[0],out var value)||!pair[1].Split('|').Contains(value)){match=false;break;}}if(match)return candidate.Value is JArray a?a[0]:candidate.Value;}throw new InvalidDataException("No native block variant for "+state);}
  private static float Number(JToken t){if(t==null||(t.Type!=JTokenType.Float&&t.Type!=JTokenType.Integer))throw new InvalidDataException("Model number");float v=(float)t;if(float.IsNaN(v)||float.IsInfinity(v)||Math.Abs(v)>1024)throw new InvalidDataException("Model range");return v;}
  private static V3 Vec(JToken t){if(!(t is JArray a)||a.Count!=3)throw new InvalidDataException("Model vector");return new V3(Number(a[0])/16,Number(a[1])/16,Number(a[2])/16);}
  private static V3 Rotate(V3 p,V3 origin,string axis,float degrees,bool rescale=false){double r=degrees*Math.PI/180,c=Math.Cos(r),s=Math.Sin(r);float x=p.X-origin.X,y=p.Y-origin.Y,z=p.Z-origin.Z;double scale=rescale?1/Math.Cos(r):1;if(axis=="y")return new V3(origin.X+(float)((x*c+z*s)*scale),origin.Y+y,origin.Z+(float)((-x*s+z*c)*scale));if(axis=="x")return new V3(origin.X+x,origin.Y+(float)((y*c-z*s)*scale),origin.Z+(float)((y*s+z*c)*scale));if(axis=="z")return new V3(origin.X+(float)((x*c-y*s)*scale),origin.Y+(float)((x*s+y*c)*scale),origin.Z+z);throw new InvalidDataException("Model rotation axis");}
  public static List<ModelQuad> Build(JObject model,float xRotation=0,float yRotation=0){var output=new List<ModelQuad>();if(!(model["elements"] is JArray elements)||elements.Count>1024)throw new InvalidDataException("Model elements");var textures=model["textures"] as JObject??new JObject();foreach(JObject element in elements){var from=Vec(element["from"]);var to=Vec(element["to"]);if(from.X>to.X||from.Y>to.Y||from.Z>to.Z)throw new InvalidDataException("Model extent");bool collision=to.X>from.X&&to.Y>from.Y&&to.Z>from.Z;var faces=element["faces"] as JObject??throw new InvalidDataException("Model faces");foreach(var face in faces){string texture=(string)face.Value["texture"]??throw new InvalidDataException("Model texture");int depth=0;while(texture.StartsWith("#")){if(++depth>32)throw new InvalidDataException("Texture cycle");texture=(string)textures[texture.Substring(1)]??throw new InvalidDataException("Texture reference");}if(!texture.Contains(":"))texture="minecraft:"+texture;V3[] p;
    switch(face.Key){
     case "east":p=new[]{new V3(to.X,from.Y,to.Z),new V3(to.X,to.Y,to.Z),new V3(to.X,to.Y,from.Z),new V3(to.X,from.Y,from.Z)};break;
     case "west":p=new[]{new V3(from.X,from.Y,from.Z),new V3(from.X,to.Y,from.Z),new V3(from.X,to.Y,to.Z),new V3(from.X,from.Y,to.Z)};break;
     case "up":p=new[]{new V3(from.X,to.Y,from.Z),new V3(to.X,to.Y,from.Z),new V3(to.X,to.Y,to.Z),new V3(from.X,to.Y,to.Z)};break;
     case "down":p=new[]{new V3(from.X,from.Y,to.Z),new V3(to.X,from.Y,to.Z),new V3(to.X,from.Y,from.Z),new V3(from.X,from.Y,from.Z)};break;
     case "north":p=new[]{new V3(to.X,from.Y,from.Z),new V3(to.X,to.Y,from.Z),new V3(from.X,to.Y,from.Z),new V3(from.X,from.Y,from.Z)};break;
     case "south":p=new[]{new V3(from.X,from.Y,to.Z),new V3(from.X,to.Y,to.Z),new V3(to.X,to.Y,to.Z),new V3(to.X,from.Y,to.Z)};break;
     default:throw new InvalidDataException("Model face");}
    for(int i=0;i<4;i++){if(element["rotation"] is JObject rotation)p[i]=Rotate(p[i],Vec(rotation["origin"]),(string)rotation["axis"],Number(rotation["angle"]),(bool?)rotation["rescale"]??false);p[i]=Rotate(p[i],new V3(.5f,.5f,.5f),"x",xRotation);p[i]=Rotate(p[i],new V3(.5f,.5f,.5f),"y",-yRotation);p[i]=new V3(p[i].X,p[i].Y,1-p[i].Z);}
    var uvToken=face.Value["uv"] as JArray??new JArray(0,0,16,16);if(uvToken.Count!=4)throw new InvalidDataException("Model UV");foreach(var coordinate in uvToken)if(Number(coordinate)<0||Number(coordinate)>16)throw new InvalidDataException("Model UV range");float u0=Number(uvToken[0])/16,v0=1-Number(uvToken[3])/16,u1=Number(uvToken[2])/16,v1=1-Number(uvToken[1])/16;var uv=new[]{new[]{u0,v0},new[]{u0,v1},new[]{u1,v1},new[]{u1,v0}};int turn=(int?)face.Value["rotation"]??0;if(turn%90!=0)throw new InvalidDataException("UV rotation");var rotated=new float[4][];for(int i=0;i<4;i++)rotated[i]=uv[(i+turn/90+4)%4];output.Add(new ModelQuad{Texture=texture,Vertices=p,UV=rotated,Collision=collision});
   }}return output;}
 }
}
