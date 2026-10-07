package com.bornparanoid.peakpassthrough.terraform;
import com.google.gson.*;
import java.util.*;
import java.nio.file.*;
import java.io.IOException;
/** Persist geometry boundaries only. All voxel states remain in native Minecraft chunks. */
public final class Volumes {
 public static final int MAX_PER_RUN=256;
 public static Set<com.bornparanoid.peakpassthrough.blocks.BlockMessages.Chunk> touchedChunks(int x,int y,int z,int sx,int sy,int sz){var result=new HashSet<com.bornparanoid.peakpassthrough.blocks.BlockMessages.Chunk>();for(int cx=Math.floorDiv(x,16);cx<=Math.floorDiv(x+sx-1,16);cx++)for(int cy=Math.floorDiv(y,16);cy<=Math.floorDiv(y+sy-1,16);cy++)for(int cz=Math.floorDiv(z,16);cz<=Math.floorDiv(z+sz-1,16);cz++)result.add(new com.bornparanoid.peakpassthrough.blocks.BlockMessages.Chunk(cx,cy,cz));return Set.copyOf(result);}
 public record Cell(int x,int y,int z){}
 public record Volume(String id,int x,int y,int z,int sx,int sy,int sz,String material){
  public Volume{if(!Set.of("minecraft:stone","minecraft:dirt","minecraft:sandstone").contains(material)||sx<4||sy<4||sz<4||sx>8||sy>8||sz>8||Math.abs((long)x)>29000000||Math.abs((long)z)>29000000||y< -2032||y+sy>2032||!id.equals(key(x,y,z,sx,sy,sz)))throw new IllegalArgumentException("Volume bounds/id/material");}
  public JsonObject json(){var o=new JsonObject();o.addProperty("id",id);o.add("origin",array(x,y,z));o.add("size",array(sx,sy,sz));o.addProperty("material",material);return o;}
  public boolean contains(Cell p){return p.x>=x&&p.x<x+sx&&p.y>=y&&p.y<y+sy&&p.z>=z&&p.z<z+sz;}
 }
 private final Map<String,Volume> volumes=new LinkedHashMap<>();
 public List<Volume> all(){return List.copyOf(volumes.values());}
 public boolean add(Volume v){return volumes.putIfAbsent(v.id,v)==null;}
 public static String key(int x,int y,int z,int sx,int sy,int sz){return x+":"+y+":"+z+":"+sx+":"+sy+":"+sz;}
 public static JsonArray array(int... values){var a=new JsonArray();for(int v:values)a.add(v);return a;}
 public static int integer(JsonElement e){if(e==null||!e.isJsonPrimitive()||!e.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException("Integer required");double n=e.getAsDouble();if(!Double.isFinite(n)||n!=Math.rint(n)||Math.abs(n)>30000000)throw new IllegalArgumentException("Integer bounds");return (int)n;}
 public static Volume parse(JsonObject o){var p=o.getAsJsonArray("origin");var s=o.getAsJsonArray("size");if(p.size()!=3||s.size()!=3)throw new IllegalArgumentException("Volume vectors");return new Volume(o.get("id").getAsString(),integer(p.get(0)),integer(p.get(1)),integer(p.get(2)),integer(s.get(0)),integer(s.get(1)),integer(s.get(2)),o.get("material").getAsString());}
 public static Volumes load(Path path)throws IOException{var result=new Volumes();if(!Files.exists(path))return result;try{for(var e:JsonParser.parseString(Files.readString(path)).getAsJsonArray())if(!result.add(parse(e.getAsJsonObject())))throw new IllegalArgumentException("Duplicate volume");}catch(RuntimeException e){throw new IOException("Invalid native terraform boundaries; refusing to overwrite "+path,e);}return result;}
 public void save(Path path)throws IOException{var a=new JsonArray();volumes.values().forEach(v->a.add(v.json()));Files.createDirectories(path.toAbsolutePath().getParent());var tmp=path.resolveSibling(path.getFileName()+".tmp");Files.writeString(tmp,a.toString());try{Files.move(tmp,path,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);}catch(AtomicMoveNotSupportedException e){Files.move(tmp,path,StandardCopyOption.REPLACE_EXISTING);}}
}
