using System;
namespace PeakCreativeMode.Core
{
    public readonly struct V3
    {
        public readonly float X, Y, Z;
        public V3(float x, float y, float z) { X=x; Y=y; Z=z; }
        public static V3 operator +(V3 a, V3 b) => new V3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
    }
    public readonly struct BlockPos : IEquatable<BlockPos>
    {
        public readonly int X,Y,Z;
        public BlockPos(int x,int y,int z) { X=x;Y=y;Z=z; }
        public bool Equals(BlockPos p) => X==p.X && Y==p.Y && Z==p.Z;
        public override bool Equals(object o) => o is BlockPos p && Equals(p);
        public override int GetHashCode() => HashCode.Combine(X,Y,Z);
        public BlockPos Offset(int x,int y,int z) => new BlockPos(X+x,Y+y,Z+z);
        public BlockPos Chunk => new BlockPos(Floor16(X),Floor16(Y),Floor16(Z));
        private static int Floor16(int n) => (int)Math.Floor(n/16.0);
    }
    public static class Coords
    {
        public const float BLOCK_SIZE = 1f;
        public static float DragonYawToUnity(float yaw)=>yaw;
        public static float McYawToUnity(float yaw)=>yaw+180;
        public static V3 McModelToUnity(float x,float y,float z)=>new V3(x/16,-y/16,-z/16);
        public static V3 McToUnity(float x,float y,float z)=>new V3(x*BLOCK_SIZE,y*BLOCK_SIZE,-z*BLOCK_SIZE);
        public static V3 McBlockMinToUnity(int x,int y,int z) => new V3(x*BLOCK_SIZE,y*BLOCK_SIZE,(-z-1f)*BLOCK_SIZE);
        public static BlockPos UnityPointToMcBlock(float x,float y,float z) => new BlockPos(
            (int)Math.Floor(x/BLOCK_SIZE),(int)Math.Floor(y/BLOCK_SIZE),(int)Math.Floor(-z/BLOCK_SIZE));
    }
}
