using System;
using System.Diagnostics;
using System.IO;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Plugin
{
 /// Starts only the host's private native runtime. Welcome, not process creation, proves readiness.
 internal sealed class ServerLauncher:MonoBehaviour
 {
  private readonly string _owner="peak-"+Guid.NewGuid().ToString("N");
  private bool _attempted,_ready,_reported;private int _restarts;private float _deadline;
  private void Update()
  {
   if(!Plugin.Keys.AutoStartServer.Value||(!PhotonNetwork.OfflineMode&&!PhotonNetwork.IsMasterClient))return;
   var client=BridgeBehaviour.Instance?.Client;
   if(client==null)return;
   if(client.Status==BridgeStatus.Connected){_ready=true;_reported=false;return;}
   if(_ready&&_attempted&&_restarts==0){_restarts++;_attempted=false;_ready=false;}
   if(!_attempted&&(SceneManager.GetActiveScene().name=="Airport"||_restarts>0))
   {
    _attempted=true;_deadline=Time.unscaledTime+60f;
    string directory=Plugin.Keys.ServerPath.Value;
    string script=Path.Combine(directory,WindowsRuntime(directory)?"server_runtime.py":"server_start.sh");
    if(!File.Exists(script)){Plugin.Log.LogWarning("[server] Private runtime missing; run Peakthrough Setup and select host. Bridge remains available.");_reported=true;return;}
    Launch(directory,false,"--owner "+_owner+" --port "+Plugin.Keys.BridgePort.Value+" --heap-mb "+Plugin.Keys.ServerHeapMb.Value);
   }
   if(_attempted&&!_reported&&Time.unscaledTime>_deadline){_reported=true;Plugin.Log.LogWarning("[server] No bridge welcome within 60s; see server/launcher.log. Native fallback: tools/run_peak.sh.");}
  }
  private static bool WindowsRuntime(string directory)
  {
   var file=Path.Combine(directory,"runtime.json");
   try{return File.Exists(file)&&((string)JObject.Parse(File.ReadAllText(file))["platform"]??"").StartsWith("win",StringComparison.OrdinalIgnoreCase);}
   catch(Exception){return false;}
  }
  private static void Launch(string directory,bool stop,string arguments)
  {
   try{
    var command=ServerCommand.Create(directory,WindowsRuntime(directory),stop,arguments);
    var info=new ProcessStartInfo(command.Executable,command.Arguments){UseShellExecute=false,CreateNoWindow=true};
    Process.Start(info)?.Dispose();Plugin.Log.LogInfo("[server] Requested native runtime: "+directory);
   }catch(Exception e){Plugin.Log.LogWarning("[server] Native start unavailable: "+e.Message+". Use tools/run_peak.sh.");}
  }
  private void OnDestroy()
  {
   if(_attempted&&Plugin.Keys.StopOwnedServer.Value)Launch(Plugin.Keys.ServerPath.Value,true,"--owner "+_owner);
  }
 }
}
