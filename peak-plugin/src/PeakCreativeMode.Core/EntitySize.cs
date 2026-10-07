using System;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public static class EntitySize {
  public static bool TryParse(JToken token,out float width,out float height){width=height=0;if(!(token is JArray a)||a.Count!=2)return false;foreach(var n in a)if(n.Type!=JTokenType.Integer&&n.Type!=JTokenType.Float)return false;double w=(double)a[0],h=(double)a[1];if(double.IsNaN(w)||double.IsInfinity(w)||double.IsNaN(h)||double.IsInfinity(h)||w<.05||w>32||h<.05||h>32)return false;width=(float)w;height=(float)h;return true;}
 }
}
