package com.bornparanoid.peakpassthrough.client;

import net.fabricmc.api.ClientModInitializer;

public class PassPeakThroughClient implements ClientModInitializer {
	@Override
	public void onInitializeClient() {
		net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientLifecycleEvents.CLIENT_STARTED.register(client->{try{com.bornparanoid.peakpassthrough.client.export.EntityExporter.export();}catch(Exception e){com.bornparanoid.peakpassthrough.PassPeakThrough.LOGGER.error("Entity model export failed",e);}finally{if(Boolean.getBoolean("peak.setup.exportOnly"))client.stop();}});
	}
}