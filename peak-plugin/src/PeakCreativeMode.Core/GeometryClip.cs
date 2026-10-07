using System;
using System.Collections.Generic;
namespace PeakCreativeMode.Core
{
 public readonly struct ClipBox
 {
  public readonly V3 Min,Max;
  public ClipBox(V3 min,V3 max){if(!Finite(min.X)||!Finite(min.Y)||!Finite(min.Z)||!Finite(max.X)||!Finite(max.Y)||!Finite(max.Z)||max.X<=min.X||max.Y<=min.Y||max.Z<=min.Z)throw new ArgumentException("Clip bounds");Min=min;Max=max;}
  private static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
  public bool StrictlyContains(V3 p)=>p.X>Min.X&&p.X<Max.X&&p.Y>Min.Y&&p.Y<Max.Y&&p.Z>Min.Z&&p.Z<Max.Z;
  internal float Distance(V3 p,int plane){switch(plane){case 0:return p.X-Min.X;case 1:return p.Y-Min.Y;case 2:return p.Z-Min.Z;case 3:return Max.X-p.X;case 4:return Max.Y-p.Y;default:return Max.Z-p.Z;}}
 }
 public sealed class ClipVertex
 {
  public readonly V3 Position;public readonly float[] Attributes;public readonly int SourceIndex;
  public ClipVertex(V3 position,float[] attributes=null,int sourceIndex=-1){Position=position;Attributes=attributes??Array.Empty<float>();SourceIndex=sourceIndex;}
  internal static ClipVertex Between(ClipVertex a,ClipVertex b,float t)
  {
   if(t<=0)return a;if(t>=1)return b;if(a.Attributes.Length!=b.Attributes.Length)throw new ArgumentException("Vertex layout");var attributes=new float[a.Attributes.Length];for(int i=0;i<attributes.Length;i++)attributes[i]=a.Attributes[i]+(b.Attributes[i]-a.Attributes[i])*t;
   return new ClipVertex(new V3(a.Position.X+(b.Position.X-a.Position.X)*t,a.Position.Y+(b.Position.Y-a.Position.Y)*t,a.Position.Z+(b.Position.Z-a.Position.Z)*t),attributes);
  }
 }
 public static class GeometryClip
 {
  private const float Epsilon=.00001f;
  public static List<ClipVertex> Subtract(ClipVertex a,ClipVertex b,ClipVertex c,ClipBox box)
  {
   for(int plane=0;plane<6;plane++)if(box.Distance(a.Position,plane)<-Epsilon&&box.Distance(b.Position,plane)<-Epsilon&&box.Distance(c.Position,plane)<-Epsilon)return new List<ClipVertex>{a,b,c};
   var remaining=new List<ClipVertex>{a,b,c};var triangles=new List<ClipVertex>();
   for(int plane=0;plane<6&&remaining.Count>0;plane++){Fan(Clip(remaining,box,plane,false),triangles);remaining=Clip(remaining,box,plane,true);}return triangles;
  }
  private static List<ClipVertex> Clip(List<ClipVertex> source,ClipBox box,int plane,bool inside)
  {
   var result=new List<ClipVertex>();if(source.Count==0)return result;var previous=source[source.Count-1];float dPrevious=box.Distance(previous.Position,plane);bool keptPrevious=inside?dPrevious>=-Epsilon:dPrevious< -Epsilon;
   foreach(var current in source){float d=box.Distance(current.Position,plane);bool kept=inside?d>=-Epsilon:d< -Epsilon;if(kept!=keptPrevious)result.Add(ClipVertex.Between(previous,current,dPrevious/(dPrevious-d)));if(kept)result.Add(current);previous=current;dPrevious=d;keptPrevious=kept;}return result;
  }
  private static void Fan(List<ClipVertex> polygon,List<ClipVertex> output)
  {
   for(int i=1;i+1<polygon.Count;i++){var a=polygon[0].Position;var b=polygon[i].Position;var c=polygon[i+1].Position;float ux=b.X-a.X,uy=b.Y-a.Y,uz=b.Z-a.Z,vx=c.X-a.X,vy=c.Y-a.Y,vz=c.Z-a.Z;float nx=uy*vz-uz*vy,ny=uz*vx-ux*vz,nz=ux*vy-uy*vx;if(nx*nx+ny*ny+nz*nz<=1e-12f)continue;output.Add(polygon[0]);output.Add(polygon[i]);output.Add(polygon[i+1]);}
  }
 }
}
