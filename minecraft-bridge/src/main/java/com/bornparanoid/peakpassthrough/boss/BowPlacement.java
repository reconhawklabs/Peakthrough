package com.bornparanoid.peakpassthrough.boss;
import com.google.gson.*;
import com.bornparanoid.peakpassthrough.dimension.RunRegions;
import com.bornparanoid.peakpassthrough.world.WorldEvents;
import net.minecraft.world.phys.Vec3;
public final class BowPlacement {
 public static int segment(JsonElement e){if(e==null||!e.isJsonPrimitive())return -1;var p=e.getAsJsonPrimitive();if(p.isString())return switch(p.getAsString()){case "Beach"->0;case "Tropics"->1;case "Alpine"->2;case "Caldera"->3;case "TheKiln"->4;case "Peak"->5;default->-1;};if(!p.isNumber())return -1;double n=p.getAsDouble();return Double.isFinite(n)&&n==Math.rint(n)&&n>=0&&n<=5?(int)n:-1;}
 public static Vec3 find(JsonArray fires,int trigger,RunRegions.Region region){if(fires==null||fires.size()>16)return null;for(var token:fires){try{var o=token.getAsJsonObject();if(segment(o.get("segment"))!=trigger)continue;var fire=WorldEvents.position(o.get("pos"),region);var bow=WorldEvents.position(o.get("bowPos"),region);if(fire!=null&&bow!=null&&fire.distanceToSqr(bow)<=64)return bow;}catch(RuntimeException ignored){}}return null;}
}
