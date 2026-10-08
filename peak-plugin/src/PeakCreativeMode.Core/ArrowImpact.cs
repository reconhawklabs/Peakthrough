using System;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public readonly struct ArrowImpact {
  public readonly string Player;public readonly int Id;public readonly V3 Offset,Direction;
  private ArrowImpact(string player,int id,V3 offset,V3 direction){Player=player;Id=id;Offset=offset;Direction=direction;}
  private static bool Vector(JToken token,out V3 value){value=default;if(!(token is JArray a)||a.Count!=3)return false;var n=new float[3];for(int i=0;i<3;i++){if(a[i].Type!=JTokenType.Float&&a[i].Type!=JTokenType.Integer)return false;double d=(double)a[i];if(!double.IsFinite(d)||Math.Abs(d)>100)return false;n[i]=(float)d;}value=new V3(n[0],n[1],-n[2]);return true;}
  public static bool TryParse(JObject o,out ArrowImpact hit){hit=default;if(!Guid.TryParse((string)o["player"],out _)||!(o["arrow"] is JObject arrow)||arrow["id"]?.Type!=JTokenType.Integer)return false;long id=(long)arrow["id"];if(id<=0||id>int.MaxValue||!Vector(arrow["offset"],out var offset)||Math.Abs(offset.X)>2||Math.Abs(offset.Z)>2||offset.Y<-.5||offset.Y>3||!Vector(arrow["direction"],out var direction))return false;float length=(float)Math.Sqrt(direction.X*direction.X+direction.Y*direction.Y+direction.Z*direction.Z);if(length<.0001f)return false;hit=new ArrowImpact((string)o["player"],(int)id,offset,new V3(direction.X/length,direction.Y/length,direction.Z/length));return true;}
 }
}
