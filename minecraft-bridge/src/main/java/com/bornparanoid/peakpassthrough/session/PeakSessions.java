package com.bornparanoid.peakpassthrough.session;

import static java.nio.charset.StandardCharsets.UTF_8;

import com.bornparanoid.peakpassthrough.Coords;
import com.bornparanoid.peakpassthrough.bridge.BridgeServer;
import com.bornparanoid.peakpassthrough.bridge.Protocol;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import com.bornparanoid.peakpassthrough.dimension.*;
import com.bornparanoid.peakpassthrough.inventory.InventoryStore;
import net.minecraft.world.level.storage.LevelResource;
import com.bornparanoid.peakpassthrough.blocks.BlockWorld;
import com.bornparanoid.peakpassthrough.blocks.BlockMessages;
import net.minecraft.world.InteractionHand;
import net.minecraft.world.item.ItemStack;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.resources.Identifier;
import com.mojang.authlib.GameProfile;
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import java.util.UUID;
import net.fabricmc.fabric.api.entity.FakePlayer;
import net.minecraft.server.MinecraftServer;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/** One FakePlayer per connected PEAK client. Runs on the server thread only. */
public final class PeakSessions {
	private static final Logger LOGGER = LoggerFactory.getLogger("passpeakthrough/sessions");
	private static final int DEBUG_STATE_EVERY_TICKS = 5;

	private record Session(String playerId, String seed, RunRegions.Region region, BridgePlayer player) {}
	private final RunRegions runs;
	private final InventoryStore inventories;

	private final BridgeServer bridge;
	private final BlockWorld blocks;
	private final com.bornparanoid.peakpassthrough.mirror.WorldMirror mirror;
	private final PlayerActions actions;
	private final EntityFeed entities;
    private final com.bornparanoid.peakpassthrough.world.WorldEvents worldEvents;
    private final com.bornparanoid.peakpassthrough.boss.DragonArena dragonArena;
    private final com.bornparanoid.peakpassthrough.world.LootSpawns lootSpawns;
	private final Map<String,com.bornparanoid.peakpassthrough.mobs.MobEnvironment> environments=new HashMap<>();
    private final Map<String,java.util.List<com.bornparanoid.peakpassthrough.mobs.SpawnHints.Cell>> hintCells=new HashMap<>();
    private final Map<String,String> selected = new HashMap<>();
	private final Map<Integer, Session> sessions = new HashMap<>();
	
	private final Set<String> warnedOnce = new HashSet<>();
	private final Map<Integer,String> avatarHeld=new HashMap<>();
	private long ticks;
    private final com.bornparanoid.peakpassthrough.mobs.MobDirector director;
    private final com.bornparanoid.peakpassthrough.observer.MinecraftObservers observers;
    private final Map<String,Long> envTicks=new HashMap<>(),hintTicks=new HashMap<>();

