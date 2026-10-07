using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
 internal sealed class TerrainSampler:MonoBehaviour
 {
  private readonly List<Vector2Int> _offsets=new List<Vector2Int>();
  private readonly Dictionary<Vector2Int,(int solid,int water)> _known=new Dictionary<Vector2Int,(int solid,int water)>();
  private Vector2Int _center;private int _cursor;private float _nextScan;private bool _active;
  private int _sentCells;
  private WaterZone[] _waterZones=System.Array.Empty<WaterZone>();private float _nextWaterRefresh;
  private void Awake(){for(int x=-48;x<=48;x++)for(int z=-48;z<=48;z++)if(x*x+z*z<=48*48)_offsets.Add(new Vector2Int(x,z));_offsets.Sort((a,b)=>a.sqrMagnitude.CompareTo(b.sqrMagnitude));}
  private void Update()
  {
   var b=BridgeBehaviour.Instance;var ch=Character.localCharacter;
   if(b?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(ch)){_known.Clear();_cursor=0;_active=false;return;}
   var feet=WorldDiagnostics.Feet(ch);var center=new Vector2Int(Mathf.FloorToInt(feet.x),Mathf.FloorToInt(feet.z));
   if(!_active||(center-_center).sqrMagnitude>=16){_center=center;_cursor=0;_active=true;}
   if(_cursor>=_offsets.Count){if(Time.unscaledTime<_nextScan)return;_cursor=0;}
   if(Time.unscaledTime>=_nextWaterRefresh){_waterZones=Resources.FindObjectsOfTypeAll<WaterZone>();_nextWaterRefresh=Time.unscaledTime+1;}
   var solids=new JArray();var waters=new JArray();int sampled=0;
   while(_cursor<_offsets.Count&&sampled++<(DragonTerrainSampler.Active?32:64))
   {
    var cell=_center+_offsets[_cursor++];var origin=new Vector3(cell.x+.5f,feet.y+48,cell.y+.5f);
    var hits=Physics.RaycastAll(origin,Vector3.down,96,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);
    float top=float.NegativeInfinity;
    foreach(var hit in hits){if(hit.collider.transform.root.name=="PeakMinecraftBlocks"||hit.collider.CompareTag("Water"))continue;if(hit.point.y>top)top=hit.point.y;}
    int y=float.IsNegativeInfinity(top)?int.MinValue:TerrainColumns.Surface(top);int waterY=int.MinValue;
    foreach(var zone in _waterZones){if(zone==null||!zone.isActiveAndEnabled)continue;var bounds=zone.waterPlane!=null?zone.waterPlane.bounds:zone.zoneBounds;if(cell.x+.5f<bounds.min.x||cell.x+.5f>bounds.max.x||cell.y+.5f<bounds.min.z||cell.y+.5f>bounds.max.z)continue;float waterTop=zone.waterPlane!=null?bounds.center.y:bounds.max.y;int surface=TerrainColumns.Surface(waterTop);if(surface>=y+1&&(y==int.MinValue||waterTop>top+.05f)&&Mathf.Abs(waterTop-feet.y)<48)waterY=Mathf.Max(waterY,surface);}
    if(y==int.MinValue&&waterY==int.MinValue)continue;
    if(_known.TryGetValue(cell,out var old)&&old.solid==y&&old.water==waterY)continue;_known[cell]=(y,waterY);
    if(y!=int.MinValue)for(int depth=0;depth<3;depth++)solids.Add(new JArray(cell.x,y-depth,cell.y));
    if(waterY!=int.MinValue)for(int depth=0;depth<3;depth++)if(waterY-depth>y)waters.Add(new JArray(cell.x,waterY-depth,cell.y));
   }
   if(solids.Count>0||waters.Count>0){_sentCells+=solids.Count+waters.Count;b.Client.Send(new JObject{["t"]="terrain",["origin"]=new JArray(0,0,0),["size"]=new JArray(97,96,97),["solid"]=solids,["water"]=waters}.ToString(Newtonsoft.Json.Formatting.None));}
   if(_cursor>=_offsets.Count){_nextScan=Time.unscaledTime+10;if(_sentCells>0){Plugin.Log.LogInfo($"[terrain] scan mirrored {_sentCells} solid/water samples near {_center.x},{_center.y}");_sentCells=0;}foreach(var key in new List<Vector2Int>(_known.Keys))if((key-_center).sqrMagnitude>56*56)_known.Remove(key);}
  }
 }
}
