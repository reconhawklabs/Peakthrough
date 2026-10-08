using HarmonyLib;
using Photon.Pun;
namespace PeakCreativeMode.Plugin {
 /// PEAK may retain a player-name entry briefly after its character is destroyed.
 [HarmonyPatch(typeof(UIPlayerNames),nameof(UIPlayerNames.UpdateName))]
 internal static class HostPlayerNameGuard {
  private static bool Prefix(UIPlayerNames __instance,int index){
   if(!PhotonNetwork.IsMasterClient)return true;
   if(index<0||__instance.playerNameText==null||index>=__instance.playerNameText.Length)return false;
   var entry=__instance.playerNameText[index];if(entry==null)return false;
   var interactible=entry.characterInteractable;
   if(interactible==null||interactible.character==null||interactible.character.photonView==null||interactible.character.photonView.Owner==null){entry.gameObject.SetActive(false);return false;}
   return true;
  }
 }
}
