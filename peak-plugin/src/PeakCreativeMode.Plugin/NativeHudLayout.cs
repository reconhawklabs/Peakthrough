using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PeakCreativeMode.Plugin {
 internal sealed class NativeHudLayout:MonoBehaviour {
  private RectTransform _root;
  private Vector2 _original;
  private readonly Vector3[] _corners=new Vector3[4];
  internal static JObject Diagnostics {get;private set;}

  private void LateUpdate(){
   var gui=GUIManager.instance;
   if(!Plugin.Keys.PeakBarsTopLeft.Value||gui==null||gui.bar==null||gui.bar.fullBar==null){Restore();return;}
   // Native afflictions are siblings of StaminaBar; move the whole native status group.
   var group=gui.staminaCanvasGroup!=null?gui.staminaCanvasGroup.transform as RectTransform:null;
   var root=group!=null&&gui.bar.transform.IsChildOf(group)&&group!=gui.hudCanvas?.transform
    ?group:gui.bar.transform.parent as RectTransform;
   if(root==null||!(root.parent is RectTransform parent))return;
   if(_root!=root){Restore();_root=root;_original=root.anchoredPosition;Plugin.Log.LogInfo($"[hud] PEAK health/stamina moved to top-left ({root.name})");}
   var canvas=gui.bar.GetComponentInParent<Canvas>();
   var camera=canvas!=null&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
   gui.bar.fullBar.GetWorldCorners(_corners);
   var lower=RectTransformUtility.WorldToScreenPoint(camera,_corners[0]);
   var upper=RectTransformUtility.WorldToScreenPoint(camera,_corners[1]);
   var safe=Screen.safeArea;float scale=Mathf.Max(.25f,Screen.height/1080f);
   // Leave room above the main bar for native status icons, and below for bonus stamina.
   var delta=new Vector2(safe.xMin+32*scale-lower.x,safe.yMax-96*scale-upper.y);
   var origin=RectTransformUtility.WorldToScreenPoint(camera,root.position);
   if(delta.sqrMagnitude>.25f&&RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,origin,camera,out var from)
    &&RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,origin+delta,camera,out var to))root.anchoredPosition+=to-from;
   Diagnostics=new JObject{["group"]=root.name,["mainBarLeft"]=lower.x+delta.x,["mainBarTop"]=Screen.height-upper.y-delta.y,["topLeft"]=true};
  }
  private void Restore(){if(_root!=null)_root.anchoredPosition=_original;_root=null;Diagnostics=null;}
  private void OnDestroy()=>Restore();
 }
}