	public PeakSessions(MinecraftServer server, BridgeServer bridge) {
		this.blocks = new BlockWorld(server, bridge);
        this.observers=new com.bornparanoid.peakpassthrough.observer.MinecraftObservers(server,blocks.level());
        this.director=new com.bornparanoid.peakpassthrough.mobs.MobDirector(server);
		this.bridge = bridge;
        try{runs=new RunRegions(server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-runs.json"));}catch(java.io.IOException e){throw new IllegalStateException("Cannot load run identities; refusing to reset",e);}
        inventories=new InventoryStore(server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-inventories"));
        bridge.outbound((id,line)->{var session=sessions.get(id);if(session==null)return line;var msg=RunSpace.outgoing(session.region(),JsonParser.parseString(line).getAsJsonObject());return msg==null?null:msg.toString();});
		this.mirror=new com.bornparanoid.peakpassthrough.mirror.WorldMirror(blocks);
		this.actions=new PlayerActions(blocks,bridge);
		this.entities=new EntityFeed(blocks,bridge);
        this.lootSpawns=new com.bornparanoid.peakpassthrough.world.LootSpawns(blocks,new com.bornparanoid.peakpassthrough.world.RunEvents(server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-run-events")));
        this.dragonArena=new com.bornparanoid.peakpassthrough.boss.DragonArena(blocks,bridge,new com.bornparanoid.peakpassthrough.world.RunEvents(server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-run-events")));
        this.worldEvents=new com.bornparanoid.peakpassthrough.world.WorldEvents(blocks,new com.bornparanoid.peakpassthrough.world.RunEvents(server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-run-events")));
        com.bornparanoid.peakpassthrough.sound.SoundFeed.sink=(level,x,y,z,event,volume,pitch)->{
            if(level!=blocks.level())return;double range=Math.min(64,event.getRange(volume));
            sessions.forEach((id,session)->{if(!session.player().isSpectator()&&session.player().distanceToSqr(x,y,z)<=range*range)bridge.send(id,com.bornparanoid.peakpassthrough.sound.SoundFeed.message(x,y,z,event,volume,pitch));});
        };
	}

	public void close(){save();mirror.releaseAll();}
	public void save() { blocks.save();sessions.values().forEach(s->inventories.capture(s.seed(),s.playerId(),s.player()));try{inventories.flush();}catch(java.io.IOException e){LOGGER.error("Cannot save native inventories",e);} }

	public int count() {
		return sessions.size();
	}

	public void tick(MinecraftServer server) {
		BridgeServer.Inbound in;
		for(int n=0;n<1024 && (in=bridge.poll())!=null;n++){try{handle(server,in);}catch(RuntimeException e){warnOnce("bad-message","Ignoring malformed bridge message: "+e.getMessage());}}
		if(ticks%20==0)lootSpawns.migrateLadders();
		sessions.forEach((id,s)->actions.tick(id,s.player()));
        boolean avatarChanged=false;for(var entry:sessions.entrySet()){String h=com.bornparanoid.peakpassthrough.inventory.InventoryMessages.avatarHeld(entry.getValue().player());if(!h.equals(avatarHeld.put(entry.getKey(),h)))avatarChanged=true;}
        // A new connection receives the full set, even if the other avatars have not changed.
        if(avatarChanged)for(var source:sessions.entrySet())for(var recipient:sessions.entrySet())if(!source.getKey().equals(recipient.getKey())&&source.getValue().seed().equals(recipient.getValue().seed()))bridge.send(recipient.getKey(),avatarHeld.get(source.getKey()));
        dragonArena.tick(sessions.entrySet().stream().map(e->new com.bornparanoid.peakpassthrough.boss.DragonArena.Peer(e.getKey(),e.getValue().seed(),e.getValue().region(),e.getValue().player())).toList(),ticks);mirror.arenas(dragonArena.bounds());
		if(ticks%20==0)mirror.tick(sessions.values().stream().map(Session::player).toList());
		if(ticks%20==0){
            var active=sessions.values().stream().filter(s->!s.player().isSpectator()).collect(java.util.stream.Collectors.groupingBy(Session::seed));
            var contexts=new java.util.ArrayList<com.bornparanoid.peakpassthrough.mobs.MobDirector.Run>();
            active.forEach((seed,peers)->{if(ticks-envTicks.getOrDefault(seed,-1000L)<=100&&ticks-hintTicks.getOrDefault(seed,-1000L)<=100&&peers.stream().anyMatch(s->!s.playerId().startsWith("photon:")))contexts.add(new com.bornparanoid.peakpassthrough.mobs.MobDirector.Run(seed,peers.getFirst().region(),peers.stream().map(s->(net.minecraft.server.level.ServerPlayer)s.player()).toList(),environments.get(seed),hintCells.get(seed)));});
            director.tick(server,blocks.level(),ticks,contexts);
        }
        entities.tick(sessions.entrySet().stream().collect(java.util.stream.Collectors.toMap(Map.Entry::getKey,e->e.getValue().player())));
		for (String line : blocks.changes(sessions.values().stream().map(Session::region).distinct().toList())) broadcast(line);
        if(ticks%20==0&&!server.getPlayerList().getPlayers().isEmpty())observers.tick(sessions.values().stream().map(s->(net.minecraft.server.level.ServerPlayer)s.player()).toList(),sessions.entrySet().stream().map(e->{var s=e.getValue();var p=s.player();return new com.bornparanoid.peakpassthrough.observer.ObserverPolicy.Anchor(e.getKey(),s.seed(),!p.isSpectator(),s.playerId().startsWith("photon:"),p.getX(),p.getY(),p.getZ());}).toList());
		if (ticks % 20 == 0) save();
		if (++ticks % DEBUG_STATE_EVERY_TICKS == 0) {
			sessions.forEach((id, s) -> bridge.send(id, Protocol.debugState(
					s.player().getX(), s.player().getY(), s.player().getZ(),
					s.player().getYRot(), s.player().getXRot(),
					s.player().level().dimension().identifier().toString())));
		}
	}

	private void handle(MinecraftServer server, BridgeServer.Inbound in) {
		if (in.disconnected()) {
			Session s = sessions.remove(in.clientId());avatarHeld.remove(in.clientId());
            if(s!=null){String empty=com.bornparanoid.peakpassthrough.inventory.InventoryMessages.avatarHeld(s.player().getUUID().toString(),null,false);sessions.forEach((id,other)->{if(s.seed().equals(other.seed()))bridge.send(id,empty);});}
			if(s!=null){var carried=s.player().inventoryMenu.getCarried();if(!carried.isEmpty()){s.player().getInventory().placeItemBackInInventory(carried,net.minecraft.util.Prediction.SERVER_ONLY);s.player().inventoryMenu.setCarried(ItemStack.EMPTY);}inventories.capture(s.seed(),s.playerId(),s.player());try{inventories.flush();}catch(java.io.IOException e){LOGGER.error("Cannot save disconnected inventory",e);}blocks.level().removePlayerImmediately(s.player(),net.minecraft.world.entity.Entity.RemovalReason.DISCARDED);actions.remove(in.clientId(),s.player());mirror.remove(in.clientId());entities.remove(in.clientId());}
			if (s != null) LOGGER.info("PEAK player {} left", s.playerId());
			return;
		}
		JsonObject msg = in.msg();
        var session=sessions.get(in.clientId());if(session!=null)msg=RunSpace.incoming(session.region(),msg);
		String t = msg.get("t").getAsString();
		switch (t) {
			case "hello" -> onHello(server, in.clientId(), msg);
			case "pose" -> onPose(in.clientId(), msg);
			case "action" -> onAction(in.clientId(), msg);
			case "loot_spawn" -> {var s=sessions.get(in.clientId());if(s!=null&&!s.player().isSpectator()&&!s.playerId().startsWith("photon:")){try{lootSpawns.receive(s.seed(),s.region(),msg);}catch(java.io.IOException e){warnOnce("loot:"+s.seed(),"Cannot persist natural loot; refusing spawn: "+e.getMessage());}}}
            case "world_event" -> {var s=sessions.get(in.clientId());if(s!=null&&!s.player().isSpectator()&&!s.playerId().startsWith("photon:")){try{worldEvents.receive(s.seed(),s.region(),s.player(),msg);dragonArena.receive(s.seed(),s.region(),s.player(),msg);}catch(java.io.IOException e){warnOnce("run-events:"+s.seed(),"Cannot persist run event; refusing spawn: "+e.getMessage());}}}
            case "env" -> {var s=sessions.get(in.clientId());if(s!=null&&!s.player().isSpectator()&&!s.playerId().startsWith("photon:")){var env=com.bornparanoid.peakpassthrough.mobs.MobEnvironment.parse(msg);if(env!=null){environments.put(s.seed(),env);envTicks.put(s.seed(),ticks);}}}
            case "spawn_hints" -> {var s=sessions.get(in.clientId());if(s!=null&&!s.player().isSpectator()&&!s.playerId().startsWith("photon:")){var hints=com.bornparanoid.peakpassthrough.mobs.SpawnHints.parse(msg,s.player().getX(),s.player().getY(),s.player().getZ());hintCells.put(s.seed(),hints);hintTicks.put(s.seed(),ticks);var env=environments.get(s.seed());if(env==null||env.deposits())for(var cell:hints){int x=(int)Math.floor(cell.x()),y=(int)Math.floor(cell.y()),z=(int)Math.floor(cell.z());if(s.region().owns(x,z)&&com.bornparanoid.peakpassthrough.world.BlockDeposits.candidate(x-s.region().x(),z-s.region().z()))blocks.deposit(s.seed(),x,y,z);}}}
            case "terrain" -> {var s=sessions.get(in.clientId());if(s!=null){if(msg.has("convert")){var conversion=com.bornparanoid.peakpassthrough.terraform.TerrainConversion.parse(msg,s.player().getX(),s.player().getY(),s.player().getZ());if(!s.player().isSpectator()&&blocks.convert(conversion,s.region())){boolean quick=msg.has("quick")&&msg.get("quick").getAsBoolean();sessions.forEach((id,peer)->{if(peer.seed().equals(s.seed())){if(quick)blocks.quickSnapshot(id,conversion.volume());else blocks.snapshot(id,peer.region());}});}}else mirror.apply(in.clientId(),com.bornparanoid.peakpassthrough.mirror.TerrainSamples.parse(msg),s.player());}}
			default -> warnOnce("type:" + t, "Ignoring unknown message type '" + t + "'");
		}
	}

	private void onHello(MinecraftServer server, int clientId, JsonObject msg) {
		if (sessions.containsKey(clientId)) return;
		Handshake.Result r = Handshake.check(msg);
		if (!r.ok()) {
			LOGGER.warn("Rejecting PEAK client {}: {}", clientId, r.reason());
			bridge.sendAndClose(clientId, Protocol.error(r.reason()));
			return;
		}
		String seed=msg.has("mapSeed")?msg.get("mapSeed").getAsString():"dev";
        RunRegions.Region region;
        try{region=runs.region(seed);}catch(Exception e){bridge.sendAndClose(clientId,Protocol.error("invalid mapSeed"));return;}
        if(sessions.values().stream().anyMatch(s->s.seed().equals(seed)&&s.playerId().equals(r.playerId()))){bridge.sendAndClose(clientId,Protocol.error("player already connected in this run"));return;}
        UUID uuid = UUID.nameUUIDFromBytes(("peak:"+seed+"\0"+r.playerId()).getBytes(UTF_8));
		BridgePlayer player = new BridgePlayer(blocks.level(),new GameProfile(uuid,Handshake.profileName(r.playerId())),(amount,source,knock)->{String line=CombatMessages.damage(amount,source,knock);if(line!=null)bridge.send(clientId,line);},(nutrition,saturation)->{String line=FoodMessages.eat(nutrition,saturation);if(line!=null)bridge.send(clientId,line);});
        player.effects=line->bridge.send(clientId,line);
        player.bridgeTeleport=pos->{var o=new JsonObject();o.addProperty("t","teleport");var a=new com.google.gson.JsonArray();a.add(pos.x);a.add(pos.y);a.add(pos.z);o.add("pos",a);bridge.send(clientId,o.toString());};
        try{inventories.restore(seed,r.playerId(),player);}catch(java.io.IOException e){LOGGER.error("Cannot restore inventory",e);bridge.sendAndClose(clientId,Protocol.error("saved inventory unavailable"));return;}
        player.gameMode.changeGameModeForPlayer(net.minecraft.world.level.GameType.SPECTATOR);
        player.snapTo(region.x()+.5,402.4,region.z()+.5,0,0);
        observers.beforeAvatarAdded(player);
        blocks.level().addNewPlayer(player);
		sessions.put(clientId, new Session(r.playerId(),seed,region,player));
		var welcome=JsonParser.parseString(Protocol.welcome(server.getServerVersion())).getAsJsonObject();welcome.addProperty("playerUuid",uuid.toString());welcome.addProperty("mapSeed",seed);bridge.send(clientId,welcome.toString());
		blocks.snapshot(clientId,region);
		actions.sync(clientId,player);
		LOGGER.info("PEAK player {} joined as {}", r.playerId(), player.getGameProfile().name());
	}

	private void onPose(int clientId, JsonObject msg) {
		Session s = sessions.get(clientId);
		if (s == null) {
			warnOnce("pose-before-hello", "Ignoring pose from client " + clientId + " before hello");
			return;
		}
        if(msg.has("active")&&msg.get("active").isJsonPrimitive()&&msg.get("active").getAsJsonPrimitive().isBoolean()&&!msg.get("active").getAsBoolean()){s.player().gameMode.changeGameModeForPlayer(net.minecraft.world.level.GameType.SPECTATOR);actions.pause(clientId,s.player());return;}
		Pose p = Pose.fromJson(msg);
		if (p == null) {
			warnOnce("bad-pose", "Ignoring malformed pose (further ones dropped silently)");
			return;
		}
        s.player().gameMode.changeGameModeForPlayer(net.minecraft.world.level.GameType.SURVIVAL);
		var previousChunk=s.player().chunkPosition();
		double[] mc = Coords.unityToMc(p.x(), p.y(), p.z());
		s.player().snapTo(mc[0], mc[1], mc[2], Coords.unityYawToMc(p.yaw()), Coords.unityPitchToMc(p.pitch()));
        if(!previousChunk.equals(s.player().chunkPosition()))mirror.tick(sessions.values().stream().map(Session::player).toList());
	}

    private void broadcast(String line) { sessions.keySet().forEach(id -> bridge.send(id, line)); }
    private void sendHeld(int client,String item) {
        JsonObject o=new JsonObject();o.addProperty("t","held");o.addProperty("item",item);bridge.send(client,o.toString());
    }
    private void onAction(int client,JsonObject msg) {
        Session s=sessions.get(client);if(s==null)return;
        try {
            if(!msg.has("kind")||!msg.get("kind").getAsJsonPrimitive().isString()||!msg.has("target")||!msg.get("target").isJsonObject())return;
            String kind=msg.get("kind").getAsString();JsonObject target=msg.getAsJsonObject("target");
            if(kind.equals("impact")&&s.playerId().startsWith("photon:")&&target.has("source")&&target.get("source").getAsString().equals("explosion"))return;
            if(kind.equals("place")){String line=blocks.place(BlockMessages.parsePlace(target),s.player(),selected.get(s.seed()+"\0"+s.playerId()));if(line!=null)broadcast(line);}
            else {actions.action(client,s.player(),kind,target);if(kind.equals("creative_give")&&target.has("block"))selected.put(s.seed()+"\0"+s.playerId(),target.get("block").getAsString());}
        } catch(RuntimeException e) { warnOnce("bad-action","Ignoring malformed action: "+e.getMessage()); }
    }

	private void warnOnce(String key, String message) {
		if (warnedOnce.add(key)) LOGGER.warn(message);
	}
}
