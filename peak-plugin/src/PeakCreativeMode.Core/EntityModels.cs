using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 public sealed class ModelPartData
 {
  public string Name;public V3 Pivot,Rotation,Scale;
  public readonly List<float[][]> Faces=new List<float[][]>();
  public readonly List<ModelPartData> Children=new List<ModelPartData>();
 }
 public sealed class EntityModels
 {
  public string Texture {get;private set;}
  public ModelPartData Root {get;private set;}
  public static EntityModels Parse(string json)
  {
   try{var o=JObject.Parse(json);return new EntityModels{Texture=(string)o["texture"],Root=Part(o["root"] as JObject,0)};}
   catch(Exception e)when(!(e is InvalidDataException)){throw new InvalidDataException("Invalid entity model",e);}
  }
  private static float Number(JToken t){if(t==null||(t.Type!=JTokenType.Integer&&t.Type!=JTokenType.Float))throw new InvalidDataException("Model number");float n=(float)t;if(float.IsNaN(n)||float.IsInfinity(n)||Math.Abs(n)>10000)throw new InvalidDataException("Model range");return n;}
  private static V3 Vector(JToken t){if(!(t is JArray a)||a.Count!=3)throw new InvalidDataException("Model vector");return new V3(Number(a[0]),Number(a[1]),Number(a[2]));}
  private static ModelPartData Part(JObject o,int depth)
  {
   if(o==null||depth>32||o["name"]?.Type!=JTokenType.String||!(o["faces"] is JArray faces)||!(o["children"] is JArray children)||faces.Count>10000||children.Count>1000)throw new InvalidDataException("Model part");
   var part=new ModelPartData{Name=(string)o["name"],Pivot=Vector(o["pivot"]),Rotation=Vector(o["rot"]),Scale=o["scale"]==null?new V3(1,1,1):Vector(o["scale"])};
   foreach(var face in faces){if(!(face is JArray f)||f.Count!=4)throw new InvalidDataException("Model quad");var q=new float[4][];for(int i=0;i<4;i++){if(!(f[i] is JArray v)||v.Count!=5)throw new InvalidDataException("Model vertex");q[i]=new float[5];for(int n=0;n<5;n++)q[i][n]=Number(v[n]);}part.Faces.Add(q);}
   foreach(var child in children)part.Children.Add(Part(child as JObject,depth+1));return part;
  }
 }
}
