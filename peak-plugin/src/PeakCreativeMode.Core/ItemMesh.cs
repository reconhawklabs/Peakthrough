using System;
using System.Collections.Generic;
namespace PeakCreativeMode.Core
{
 public struct ItemQuad{public V3 A,B,C,D;public float U0,V0,U1,V1;}
 /// Minecraft-style extruded sprite: front+back per opaque pixel, side faces only on transparent edges.
 public static class ItemMesh
 {
  public static List<ItemQuad> Extrude(int width,int height,Func<int,int,bool> opaque)
  {
   if(width<1||width>512)throw new ArgumentOutOfRangeException(nameof(width));if(height<1||height>512)throw new ArgumentOutOfRangeException(nameof(height));
   var q=new List<ItemQuad>();float px=1f/width,py=1f/height,h=1/32f;
   bool O(int x,int y)=>x>=0&&y>=0&&x<width&&y<height&&opaque(x,y);
   for(int y=0;y<height;y++)for(int x=0;x<width;x++){if(!O(x,y))continue;
    float x0=-.5f+x*px,x1=x0+px,y1=.5f-y*py,y0=y1-py,u0=x*px,u1=u0+px,v1=1-y*py,v0=v1-py;
    q.Add(Q(new V3(x0,y0,-h),new V3(x1,y0,-h),new V3(x1,y1,-h),new V3(x0,y1,-h),u0,v0,u1,v1));
    q.Add(Q(new V3(x1,y0,h),new V3(x0,y0,h),new V3(x0,y1,h),new V3(x1,y1,h),u1,v0,u0,v1));
    if(!O(x-1,y))q.Add(Q(new V3(x0,y0,h),new V3(x0,y0,-h),new V3(x0,y1,-h),new V3(x0,y1,h),u0,v0,u0+px/2,v1));
    if(!O(x+1,y))q.Add(Q(new V3(x1,y0,-h),new V3(x1,y0,h),new V3(x1,y1,h),new V3(x1,y1,-h),u1-px/2,v0,u1,v1));
    if(!O(x,y-1))q.Add(Q(new V3(x0,y1,-h),new V3(x1,y1,-h),new V3(x1,y1,h),new V3(x0,y1,h),u0,v1-py/2,u1,v1));
    if(!O(x,y+1))q.Add(Q(new V3(x0,y0,h),new V3(x1,y0,h),new V3(x1,y0,-h),new V3(x0,y0,-h),u0,v0,u1,v0+py/2));}
   return q;
  }
  private static ItemQuad Q(V3 a,V3 b,V3 c,V3 d,float u0,float v0,float u1,float v1)=>new ItemQuad{A=a,B=b,C=c,D=d,U0=u0,V0=v0,U1=u1,V1=v1};
 }
}
