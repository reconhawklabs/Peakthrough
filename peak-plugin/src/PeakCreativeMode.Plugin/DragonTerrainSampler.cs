using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 /// Shares a64-column/frame budget with the ordinary survey while a host arena is active.
 internal sealed class DragonTerrainSampler:MonoBehaviour {
  private string _id;private int _radius,_cursor;private float _rescan;private IReadOnlyList<(int,int)> _offsets;private readonly Dictionary<Vector2Int,int> _known=new Dictionary<Vector2Int,int>();
  public static bool Active=>PhotonNetwork.IsMasterClient&&BossHud.Instance?.ArenaOrigin!=null&&BossHud.Instance.State.Visible&&WorldDiagnostics.Playing(Character.localCharacter)&&BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected;
  private void Update(){if(!Active){_id=null;_known.Clear();return;}var hud=BossHud.Instance;var origin=hud.ArenaOrigin.Value;string id=hud.State.Id;if(id!=_id||_radius!=hud.Orbit){_id=id;_radius=hud.Orbit;_offsets=ArenaSurvey.Offsets(_radius);_cursor=0;_known.Clear();}if(_cursor>=_offsets.Count){if(Time.unscaledTime<_rescan)return;_cursor=0;}var solids=new JArray();int n=0;while(_cursor<_offsets.Count&&n++<32){var offset=_offsets[_cursor++];var cell=new Vector2Int(Mathf.FloorToInt(origin.X)+offset.Item1,Mathf.FloorToInt(origin.Z)+offset.Item2);var hits=Physics.RaycastAll(new Vector3(cell.x+.5f,origin.Y+128,cell.y+.5f),Vector3.down,256,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);float top=float.NegativeInfinity;foreach(var hit in hits)if(NativeTerrainClipper.StaticTerrain(hit.collider)&&hit.point.y>top)top=hit.point.y;if(float.IsNegativeInfinity(top))continue;int y=TerrainColumns.Surface(top);if(_known.TryGetValue(cell,out int old)&&old==y)continue;_known[cell]=y;for(int d=0;d<3;d++)solids.Add(new JArray(cell.x,y-d,cell.y));}if(solids.Count>0)BridgeBehaviour.Instance.Client.Send(new JObject{["t"]="terrain",["origin"]=new JArray(0,0,0),["size"]=new JArray(_radius*2+1,256,_radius*2+1),["solid"]=solids,["water"]=new JArray()}.ToString(Newtonsoft.Json.Formatting.None));if(_cursor>=_offsets.Count)_rescan=Time.unscaledTime+10;}
 }
}
