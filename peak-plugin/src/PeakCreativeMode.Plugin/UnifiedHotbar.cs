using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using Zorro.Core;
namespace PeakCreativeMode.Plugin {
 internal enum SelectedKind{Peak,McBlock,McItem,Empty,Backpack}
 internal sealed class UnifiedHotbar:MonoBehaviour {
  public static UnifiedHotbar Instance;public HandState Hand=new HandState();
  public static bool Enabled=>Plugin.Keys?.UnifiedHotbar.Value==true&&BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected&&WorldDiagnostics.Playing(Character.localCharacter);
  public bool Backpack;public static SelectedKind Kind{get{if(Instance?.Backpack==true)return SelectedKind.Backpack;var inv=ItemUI.Instance?.State;if(inv==null||inv.Stowed)return SelectedKind.Empty;var s=inv.Slots[inv.Selected];return s.Peak!=null?SelectedKind.Peak:s.Item==null?SelectedKind.Empty:InputMode.IsBlock(s.Item)?SelectedKind.McBlock:SelectedKind.McItem;}}
  public static bool McInput=>Enabled?(Kind!=SelectedKind.Peak&&Kind!=SelectedKind.Backpack):Plugin.Keys?.UnifiedHotbar.Value!=true&&InputMode.Instance?.McMode==true;
  private float _refresh,_toastUntil;private string _toast;private readonly Dictionary<int,HandCommand> _requests=new Dictionary<int,HandCommand>();private int _deathRef=1000000000;private Vector3 _deathPos;private readonly List<PeakToken> _nativeDeathDrops=new List<PeakToken>();private bool _wasEnabled;private string _seed;
  private void Awake(){Instance=this;}
  private void Update(){var ch=Character.localCharacter;if(!Enabled){Hand.Connected=false;Hud(false);_wasEnabled=false;return;}if(_seed!=BridgeBehaviour.Instance.MapSeed){_seed=BridgeBehaviour.Instance.MapSeed;_requests.Clear();Hand=new HandState{Connected=true};Backpack=false;}Hand.Connected=true;
   if(!_wasEnabled){_wasEnabled=true;foreach(var request in new List<HandCommand>(_requests.Values)){
    int saved=-1;if(request.Kind==HandCommandKind.Store)for(int i=0;i<36;i++)if(request.Token.SameIdentity(ItemUI.Instance.State.Slots[i].Peak)){saved=i;break;}
    if(saved>=0)Ack(new JObject{["ref"]=request.Ref,["slot"]=saved});else Execute(request);
   }}
   Hud(true);var kb=Keyboard.current;if(kb!=null&&ItemUI.Instance?.Open!=true&&InputMode.Instance?.PickerOpen!=true){
    for(int i=0;i<9;i++)if(kb[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)Select(i);
    float scroll=Mouse.current?.scroll.ReadValue().y??0;if(scroll!=0)Select((ItemUI.Instance.State.Selected+(scroll>0?8:1))%9);
    if(kb[Plugin.Keys.Backpack.Value].wasPressedThisFrame){Backpack=true;ch.refs.items.EquipSlot(Optionable<byte>.Some(3));}
    if(kb.qKey.wasPressedThisFrame&&McInput&&!ItemUI.Instance.State.Stowed)ItemUI.Send("drop",new JObject());
   }
   try{var temporary=Hand.StoreNative(250,PeakItemBridge.Read(250));if(temporary.HasValue)Execute(temporary.Value);foreach(var c in Hand.Update(ItemUI.Instance.State,new[]{PeakItemBridge.Read(0),PeakItemBridge.Read(1),PeakItemBridge.Read(2)},Time.unscaledTime))Execute(c);if(Time.unscaledTime>=_refresh){_refresh=Time.unscaledTime+2;var c=Hand.Refresh(PeakItemBridge.Read(0));if(c.HasValue)Execute(c.Value);}
    if(!Backpack&&Hand.MaterializedSlot>=0&&ch.data.currentItem==null&&!Player.localPlayer.itemSlots[0].IsEmpty())ch.refs.items.EquipSlot(Optionable<byte>.Some(0));
   }catch(Exception e){Plugin.Log.LogWarning("[hand] retaining native inventory: "+e.GetBaseException().Message);}
  }
  private void Select(int slot){bool stowed=!Backpack&&slot==ItemUI.Instance.State.Selected&&!ItemUI.Instance.State.Stowed;if(Backpack){Backpack=false;Character.localCharacter.refs.items.EquipSlot(Optionable<byte>.None);}ItemUI.Send("select_slot",new JObject{["slot"]=slot,["stowed"]=stowed});}
  private static void Hud(bool unified){var gui=GUIManager.instance;if(gui?.items==null)return;foreach(var ui in gui.items)if(ui!=null&&ui.gameObject.activeSelf==unified)ui.gameObject.SetActive(!unified);}
  private void Execute(HandCommand c){switch(c.Kind){
   case HandCommandKind.Store:case HandCommandKind.UpdateSlot:_requests[c.Ref]=c;ItemUI.Send(c.Kind==HandCommandKind.Store?"peak_store":"peak_slot",new JObject{["ref"]=c.Ref,[c.Kind==HandCommandKind.Store?"preferred":"slot"]=c.Slot,["peak"]=c.Token?.ToJson()});break;
   case HandCommandKind.Materialize:PeakItemBridge.Instance.Materialize(c.Token);break;
   case HandCommandKind.EmptyNative:PeakItemBridge.Instance.Empty(c.NativeSlot);break;
   case HandCommandKind.DropNative:PeakItemBridge.Instance.Drop(c.NativeSlot);_toast="Inventory full";_toastUntil=Time.unscaledTime+2;break;
  }}
  public void Ack(JObject msg){if(msg["ref"]?.Type!=JTokenType.Integer||msg["slot"]?.Type!=JTokenType.Integer)return;int r=(int)msg["ref"],slot=(int)msg["slot"];if(r==_deathRef&&msg["tokens"] is JArray tokens){foreach(var t in tokens){var token=PeakToken.FromJson(t);if(token!=null&&!_nativeDeathDrops.Exists(t=>t.SameIdentity(token))){if(PhotonNetwork.IsMasterClient)PeakItemBridge.Spawn(Player.localPlayer,token,_deathPos);else BridgeBehaviour.Instance.Client.Send(new JObject{["t"]="hand",["op"]="death",["peak"]=token.ToJson()}.ToString(Newtonsoft.Json.Formatting.None));}}return;}if(!Enabled)return;_requests.Remove(r);foreach(var c in Hand.Ack(r,slot))Execute(c);}
  internal void Died(Character ch){if(ch!=Character.localCharacter||BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||!Plugin.Keys.UnifiedHotbar.Value)return;_nativeDeathDrops.Clear();var inv=ItemUI.Instance.State;if(Hand.MaterializedSlot>=0&&inv.Slots[Hand.MaterializedSlot].Peak!=null)_nativeDeathDrops.Add(inv.Slots[Hand.MaterializedSlot].Peak);foreach(var request in _requests.Values)if(request.NativeSlot>=0&&request.Token!=null)_nativeDeathDrops.Add(request.Token);_deathPos=ch.Center;Hand=new HandState();_requests.Clear();_deathRef++;ItemUI.Send("peak_clear_all",new JObject{["ref"]=_deathRef});if(!Plugin.Keys.KeepMcItemsOnDeath.Value)for(int i=0;i<36;i++)ItemUI.Send("drop",new JObject{["slot"]=i,["all"]=true});}
  private void OnGUI(){if(Time.unscaledTime<_toastUntil)GUI.Box(new Rect(Screen.width/2-100,Screen.height/2+70,200,35),_toast);}
  private void OnDestroy(){Hud(false);}
 }
 // GlobalEvents is reset by native scene transitions, so patch its verified dispatch instead of a stale subscription.
 [HarmonyLib.HarmonyPatch(typeof(GlobalEvents),"TriggerCharacterDied")]
 internal static class UnifiedDeath {private static void Postfix(Character character)=>UnifiedHotbar.Instance?.Died(character);}
}

namespace PeakCreativeMode.Plugin {
 [HarmonyLib.HarmonyPatch(typeof(CharacterItems),"OnPickupAccepted")]
 internal static class UnifiedBackpackPickup {
  private static void Postfix(Character ___character,byte slotID){if(UnifiedHotbar.Enabled&&___character==Character.localCharacter&&slotID==3)UnifiedHotbar.Instance.Backpack=true;}
 }
}
