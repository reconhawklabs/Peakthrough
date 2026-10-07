using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class BossHud:MonoBehaviour {
  public static BossHud Instance;public V3? ArenaOrigin;public int Orbit;public readonly BossBar State=new BossBar();private float _victoryUntil;public bool VictoryVisible=>Time.unscaledTime<_victoryUntil;
  private void Awake(){Instance=this;}
  public void Clear(){State.Clear();_victoryUntil=0;ArenaOrigin=null;Orbit=0;}
  public void Receive(JObject o){if(!State.Apply(o))return;if(State.Killed){_victoryUntil=Time.unscaledTime+6;ArenaOrigin=null;Plugin.Log.LogInfo("[boss] Dragon defeated "+State.Id+", victory banner6seconds");}else if((string)o["t"]=="boss"){if(ArenaSurvey.TryOrigin(o["origin"],o["orbit"],out var origin,out int orbit)){ArenaOrigin=origin;Orbit=orbit;}else ArenaOrigin=null;}}
  private void Update(){if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(Character.localCharacter))Clear();}
  private void OnGUI(){if(!WorldDiagnostics.Playing(Character.localCharacter))return;if(State.Visible){float width=Mathf.Min(420,Screen.width*.6f),left=(Screen.width-width)/2;var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=20};style.normal.textColor=Color.white;GUI.Box(new Rect(left,20,width,32),"");GUI.Label(new Rect(left,22,width,28),State.Name,style);GUI.Box(new Rect(left,52,width,14),"");var old=GUI.color;GUI.color=new Color(.65f,.1f,.85f);GUI.DrawTexture(new Rect(left+2,54,(width-4)*State.Progress,10),Texture2D.whiteTexture);GUI.color=old;}if(Time.unscaledTime<_victoryUntil){float width=Mathf.Min(640,Screen.width*.9f);var rect=new Rect((Screen.width-width)/2,Screen.height*.25f,width,72);var color=GUI.color;GUI.color=new Color(0,0,0,.8f);GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=color;var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=32,fontStyle=FontStyle.Bold};style.normal.textColor=Color.white;GUI.Label(rect,"THE END — Dragon defeated",style);}}
 }
}
