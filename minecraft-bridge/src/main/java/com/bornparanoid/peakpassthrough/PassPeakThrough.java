package com.bornparanoid.peakpassthrough;

import com.bornparanoid.peakpassthrough.bridge.BridgeServer;
import com.bornparanoid.peakpassthrough.bridge.Protocol;
import com.bornparanoid.peakpassthrough.session.PeakSessions;
import java.io.IOException;
import java.net.BindException;
import net.fabricmc.api.ModInitializer;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerLifecycleEvents;
import net.fabricmc.fabric.api.event.lifecycle.v1.ServerTickEvents;
import net.minecraft.resources.Identifier;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class PassPeakThrough implements ModInitializer {
	public static final String MOD_ID = "passpeakthrough";
	public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

	private BridgeServer bridge;
	private PeakSessions sessions;

	@Override
	public void onInitialize() {
		ServerLifecycleEvents.SERVER_STARTED.register(server -> {
			if (server instanceof net.minecraft.server.dedicated.DedicatedServer dedicated) {
				dedicated.setPauseWhenEmptySeconds(0);
                dedicated.setUsingWhitelist(false);
                LOGGER.info("Minecraft spectator observers enabled; account whitelist disabled");
				LOGGER.info("Empty-server pause disabled: bridge FakePlayers are not vanilla connections");
			}
			LOGGER.info("PassPeakThrough server mod ready (Minecraft {})", server.getServerVersion());
			int port = Integer.getInteger("peakbridge.port", Protocol.DEFAULT_PORT);
			BridgeServer b = new BridgeServer(port);
			try {
				b.start();
				bridge = b;
				sessions = new PeakSessions(server, b);
			} catch (RuntimeException e) {
				b.stop();
				LOGGER.error("PEAK block bridge disabled: {}", e.toString());
			} catch (BindException e) {
				LOGGER.error("PEAK bridge disabled: port {} is already in use (is another server running?)", port);
			} catch (IOException e) {
				LOGGER.error("PEAK bridge disabled: {}", e.toString());
			}
		});
		ServerTickEvents.END_SERVER_TICK.register(server -> {
			if (sessions != null) sessions.tick(server);
		});
		ServerLifecycleEvents.SERVER_STOPPING.register(server -> {
			if (sessions != null) sessions.close();
			com.bornparanoid.peakpassthrough.sound.SoundFeed.sink=null;
			if (bridge != null) bridge.stop();
			bridge = null;
			sessions = null;
		});
	}

	public static Identifier id(String path) {
		return Identifier.fromNamespaceAndPath(MOD_ID, path);
	}
}
