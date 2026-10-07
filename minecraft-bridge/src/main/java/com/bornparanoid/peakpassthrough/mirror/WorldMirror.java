package com.bornparanoid.peakpassthrough.mirror;
import com.bornparanoid.peakpassthrough.blocks.BlockWorld;
import java.util.*;
import net.minecraft.core.BlockPos;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.level.block.Blocks;
/** Invisible terrain ownership. Never overwrites player content or erases shared support. */
public final class WorldMirror {
 private final BlockWorld blocks;
 private final Map<Integer,Set<BlockPos>> owners=new HashMap<>();
 private final Set<BlockPos> mirrored=new HashSet<>();
 private final ChunkLeases leases;private java.util.List<com.bornparanoid.peakpassthrough.boss.DragonArena.Bounds> arenas=java.util.List.of();
 public void arenas(java.util.List<com.bornparanoid.peakpassthrough.boss.DragonArena.Bounds> bounds){arenas=java.util.List.copyOf(bounds);}
 private boolean arenaNear(BlockPos pos,ServerPlayer player){for(var a:arenas){double dx=pos.getX()-a.origin().getX(),dz=pos.getZ()-a.origin().getZ();if(dx*dx+dz*dz<=(a.orbit()+8)*(a.orbit()+8)&&Math.abs(pos.getY()-a.origin().getY())<=128&&(player==null||player.distanceToSqr(net.minecraft.world.phys.Vec3.atCenterOf(a.origin()))<=200*200))return true;}return false;}
 public WorldMirror(BlockWorld blocks){this.blocks=blocks;var restored=new HashSet<Long>();for(long c:blocks.level().getForceLoadedChunks().toLongArray())restored.add(c);leases=new ChunkLeases(restored);}
 public void apply(int client,TerrainSamples.Batch batch,ServerPlayer player){if(batch==null)return;var owned=owners.computeIfAbsent(client,id->new HashSet<>());for(var cell:batch.solid())write(cell,false,owned,player);for(var cell:batch.water())write(cell,true,owned,player);}
 private void write(TerrainSamples.Cell cell,boolean water,Set<BlockPos> owned,ServerPlayer player){
  var pos=new BlockPos(cell.x(),cell.y(),cell.z());double dx=pos.getX()+.5-player.getX(),dz=pos.getZ()+.5-player.getZ();
  if(((dx*dx+dz*dz>52*52||Math.abs(pos.getY()-player.getY())>96)&&!arenaNear(pos,player))||blocks.level().isOutsideBuildHeight(pos)||blocks.owns(pos))return;
  var old=blocks.level().getBlockState(pos);if(!old.isAir()&&!old.is(Blocks.BARRIER)&&!old.is(Blocks.WATER))return;
  blocks.level().setBlock(pos,(water?Blocks.WATER:Blocks.BARRIER).defaultBlockState(),3);owned.add(pos);mirrored.add(pos);
 }
 public void tick(Collection<? extends ServerPlayer> players){
  players=players.stream().filter(p->!p.isSpectator()).toList();
  // Force only active 7x7 chunk footprints; release tickets when players move/disconnect.
  var desired=new HashSet<Long>();for(var p:players){int cx=Math.floorDiv((int)Math.floor(p.getX()),16),cz=Math.floorDiv((int)Math.floor(p.getZ()),16);for(int x=cx-3;x<=cx+3;x++)for(int z=cz-3;z<=cz+3;z++)desired.add(pack(x,z));}
  for(var a:arenas)desired.addAll(com.bornparanoid.peakpassthrough.boss.ArenaFootprint.chunks(a.origin(),a.orbit()));
  var delta=leases.replace(desired);for(long c:delta.removed())blocks.level().setChunkForced(com.bornparanoid.peakpassthrough.boss.ArenaFootprint.x(c),com.bornparanoid.peakpassthrough.boss.ArenaFootprint.z(c),false);for(long c:delta.added())blocks.level().setChunkForced(com.bornparanoid.peakpassthrough.boss.ArenaFootprint.x(c),com.bornparanoid.peakpassthrough.boss.ArenaFootprint.z(c),true);
  // Evict distant samples; a shared near-player cell remains even if its first sampler moved.
  for(var pos:new HashSet<>(mirrored)){boolean near=arenaNear(pos,null);for(var p:players){double dx=pos.getX()-p.getX(),dz=pos.getZ()-p.getZ();if(dx*dx+dz*dz<=56*56){near=true;break;}}if(!near){var state=blocks.level().getBlockState(pos);if(!blocks.owns(pos)&&(state.is(Blocks.BARRIER)||state.is(Blocks.WATER)))blocks.level().setBlock(pos,Blocks.AIR.defaultBlockState(),3);mirrored.remove(pos);for(var set:owners.values())set.remove(pos);}}
 }
 public void remove(int id){owners.remove(id);}
 public void releaseAll(){for(long c:blocks.level().getForceLoadedChunks().toLongArray())blocks.level().setChunkForced(com.bornparanoid.peakpassthrough.boss.ArenaFootprint.x(c),com.bornparanoid.peakpassthrough.boss.ArenaFootprint.z(c),false);arenas=java.util.List.of();}
 private static long pack(int x,int z){return com.bornparanoid.peakpassthrough.boss.ArenaFootprint.key(x,z);}
}
