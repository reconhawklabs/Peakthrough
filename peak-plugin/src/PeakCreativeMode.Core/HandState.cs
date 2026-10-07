using System.Collections.Generic;
namespace PeakCreativeMode.Core
{
 public enum HandCommandKind{Store,UpdateSlot,Materialize,EmptyNative,DropNative}
 public readonly struct HandCommand{public readonly HandCommandKind Kind;public readonly int Ref,Slot,NativeSlot;public readonly PeakToken Token;public HandCommand(HandCommandKind kind,int @ref,int slot,int nativeSlot,PeakToken token){Kind=kind;Ref=@ref;Slot=slot;NativeSlot=nativeSlot;Token=token;}}
 /// Keeps PEAK native hand slot 0 in sync with the selected Minecraft slot. Never empties a native slot before an ack.
 public sealed class HandState
 {
  public const float MaterializeTimeout=3f;
  private sealed class Pending{public HandCommandKind Kind;public int NativeSlot,Slot;public bool EmptyAfter;public PeakToken Token;}
  private readonly Dictionary<int,Pending> _pending=new Dictionary<int,Pending>();
  private int _next=1,_materializing=-1;private float _materializeAt;
  public bool Connected;public int MaterializedSlot{get;private set;}=-1;
  private bool Busy(int nativeSlot){foreach(var p in _pending.Values)if(p.NativeSlot==nativeSlot||(nativeSlot==0&&p.NativeSlot<0))return true;return false;}
  private HandCommand Send(HandCommandKind kind,int slot,int nativeSlot,PeakToken token,bool emptyAfter){int r=_next++;_pending[r]=new Pending{Kind=kind,NativeSlot=nativeSlot,Slot=slot,EmptyAfter=emptyAfter,Token=token};return new HandCommand(kind,r,slot,nativeSlot,token);}
  public HandCommand? StoreNative(int nativeSlot,PeakToken token)=>!Connected||token==null||Busy(nativeSlot)?(HandCommand?)null:Send(HandCommandKind.Store,-1,nativeSlot,token,true);
  public List<HandCommand> Update(InventoryState inv,PeakToken[] native,float now)
  {
   var o=new List<HandCommand>();if(!Connected)return o;
   for(int k=1;k<3;k++)if(native[k]!=null&&!Busy(k))o.Add(Send(HandCommandKind.Store,-1,k,native[k],true));
   if(Busy(0))return o;int sel=inv.Selected;var selTok=inv.Stowed?null:inv.Slots[sel].Peak;
   if(MaterializedSlot>=0)
   {
    if(native[0]!=null&&!native[0].SameIdentity(inv.Slots[MaterializedSlot].Peak)){
     if(native[0].SameIdentity(inv.Carried.Peak))return o;
     for(int i=0;i<36;i++)if(native[0].SameIdentity(inv.Slots[i].Peak)){MaterializedSlot=i;break;}
    }
    if(native[0]==null){o.Add(Send(HandCommandKind.UpdateSlot,MaterializedSlot,-1,null,false));MaterializedSlot=-1;}
    else if(MaterializedSlot!=sel||inv.Stowed)o.Add(Send(HandCommandKind.UpdateSlot,MaterializedSlot,0,native[0],true));
    return o;
   }
   if(_materializing>=0)
   {
    if(native[0]!=null){MaterializedSlot=_materializing;_materializing=-1;_lastRefreshed=native[0];}
    else if(_materializing!=sel||now-_materializeAt>MaterializeTimeout)_materializing=-1;
    if(_materializing>=0||MaterializedSlot>=0)return o;
   }
   if(native[0]!=null){o.Add(Send(HandCommandKind.Store,inv.Slots[sel].Item==null?sel:-1,0,native[0],false));return o;}
   if(selTok!=null){_materializing=sel;_materializeAt=now;o.Add(new HandCommand(HandCommandKind.Materialize,0,sel,0,selTok));}
   return o;
  }
  public List<HandCommand> Ack(int @ref,int slot)
  {
   var o=new List<HandCommand>();if(!Connected||!_pending.TryGetValue(@ref,out var p))return o;_pending.Remove(@ref);
   if(p.NativeSlot<0)return o;
   if(slot<0){o.Add(new HandCommand(HandCommandKind.DropNative,0,-1,p.NativeSlot,p.Token));if(p.NativeSlot==0)MaterializedSlot=-1;return o;}
   if(p.Kind==HandCommandKind.Store&&p.NativeSlot==0&&p.Slot>=0&&slot==p.Slot){MaterializedSlot=slot;_lastRefreshed=p.Token;return o;}
   if(p.EmptyAfter||p.Kind==HandCommandKind.Store){o.Add(new HandCommand(HandCommandKind.EmptyNative,0,-1,p.NativeSlot,null));if(p.NativeSlot==0)MaterializedSlot=-1;}
   return o;
  }
  /// Call every ~2 s with the current hand item; stores changed uses/fuel without emptying the hand.
  public HandCommand? Refresh(PeakToken handNow)
  {
   if(!Connected||MaterializedSlot<0||handNow==null||Busy(0))return null;
   if(_lastRefreshed!=null&&_lastRefreshed.SameAs(handNow))return null;_lastRefreshed=handNow;
   return Send(HandCommandKind.UpdateSlot,MaterializedSlot,-2,handNow,false);
  }
  private PeakToken _lastRefreshed;
 }
}
