using System;
using System.Collections;
using System.Text.RegularExpressions;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;
namespace PeakCreativeMode.Plugin {
 internal sealed class PeakItemBridge:MonoBehaviour {
  public static PeakItemBridge Instance;private bool _warned;private static readonly bool[] Dropping=new bool[256];
  private static readonly System.Reflection.MethodInfo Writer=AccessTools.Method(typeof(CustomTypeRPCSerialization),"SerializeItemData"),Reader=AccessTools.Method(typeof(CustomTypeRPCSerialization),"DeserializeItemData");
  private void Awake(){Instance=this;}
  internal static byte[] Serialize(ItemInstanceData data)=>(byte[])Writer.Invoke(null,new object[]{data});
  internal static ItemInstanceData Deserialize(byte[] data)=>(ItemInstanceData)Reader.Invoke(null,new object[]{data});
  public static PeakToken Read(int nativeSlot){var p=Player.localPlayer;if(p==null||nativeSlot<0||(nativeSlot>2&&nativeSlot!=250))return null;var s=p.GetItemSlot((byte)nativeSlot);if(s==null||s.IsEmpty()||Dropping[nativeSlot])return null;string name=Regex.Replace(s.prefab.UIData.itemName??s.GetPrefabName(),"[^A-Za-z0-9 _().,'!-]","");if(name.Length==0)name="PEAK item";if(name.Length>64)name=name.Substring(0,64);return new PeakToken(s.prefab.itemID,name,Serialize(s.data));}
  public bool Materialize(PeakToken token){try{
   var player=Player.localPlayer;if(player==null||token==null||!player.itemSlots[0].IsEmpty())return false;
   if(!PhotonNetwork.IsMasterClient)return BridgeBehaviour.Instance.Client.Send(new JObject{["t"]="hand",["op"]="materialize",["peak"]=token.ToJson()}.ToString(Newtonsoft.Json.Formatting.None));
   return MaterializeFor(player,token,true);
  }catch(Exception e){Warn(e);return false;}}
  internal static bool MaterializeFor(Player player,PeakToken token,bool equip){
   if(!PhotonNetwork.IsMasterClient||player==null||!player.itemSlots[0].IsEmpty()||!ItemDatabase.TryGetItem(token.ItemId,out var prefab)||prefab is Backpack)return false;
   var data=Deserialize(token.Data);if(!player.AddItem(token.ItemId,data,out var slot))return false;
   // With slot 0 checked empty, native AddItem must pick it. Never empty an unexpected native slot.
   if(slot.itemSlotID!=0){Plugin.Log.LogWarning("[hand] Native AddItem chose unexpected slot; retained item");return false;}
   if(equip)player.character.refs.items.EquipSlot(Optionable<byte>.Some(0));return true;
  }
  public void Empty(int slot){var p=Player.localPlayer;if(p==null||slot<0||(slot>2&&slot!=250))return;if(p.character.refs.items.currentSelectedSlot.IsSome&&p.character.refs.items.currentSelectedSlot.Value==slot)p.character.refs.items.EquipSlot(Optionable<byte>.None);p.EmptySlot(Optionable<byte>.Some((byte)slot));}
  public void Drop(int slot){if(slot<0||(slot>2&&slot!=250)||Dropping[slot])return;Dropping[slot]=true;StartCoroutine(DropLater(slot));}
  private IEnumerator DropLater(int slot){var p=Player.localPlayer;if(p==null||p.GetItemSlot((byte)slot).IsEmpty()){Dropping[slot]=false;yield break;}p.character.refs.items.EquipSlot(Optionable<byte>.Some((byte)slot));yield return new WaitForSecondsRealtime(.25f);if(p.character.data.currentItem==null){Dropping[slot]=false;yield break;}var item=p.character.data.currentItem;p.character.photonView.RPC("DropItemRpc",RpcTarget.All,0f,(byte)slot,p.character.Center+Vector3.up*.2f,Vector3.zero,item.transform.rotation,p.GetItemSlot((byte)slot).data,false);p.character.refs.items.EquipSlot(Optionable<byte>.None);Dropping[slot]=false;}
  internal static bool Spawn(Player player,PeakToken token,Vector3 pos){if(!PhotonNetwork.IsMasterClient||player==null||!ItemDatabase.TryGetItem(token.ItemId,out var prefab))return false;var data=Deserialize(token.Data);var go=PhotonNetwork.InstantiateItemRoom(prefab.name,pos,Quaternion.identity);go.GetComponent<PhotonView>().RPC("SetItemInstanceDataRPC",RpcTarget.All,data);return true;}
  internal static bool HandleHand(int actor,JObject message){var target=PhotonNetwork.CurrentRoom?.GetPlayer(actor);var player=target==null?null:PlayerHandler.GetPlayer(target);if(player==null)return false;string op=(string)message["op"];var token=PeakToken.FromJson(message["peak"]);if(op=="death"&&token!=null&&player.character.data.dead)return Spawn(player,token,player.character.Center);if(op=="materialize"&&token!=null)return MaterializeFor(player,token,false);return false;}
  private void Warn(Exception e){if(_warned)return;_warned=true;Plugin.Log.LogWarning("[hand] Native item retained: "+e.GetBaseException().Message);}
 }
}
