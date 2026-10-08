using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class EmbeddedArrows:MonoBehaviour {
  private sealed class Stuck {public string Key;public Character Player;public GameObject Visual;public float Expires;}
  public static EmbeddedArrows Instance;private readonly List<Stuck> _stuck=new List<Stuck>();private bool _warned;
  private void Awake(){Instance=this;}
  public void Clear(){foreach(var a in _stuck)EntityRenderer.Instance?.ReleaseAttachedArrow(a.Visual);_stuck.Clear();}
  private static Character Find(string uuid){var bridge=BridgeBehaviour.Instance;if(uuid==bridge.PlayerUuid)return Character.localCharacter;foreach(var ch in PlayerHandler.GetAllPlayerCharacters()){if(ch==null||ch.photonView?.Owner==null)continue;var owner=ch.photonView.Owner;string id=owner.IsMasterClient?owner.UserId:RelayAuthority.PlayerId(owner.UserId,owner.ActorNumber);if(id!=null&&HeldItemRenderer.Uuid(bridge.MapSeed,id)==uuid)return ch;}return null;}
  public void Receive(JObject msg){if(!ArrowImpact.TryParse(msg,out var hit))return;var ch=Find(hit.Player);if(!WorldDiagnostics.Playing(ch))return;string key=hit.Player+":"+hit.Id;if(_stuck.Exists(a=>a.Key==key))return;
   try{var offset=hit.Offset;var point=WorldDiagnostics.Feet(ch)+new Vector3(offset.X,offset.Y,offset.Z);var direction=new Vector3(hit.Direction.X,hit.Direction.Y,hit.Direction.Z);Transform parent=null;Vector3 contact=point;float closest=float.MaxValue;bool rayHit=false;var ray=new Ray(point-direction*2,direction);
    foreach(var part in ch.refs.ragdoll.partList){if(part.Rig==null)continue;foreach(var collider in part.GetComponentsInChildren<Collider>()){if(collider.isTrigger||collider.attachedRigidbody!=part.Rig)continue;if(collider.Raycast(ray,out var surface,4)){if(!rayHit||surface.distance<closest){rayHit=true;closest=surface.distance;contact=surface.point;parent=part.Rig.transform;}}else if(!rayHit){var surfacePoint=collider.ClosestPoint(point);float distance=(surfacePoint-point).sqrMagnitude;if(distance<closest){closest=distance;contact=surfacePoint;parent=part.Rig.transform;}}}}
    if(parent==null)return;if(_stuck.Count>=48){EntityRenderer.Instance.ReleaseAttachedArrow(_stuck[0].Visual);_stuck.RemoveAt(0);}var visual=EntityRenderer.Instance.CreateAttachedArrow();visual.transform.SetParent(parent,true);visual.transform.position=contact-direction*.18f;visual.transform.rotation=Quaternion.FromToRotation(Vector3.right,direction);var scale=parent.lossyScale;visual.transform.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));_stuck.Add(new Stuck{Key=key,Player=ch,Visual=visual,Expires=Time.unscaledTime+90});
   }catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[arrows] "+e.Message);}}
  }
  private void Update(){if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected){Clear();return;}for(int i=_stuck.Count-1;i>=0;i--){var a=_stuck[i];if(a.Visual==null||!WorldDiagnostics.Playing(a.Player)||Time.unscaledTime>=a.Expires){EntityRenderer.Instance?.ReleaseAttachedArrow(a.Visual);_stuck.RemoveAt(i);}}}
  private void OnDestroy(){Clear();Instance=null;}
 }
}
