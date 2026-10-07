using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 /// A PEAK item stored in a Minecraft slot. Data = PEAK's own ItemInstanceData bytes (opaque here).
 public sealed class PeakToken
 {
  public const int MaxDataBytes=4096;
  private static readonly Regex SafeName=new Regex("^[A-Za-z0-9 _().,'!-]{1,64}$");
  public readonly ushort ItemId;public readonly string Name;public readonly byte[] Data;
  public PeakToken(ushort itemId,string name,byte[] data){if(itemId==ushort.MaxValue)throw new ArgumentOutOfRangeException(nameof(itemId));if(name==null||!SafeName.IsMatch(name))throw new ArgumentException(nameof(name));if(data==null||data.Length>MaxDataBytes)throw new ArgumentException(nameof(data));ItemId=itemId;Name=name;Data=data;}
  public JObject ToJson()=>new JObject{["id"]=ItemId,["name"]=Name,["data"]=Convert.ToBase64String(Data)};
  public bool SameIdentity(PeakToken o)=>o!=null&&ItemId==o.ItemId&&(Data.Length>=16&&o.Data.Length>=16?Data.Take(16).SequenceEqual(o.Data.Take(16)):SameAs(o));
  public bool SameAs(PeakToken o)=>o!=null&&o.ItemId==ItemId&&o.Name==Name&&o.Data.SequenceEqual(Data);
  public static PeakToken FromJson(JToken t)
  {
   if(!(t is JObject o)||o["id"]?.Type!=JTokenType.Integer||o["name"]?.Type!=JTokenType.String||o["data"]?.Type!=JTokenType.String)return null;
   long id=(long)o["id"];if(id<0||id>=ushort.MaxValue)return null;string name=(string)o["name"];if(!SafeName.IsMatch(name))return null;
   byte[] data;try{data=Convert.FromBase64String((string)o["data"]);}catch(FormatException){return null;}
   return data.Length>MaxDataBytes?null:new PeakToken((ushort)id,name,data);
  }
 }
}
