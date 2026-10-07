using System;
using System.Collections.Generic;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.Rendering;
namespace PeakCreativeMode.Plugin
{
 /// Copies the user's loaded static mesh; neither original mesh nor game assets are modified.
 internal sealed class NativeTerrainMesh
 {
  private sealed class Attribute {public VertexAttribute Kind;public int Dimension,Offset;}
  private Vector3[] _positions;private float[][] _attributes;private readonly List<Attribute> _layout=new List<Attribute>();private int[][] _indices;
  public long PayloadBytes=>12L*_positions.Length+System.Linq.Enumerable.Sum(_attributes,a=>4L*a.Length)+System.Linq.Enumerable.Sum(_indices,a=>4L*a.Length);
  public static NativeTerrainMesh Read(Mesh mesh)
  {
   if(mesh==null||mesh.vertexCount<=0||mesh.vertexCount>1000000)throw new ArgumentException("Unsupported terrain vertex count");
   var result=new NativeTerrainMesh();int count=mesh.vertexCount,components=0;var descriptors=mesh.GetVertexAttributes();
   foreach(var d in descriptors){VertexBytes.Size(d.format.ToString());if(d.attribute==VertexAttribute.Position)continue;if(d.attribute>=VertexAttribute.BlendWeight)throw new ArgumentException("Skinned terrain unsupported");result._layout.Add(new Attribute{Kind=d.attribute,Dimension=d.dimension,Offset=components});components+=d.dimension;}
   result._positions=new Vector3[count];result._attributes=new float[count][];var buffers=new Dictionary<int,byte[]>();
   foreach(var d in descriptors)if(!buffers.ContainsKey(d.stream))
   {
    using(var buffer=mesh.GetVertexBuffer(d.stream)){if(buffer==null)throw new InvalidOperationException("Terrain GPU vertex buffer unavailable");var data=new byte[checked(count*mesh.GetVertexBufferStride(d.stream))];buffer.GetData(data);buffers[d.stream]=data;}
   }
   for(int vertex=0;vertex<count;vertex++)
   {
    var values=new float[components];result._attributes[vertex]=values;
    foreach(var d in descriptors)
    {
     int offset=vertex*mesh.GetVertexBufferStride(d.stream)+mesh.GetVertexAttributeOffset(d.attribute);string format=d.format.ToString();int step=VertexBytes.Size(format);var bytes=buffers[d.stream];
     if(d.attribute==VertexAttribute.Position){if(d.dimension<3)throw new ArgumentException("Position dimension");result._positions[vertex]=new Vector3(VertexBytes.Read(bytes,offset,format),VertexBytes.Read(bytes,offset+step,format),VertexBytes.Read(bytes,offset+2*step,format));}
     else {var attribute=result._layout.Find(a=>a.Kind==d.attribute);for(int i=0;i<d.dimension;i++)values[attribute.Offset+i]=VertexBytes.Read(bytes,offset+i*step,format);}
    }
    var p=result._positions[vertex];if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw new ArgumentException("Invalid terrain position");
   }
   result._indices=new int[mesh.subMeshCount][];
   byte[] raw=null;if(!mesh.isReadable)using(var buffer=mesh.GetIndexBuffer()){if(buffer==null)throw new InvalidOperationException("Terrain GPU index buffer unavailable");raw=new byte[checked(buffer.count*buffer.stride)];buffer.GetData(raw);}
   for(int s=0;s<mesh.subMeshCount;s++)
   {
    if(mesh.GetTopology(s)!=MeshTopology.Triangles)throw new ArgumentException("Non-triangle terrain unsupported");
    if(mesh.isReadable)result._indices[s]=mesh.GetIndices(s);else{var sub=mesh.GetSubMesh(s);var indices=new int[sub.indexCount];for(int i=0;i<indices.Length;i++){int at=sub.indexStart+i;indices[i]=mesh.indexFormat==IndexFormat.UInt16?BitConverter.ToUInt16(raw,at*2):checked((int)BitConverter.ToUInt32(raw,at*4));indices[i]+=sub.baseVertex;}result._indices[s]=indices;}
    foreach(int i in result._indices[s])if(i<0||i>=count)throw new ArgumentException("Terrain index outside vertices");
   }
   Plugin.Log.LogInfo("[terraform mesh] Read "+mesh.name+" vertices="+count+" readable="+mesh.isReadable+" via GPU vertex buffers");return result;
  }
  private static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
  public Mesh Cut(Transform transform,IReadOnlyList<ClipBox> boxes,out int changed)
  {
   var matrix=transform.localToWorldMatrix;var inverse=matrix.inverse;var vertices=new List<ClipVertex>(_positions.Length);for(int i=0;i<_positions.Length;i++){var p=matrix.MultiplyPoint3x4(_positions[i]);vertices.Add(new ClipVertex(new V3(p.x,p.y,p.z),_attributes[i],i));}
   var output=new List<int>[_indices.Length];changed=0;
   for(int s=0;s<_indices.Length;s++)
   {
    output[s]=new List<int>();var indices=_indices[s];
    for(int i=0;i<indices.Length;i+=3)
    {
     var original=new[]{vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]};var triangles=new List<ClipVertex>(original);
     var a=original[0].Position;var b=original[1].Position;var c=original[2].Position;
     float minX=Math.Min(a.X,Math.Min(b.X,c.X)),maxX=Math.Max(a.X,Math.Max(b.X,c.X)),minY=Math.Min(a.Y,Math.Min(b.Y,c.Y)),maxY=Math.Max(a.Y,Math.Max(b.Y,c.Y)),minZ=Math.Min(a.Z,Math.Min(b.Z,c.Z)),maxZ=Math.Max(a.Z,Math.Max(b.Z,c.Z));
     foreach(var box in boxes){if(maxX<box.Min.X||minX>box.Max.X||maxY<box.Min.Y||minY>box.Max.Y||maxZ<box.Min.Z||minZ>box.Max.Z)continue;if(box.StrictlyContains(a)&&box.StrictlyContains(b)&&box.StrictlyContains(c)){triangles.Clear();break;}var next=new List<ClipVertex>();for(int j=0;j<triangles.Count;j+=3)next.AddRange(GeometryClip.Subtract(triangles[j],triangles[j+1],triangles[j+2],box));triangles=next;if(triangles.Count==0)break;}
     if(triangles.Count!=3||!ReferenceEquals(triangles[0],original[0])||!ReferenceEquals(triangles[1],original[1])||!ReferenceEquals(triangles[2],original[2]))changed++;
     foreach(var v in triangles){if(v.SourceIndex>=0)output[s].Add(v.SourceIndex);else{output[s].Add(vertices.Count);vertices.Add(v);}}
    }
   }
   if(changed==0)return null;
   var mesh=new Mesh{name="PeakCreativeMode clipped terrain",indexFormat=IndexFormat.UInt32};var local=new Vector3[vertices.Count];for(int i=0;i<local.Length;i++){var p=vertices[i].Position;local[i]=inverse.MultiplyPoint3x4(new Vector3(p.X,p.Y,p.Z));}mesh.vertices=local;
   foreach(var attr in _layout)
   {
    var values=new List<Vector4>(vertices.Count);foreach(var vertex in vertices){var data=vertex.Attributes;int o=attr.Offset;values.Add(new Vector4(data[o],attr.Dimension>1?data[o+1]:0,attr.Dimension>2?data[o+2]:0,attr.Dimension>3?data[o+3]:0));}
    if(attr.Kind==VertexAttribute.Normal){var normals=new List<Vector3>();foreach(var v in values)normals.Add(new Vector3(v.x,v.y,v.z).normalized);mesh.SetNormals(normals);}
    else if(attr.Kind==VertexAttribute.Tangent)mesh.SetTangents(values);
    else if(attr.Kind==VertexAttribute.Color){var colors=new List<Color>();foreach(var v in values)colors.Add(new Color(v.x,v.y,v.z,v.w));mesh.SetColors(colors);}
    else {int channel=(int)attr.Kind-(int)VertexAttribute.TexCoord0;if(attr.Dimension==2){var uv=new List<Vector2>();foreach(var v in values)uv.Add(new Vector2(v.x,v.y));mesh.SetUVs(channel,uv);}else if(attr.Dimension==3){var uv=new List<Vector3>();foreach(var v in values)uv.Add(new Vector3(v.x,v.y,v.z));mesh.SetUVs(channel,uv);}else mesh.SetUVs(channel,values);}
   }
   mesh.subMeshCount=output.Length;for(int s=0;s<output.Length;s++)mesh.SetIndices(output[s].ToArray(),MeshTopology.Triangles,s,false);mesh.RecalculateBounds();return mesh;
  }
  public System.Collections.IEnumerator CutAsync(Transform transform,IReadOnlyList<ClipBox> boxes,Action<Mesh,int> complete)
  {
   int changed=0;var slice=System.Diagnostics.Stopwatch.StartNew();
   var matrix=transform.localToWorldMatrix;var inverse=matrix.inverse;var vertices=new List<ClipVertex>(_positions.Length);for(int i=0;i<_positions.Length;i++){if(i%256==0&&slice.Elapsed.TotalMilliseconds>=8){yield return null;slice.Restart();}var p=matrix.MultiplyPoint3x4(_positions[i]);vertices.Add(new ClipVertex(new V3(p.x,p.y,p.z),_attributes[i],i));}

   var output=new List<int>[_indices.Length];changed=0;
   for(int s=0;s<_indices.Length;s++)
   {
    output[s]=new List<int>();var indices=_indices[s];
    for(int i=0;i<indices.Length;i+=3)
    {
     if(i%768==0&&slice.Elapsed.TotalMilliseconds>=8){yield return null;slice.Restart();}
     var original=new[]{vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]};var triangles=new List<ClipVertex>(original);
     var a=original[0].Position;var b=original[1].Position;var c=original[2].Position;
     float minX=Math.Min(a.X,Math.Min(b.X,c.X)),maxX=Math.Max(a.X,Math.Max(b.X,c.X)),minY=Math.Min(a.Y,Math.Min(b.Y,c.Y)),maxY=Math.Max(a.Y,Math.Max(b.Y,c.Y)),minZ=Math.Min(a.Z,Math.Min(b.Z,c.Z)),maxZ=Math.Max(a.Z,Math.Max(b.Z,c.Z));
     foreach(var box in boxes){if(maxX<box.Min.X||minX>box.Max.X||maxY<box.Min.Y||minY>box.Max.Y||maxZ<box.Min.Z||minZ>box.Max.Z)continue;if(box.StrictlyContains(a)&&box.StrictlyContains(b)&&box.StrictlyContains(c)){triangles.Clear();break;}var next=new List<ClipVertex>();for(int j=0;j<triangles.Count;j+=3)next.AddRange(GeometryClip.Subtract(triangles[j],triangles[j+1],triangles[j+2],box));triangles=next;if(triangles.Count==0)break;}
     if(triangles.Count!=3||!ReferenceEquals(triangles[0],original[0])||!ReferenceEquals(triangles[1],original[1])||!ReferenceEquals(triangles[2],original[2]))changed++;
     foreach(var v in triangles){if(v.SourceIndex>=0)output[s].Add(v.SourceIndex);else{output[s].Add(vertices.Count);vertices.Add(v);}}
    }
   }

