using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal static class LightingProbe {
  private static readonly Type Volume=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("LightVolume",false)).FirstOrDefault(t=>t!=null);
  private static readonly MethodInfo Instance=Volume?.GetMethod("Instance",BindingFlags.Public|BindingFlags.Static),Sample=Volume?.GetMethod("SamplePositionAlpha",BindingFlags.Public|BindingFlags.Instance);
  public static float? Openness(Vector3 point){try{var volume=Instance?.Invoke(null,null);if(volume==null||Sample==null)return null;float n=(float)Sample.Invoke(volume,new object[]{point,false});return float.IsNaN(n)||float.IsInfinity(n)?(float?)null:n;}catch{return null;}}
  public static bool SunOccluded(Vector3 point){var sun=DayNightManager.instance?.sun;if(sun==null)return false;var hit=HelperFunctions.LineCheck(point-sun.transform.forward*1000,point,HelperFunctions.LayerType.AllPhysical);return hit.transform!=null&&(Character.localCharacter==null||hit.transform.root!=Character.localCharacter.transform.root);}
  public static JArray Grid(V3 center){var result=new JArray();for(int x=-2;x<=2;x++)for(int z=-2;z<=1;z++){var p=new Vector3(center.X+x*8,center.Y,center.Z+z*8);float? open=Openness(p);var sample=new JObject{["pos"]=new JArray(p.x,p.y,p.z),["openness"]=open.HasValue?new JValue(open.Value):JValue.CreateNull(),["sunOccluded"]=SunOccluded(p)};if(Physics.Raycast(p,Vector3.down,out var below,20,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore)){sample["ground"]=below.collider.name;sample["groundDistance"]=below.distance;}if(Physics.Raycast(p,Vector3.up,out var above,20,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore)){sample["ceiling"]=above.collider.name;sample["headroom"]=above.distance;}result.Add(sample);}return result;}
 }
}
