package com.bornparanoid.peakpassthrough.session;
import com.mojang.authlib.GameProfile;
import java.util.function.BiConsumer;
import net.fabricmc.fabric.api.entity.FakePlayer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.damagesource.DamageSource;
import net.minecraft.world.entity.Mob;
import net.minecraft.world.phys.Vec3;
/** Native targetable player with a no-op connection; PEAK still owns pose and health. */
public final class BridgePlayer extends FakePlayer {
 @FunctionalInterface public interface DamageSink {void accept(float amount,String source,double[] knock);}
 public boolean stowed;
 public Vec3 arrowImpact;public java.util.function.Consumer<com.google.gson.JsonObject> bridgeArrow=o->{};
 @Override public net.minecraft.world.item.ItemStack getItemBySlot(net.minecraft.world.entity.EquipmentSlot slot){return stowed&&slot==net.minecraft.world.entity.EquipmentSlot.MAINHAND?net.minecraft.world.item.ItemStack.EMPTY:super.getItemBySlot(slot);}
 private final DamageSink damage;
 private final BiConsumer<Integer,Float> eaten;
 public java.util.function.Consumer<Vec3> bridgeTeleport=pos->{};
 public java.util.function.Consumer<String> effects=line->{};
 public void bridgeEffect(net.minecraft.world.effect.MobEffectInstance effect){var id=net.minecraft.core.registries.BuiltInRegistries.MOB_EFFECT.getKey(effect.getEffect().value()).toString();var line=EffectFeed.message(id,effect.getDuration()/20.0,effect.getAmplifier());if(line!=null)effects.accept(line);}
 public BridgePlayer(ServerLevel level,GameProfile profile,DamageSink damage,BiConsumer<Integer,Float> eaten){super(level,profile);this.damage=damage;this.eaten=eaten;setNoGravity(true);}
 @Override public boolean isInvulnerableTo(ServerLevel level,DamageSource source){return isSpectator()||(!(source.getEntity() instanceof Mob)&&!(source.getDirectEntity() instanceof net.minecraft.world.entity.item.PrimedTnt));}
 @Override public boolean hurtServer(ServerLevel level,DamageSource source,float amount){if(!Float.isFinite(amount)||amount<=0)return false;float max=getMaxHealth();setHealth(max);boolean hit=super.hurtServer(level,source,Math.min(amount,max-1));float applied=max-getHealth();setHealth(max);if(hit&&applied>0){var origin=source.getSourcePosition();if(origin==null&&source.getEntity()!=null)origin=source.getEntity().position();double[] knock=origin==null?new double[]{0,0,0}:CombatKnockback.vector(getX(),getZ(),origin.x,origin.z,applied,source.getEntity() instanceof net.minecraft.world.entity.monster.Creeper||source.getDirectEntity() instanceof net.minecraft.world.entity.item.PrimedTnt);damage.accept(applied,source.getEntity() instanceof net.minecraft.world.entity.monster.Creeper?"creeper":source.getDirectEntity() instanceof net.minecraft.world.entity.item.PrimedTnt?"tnt":source.getMsgId(),knock);if(arrowImpact!=null&&source.getDirectEntity() instanceof net.minecraft.world.entity.projectile.arrow.AbstractArrow arrow&&source.getEntity() instanceof net.minecraft.world.entity.monster.skeleton.AbstractSkeleton){var o=new com.google.gson.JsonObject();o.addProperty("t","damage");o.addProperty("amount",0);o.addProperty("source","arrow");o.addProperty("player",getUUID().toString());var a=new com.google.gson.JsonObject();a.addProperty("id",arrow.getId());var offset=arrowImpact.subtract(position());var direction=arrow.getDeltaMovement();var p=new com.google.gson.JsonArray();p.add(offset.x);p.add(offset.y);p.add(offset.z);a.add("offset",p);var d=new com.google.gson.JsonArray();d.add(direction.x);d.add(direction.y);d.add(direction.z);a.add("direction",d);o.add("arrow",a);bridgeArrow.accept(o);}}return hit;}
 @Override protected void completeUsingItem(){var stack=getUseItem();var food=stack.get(net.minecraft.core.component.DataComponents.FOOD);int count=stack.getCount();super.completeUsingItem();if(food!=null&&stack.getCount()<count)eaten.accept(food.nutrition(),food.saturation());}
 public void bridgeTick(){double x=getX(),y=getY(),z=getZ();float yaw=getYRot(),pitch=getXRot();setDeltaMovement(Vec3.ZERO);setOnGround(true);getFoodData().setFoodLevel(19);if(damageCooldownTime>0)damageCooldownTime--;try{super.doTick();}finally{snapTo(x,y,z,yaw,pitch);setDeltaMovement(Vec3.ZERO);setHealth(getMaxHealth());}}
}
