using System;
namespace PeakCreativeMode.Core {
 public readonly struct HotbarLayout {
  public readonly int Scale,Width,Height,SlotPitch;public readonly float Left,Top;
  private HotbarLayout(int width,int height,int scale){Scale=scale;Width=206*scale;Height=22*scale;SlotPitch=20*scale;Left=(width-Width)/2f;Top=height-Height-12*scale;}
  public static HotbarLayout For(int width,int height,int configuredScale){int scale=Math.Max(1,Math.Min(6,configuredScale));scale=Math.Min(scale,Math.Max(1,(int)Math.Round(height/1080.0*scale)));scale=Math.Min(scale,Math.Max(1,width/206));return new HotbarLayout(width,height,scale);}
 }
}
