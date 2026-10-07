namespace PeakCreativeMode.Core {
 public static class ClimbPolicy {public static bool Blocks(bool unified,ItemSlot slot,bool backpackHolding)=>unified&&(backpackHolding||slot.Peak!=null||slot.Item!=null&&slot.Count>0);}
}
