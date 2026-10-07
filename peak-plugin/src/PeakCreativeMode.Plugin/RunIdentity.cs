using HarmonyLib;
using PeakCreativeMode.Core;
using UnityEngine.SceneManagement;
using Zorro.Core;
namespace PeakCreativeMode.Plugin
{
 internal static class RunIdentity
 {
  private static string _selected;
  public static void Capture(string scene)
  {
   var service=GameHandler.GetService<NextLevelService>();
   _selected=RunKey.ForMap(service.NextLevelIndexOrFallback+NextLevelService.debugLevelIndexOffset,scene);
   Plugin.Log.LogInfo("[run] selected "+_selected);
  }
  public static string Current()
  {
   if(!string.IsNullOrWhiteSpace(Plugin.Keys.MapSeedOverride.Value))return Plugin.Keys.MapSeedOverride.Value;
   if(SceneManager.GetActiveScene().name=="Airport")_selected=null;
   if(_selected!=null)return _selected;
   try{
    int index=GameHandler.GetService<NextLevelService>().NextLevelIndexOrFallback+NextLevelService.debugLevelIndexOffset;
    string scene=SingletonAsset<MapBaker>.Instance.GetLevel(index);
    return RunKey.ForMap(index,string.IsNullOrEmpty(scene)?"WilIsland":scene);
   }catch{return "startup";}
  }
  public static string[] Biomes()
  {
   var map=MapHandler.Instance;
   return map==null?System.Array.Empty<string>():map.biomes.ConvertAll(b=>b.ToString()).ToArray();
  }
 }
 [HarmonyPatch(typeof(AirportCheckInKiosk),nameof(AirportCheckInKiosk.BeginIslandLoadRPC))]
 internal static class CaptureRun
 {
  private static void Prefix(string sceneName)=>RunIdentity.Capture(sceneName);
 }
}
