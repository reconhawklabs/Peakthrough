package com.bornparanoid.peakpassthrough.world;
import java.util.*;
/** Sparse, deterministic low outcrops. Positions stay indexed after mining, so they never refill. */
public final class BlockDeposits {
 public record Cell(int x,int y,int z,String block){}
 public static boolean candidate(int x,int z){return Math.floorMod(x,16)==8&&Math.floorMod(z,16)==8;}
 public static List<Cell> layout(String seed,int x,int y,int z){
  var random=new Random(Objects.hash(seed,x,z));String[] materials={"minecraft:cobblestone","minecraft:sandstone","minecraft:oak_log"};String block=materials[Math.floorMod(seed.hashCode()+x+z,materials.length)];var result=new ArrayList<Cell>();
  for(int dx=0;dx<2;dx++)for(int dz=0;dz<2;dz++)result.add(new Cell(x+dx,y,z+dz,block));
  result.add(new Cell(x,y+1,z,block));if(random.nextBoolean())result.add(new Cell(x+2,y,z,block));if(random.nextBoolean())result.add(new Cell(x,y,z+2,block));if(random.nextDouble()<.10)result.set(4,new Cell(x,y+1,z,"minecraft:tnt"));return List.copyOf(result);
 }
}
