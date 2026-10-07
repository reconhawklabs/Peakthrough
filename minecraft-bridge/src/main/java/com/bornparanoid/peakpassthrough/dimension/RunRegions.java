package com.bornparanoid.peakpassthrough.dimension;
import com.google.gson.*;
import java.io.IOException;
import java.nio.file.*;
import java.util.*;
/** Persisted private regions; coordinates remain on the same integer/chunk grid. */
public final class RunRegions {
 public static final int SPACING=65536,RADIUS=16384,MAX_RUNS=4096;
 public record Region(int x,int z){public boolean owns(double px,double pz){return px>=x-RADIUS&&px<x+RADIUS&&pz>=z-RADIUS&&pz<z+RADIUS;}}
 private final Path file;private final Map<String,Integer> slots=new LinkedHashMap<>();
 public RunRegions(Path file)throws IOException{this.file=file;if(!Files.exists(file)){slots.put("dev",0);return;}try{if(Files.size(file)>1048576)throw new IOException("Run registry too large");var root=JsonParser.parseString(Files.readString(file)).getAsJsonObject();if(root.get("version").getAsInt()!=1)throw new IOException("Run registry version");for(var entry:root.getAsJsonObject("runs").entrySet()){validate(entry.getKey());var value=entry.getValue();if(!value.isJsonPrimitive()||!value.getAsJsonPrimitive().isNumber())throw new IOException("Run slot type");double n=value.getAsDouble();if(!Double.isFinite(n)||n!=Math.rint(n)||n<0||n>=MAX_RUNS)throw new IOException("Run slot range");int slot=(int)n;if(slots.containsValue(slot)&&slot!=0)throw new IOException("Run slot collision");slots.put(entry.getKey(),slot);}if(!Objects.equals(slots.get("dev"),0)||slots.size()>MAX_RUNS+1)throw new IOException("Run registry legacy slot");long aliases=slots.entrySet().stream().filter(e->e.getValue()==0&&!e.getKey().equals("dev")).count();if(aliases>1)throw new IOException("Run alias collision");}catch(RuntimeException e){throw new IOException("Invalid run registry; preserving file",e);}}
 public Region region(String seed)throws IOException{validate(seed);Integer slot=slots.get(seed);if(slot==null){boolean migrated=slots.entrySet().stream().anyMatch(e->e.getValue()==0&&!e.getKey().equals("dev"));if(seed.startsWith("peak:")&&!migrated)slot=0;else{slot=1;while(slots.containsValue(slot))slot++;if(slot>=MAX_RUNS)throw new IOException("Run registry full");}slots.put(seed,slot);try{save();}catch(IOException e){slots.remove(seed);throw e;}}return new Region(slot%64*SPACING,slot/64*SPACING);}
 public static void validate(String seed){if(seed==null||seed.isBlank()||seed.length()>128||seed.indexOf('\0')>=0)throw new IllegalArgumentException("Invalid map seed");}
 private void save()throws IOException{var root=new JsonObject();root.addProperty("version",1);var entries=new JsonObject();slots.forEach(entries::addProperty);root.add("runs",entries);Files.createDirectories(file.toAbsolutePath().getParent());var tmp=file.resolveSibling(file.getFileName()+".tmp");Files.writeString(tmp,root.toString());try{Files.move(tmp,file,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);}catch(AtomicMoveNotSupportedException e){Files.move(tmp,file,StandardCopyOption.REPLACE_EXISTING);}}
}
