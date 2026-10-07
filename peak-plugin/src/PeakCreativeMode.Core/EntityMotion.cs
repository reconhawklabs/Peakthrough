using System;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
 public sealed class EntityMotion
 {
  public V3 Position {get;private set;}
  public V3 Previous {get;private set;}
  public float BodyYaw {get;private set;}
  public float OldYaw {get;private set;}
  public float HeadYaw {get;private set;}
  public float Pitch {get;private set;}
  public float Swing {get;private set;}
  public float SwingAmount {get;private set;}
  public bool Grounded {get;private set;}
  public MobEquipment Equipment {get;private set;}
  public DragonPose Dragon {get;private set;}
  private bool _initialized;
  public static float Angle(float a,float b,float t){float d=(b-a)%360;if(d>180)d-=360;if(d< -180)d+=360;return a+d*Math.Max(0,Math.Min(1,t));}
  private static bool Number(JToken t,out float n){n=0;if(t==null)return true;if(t.Type!=JTokenType.Float&&t.Type!=JTokenType.Integer)return false;double d=(double)t;if(double.IsNaN(d)||double.IsInfinity(d)||Math.Abs(d)>30000000)return false;n=(float)d;return true;}
  public bool Apply(JObject o)
  {
   if(!(o["pos"] is JArray p)||p.Count!=3||p[0].Type==JTokenType.Null||p[1].Type==JTokenType.Null||p[2].Type==JTokenType.Null||!Number(p[0],out float x)||!Number(p[1],out float y)||!Number(p[2],out float z))return false;
   var r=o["rot"] as JObject;var pose=o["pose"] as JObject;
   if(pose?["grounded"]!=null&&pose["grounded"].Type!=JTokenType.Boolean)return false;
   if(!Number(r?["bodyYaw"],out float yaw)||!Number(r?["headYaw"],out float head)||!Number(r?["pitch"],out float pitch)||!Number(pose?["limbSwing"],out float swing)||!Number(pose?["limbAmount"],out float amount))return false;
   if(!MobEquipment.TryParse(pose?["held"],out var equipment))return false;
   DragonPose dragon=null;if(pose?["flap"]!=null&&!DragonPose.TryParse(pose,out dragon))return false;Dragon=dragon;
   Equipment=equipment;Grounded=pose?["grounded"]?.Value<bool>()??false;
   Previous=_initialized?Position:new V3(x,y,z);OldYaw=_initialized?BodyYaw:yaw;Position=new V3(x,y,z);BodyYaw=yaw;HeadYaw=head;Pitch=pitch;Swing=swing;SwingAmount=Math.Max(0,Math.Min(1,amount));_initialized=true;return true;
  }
  public V3 Interpolate(float t){t=Math.Max(0,Math.Min(1,t));return new V3(Previous.X+(Position.X-Previous.X)*t,Previous.Y+(Position.Y-Previous.Y)*t,Previous.Z+(Position.Z-Previous.Z)*t);}
 }
}
