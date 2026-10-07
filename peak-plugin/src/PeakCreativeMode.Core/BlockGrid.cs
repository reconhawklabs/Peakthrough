using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
namespace PeakCreativeMode.Core
{
    public sealed class BlockGrid
    {
        public const string Air="minecraft:air";
        private readonly Dictionary<BlockPos,string> _blocks=new Dictionary<BlockPos,string>();
        public readonly HashSet<BlockPos> Dirty=new HashSet<BlockPos>();
        public int Count=>_blocks.Count;
        public string Get(BlockPos p)=>_blocks.TryGetValue(p,out var s)?s:Air;
        public void Clear() { _blocks.Clear(); Dirty.Clear(); }
        public void Set(BlockPos p,string state)
        {
            if(state=="minecraft:barrier")state=Air;
            if(Get(p)==state)return;
            if(state==Air) _blocks.Remove(p); else _blocks[p]=state;
            var c=p.Chunk;Dirty.Add(c);
            int x=p.X-c.X*16,y=p.Y-c.Y*16,z=p.Z-c.Z*16;
            if(x==0)Dirty.Add(c.Offset(-1,0,0));if(x==15)Dirty.Add(c.Offset(1,0,0));
            if(y==0)Dirty.Add(c.Offset(0,-1,0));if(y==15)Dirty.Add(c.Offset(0,1,0));
            if(z==0)Dirty.Add(c.Offset(0,0,-1));if(z==15)Dirty.Add(c.Offset(0,0,1));
        }
        private void Mark(BlockPos c)
        {
            Dirty.Add(c);
            Dirty.Add(c.Offset(1,0,0)); Dirty.Add(c.Offset(-1,0,0));
            Dirty.Add(c.Offset(0,1,0)); Dirty.Add(c.Offset(0,-1,0));
            Dirty.Add(c.Offset(0,0,1)); Dirty.Add(c.Offset(0,0,-1));
        }
        public bool Apply(JObject msg)
        {
            string t=(string)msg["t"];
            if(t=="block")
            {
                if(!Int(msg,"x",out int x)||!Int(msg,"y",out int y)||!Int(msg,"z",out int z)||msg["state"]?.Type!=JTokenType.String) return false;
                Set(new BlockPos(x,y,z),(string)msg["state"]); return true;
            }
            if(t!="chunk" || !Int(msg,"cx",out int cx)||!Int(msg,"cy",out int cy)||!Int(msg,"cz",out int cz)) return false;
            if(!(msg["palette"] is JArray palette)||palette.Count==0||!(msg["data"] is JArray data)||data.Count!=4096) return false;
            foreach(var s in palette) if(s.Type!=JTokenType.String) return false;
            var states=new string[4096];
            for(int i=0;i<4096;i++)
            {
                if(data[i].Type!=JTokenType.Integer) return false;
                long index=(long)data[i]; if(index<0||index>=palette.Count)return false;
                states[i]=(string)palette[(int)index];
            }
            for(int y=0;y<16;y++)for(int z=0;z<16;z++)for(int x=0;x<16;x++)
            {
                var p=new BlockPos(cx*16+x,cy*16+y,cz*16+z); string state=states[x+16*(z+16*y)];
                if(state==Air||state=="minecraft:barrier")_blocks.Remove(p);else _blocks[p]=state;
            }
            Mark(new BlockPos(cx,cy,cz)); return true;
        }
        private static bool Int(JObject o,string key,out int value)
        {
            value=0; if(o[key]?.Type!=JTokenType.Integer)return false;
            long n=(long)o[key]; if(n < -30000000 || n > 30000000)return false;
            value=(int)n;return true;
        }
    }
}
