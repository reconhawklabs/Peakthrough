using System;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public static class TeleportTarget {
  public static bool TryParse(JToken token,V3 currentFeet,out V3 target){target=default;if(!(token is JArray a)||a.Count!=3)return false;double[] n=new double[3];for(int i=0;i<3;i++){if(a[i].Type!=JTokenType.Float&&a[i].Type!=JTokenType.Integer)return false;n[i]=(double)a[i];if(!double.IsFinite(n[i]))return false;}if(Math.Abs(n[0])>30000000||Math.Abs(n[2])>30000000||Math.Abs(n[1])>2032)return false;double dx=n[0]-currentFeet.X,dy=n[1]-currentFeet.Y,dz=-n[2]-currentFeet.Z;if(!(dx*dx+dy*dy+dz*dz<=64*64))return false;target=new V3((float)n[0],(float)n[1],(float)-n[2]);return true;}
 }
}
