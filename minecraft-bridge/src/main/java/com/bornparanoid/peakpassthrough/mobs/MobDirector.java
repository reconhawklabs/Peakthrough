package com.bornparanoid.peakpassthrough.mobs;

import com.bornparanoid.peakpassthrough.dimension.RunRegions;
import java.util.*;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.Identifier;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.clock.WorldClocks;
import net.minecraft.world.entity.*;
import net.minecraft.world.level.LightLayer;
import net.minecraft.world.level.gamerules.GameRules;

/** Runs on the server thread; native mobs own all movement, attacks and loot. */
public final class MobDirector {
 public record Run(String seed,RunRegions.Region region,List<ServerPlayer> players,MobEnvironment env,List<SpawnHints.Cell> hints) {}
 public static boolean sunBurn=false;
 private final Map<String,Long> lastSpawn=new HashMap<>();
 private final Map<String,Long> arrived=new HashMap<>(),lastActive=new HashMap<>();
 private final Map<UUID,Long> born=new HashMap<>();
 private final Random random=new Random();
 public MobDirector(MinecraftServer server){
  server.getGlobalGameRules().set(GameRules.SPAWN_MOBS,false,server);
  server.getGlobalGameRules().set(GameRules.MOB_GRIEFING,false,server);
  server.getGlobalGameRules().set(GameRules.ADVANCE_TIME,false,server);
 }
 public void tick(MinecraftServer server,ServerLevel level,long tick,List<Run> runs){
  Set<UUID> present=new HashSet<>();
  for(var run:runs){
   var env=run.env();
   if(tick-lastActive.getOrDefault(run.seed(),-1000L)>100)arrived.put(run.seed(),tick);lastActive.put(run.seed(),tick);long elapsed=tick-arrived.get(run.seed());
   sunBurn=env.sunBurn();
   server.getGlobalGameRules().set(GameRules.MOB_GRIEFING,env.griefing(),server);
   var clock=level.dimensionType().defaultClock().or(()->level.registryAccess().get(WorldClocks.OVERWORLD));
   clock.ifPresent(c->server.clockManager().setTotalTicks(c,Math.floorMod((long)((env.time()-6)/24*24000),24000)));
   var mobs=new ArrayList<Mob>();
   for(var entity:level.getAllEntities())if(entity instanceof Mob mob&&!mob.isRemoved()&&run.region().owns(mob.getX(),mob.getZ())){
    present.add(mob.getUUID());
    if(mob.entityTags().contains("peak_director")){
     long age=tick-born.computeIfAbsent(mob.getUUID(),id->tick);
     double nearest=run.players().stream().mapToDouble(p->Math.sqrt(mob.distanceToSqr(p))).min().orElse(Double.POSITIVE_INFINITY);
     var closest=run.hints().stream().min(Comparator.comparingDouble(h->distanceSquared(h,mob.getX(),mob.getY(),mob.getZ())));
     boolean open=closest.isPresent()&&distanceSquared(closest.get(),mob.getX(),mob.getY(),mob.getZ())<=64&&closest.get().open();
     if(MobArrivalSafety.clear(MobTable.hostile(type(mob)),nearest,elapsed)){mob.discard();continue;}
     if(MobBudget.despawn(MobTable.hostile(type(mob)),nearest,!env.night(),open,age)){mob.discard();continue;}
    }
    mobs.add(mob);
   }
   if(!env.mobs()||run.hints().isEmpty()||tick-lastSpawn.getOrDefault(run.seed(),-100L)<100)continue;
   var cell=run.hints().get(random.nextInt(run.hints().size()));
   String type=MobTable.pick(MobTable.Biome.of(env.biome()),env.night(),cell.dark(),random.nextDouble());
   if(type==null)continue;
   boolean hostile=MobTable.hostile(type);
   double distance=run.players().stream().mapToDouble(p->Math.sqrt(Math.pow(p.getX()-cell.x(),2)+Math.pow(p.getY()-cell.y(),2)+Math.pow(p.getZ()-cell.z(),2))).min().orElse(0);
   if(!MobArrivalSafety.spawn(hostile,distance,elapsed))continue;
   int hostileNear=0,passiveNear=0;
   for(var mob:mobs)if(run.players().stream().anyMatch(p->mob.distanceToSqr(p)<=48*48)){if(MobTable.hostile(type(mob)))hostileNear++;else passiveNear++;}
   if(!MobBudget.allow(hostileNear,passiveNear,mobs.size(),lastSpawn.getOrDefault(run.seed(),-100L),tick,hostile,env.hostileCap(),env.passiveCap(),env.runCap()))continue;
   var pos=BlockPos.containing(cell.x(),Math.floor(cell.y())+1,cell.z());
   if(!run.region().owns(pos.getX(),pos.getZ())||!level.getBlockState(pos).isAir()||!level.getBlockState(pos.above()).isAir()||level.getBlockState(pos.below()).isAir())continue;
   if(hostile&&(level.getBrightness(LightLayer.BLOCK,pos)>=8||env.campfires().stream().anyMatch(f->distanceSquared(cell,f.x(),f.y(),f.z())<=144)))continue;
   var entityType=BuiltInRegistries.ENTITY_TYPE.getValue(Identifier.parse(type));
   var entity=entityType.spawn(level,pos,EntitySpawnReason.NATURAL);
   if(entity instanceof Mob mob){mob.addTag("peak_director");born.put(mob.getUUID(),tick);present.add(mob.getUUID());lastSpawn.put(run.seed(),tick);}
  }
  // Keep age for loaded director mobs even while their host is temporarily absent.
  for(var e:level.getAllEntities())if(e instanceof Mob&&e.entityTags().contains("peak_director"))present.add(e.getUUID());
  born.keySet().retainAll(present);
 }
 private static String type(Mob mob){return BuiltInRegistries.ENTITY_TYPE.getKey(mob.getType()).toString();}
 private static double distanceSquared(SpawnHints.Cell h,double x,double y,double z){return Math.pow(h.x()-x,2)+Math.pow(h.y()-y,2)+Math.pow(h.z()-z,2);}
}
