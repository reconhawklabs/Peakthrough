package com.bornparanoid.peakpassthrough.session;
import com.bornparanoid.peakpassthrough.blocks.*;
import com.bornparanoid.peakpassthrough.bridge.BridgeServer;
import com.bornparanoid.peakpassthrough.inventory.*;
import com.google.gson.*;
import java.util.*;
import net.minecraft.core.*;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.network.protocol.game.ServerboundPlayerActionPacket.Action;
import net.minecraft.resources.Identifier;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.entity.item.ItemEntity;
import net.minecraft.world.inventory.ContainerInput;
import net.minecraft.world.item.*;
import net.minecraft.world.level.block.Blocks;
import net.minecraft.world.phys.*;
/** Native survival rules for non-ticking bridge players. All calls on server thread. */
public final class PlayerActions {
 private record Mining(BlockPos pos,Direction face,float progress){}
 private final BlockWorld blocks;private final BridgeServer bridge;private final com.bornparanoid.peakpassthrough.boss.DragonCombat combat;
 private final Map<Integer,Mining> mining=new HashMap<>();
 private final Map<Integer,String> inventory=new HashMap<>(),held=new HashMap<>();
 public PlayerActions(BlockWorld blocks,BridgeServer bridge){this.blocks=blocks;this.bridge=bridge;this.combat=new com.bornparanoid.peakpassthrough.boss.DragonCombat(blocks.level());}
 public static int integer(JsonObject o,String key,int min,int max){var e=o.get(key);if(e==null||!e.isJsonPrimitive()||!e.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException(key);double n=e.getAsDouble();if(!Double.isFinite(n)||n!=Math.rint(n)||n<min||n>max)throw new IllegalArgumentException(key);return (int)n;}
 public static BlockPos pos(JsonObject o){return new BlockPos(integer(o,"x",-30000000,30000000),integer(o,"y",-2032,2031),integer(o,"z",-30000000,30000000));}
 public boolean near(ServerPlayer p,BlockPos pos){return p.getEyePosition().distanceToSqr(Vec3.atCenterOf(pos))<=36;}
 private Direction face(JsonObject t){return t.has("face")?Direction.valueOf(t.get("face").getAsString().toUpperCase(Locale.ROOT)):Direction.UP;}
 public void action(int client,ServerPlayer player,String kind,JsonObject t){if(player.isSpectator()&&!kind.startsWith("peak_")&&!(kind.equals("drop")&&t.has("slot")))return;switch(kind){
  case "peak_store"->{int ref=integer(t,"ref",1,Integer.MAX_VALUE);var tok=PeakTokens.parse(t.get("peak"));int slot=tok==null?-1:PeakInventory.store(player,tok,t.has("preferred")?integer(t,"preferred",-1,35):-1);bridge.send(client,PeakInventory.ack(ref,slot,null));}
  case "peak_slot"->{int ref=integer(t,"ref",1,Integer.MAX_VALUE);int s=integer(t,"slot",0,35);var e=t.get("peak");var tok=e==null||e.isJsonNull()?null:PeakTokens.parse(e);int slot=(e!=null&&!e.isJsonNull()&&tok==null)?-1:PeakInventory.replace(player,s,tok);bridge.send(client,PeakInventory.ack(ref,slot,null));}
  case "peak_clear_all"->{int ref=integer(t,"ref",1,Integer.MAX_VALUE);bridge.send(client,PeakInventory.ack(ref,0,PeakInventory.clearAll(player)));}

  case "creative_give"->{String id=t.has("item")?t.get("item").getAsString():t.get("block").getAsString();if(id.length()>128)return;var item=BuiltInRegistries.ITEM.getValue(Identifier.parse(id));if(item==null||item==Items.AIR)return;if(PeakTokens.of(player.getMainHandItem())!=null){var empty=new boolean[36];for(int i=0;i<36;i++)empty[i]=player.getInventory().getItem(i).isEmpty();int free=PeakInventory.choose(empty,-1);if(free<0)return;if(free>=9){player.getInventory().setItem(free,new ItemStack(item,Math.min(t.has("count")?integer(t,"count",1,64):64,item.getDefaultMaxStackSize())));sync(client,player);return;}player.getInventory().setSelectedSlot(free);}var stack=new ItemStack(item,Math.min(t.has("count")?integer(t,"count",1,64):64,item.getDefaultMaxStackSize()));if(t.has("damage")){if(!stack.isDamageableItem())return;stack.setDamageValue(integer(t,"damage",0,stack.getMaxDamage()-1));}player.getInventory().setSelectedItem(stack);inventory.remove(client);held.remove(client);}
  case "select_slot"->{int slot=integer(t,"slot",0,8);boolean stowed=false;if(t.has("stowed")){var flag=t.get("stowed");if(!flag.isJsonPrimitive()||!flag.getAsJsonPrimitive().isBoolean())return;stowed=flag.getAsBoolean();}stop(client,player);player.stopUsingItem();player.getInventory().setSelectedSlot(slot);if(player instanceof BridgePlayer bp)bp.stowed=stowed;}
  case "drop"->{if(t.has("slot")){int slot=integer(t,"slot",0,35);var stack=player.getInventory().getItem(slot);if(PeakTokens.of(stack)!=null||stack.isEmpty())return;var dropped=player.drop(stack.copy(),false,net.minecraft.util.Prediction.SERVER_ONLY);if(dropped!=null)player.getInventory().setItem(slot,ItemStack.EMPTY);}else{if(PeakTokens.of(player.getMainHandItem())!=null)return;stop(client,player);player.drop(false);}}
  case "inv_click"->{stop(client,player);int slot=integer(t,"slot",0,35);int button=integer(t,"button",0,1);player.inventoryMenu.clicked(InventoryMessages.menuSlot(slot),button,ContainerInput.PICKUP,player);}
  case "use"->{if(t.has("release")&&t.get("release").getAsBoolean()){player.releaseUsingItem();return;}stop(client,player);var stack=player.getMainHandItem();if(t.has("x")){var pos=pos(t);if(!near(player,pos))return;var f=face(t);var dest=pos.relative(f);String before=blocks.blockState(dest);var used=player.gameMode.useItemOn(player,blocks.level(),stack,InteractionHand.MAIN_HAND,new BlockHitResult(Vec3.atCenterOf(pos).add(f.getStepX()*.5,f.getStepY()*.5,f.getStepZ()*.5),f,pos,false));if(used==net.minecraft.world.InteractionResult.PASS)player.gameMode.useItem(player,blocks.level(),stack,InteractionHand.MAIN_HAND);if(!before.equals(blocks.blockState(dest)))blocks.track(dest);if(!blocks.level().getBlockState(pos).is(Blocks.BARRIER)&&!blocks.level().getBlockState(pos).isAir())blocks.track(pos);}else player.gameMode.useItem(player,blocks.level(),stack,InteractionHand.MAIN_HAND);}
  case "pickup"->{int id=CombatMessages.entityId(t);var e=blocks.level().getEntity(id);if(e instanceof ItemEntity item&&!item.hasPickUpDelay()&&CombatMessages.withinReach(player.position().distanceToSqr(item.position()),3))pickup(player,item);}
  case "impact"->combat.impact(player,t);
  case "attack"->{stop(client,player);player.swing(InteractionHand.MAIN_HAND,net.minecraft.world.item.component.SwingAnimation.DEFAULT,false);if(!t.has("entityId"))break;int id=CombatMessages.entityId(t);var e=blocks.level().getEntity(id);if(e instanceof net.minecraft.world.entity.boss.enderdragon.EnderDragon dragon){combat.melee(player,dragon,t.has("part")?t.get("part").getAsString():null);break;}if(e!=null&&e instanceof net.minecraft.world.entity.Mob&&CombatMessages.withinReach(player.getEyePosition().distanceToSqr(e.getBoundingBox().getCenter()))&&player.hasLineOfSight(e))player.attack(e);}
  case "mine_start"->{var pos=pos(t);if(!near(player,pos)||!blocks.owns(pos)||blocks.level().getBlockState(pos).is(Blocks.BARRIER))return;if(mining.containsKey(client)&&mining.get(client).pos().equals(pos))return;stop(client,player);var f=face(t);player.gameMode.handleBlockBreakAction(pos,Action.START_DESTROY_BLOCK,f,blocks.level().getMaxY(),0);mining.put(client,new Mining(pos,f,0));}
  case "mine_stop"->stop(client,player);
  default->{}
 }sync(client,player);}
 public void tick(int client,ServerPlayer player){
  // Ground truth is PEAK; FakePlayer never applies its own movement/gravity.
  player.setOnGround(true);if(player instanceof BridgePlayer bridgePlayer)bridgePlayer.bridgeTick();else player.getInventory().tick();player.gameMode.tick();
  var m=mining.get(client);if(m!=null){var state=blocks.level().getBlockState(m.pos());if(state.isAir()||!near(player,m.pos()))stop(client,player);else{if(!player.isSwinging())player.swing(InteractionHand.MAIN_HAND,net.minecraft.world.item.component.SwingAnimation.DEFAULT,false);float progress=m.progress()+state.getDestroyProgress(player,blocks.level(),m.pos());mining.put(client,new Mining(m.pos(),m.face(),progress));var o=new JsonObject();o.addProperty("t","break_progress");o.addProperty("x",m.pos().getX());o.addProperty("y",m.pos().getY());o.addProperty("z",m.pos().getZ());o.addProperty("stage",InventoryMessages.stage(progress));bridge.send(client,o.toString());if(progress>=1){player.gameMode.handleBlockBreakAction(m.pos(),Action.STOP_DESTROY_BLOCK,m.face(),blocks.level().getMaxY(),0);if(blocks.level().getBlockState(m.pos()).isAir())com.bornparanoid.peakpassthrough.sound.SoundFeed.emit(blocks.level(),m.pos().getX()+.5,m.pos().getY()+.5,m.pos().getZ()+.5,state.getSoundType().getBreakSound(),1,1);mining.remove(client);}}}
  for(var item:blocks.level().getEntitiesOfClass(ItemEntity.class,player.getBoundingBox().inflate(2)))if(!item.hasPickUpDelay())pickup(player,item);
  sync(client,player);
 }
 private void pickup(ServerPlayer player,ItemEntity item){int before=item.getItem().getCount();double x=item.getX(),y=item.getY(),z=item.getZ();item.playerTouch(player);if(item.isRemoved()||item.getItem().getCount()<before)com.bornparanoid.peakpassthrough.sound.SoundFeed.emit(blocks.level(),x,y,z,net.minecraft.sounds.SoundEvents.ITEM_PICKUP,.2f,1);}
 public void sync(int client,ServerPlayer p){String inv=InventoryMessages.snapshot(p);if(!inv.equals(inventory.put(client,inv)))bridge.send(client,inv);String h=InventoryMessages.held(p);if(!h.equals(held.put(client,h)))bridge.send(client,h);}
 private void stop(int client,ServerPlayer player){var m=mining.remove(client);if(m==null)return;player.gameMode.handleBlockBreakAction(m.pos(),Action.ABORT_DESTROY_BLOCK,m.face(),blocks.level().getMaxY(),0);var o=new JsonObject();o.addProperty("t","break_progress");o.addProperty("x",m.pos().getX());o.addProperty("y",m.pos().getY());o.addProperty("z",m.pos().getZ());o.addProperty("stage",-1);bridge.send(client,o.toString());}
 public void pause(int client,ServerPlayer player){stop(client,player);player.stopUsingItem();}
 public void remove(int client,ServerPlayer player){combat.remove(player);stop(client,player);inventory.remove(client);held.remove(client);}
}
