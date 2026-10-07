using System.Collections.Generic;
using System;
using System.Linq;
namespace PeakCreativeMode.Core
{
    public sealed class Quad
    {
        public readonly string State,Face;
        public readonly V3[] Vertices;
        public readonly string Texture;public readonly float[][] UV;public readonly bool Collision=true;
        public Quad(string s,string f,V3[] v) {State=s;Face=f;Vertices=v;}
        public Quad(string s,ModelQuad model,V3 min){State=s;Face="model";Texture=model.Texture;UV=model.UV;Collision=model.Collision;Vertices=new V3[4];for(int i=0;i<4;i++)Vertices[i]=min+model.Vertices[i];}
    }
    public static class ChunkMesher
    {
        // Counter-clockwise cross products point outwards; Unity sees clockwise front faces.
        private static readonly (int x,int y,int z,string name,V3[] corners)[] Faces={
            (1,0,0,"east",new[]{new V3(1,0,0),new V3(1,1,0),new V3(1,1,1),new V3(1,0,1)}),
            (-1,0,0,"west",new[]{new V3(0,0,1),new V3(0,1,1),new V3(0,1,0),new V3(0,0,0)}),
            (0,1,0,"up",new[]{new V3(0,1,1),new V3(1,1,1),new V3(1,1,0),new V3(0,1,0)}),
            (0,-1,0,"down",new[]{new V3(0,0,0),new V3(1,0,0),new V3(1,0,1),new V3(0,0,1)}),
            (0,0,-1,"north",new[]{new V3(1,0,1),new V3(1,1,1),new V3(0,1,1),new V3(0,0,1)}),
            (0,0,1,"south",new[]{new V3(0,0,0),new V3(0,1,0),new V3(1,1,0),new V3(1,0,0)})
        };
        private static bool FullFace(ModelQuad q,out int x,out int y,out int z){x=y=z=0;var v=q.Vertices;float ax=v.Min(p=>p.X),bx=v.Max(p=>p.X),ay=v.Min(p=>p.Y),by=v.Max(p=>p.Y),az=v.Min(p=>p.Z),bz=v.Max(p=>p.Z);if(ay==0&&by==1&&az==0&&bz==1&&ax==bx&&(ax==0||ax==1)){x=ax==0?-1:1;return true;}if(ax==0&&bx==1&&az==0&&bz==1&&ay==by&&(ay==0||ay==1)){y=ay==0?-1:1;return true;}if(ax==0&&bx==1&&ay==0&&by==1&&az==bz&&(az==0||az==1)){z=az==0?1:-1;return true;}return false;}
        public static List<Quad> Build(BlockGrid grid,BlockPos chunk,Func<string,List<ModelQuad>> models=null)
        {
            var result=new List<Quad>();var boundaries=new Dictionary<ModelQuad,(bool full,int x,int y,int z)>();var opaque=new Dictionary<string,bool>();
            (bool full,int x,int y,int z) Boundary(ModelQuad q){if(!boundaries.TryGetValue(q,out var b)){bool f=FullFace(q,out int x,out int y,out int z);b=(f,x,y,z);boundaries[q]=b;}return b;}
            bool Opaque(string state){if(opaque.TryGetValue(state,out bool value))return value;var quads=state==BlockGrid.Air?null:models?.Invoke(state);value=quads!=null&&quads.Count==6&&quads.All(q=>Boundary(q).full)&&BlockModelGeometry.HasCollision(state)&&!BlockModelGeometry.ClimbCell(state);opaque[state]=value;return value;}

            for(int y=0;y<16;y++)for(int z=0;z<16;z++)for(int x=0;x<16;x++)
            {
                var p=new BlockPos(chunk.X*16+x,chunk.Y*16+y,chunk.Z*16+z);string state=grid.Get(p);
                if(state==BlockGrid.Air)continue;
                var model=models?.Invoke(state);if(model!=null){var min=Coords.McBlockMinToUnity(p.X,p.Y,p.Z);foreach(var q in model){var boundary=Boundary(q);if(boundary.full&&Opaque(grid.Get(p.Offset(boundary.x,boundary.y,boundary.z))))continue;result.Add(new Quad(state,q,min));}continue;}
                foreach(var f in Faces)
                {
                    string neighbour=grid.Get(p.Offset(f.x,f.y,f.z));if(neighbour!=BlockGrid.Air&&!neighbour.StartsWith("minecraft:campfire",StringComparison.Ordinal))continue;
                    V3 min=Coords.McBlockMinToUnity(p.X,p.Y,p.Z);var v=new V3[4];
                    for(int i=0;i<4;i++)v[i]=min+new V3(f.corners[i].X*Coords.BLOCK_SIZE,f.corners[i].Y*Coords.BLOCK_SIZE,f.corners[i].Z*Coords.BLOCK_SIZE);
                    result.Add(new Quad(state,f.name,v));
                }
            }
            return result;
        }
    }
}
