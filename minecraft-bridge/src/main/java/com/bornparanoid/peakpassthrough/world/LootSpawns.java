package com.bornparanoid.peakpassthrough.world;
import com.bornparanoid.peakpassthrough.blocks.BlockWorld;
import com.bornparanoid.peakpassthrough.dimension.RunRegions;
import com.google.gson.JsonObject;
import java.io.IOException;
import net.minecraft.core.component.DataComponents;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.Identifier;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.item.ItemStack;
import net.minecraft.world.item.alchemy.PotionContents;
public final class LootSpawns {
 private final BlockWorld blocks;private final RunEvents events;
 public LootSpawns(BlockWorld blocks,RunEvents events){this.blocks=blocks;this.events=events;}
 public void migrateLadders(){for(var entity:blocks.level().getAllEntities())if(entity instanceof ItemEntity drop&&!drop.isRemoved()){var stack=drop.getItem();String id=BuiltInRegistries.ITEM.getKey(stack.getItem()).toString();int count=LadderReplacement.pearls(id,stack.getCount(),drop.isNoGravity(),drop.getAge());if(count>0)drop.setItem(new ItemStack(net.minecraft.world.item.Items.ENDER_PEARL,count));}}
 public void receive(String seed,RunRegions.Region region,JsonObject msg)throws IOException{
  var r=LootSpawnRequest.parse(msg);if(r==null||!region.owns(r.x(),r.z()))return;
  var item=BuiltInRegistries.ITEM.getValue(Identifier.parse(r.item()));if(item==null||item==net.minecraft.world.item.Items.AIR)return;
  var stack=new ItemStack(item,r.count());
  if(r.potion()!=null){var potion=BuiltInRegistries.POTION.get(Identifier.parse(r.potion()));if(potion.isEmpty())return;stack.set(DataComponents.POTION_CONTENTS,new PotionContents(potion.get()));}
  if(!events.claimLoot(seed,r.key()))return;
  var entity=new ItemEntity(blocks.level(),r.x(),r.y()+.3,r.z(),stack);entity.setUnlimitedLifetime();entity.setNoGravity(true);entity.setDeltaMovement(net.minecraft.world.phys.Vec3.ZERO);entity.setDefaultPickUpDelay();blocks.level().addFreshEntity(entity);
 }
}
