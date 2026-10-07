package com.bornparanoid.peakpassthrough.terraform;
import com.google.gson.*;
import java.util.*;
import com.bornparanoid.peakpassthrough.mirror.TerrainSamples;
/** Strict bounded conversion request decoder; client cannot select valuable blocks. */
public record TerrainConversion(Volumes.Volume volume,Set<Volumes.Cell> solid){
 public static TerrainConversion parse(JsonObject o,double px,double py,double pz){try{
  if(!o.has("convert")||!o.get("convert").getAsJsonPrimitive().isBoolean()||!o.get("convert").getAsBoolean())return null;
  var origin=o.getAsJsonArray("origin");var size=o.getAsJsonArray("size");if(origin.size()!=3||size.size()!=3)return null;
  int ox=Volumes.integer(origin.get(0)),oy=Volumes.integer(origin.get(1)),oz=Volumes.integer(origin.get(2)),sx=Volumes.integer(size.get(0)),sy=Volumes.integer(size.get(1)),sz=Volumes.integer(size.get(2));
  if(o.has("quick")){if(!o.get("quick").isJsonPrimitive()||!o.get("quick").getAsJsonPrimitive().isBoolean())return null;if(o.get("quick").getAsBoolean()&&(sx!=4||sy!=4||sz!=4||Math.floorMod(ox,4)!=0||Math.floorMod(oy,4)!=0||Math.floorMod(oz,4)!=0))return null;}
  var v=new Volumes.Volume(Volumes.key(ox,oy,-oz-sz,sx,sy,sz),ox,oy,-oz-sz,sx,sy,sz,o.get("material").getAsString());
  double dx=ox+sx*.5-px,dy=oy+sy*.5-py,dz=v.z()+sz*.5-pz;if(dx*dx+dy*dy+dz*dz>16*16)return null;
  var batch=TerrainSamples.parse(o);if(batch==null||!batch.water().isEmpty())return null;var cells=new HashSet<Volumes.Cell>();for(var c:batch.solid()){var p=new Volumes.Cell(c.x(),c.y(),c.z());if(!v.contains(p))return null;cells.add(p);}return new TerrainConversion(v,Set.copyOf(cells));
 }catch(RuntimeException e){return null;}}
}
