using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PeakCreativeMode.Plugin
{
    /// Testing aid: pressing QuickStart in the Airport starts a run as if the host used the check-in kiosk.
    internal sealed class QuickStart : MonoBehaviour
    {
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb[Plugin.Keys.QuickStart.Value].wasPressedThisFrame) return;
            if (SceneManager.GetActiveScene().name != "Airport")
            {
                Plugin.Log.LogInfo("[quickstart] only works in the Airport");
                return;
            }
            if (!Photon.Pun.PhotonNetwork.IsMasterClient||LoadingScreenHandler.loading) return;
            var kiosk = Object.FindAnyObjectByType<AirportCheckInKiosk>();
            if (kiosk == null)
            {
                Plugin.Log.LogWarning("[quickstart] no AirportCheckInKiosk found");
                return;
            }
            Plugin.Log.LogInfo("[quickstart] starting run (ascent 0)");
            kiosk.LoadIslandMaster(0, RunSettings.GetSerializedRunSettings());
        }
    }
}
