using System;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 public readonly struct ItemSlot {public readonly string Item;public readonly int Count;public readonly PeakToken Peak;public readonly int Damage,MaxDamage;public float Durability=>MaxDamage>0?Math.Max(0,Math.Min(1,1-Damage/(float)MaxDamage)):1;public ItemSlot(string item,int count,PeakToken peak=null,int damage=0,int maxDamage=0){Item=item;Count=count;Peak=peak;Damage=damage;MaxDamage=maxDamage;}}
 public sealed class InventoryState
 {
  public ItemSlot[] Slots {get;private set;}=new ItemSlot[36];
  public bool Stowed {get;set;}
  public int Selected {get;private set;}
  public ItemSlot Carried {get;private set;}
  public static int MenuSlot(int slot){if(slot<0||slot>=36)throw new ArgumentOutOfRangeException(nameof(slot));return slot<9?36+slot:slot;}
  private static bool Slot(JToken t,out ItemSlot slot){slot=default;if(!(t is JObject o)||o["count"]?.Type!=JTokenType.Integer)return false;long count=(long)o["count"];if(count<0||count>999)return false;var id=o["item"];if(id!=null&&id.Type!=JTokenType.Null&&id.Type!=JTokenType.String)return false;PeakToken peak=null;if(o["peak"]!=null&&o["peak"].Type!=JTokenType.Null){peak=PeakToken.FromJson(o["peak"]);if(peak==null||count!=1||(string)id!="minecraft:paper")return false;}int damage=0,maxDamage=0;foreach(string key in new[]{"damage","maxDamage"})if(o[key]!=null){if(o[key].Type!=JTokenType.Integer||(long)o[key]<0||(long)o[key]>int.MaxValue)return false;if(key=="damage")damage=(int)o[key];else maxDamage=(int)o[key];}slot=new ItemSlot((string)id,(int)count,peak,damage,maxDamage);return count==0||slot.Item!=null;}
  public bool Apply(JObject o){if(o["stowed"]!=null&&o["stowed"].Type!=JTokenType.Boolean)return false;if(!(o["slots"] is JArray a)||a.Count!=36||o["selected"]?.Type!=JTokenType.Integer)return false;long selected=(long)o["selected"];if(selected<0||selected>8)return false;var slots=new ItemSlot[36];for(int i=0;i<36;i++)if(!Slot(a[i],out slots[i]))return false;ItemSlot carried=default;if(o["carried"]!=null&&!Slot(o["carried"],out carried))return false;Stowed=o["stowed"]?.Value<bool>()??false;Slots=slots;Selected=(int)selected;Carried=carried;return true;}
  public void Clear(){Stowed=false;Slots=new ItemSlot[36];Selected=0;Carried=default;}
 }
}
