using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public static class ArenaSurvey {
  public static IReadOnlyList<(int,int)> Offsets(int radius){if(radius<20||radius>160)throw new ArgumentOutOfRangeException(nameof(radius));var rows=new List<(int,int)>();for(int x=-radius;x<=radius;x++)for(int z=-radius;z<=radius;z++)if(x*x+z*z<=radius*radius)rows.Add((x,z));rows.Sort((a,b)=>(a.Item1*a.Item1+a.Item2*a.Item2).CompareTo(b.Item1*b.Item1+b.Item2*b.Item2));return rows;}
  public static bool TryOrigin(JToken token,JToken orbit,out V3 origin,out int radius){origin=default;radius=0;if(!(token is JArray a)||a.Count!=3||orbit?.Type!=JTokenType.Integer)return false;long r=(long)orbit;if(r<20||r>160)return false;var p=new float[3];for(int i=0;i<3;i++){if(a[i].Type!=JTokenType.Integer&&a[i].Type!=JTokenType.Float)return false;double n=(double)a[i];if(double.IsNaN(n)||double.IsInfinity(n)||Math.Abs(n)>30000000||(i==1&&(n< -2032||n>2031)))return false;p[i]=(float)n;}origin=Coords.McToUnity(p[0],p[1],p[2]);radius=(int)r;return true;}
 }
}
