using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 internal sealed class HotbarHud:MonoBehaviour {
  private void OnGUI(){if(!UnifiedHotbar.Enabled||ItemUI.Instance==null)return;var inv=ItemUI.Instance.State;var h=HotbarLayout.For(Screen.width,Screen.height,Plugin.Keys.HudScale.Value);float s=h.Scale,left=h.Left,top=h.Top;var bg=MinecraftHud.Texture("gui/sprites/hud/hotbar");if(bg!=null)GUI.DrawTexture(new Rect(left,top,182*s,22*s),bg);else MinecraftHud.Fill(new Rect(left,top,182*s,22*s),new Color(.2f,.2f,.2f,.9f));
   if(!UnifiedHotbar.Instance.Backpack&&!inv.Stowed){var select=MinecraftHud.Texture("gui/sprites/hud/hotbar_selection");if(select!=null)GUI.DrawTexture(new Rect(left+inv.Selected*20*s-s,top-s,24*s,23*s),select);}
   for(int i=0;i<9;i++){var rect=new Rect(left+s+i*20*s,top+s,20*s,20*s);ItemUI.Instance.DrawSlot(rect,inv.Slots[i],false,false);MinecraftHud.Text(rect.x+2*s,top-10*s,(i+1).ToString(),Mathf.Max(1,s*.5f),inv.Selected==i&&!inv.Stowed?Color.white:new Color(.8f,.8f,.8f));}
   var r=new Rect(left+184*s,top,22*s,22*s);MinecraftHud.Slot(r,UnifiedHotbar.Instance.Backpack);var backpack=Player.localPlayer?.backpackSlot;if(backpack!=null&&!backpack.IsEmpty())GUI.DrawTexture(new Rect(r.x+3*s,r.y+3*s,16*s,16*s),backpack.prefab.UIData.GetIcon(),ScaleMode.ScaleToFit);MinecraftHud.Text(r.x+3*s,top-10*s,"0",Mathf.Max(1,s*.5f));
   float font=Mathf.Max(1,s*.5f);string hint="I Inventory";float width=MinecraftHud.Measure(hint,font)+12;var hintRect=new Rect(left+h.Width-width,top-21*s,width,10*font+4);MinecraftHud.Fill(hintRect,new Color(0,0,0,.6f));MinecraftHud.Text(hintRect.x+6,hintRect.y+2,hint,font);
   if(inv.Stowed)MinecraftHud.Text(left,top-21*s,"Empty hand",font);else{var held=inv.Slots[inv.Selected];string name=held.Peak?.Name??held.Item?.Replace("minecraft:","").Replace('_',' ');if(name!=null)MinecraftHud.Text(left,top-21*s,name,font);}
  }
  private void OnDestroy()=>MinecraftHud.Clear();
 }
}
