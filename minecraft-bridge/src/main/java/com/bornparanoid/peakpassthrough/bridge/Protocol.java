package com.bornparanoid.peakpassthrough.bridge;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import com.google.gson.JsonParseException;
import com.google.gson.JsonParser;

/** Wire format shared with the PEAK plugin: one JSON object per line, string field "t" = message type. */
public final class Protocol {
	public static final int VERSION = 2;
	public static final int DEFAULT_PORT = 47655;

	private Protocol() {}

	/** Returns the message, or null if the line is not a JSON object with a string "t". */
	public static JsonObject parse(String line) {
		try {
			JsonElement e = JsonParser.parseString(line);
			if (!e.isJsonObject()) return null;
			JsonObject o = e.getAsJsonObject();
			JsonElement t = o.get("t");
			if (t == null || !t.isJsonPrimitive() || !t.getAsJsonPrimitive().isString()) return null;
			return o;
		} catch (JsonParseException | IllegalStateException e) {
			return null;
		}
	}

	public static String welcome(String mcVersion) {
		JsonObject o = type("welcome");
		o.addProperty("protocol", VERSION);
		o.addProperty("mcVersion", mcVersion);
		return o.toString();
	}

	public static String error(String reason) {
		JsonObject o = type("error");
		o.addProperty("reason", reason);
		return o.toString();
	}

	public static String debugState(double x, double y, double z, float yaw, float pitch, String dimension) {
		JsonObject o = type("debug_state");
		o.addProperty("x", x);
		o.addProperty("y", y);
		o.addProperty("z", z);
		o.addProperty("yaw", yaw);
		o.addProperty("pitch", pitch);
		o.addProperty("dim", dimension);
		return o.toString();
	}

	private static JsonObject type(String t) {
		JsonObject o = new JsonObject();
		o.addProperty("t", t);
		return o;
	}
}
