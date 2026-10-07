using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core {
 public sealed class BossBar {
  private static readonly Regex NamePattern=new Regex("^[A-Za-z0-9 _().,'!-]{1,64}$"),IdPattern=new Regex("^[A-Za-z0-9_-]{1,64}$");
  public string Id {get;private set;} public string Name {get;private set;} public float Progress {get;private set;} public bool Visible {get;private set;} public bool Killed {get;private set;}
  public bool Apply(JObject o){if(o?["id"]?.Type!=JTokenType.String||!IdPattern.IsMatch((string)o["id"]))return false;string id=(string)o["id"],t=(string)o["t"];if(t=="boss_remove"){if(!Visible||id!=Id||o["killed"]?.Type!=JTokenType.Boolean)return false;Visible=false;Killed=(bool)o["killed"];return true;}if(t!="boss"||o["name"]?.Type!=JTokenType.String||!NamePattern.IsMatch((string)o["name"])||(o["progress"]?.Type!=JTokenType.Integer&&o["progress"]?.Type!=JTokenType.Float))return false;double p=(double)o["progress"];if(double.IsNaN(p)||double.IsInfinity(p)||p<0||p>1)return false;Id=id;Name=(string)o["name"];Progress=(float)p;Visible=true;Killed=false;return true;}
  public void Clear(){Id=Name=null;Progress=0;Visible=Killed=false;}
 }
}
