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
 @Override public net.minecraft.world.item.ItemStack getItemBySlot(net.minecraft.world.entity.EquipmentSlot slot){return stowed&&slot==net.minecraft.world.entity.EquipmentSlot.MAINHAND?net.minecraft.world.item.ItemStack.EMPTY:super.getItemBySlot(slot);}
 private final DamageSink damage;
 private final BiConsumer<Integer,Float> eaten;
 public java.util.function.Consumer<Vec3> bridgeTeleport=pos->{};
 public java.util.function.Consumer<String> effects=line->{};
 public void bridgeEffect(net.minecraft.world.effect.MobEffectInstance effect){var id=net.minecraft.core.registries.BuiltInRegistries.MOB_EFFECT.getKey(effect.getEffect().value()).toString();var line=EffectFeed.message(id,effect.getDuration()/20.0,effect.getAmplifier());if(line!=null)effects.accept(line);}
 public BridgePlayer(ServerLevel level,GameProfile profile,DamageSink damage,BiConsumer<Integer,Float> eaten){super(level,profile);this.damage=damage;this.eaten=eaten;setNoGravity(true);}
 @Override public boolean isInvulnerableTo(ServerLevel level,DamageSource source){return isSpectator()||!(source.getEntity() instanceof Mob);}
 @Override public boolean hurtServer(ServerLevel level,DamageSource source,float amount){if(!Float.isFinite(amount)||amount<=0)return false;float max=getMaxHealth();setHealth(max);boolean hit=super.hurtServer(level,source,Math.min(amount,max-1));float applied=max-getHealth();setHealth(max);if(hit&&applied>0){var origin=source.getSourcePosition();if(origin==null&&source.getEntity()!=null)origin=source.getEntity().position();double[] knock=origin==null?new double[]{0,0,0}:CombatKnockback.vector(getX(),getZ(),origin.x,origin.z,applied,source.getEntity() instanceof net.minecraft.world.entity.monster.Creeper);damage.accept(applied,source.getEntity() instanceof net.minecraft.world.entity.monster.Creeper?"creeper":source.getMsgId(),knock);}return hit;}
 @Override protected void completeUsingItem(){var stack=getUseItem();var food=stack.get(net.minecraft.core.component.DataComponents.FOOD);int count=stack.getCount();super.completeUsingItem();if(food!=null&&stack.getCount()<count)eaten.accept(food.nutrition(),food.saturation());}
 public void bridgeTick(){double x=getX(),y=getY(),z=getZ();float yaw=getYRot(),pitch=getXRot();setDeltaMovement(Vec3.ZERO);setOnGround(true);getFoodData().setFoodLevel(19);if(damageCooldownTime>0)damageCooldownTime--;try{super.doTick();}finally{snapTo(x,y,z,yaw,pitch);setDeltaMovement(Vec3.ZERO);setHealth(getMaxHealth());}}
}
