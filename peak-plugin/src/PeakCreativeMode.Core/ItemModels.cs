using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 public struct Display{public V3 Rotation,Translation,Scale;public static readonly Display Identity=new Display{Scale=new V3(1,1,1)};}
 public sealed class ItemModel{public string Layer0,BlockModel;public Display FirstPersonRight=Display.Identity,ThirdPersonRight=Display.Identity,Ground=Display.Identity;}
 /// Resolves items/<id>.json → models/item parents → layer0 or block model, plus display transforms. Paths are relative to the asset cache's assets/minecraft/.
 public sealed class ItemModels
 {
  private readonly Func<string,string> _read;public ItemModels(Func<string,string> read){_read=read;}
  private static string Path(string id){var s=id.StartsWith("minecraft:")?id.Substring(10):id;return s;}
  private static string Definition(JObject o,bool usingItem,float ticks,int depth=0){if(o==null||depth>16)return null;switch((string)o["type"]){case "minecraft:model":return (string)o["model"];case "minecraft:condition":if((string)o["property"]!="minecraft:using_item")return null;return Definition(o[usingItem?"on_true":"on_false"] as JObject,usingItem,ticks,depth+1);case "minecraft:range_dispatch":if((string)o["property"]!="minecraft:use_duration")return null;double scale=(double?)o["scale"]??1;if(double.IsNaN(scale)||double.IsInfinity(scale)||scale<0)return null;double value=(float.IsNaN(ticks)||float.IsInfinity(ticks)?0:Math.Max(0,ticks))*scale;var selected=o["fallback"] as JObject;double best=double.NegativeInfinity;if(o["entries"] is JArray entries){if(entries.Count>128)return null;foreach(var entry in entries){double threshold=(double)entry["threshold"];if(double.IsNaN(threshold)||double.IsInfinity(threshold))return null;if(threshold<=value&&threshold>=best){best=threshold;selected=entry["model"] as JObject;}}}return Definition(selected,usingItem,ticks,depth+1);default:return null;}}
  public ItemModel Resolve(string itemId,bool usingItem=false,float useTicks=0)
  {
   try{var def=_read("items/"+Path(itemId)+".json");if(def==null)return null;var model=Definition(JObject.Parse(def)["model"] as JObject,usingItem,useTicks);if(model==null)return null;
    var m=new ItemModel();if(Path(model).StartsWith("block/")){m.BlockModel=model;ApplyDisplay(m,"block/block",0);return m;}
    var seen=new HashSet<string>();string cur=Path(model);var displays=new List<JObject>();
    while(cur!=null){if(!seen.Add(cur)||seen.Count>16)return null;var text=_read("models/"+cur+".json");if(text==null){if(cur=="item/generated"||cur=="builtin/generated")break;return null;}var o=JObject.Parse(text);if(m.Layer0==null)m.Layer0=(string)o["textures"]?["layer0"];if(o["display"] is JObject d)displays.Add(d);var parent=(string)o["parent"];cur=parent==null?null:Path(parent);}
    for(int i=displays.Count-1;i>=0;i--)Apply(m,displays[i]);return m;}
   catch(Exception){return null;}
  }
  private void ApplyDisplay(ItemModel m,string model,int depth){if(depth>16)return;var t=_read("models/"+model+".json");if(t==null)return;var o=JObject.Parse(t);var p=(string)o["parent"];if(p!=null)ApplyDisplay(m,Path(p),depth+1);if(o["display"] is JObject d)Apply(m,d);}
  private static void Apply(ItemModel m,JObject d){if(d["firstperson_righthand"] is JObject f)m.FirstPersonRight=Read(f);if(d["thirdperson_righthand"] is JObject t)m.ThirdPersonRight=Read(t);if(d["ground"] is JObject g)m.Ground=Read(g);}
  private static Display Read(JObject o)=>new Display{Rotation=V(o["rotation"],0),Translation=V(o["translation"],0),Scale=V(o["scale"],1)};
  private static V3 V(JToken t,float dflt)=>t is JArray a&&a.Count==3?new V3((float)a[0],(float)a[1],(float)a[2]):new V3(dflt,dflt,dflt);
 }
}
