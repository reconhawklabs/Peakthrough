package com.bornparanoid.peakpassthrough.inventory;

import com.google.gson.*;
import com.mojang.serialization.JsonOps;
import net.minecraft.resources.RegistryOps;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
import java.nio.file.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.io.IOException;

/** Full native stack components and equipment; server-thread only, atomic disk writes. */
public final class InventoryStore {
 private final Path directory;
 private final Map<String,String> pending=new HashMap<>(), written=new HashMap<>();
 public InventoryStore(Path directory){this.directory=directory;}
 private String key(String seed,String player){try{return HexFormat.of().formatHex(MessageDigest.getInstance("SHA-256").digest((seed+"\0"+player).getBytes(StandardCharsets.UTF_8)));}catch(Exception e){throw new IllegalStateException(e);}}
 public static boolean readStowed(JsonObject root){
  if(!root.has("stowed"))return false; // Existing version-1 saves remain compatible.
  var flag=root.get("stowed");
  if(!flag.isJsonPrimitive()||!flag.getAsJsonPrimitive().isBoolean())throw new IllegalArgumentException("Invalid saved stow");
  return flag.getAsBoolean();
 }
 public void restore(String seed,String id,ServerPlayer player)throws IOException {
  String k=key(seed,id);Path file=directory.resolve(k+".json");String text=pending.get(k);
  if(text==null){if(!Files.exists(file))return;if(Files.size(file)>4*1024*1024)throw new IOException("Inventory file too large");text=Files.readString(file);}
  try{
   var root=JsonParser.parseString(text).getAsJsonObject();var slots=root.getAsJsonArray("slots");
   int count=player.getInventory().getContainerSize(),selected=root.get("selected").getAsInt();
   if(root.get("version").getAsInt()!=1||slots.size()!=count||selected<0||selected>8)throw new IllegalArgumentException("Inventory shape");
   var ops=RegistryOps.create(JsonOps.INSTANCE,player.registryAccess());var items=new ItemStack[count];
   for(int i=0;i<count;i++)items[i]=ItemStack.OPTIONAL_CODEC.parse(ops,slots.get(i)).getOrThrow();
   var carried=ItemStack.OPTIONAL_CODEC.parse(ops,root.get("carried")).getOrThrow();
   boolean stowed=readStowed(root);
   for(int i=0;i<count;i++)player.getInventory().setItem(i,items[i]);
   player.getInventory().setSelectedSlot(selected);player.inventoryMenu.setCarried(carried);
   if(player instanceof com.bornparanoid.peakpassthrough.session.BridgePlayer bp)bp.stowed=stowed;
   written.put(k,text);
  }catch(RuntimeException e){throw new IOException("Invalid saved inventory; refusing to replace "+file,e);}
 }
 public void capture(String seed,String id,ServerPlayer player){
  var ops=RegistryOps.create(JsonOps.INSTANCE,player.registryAccess());var root=new JsonObject();var slots=new JsonArray();
  for(int i=0;i<player.getInventory().getContainerSize();i++)slots.add(ItemStack.OPTIONAL_CODEC.encodeStart(ops,player.getInventory().getItem(i)).getOrThrow());
  root.addProperty("version",1);root.addProperty("selected",player.getInventory().getSelectedSlot());root.add("slots",slots);
  root.addProperty("stowed",player instanceof com.bornparanoid.peakpassthrough.session.BridgePlayer bp&&bp.stowed);
  root.add("carried",ItemStack.OPTIONAL_CODEC.encodeStart(ops,player.inventoryMenu.getCarried()).getOrThrow());
  String k=key(seed,id),text=root.toString();if(!text.equals(written.get(k)))pending.put(k,text);
 }
 public void flush()throws IOException {
  if(pending.isEmpty())return;Files.createDirectories(directory);
  for(var entry:new ArrayList<>(pending.entrySet())){
   Path file=directory.resolve(entry.getKey()+".json"),tmp=directory.resolve(entry.getKey()+".tmp");Files.writeString(tmp,entry.getValue());
   try{Files.move(tmp,file,StandardCopyOption.REPLACE_EXISTING,StandardCopyOption.ATOMIC_MOVE);}catch(AtomicMoveNotSupportedException e){Files.move(tmp,file,StandardCopyOption.REPLACE_EXISTING);}
   written.put(entry.getKey(),entry.getValue());pending.remove(entry.getKey());
  }
 }
}
