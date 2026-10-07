using UnityEngine;using PeakCreativeMode.Core;
namespace PeakCreativeMode.Plugin {
 /** Visual-only effects follow native death ticks. Native Minecraft owns death/XP. */
 internal sealed class DragonDeathVisual:MonoBehaviour {
  private readonly LineRenderer[] _beams=new LineRenderer[64];private readonly Transform[] _burst=new Transform[36],_xp=new Transform[24];private Material _purple,_green;private int _tick;
  private Material ColorMaterial(Color color){var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetColor("_BaseColor",color);m.SetFloat("_Cull",0);return m;}
  private void Awake(){Plugin.Log.LogInfo("[boss] Native dragon death visuals started");_purple=ColorMaterial(new Color(.8f,.1f,1));_green=ColorMaterial(new Color(.4f,1,.1f));for(int i=0;i<_beams.Length;i++){var go=new GameObject("dragon death ray");go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=_purple;line.useWorldSpace=false;line.positionCount=2;line.startWidth=.12f;line.endWidth=2.5f;line.SetPosition(0,Vector3.up*2);float angle=i*2.399963f;var dir=new Vector3(Mathf.Cos(angle),Mathf.Sin(i*1.731f),Mathf.Sin(angle)).normalized;line.SetPosition(1,Vector3.up*2+dir*(15+i%20));line.enabled=false;_beams[i]=line;}
   for(int i=0;i<_burst.Length;i++)_burst[i]=Point("dragon explosion",_purple);for(int i=0;i<_xp.Length;i++)_xp[i]=Point("dragon XP burst",_green);
  }
  private Transform Point(string name,Material mat){var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name=name;Destroy(go.GetComponent<Collider>());go.transform.SetParent(transform,false);go.GetComponent<MeshRenderer>().sharedMaterial=mat;go.SetActive(false);return go.transform;}
  public void NativeTick(int tick){_tick=tick;int n=DeathFx.Beams(tick);for(int i=0;i<_beams.Length;i++)_beams[i].enabled=i<n;}
  private void Update(){var cam=MainCamera.instance?.transform;for(int i=0;i<_burst.Length;i++){var p=_burst[i];p.gameObject.SetActive(DeathFx.Burst(_tick));if(!p.gameObject.activeSelf)continue;float t=Mathf.Clamp01((_tick-180)/20f),a=i*2.399963f;p.localPosition=Vector3.up*2+new Vector3(Mathf.Cos(a),Mathf.Sin(i*1.731f),Mathf.Sin(a))*(1+t*8);p.localScale=Vector3.one*(.6f+(i%4)*.3f);if(cam!=null)p.rotation=cam.rotation;}
   for(int i=0;i<_xp.Length;i++){var p=_xp[i];p.gameObject.SetActive(_tick>150&&_tick<=200);if(!p.gameObject.activeSelf)continue;float a=i*2.399963f+Time.time;p.localPosition=new Vector3(Mathf.Cos(a)*(2+i%5),1+Mathf.Sin(Time.time*3+i),Mathf.Sin(a)*(2+i%5));p.localScale=Vector3.one*.25f;if(cam!=null)p.rotation=cam.rotation;}
  }
  private void OnDestroy(){if(_purple!=null)Destroy(_purple);if(_green!=null)Destroy(_green);}
 }
}
