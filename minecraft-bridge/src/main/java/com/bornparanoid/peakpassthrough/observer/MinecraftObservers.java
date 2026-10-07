package com.bornparanoid.peakpassthrough.observer;
import java.util.*;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.level.GameType;
import net.minecraft.network.chat.Component;
import net.minecraft.network.protocol.game.ClientboundPlayerInfoUpdatePacket;
import net.minecraft.network.protocol.game.ClientboundPlayerInfoRemovePacket;
/** Normal Minecraft connections are read-only spectators; bridge avatars retain their own inventories. */
public final class MinecraftObservers {
 private static final class State {String seed;boolean announced;final Set<UUID> profiles=new HashSet<>();}
 private final Map<UUID,State> observers=new HashMap<>();
 private final MinecraftServer server;
 private final ServerLevel level;
 public MinecraftObservers(MinecraftServer server,ServerLevel level){this.server=server;this.level=level;}
 public void beforeAvatarAdded(ServerPlayer avatar){
  for(var observer:server.getPlayerList().getPlayers()){
   var state=observers.computeIfAbsent(observer.getUUID(),id->new State());
   if(state.profiles.add(avatar.getUUID()))observer.connection.send(ClientboundPlayerInfoUpdatePacket.createPlayerInitializing(List.of(avatar)));
  }
 }
 public void tick(Collection<ServerPlayer> avatars,Collection<ObserverPolicy.Anchor> anchors){
  var players=server.getPlayerList().getPlayers();var connected=new HashSet<UUID>();var current=new HashSet<UUID>();for(var avatar:avatars)current.add(avatar.getUUID());
  var target=ObserverPolicy.choose(anchors);
  for(var observer:players){
   connected.add(observer.getUUID());var state=observers.computeIfAbsent(observer.getUUID(),id->new State());
   if(!observer.isSpectator())observer.gameMode.changeGameModeForPlayer(GameType.SPECTATOR);
   var added=avatars.stream().filter(p->!state.profiles.contains(p.getUUID())).toList();
   if(!added.isEmpty()){observer.connection.send(ClientboundPlayerInfoUpdatePacket.createPlayerInitializing(added));added.forEach(p->state.profiles.add(p.getUUID()));}
   var removed=state.profiles.stream().filter(id->!current.contains(id)).toList();
   if(!removed.isEmpty()){observer.connection.send(new ClientboundPlayerInfoRemovePacket(removed));state.profiles.removeAll(removed);}
   if(!state.announced){observer.sendSystemMessage(Component.literal("PEAK observer: spectator flight is enabled. Waiting for an active PEAK run; blocks, mobs and Minecraft avatars are visible. PEAK scenery is not rendered here."));state.announced=true;}
   String dimension=observer.level()==level&&target!=null?target.seed():null;
   if(ObserverPolicy.move(state.seed,dimension,observer.getX(),observer.getY(),observer.getZ(),target)){
    if(observer.teleportTo(level,target.x()+4,target.y()+3,target.z()+4,Set.of(),observer.getYRot(),observer.getXRot(),true)){
     state.seed=target.seed();observer.sendSystemMessage(Component.literal("Watching PEAK. Fly freely nearby; moving more than 128 blocks away brings you back to the run."));
    }
   }
  }
  observers.keySet().retainAll(connected);
 }
}
