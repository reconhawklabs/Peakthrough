package com.bornparanoid.peakpassthrough.world;
import com.google.gson.*;
import java.io.IOException;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import java.security.*;
import java.util.HexFormat;
/** Refuse malformed event state; never reset an existing run to unspawned. */
public final class RunEvents {
 private final Path directory;
 public RunEvents(Path directory){this.directory=directory;}
 private Path path(String seed){com.bornparanoid.peakpassthrough.dimension.RunRegions.validate(seed);try{return directory.resolve(HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest(seed.getBytes(StandardCharsets.UTF_8)))+".json");}catch(NoSuchAlgorithmException e){throw new IllegalStateException(e);}}
 private JsonObject load(String seed)throws IOException{Path file=path(seed);if(!Files.exists(file)){var o=new JsonObject();o.addProperty("version",1);o.addProperty("seed",seed);o.addProperty("pickaxePlaced",false);o.addProperty("bowPlaced",false);o.addProperty("dragonState","not_started");return o;}
  try{if(Files.size(file)>8388608)throw new IOException("Run events too large");var o=JsonParser.parseString(Files.readString(file)).getAsJsonObject();if(o.get("version").getAsInt()!=1||!o.get("seed").getAsString().equals(seed))throw new IllegalArgumentException("Run event identity");for(String k:new String[]{"pickaxePlaced","bowPlaced"})if(!o.get(k).isJsonPrimitive()||!o.get(k).getAsJsonPrimitive().isBoolean())throw new IllegalArgumentException("Run event flag");if(!o.get("dragonState").isJsonPrimitive()||!o.get("dragonState").getAsJsonPrimitive().isString())throw new IllegalArgumentException("Run dragon state");return o;}catch(RuntimeException e){throw new IOException("Invalid run events; preserving "+file,e);}}
 private void save(String seed,JsonObject o)throws IOException{Files.createDirectories(directory);var file=path(seed);var tmp=file.resolveSibling(file.getFileName()+".tmp");Files.writeString(tmp,o.toString());try{Files.move(tmp,file,StandardCopyOption.ATOMIC_MOVE,StandardCopyOption.REPLACE_EXISTING);}catch(AtomicMoveNotSupportedException e){Files.move(tmp,file,StandardCopyOption.REPLACE_EXISTING);}}
 public boolean claimLoot(String seed,String key)throws IOException{if(key==null||key.isBlank()||key.length()>256)throw new IOException("Invalid loot key");var o=load(seed);JsonArray keys;if(o.has("lootKeys")){try{keys=o.getAsJsonArray("lootKeys");if(keys.size()>8192)throw new IllegalArgumentException();for(var k:keys)if(!k.isJsonPrimitive()||!k.getAsJsonPrimitive().isString()||k.getAsString().length()>256)throw new IllegalArgumentException();}catch(RuntimeException e){throw new IOException("Invalid loot keys; preserving run events",e);}}else{keys=new JsonArray();o.add("lootKeys",keys);}for(var k:keys)if(key.equals(k.getAsString()))return false;if(keys.size()>=8192)throw new IOException("Run loot-key limit reached");keys.add(key);save(seed,o);return true;}
 public boolean claimPickaxe(String seed)throws IOException{return claim(seed,"pickaxePlaced");}
 public boolean claimBow(String seed)throws IOException{return claim(seed,"bowPlaced");}
 private boolean claim(String seed,String key)throws IOException{var o=load(seed);if(o.get(key).getAsBoolean())return false;o.addProperty(key,true);save(seed,o);return true;}
 public String dragonState(String seed)throws IOException{return load(seed).get("dragonState").getAsString();}
 public void dragonState(String seed,String state)throws IOException{var o=load(seed);o.addProperty("dragonState",state);save(seed,o);}
}
