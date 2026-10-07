using System;
namespace PeakCreativeMode.Core
{
 public static class RunKey
 {
  public static string ForMap(int index,string scene)
  {
   if(string.IsNullOrWhiteSpace(scene)||scene.Length>80)throw new ArgumentException("Invalid map scene");
   foreach(char c in scene)if(!char.IsLetterOrDigit(c)&&c!='_'&&c!='-')throw new ArgumentException("Invalid map scene");
   return "peak:level:"+index+":scene:"+scene;
  }
 }
}
