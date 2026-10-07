using McItemSlot=PeakCreativeMode.Core.ItemSlot;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.InputSystem;
namespace PeakCreativeMode.Plugin
{
 internal sealed class ItemUI:MonoBehaviour
 {
  public static ItemUI Instance {get;private set;}
  public readonly InventoryState State=new InventoryState();
  public bool Open {get;private set;}
  private readonly Dictionary<string,Texture2D> _icons=new Dictionary<string,Texture2D>();
  public JObject LastSnapshot {get;private set;}
  private int _drag=-1;
  private void Awake(){Instance=this;}
  public void Receive(JObject msg){if(!State.Apply(msg))Plugin.Log.LogWarning("[inventory] malformed snapshot dropped");else LastSnapshot=(JObject)msg.DeepClone();}
  public void Clear(){LastSnapshot=null;State.Clear();Open=false;_drag=-1;}
  public static bool Send(string kind,JObject target)=>BridgeBehaviour.Instance?.Client.Send(new JObject{["t"]="action",["kind"]=kind,["target"]=target}.ToString(Newtonsoft.Json.Formatting.None))==true;
  private void Update()
  {
   var mode=InputMode.Instance;var b=BridgeBehaviour.Instance;var ch=Character.localCharacter;
   if((!UnifiedHotbar.Enabled&&mode?.McMode!=true)||b?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(ch)){if(Open){Open=false;_drag=-1;}return;}
   if(mode.PickerOpen)return;
   var kb=Keyboard.current;if(kb==null)return;
   if(kb[Plugin.Keys.Inventory.Value].wasPressedThisFrame){Open=!Open;_drag=-1;}
   if(Open&&kb.escapeKey.wasPressedThisFrame){Open=false;_drag=-1;}
   if(Open||mode.PickerOpen||UnifiedHotbar.Enabled)return;
   for(int i=0;i<9;i++)if(kb[(Key)((int)Key.Digit1+i)].wasPressedThisFrame)Send("select_slot",new JObject{["slot"]=i});
   float scroll=Mouse.current?.scroll.ReadValue().y??0;if(scroll!=0)Send("select_slot",new JObject{["slot"]=(State.Selected+(scroll>0?8:1))%9});
   if(kb.qKey.wasPressedThisFrame)Send("drop",new JObject());
  }
  public Texture2D SlotIcon(McItemSlot slot)=>slot.Peak!=null&&ItemDatabase.TryGetItem(slot.Peak.ItemId,out var prefab)?prefab.UIData.GetIcon():Icon(slot.Item);
  public Texture2D Icon(string id)
  {
   if(string.IsNullOrEmpty(id))return null;if(_icons.TryGetValue(id,out var icon))return icon;
   string name=id.Replace("minecraft:","").Split('[')[0];if(name.Contains("/")||name.Contains(".."))return null;
   string root=Path.Combine(Plugin.Keys.AssetCache.Value,"assets","minecraft","textures");
   string p=Path.Combine(root,"item",name+".png");if(!File.Exists(p))p=Path.Combine(root,"block",name+".png");
   if(!File.Exists(p)){_icons[id]=null;return null;}
   icon=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};icon.LoadImage(File.ReadAllBytes(p));_icons[id]=icon;return icon;
  }
  public void DrawSlot(Rect rect,McItemSlot slot,bool selected=false,bool frame=true)
  {
   if(frame)MinecraftHud.Slot(rect,selected);float pixel=Mathf.Max(1,rect.width/20);
   var icon=SlotIcon(slot);if(icon!=null)GUI.DrawTexture(new Rect(rect.x+2*pixel,rect.y+2*pixel,rect.width-4*pixel,rect.height-4*pixel),icon,ScaleMode.ScaleToFit);
   else if(slot.Item!=null)GUI.Label(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4),(slot.Peak?.Name??slot.Item.Replace("minecraft:","").Replace('_',' ')));
   if(slot.MaxDamage>0&&slot.Damage>0){var bar=new Rect(rect.x+3*pixel,rect.yMax-4*pixel,13*pixel,2*pixel);MinecraftHud.Fill(bar,Color.black);MinecraftHud.Fill(new Rect(bar.x,bar.y,Mathf.Round(13*slot.Durability)*pixel,pixel),Color.HSVToRGB(slot.Durability/3f,1,1));}
   if(slot.Count>1){string count=slot.Count.ToString();float scale=Mathf.Max(2,Mathf.Floor(rect.width/20));MinecraftHud.Text(rect.xMax-MinecraftHud.Measure(count,scale)-pixel,rect.yMax-8*scale-pixel,count,scale);}
  }
  private void OnGUI()
  {
   if((!UnifiedHotbar.Enabled&&InputMode.Instance?.McMode!=true)||Character.localCharacter==null||Character.localCharacter.inAirport||BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected)return;
   float left=(Screen.width-9*52)/2f;
   if(!UnifiedHotbar.Enabled)for(int i=0;i<9;i++)DrawSlot(new Rect(left+i*52,Screen.height-140,50,50),State.Slots[i],State.Selected==i);
   var held=State.Slots[State.Selected];var icon=Icon(held.Item);if(!UnifiedHotbar.Enabled&&icon!=null)GUI.DrawTexture(new Rect(Screen.width-140,Screen.height-250,96,96),icon,ScaleMode.ScaleToFit);
   if(!Open)return;
   float x=(Screen.width-500)/2f,y=(Screen.height-300)/2f;GUI.Box(new Rect(x,y,500,310),"Minecraft inventory — I / Esc");
   var ev=Event.current;
   for(int i=0;i<36;i++)
   {
    int row=i<9?3:(i-9)/9;int col=i<9?i:(i-9)%9;var rect=new Rect(x+16+col*52,y+35+row*60,50,50);DrawSlot(rect,State.Slots[i],i==State.Selected);
    if(rect.Contains(ev.mousePosition)&&ev.type==EventType.MouseDown&&ev.button<=1){Send("inv_click",new JObject{["slot"]=i,["button"]=ev.button});_drag=i;ev.Use();}
    if(rect.Contains(ev.mousePosition)&&ev.type==EventType.MouseUp&&_drag>=0){if(i!=_drag)Send("inv_click",new JObject{["slot"]=i,["button"]=0});_drag=-1;ev.Use();}
   }
   if(ev.type==EventType.MouseUp)_drag=-1;
   var cursor=SlotIcon(State.Carried);if(cursor!=null)GUI.DrawTexture(new Rect(ev.mousePosition.x+6,ev.mousePosition.y+6,40,40),cursor);
  }
  private void OnDestroy(){foreach(var icon in _icons.Values)if(icon!=null)Destroy(icon);}
 }
}
