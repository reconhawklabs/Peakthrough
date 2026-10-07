package com.bornparanoid.peakpassthrough.boss;
import net.minecraft.core.BlockPos;
import net.minecraft.world.level.ChunkPos;
import java.util.*;
public final class ArenaFootprint {
 public static long key(int x,int z){return ChunkPos.pack(x,z);}public static int x(long key){return ChunkPos.getX(key);}public static int z(long key){return ChunkPos.getZ(key);}
 public static Set<Long> chunks(BlockPos origin,int orbit){if(orbit<20||orbit>160)throw new IllegalArgumentException("orbit");var result=new HashSet<Long>();int radius=orbit+16;for(int x=Math.floorDiv(origin.getX()-radius,16);x<=Math.floorDiv(origin.getX()+radius,16);x++)for(int z=Math.floorDiv(origin.getZ()-radius,16);z<=Math.floorDiv(origin.getZ()+radius,16);z++)result.add(key(x,z));return Set.copyOf(result);}
}
