package com.bornparanoid.peakpassthrough.session;

import com.bornparanoid.peakpassthrough.bridge.Protocol;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

public final class Handshake {
	public record Result(boolean ok, String playerId, String reason) {
		static Result fail(String reason) {
			return new Result(false, null, reason);
		}
	}

	private Handshake() {}

	public static Result check(JsonObject hello) {
		JsonElement p = hello.get("protocol");
		if (p == null || !p.isJsonPrimitive() || !p.getAsJsonPrimitive().isNumber()) return Result.fail("missing protocol");
		double rawVersion = p.getAsDouble();
        if (!Double.isFinite(rawVersion) || rawVersion != Math.rint(rawVersion) || rawVersion < 0 || rawVersion > Integer.MAX_VALUE) return Result.fail("invalid protocol");
		int version = (int) rawVersion;
		if (version != Protocol.VERSION)
			return Result.fail("protocol mismatch: server " + Protocol.VERSION + ", client " + version);
		JsonElement id = hello.get("playerId");
		if (id == null || !id.isJsonPrimitive() || !id.getAsJsonPrimitive().isString() || id.getAsString().isBlank())
			return Result.fail("missing playerId");
		if(id.getAsString().length()>128||id.getAsString().indexOf('\0')>=0)return Result.fail("invalid playerId");
        if(hello.has("mapSeed")){var seed=hello.get("mapSeed");if(!seed.isJsonPrimitive()||!seed.getAsJsonPrimitive().isString()||seed.getAsString().isBlank()||seed.getAsString().length()>128||seed.getAsString().indexOf('\0')>=0)return Result.fail("invalid mapSeed");}
        return new Result(true, id.getAsString(), null);
	}

	/** Minecraft profile names: max 16 chars of [A-Za-z0-9_]. */
	public static String profileName(String playerId) {
		String clean = playerId.replaceAll("[^A-Za-z0-9_]", "");
		String name = "pk_" + clean;
		return name.length() > 16 ? name.substring(0, 16) : name;
	}
}
