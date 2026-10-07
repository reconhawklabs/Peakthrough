using System;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 /// Hosts survey small shelves as players advance; native Minecraft owns the actual resource blocks.
 internal static class BlockDepositSampler {
  private static bool Ground(float x,float z,float y,out float height){height=0;var hits=Physics.RaycastAll(new Vector3(x,y+32,z),Vector3.down,64,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));foreach(var hit in hits)if(NativeTerrainClipper.StaticTerrain(hit.collider)){if(hit.normal.y<.65f)return false;height=hit.point.y;return height>.5f;}return false;}
  public static void Collect(Vector3 feet,System.Collections.Generic.List<(V3 pos,bool dark,bool open)> cells,bool night,float threshold){if(!Plugin.Keys.BlockDeposits.Value)return;int bx=Mathf.FloorToInt(feet.x/32)*32+8,bz=Mathf.FloorToInt(-feet.z/32)*32+8;var fires=UnityEngine.Object.FindObjectsByType<Campfire>(FindObjectsSortMode.None);
   for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++){float x=bx+dx*32+.5f,z=-(bz+dz*32+.5f);bool occupied=false;foreach(var fire in fires)if(Vector2.Distance(new Vector2(x,z),new Vector2(fire.transform.position.x,fire.transform.position.z))<8){occupied=true;break;}if(occupied||Vector3.Distance(new Vector3(x,feet.y,z),feet)<7||!Ground(x,z,feet.y,out float y))continue;if(!Ground(x+2,z-2,feet.y,out float edge)||Mathf.Abs(edge-y)>.65f)continue;var point=new Vector3(x,y,z);if(Vector3.Distance(point,feet)>47)continue;var sample=point+Vector3.up;bool sun=LightingProbe.SunOccluded(sample);float? alpha=LightingProbe.Openness(sample);bool open=alpha.HasValue?alpha.Value>=threshold:!sun;cells.Add((new V3(x,y,z),night||!open||sun,open));}
  }
 }
}
