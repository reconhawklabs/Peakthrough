package com.bornparanoid.peakpassthrough.world;
import com.google.gson.*;
import com.bornparanoid.peakpassthrough.blocks.BlockWorld;
import com.bornparanoid.peakpassthrough.dimension.RunRegions;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.Identifier;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.item.*;
import net.minecraft.world.phys.Vec3;
public final class WorldEvents {
 private final BlockWorld blocks;private final RunEvents events;
 public WorldEvents(BlockWorld blocks,RunEvents events){this.blocks=blocks;this.events=events;}
 public void receive(String seed,RunRegions.Region region,ServerPlayer player,JsonObject o)throws java.io.IOException{
  if(!o.has("kind")||!o.get("kind").getAsString().equals("run_start"))return;var pos=position(o.get("beach"),region);if(pos==null||player.distanceToSqr(pos)>16*16)return;
  String id=o.has("starterTool")?o.get("starterTool").getAsString():"minecraft:iron_pickaxe";if(id.length()>128)return;var item=BuiltInRegistries.ITEM.getValue(Identifier.parse(id));if(item==null||item==Items.AIR)return;
  int trigger=com.bornparanoid.peakpassthrough.boss.DragonConfig.parse(o.has("dragon")?o.getAsJsonObject("dragon"):null).triggerSegment();var bow=com.bornparanoid.peakpassthrough.boss.BowPlacement.find(o.has("campfires")?o.getAsJsonArray("campfires"):null,trigger,region);if(bow!=null){
   if(events.claimBow(seed)){drop(bow,new ItemStack(Items.BOW));drop(bow.add(.5,0,0),new ItemStack(Items.ARROW,32));}
   if(events.claimLoot(seed,"summit:second_bow"))drop(bow.add(-.5,0,0),new ItemStack(Items.BOW));
   for(int pack=1;pack<3;pack++)if(events.claimLoot(seed,"summit:extra_arrows:"+pack))drop(bow.add(.5,0,pack*.35),new ItemStack(Items.ARROW,32));
  }
  if(events.claimLoot(seed,"starter:fishing_rod"))drop(pos.add(.8,0,0),new ItemStack(Items.FISHING_ROD));
  if(events.claimLoot(seed,"starter:iron_sword"))drop(pos.add(-.8,0,0),new ItemStack(Items.IRON_SWORD));
  if(events.claimLoot(seed,"starter:ender_pearls"))drop(pos.add(0,0,.8),new ItemStack(Items.ENDER_PEARL,3));
  if(events.claimLoot(seed,"starter:golden_apples"))drop(pos.add(0,0,-.8),new ItemStack(Items.GOLDEN_APPLE,3));
  if(events.claimPickaxe(seed)){var drop=new ItemEntity(blocks.level(),pos.x,pos.y+1.2,pos.z,new ItemStack(item));drop.setUnlimitedLifetime();drop.setNoGravity(true);drop.setNoPickUpDelay();drop.setDeltaMovement(Vec3.ZERO);blocks.level().addFreshEntity(drop);}
 }
 private void drop(Vec3 pos,ItemStack stack){var drop=new ItemEntity(blocks.level(),pos.x,pos.y+1.2,pos.z,stack);drop.setUnlimitedLifetime();drop.setNoGravity(true);drop.setNoPickUpDelay();drop.setDeltaMovement(Vec3.ZERO);blocks.level().addFreshEntity(drop);}
 public static Vec3 position(JsonElement token,RunRegions.Region region){try{if(!(token instanceof JsonArray a)||a.size()!=3)return null;double x=a.get(0).getAsDouble(),y=a.get(1).getAsDouble(),z=a.get(2).getAsDouble();if(!Double.isFinite(x)||!Double.isFinite(y)||!Double.isFinite(z)||Math.abs(x)>=RunRegions.RADIUS||Math.abs(z)>=RunRegions.RADIUS||y< -2030||y>2030)return null;return new Vec3(x+region.x(),y,-z+region.z());}catch(RuntimeException e){return null;}}
}
