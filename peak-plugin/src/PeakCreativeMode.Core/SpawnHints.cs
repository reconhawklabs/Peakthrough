using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public static class SpawnHints {
  public static string Encode(IEnumerable<(V3 pos,bool dark,bool open)> cells){var a=new JArray();foreach(var c in cells){if(!Finite(c.pos.X)||!Finite(c.pos.Y)||!Finite(c.pos.Z))continue;a.Add(new JArray(Math.Round(c.pos.X,2),Math.Round(c.pos.Y,2),Math.Round(c.pos.Z,2),c.dark?1:0,c.open?1:0));if(a.Count>=64)break;}return new JObject{["t"]="spawn_hints",["cells"]=a}.ToString(Formatting.None);}
  private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 }
}