   if(changed==0){complete(null,0);yield break;}
   var mesh=new Mesh{name="PeakCreativeMode clipped terrain",indexFormat=IndexFormat.UInt32};var local=new Vector3[vertices.Count];for(int i=0;i<local.Length;i++){var p=vertices[i].Position;local[i]=inverse.MultiplyPoint3x4(new Vector3(p.X,p.Y,p.Z));}mesh.vertices=local;
   foreach(var attr in _layout)
   {
    if(slice.Elapsed.TotalMilliseconds>=8){yield return null;slice.Restart();}
    var values=new List<Vector4>(vertices.Count);foreach(var vertex in vertices){var data=vertex.Attributes;int o=attr.Offset;values.Add(new Vector4(data[o],attr.Dimension>1?data[o+1]:0,attr.Dimension>2?data[o+2]:0,attr.Dimension>3?data[o+3]:0));}
    if(attr.Kind==VertexAttribute.Normal){var normals=new List<Vector3>();foreach(var v in values)normals.Add(new Vector3(v.x,v.y,v.z).normalized);mesh.SetNormals(normals);}
    else if(attr.Kind==VertexAttribute.Tangent)mesh.SetTangents(values);
    else if(attr.Kind==VertexAttribute.Color){var colors=new List<Color>();foreach(var v in values)colors.Add(new Color(v.x,v.y,v.z,v.w));mesh.SetColors(colors);}
    else {int channel=(int)attr.Kind-(int)VertexAttribute.TexCoord0;if(attr.Dimension==2){var uv=new List<Vector2>();foreach(var v in values)uv.Add(new Vector2(v.x,v.y));mesh.SetUVs(channel,uv);}else if(attr.Dimension==3){var uv=new List<Vector3>();foreach(var v in values)uv.Add(new Vector3(v.x,v.y,v.z));mesh.SetUVs(channel,uv);}else mesh.SetUVs(channel,values);}
   }

   mesh.subMeshCount=output.Length;for(int s=0;s<output.Length;s++)mesh.SetIndices(output[s].ToArray(),MeshTopology.Triangles,s,false);mesh.RecalculateBounds();complete(mesh,changed);
  }
 }
}
