using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public sealed class TerraformVolume {
  public string Id,Material;public BlockPos Origin,Size;public ClipBox Box;public readonly HashSet<BlockPos> Chunks=new HashSet<BlockPos>();
  public bool Ready(BlockGrid grid){foreach(var chunk in Chunks)if(grid.Dirty.Contains(chunk))return false;return true;}
  private static int Integer(JToken t,int min,int max){if(t?.Type!=JTokenType.Integer)throw new ArgumentException();long n=(long)t;if(n<min||n>max)throw new ArgumentException();return (int)n;}
  public static TerraformVolume Parse(JObject o){try{
   if(o["id"]?.Type!=JTokenType.String||o["material"]?.Type!=JTokenType.String)return null;string id=(string)o["id"],mat=(string)o["material"];if(id.Length<1||id.Length>128||(mat!="minecraft:stone"&&mat!="minecraft:dirt"&&mat!="minecraft:sandstone"))return null;
   if(!(o["origin"] is JArray p)||p.Count!=3||!(o["size"] is JArray s)||s.Count!=3)return null;
   var origin=new BlockPos(Integer(p[0],-29000000,29000000),Integer(p[1],-2032,2028),Integer(p[2],-29000000,29000000));var size=new BlockPos(Integer(s[0],4,8),Integer(s[1],4,8),Integer(s[2],4,8));if(origin.Y+size.Y>2032)return null;
   var v=new TerraformVolume{Id=id,Material=mat,Origin=origin,Size=size,Box=new ClipBox(new V3(origin.X,origin.Y,-origin.Z-size.Z),new V3(origin.X+size.X,origin.Y+size.Y,-origin.Z))};
   var first=origin.Chunk;var last=origin.Offset(size.X-1,size.Y-1,size.Z-1).Chunk;for(int x=first.X;x<=last.X;x++)for(int y=first.Y;y<=last.Y;y++)for(int z=first.Z;z<=last.Z;z++)v.Chunks.Add(new BlockPos(x,y,z));return v;
  }catch{return null;}}
 }
}
