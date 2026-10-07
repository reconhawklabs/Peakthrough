package com.bornparanoid.peakpassthrough.boss;
import java.util.*;
import com.google.gson.JsonObject;
import net.minecraft.server.level.*;
import net.minecraft.world.entity.*;
import net.minecraft.world.entity.ai.attributes.Attributes;
import net.minecraft.world.entity.boss.enderdragon.*;
import net.minecraft.world.phys.*;
public final class DragonCombat {
 private final ServerLevel level;private final Map<UUID,Long> impacts=new HashMap<>();
 public DragonCombat(ServerLevel level){this.level=level;}
 private EnderDragonPart part(EnderDragon d,String name){int i=ImpactRequest.partIndex(name);return i<0?null:d.getSubEntities()[i];}
 private static double distance(ServerPlayer p,Entity e){return e.getBoundingBox().distanceToSqr(p.getEyePosition());}
 public void melee(ServerPlayer p,EnderDragon d,String name){var part=part(d,name);if(part==null||distance(p,part)>64||!p.hasLineOfSight(part))return;float scale=p.getAttackStrengthScale(.5f);float amount=(float)p.getAttributeValue(Attributes.ATTACK_DAMAGE)*(.2f+scale*scale*.8f);p.resetAttackStrengthTicker();d.hurt(level,part,p.damageSources().playerAttack(p),amount);}
 public void impact(ServerPlayer p,JsonObject o){var r=ImpactRequest.parse(o);if(r==null)return;long tick=level.getGameTime();if(tick-impacts.getOrDefault(p.getUUID(),-100L)<5)return;
  if(r.explosion()){var pos=new Vec3(r.x(),r.y(),r.z());if(p.position().distanceToSqr(pos)>64*64)return;impacts.put(p.getUUID(),tick);for(var e:level.getEntitiesOfClass(Mob.class,new AABB(pos,pos).inflate(r.radius()+16))){if(e instanceof EnderDragon d){EnderDragonPart best=null;double nearest=Double.POSITIVE_INFINITY;for(var part:d.getSubEntities()){double dist=part.getBoundingBox().distanceToSqr(pos);if(dist<nearest){nearest=dist;best=part;}}float damage=Impacts.damage(20,Math.sqrt(nearest),r.radius());if(best!=null&&damage>0)d.hurt(level,best,p.damageSources().explosion(p,p),damage);}else{float damage=Impacts.damage(20,Math.sqrt(e.getBoundingBox().distanceToSqr(pos)),r.radius());if(damage>0)e.hurtServer(level,p.damageSources().explosion(p,p),damage);}}}
  else{var e=level.getEntity(r.entityId());if(!(e instanceof Mob)||distance(p,e)>64*64)return;float amount=Impacts.thrown(r.mass(),r.speed());if(amount<=0)return;if(e instanceof EnderDragon d){var part=part(d,r.part());if(part==null||distance(p,part)>64*64)return;impacts.put(p.getUUID(),tick);d.hurt(level,part,p.damageSources().playerAttack(p),amount);}else{impacts.put(p.getUUID(),tick);e.hurtServer(level,p.damageSources().playerAttack(p),amount);}}
 }
 public void remove(ServerPlayer p){impacts.remove(p.getUUID());}
}
