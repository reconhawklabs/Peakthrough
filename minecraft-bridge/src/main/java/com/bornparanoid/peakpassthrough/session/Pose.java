package com.bornparanoid.peakpassthrough.session;

import com.google.gson.JsonElement;
import com.google.gson.JsonObject;

/** Player pose in Unity space, as sent by PEAK. */
public record Pose(double x, double y, double z, float yaw, float pitch) {
	public static Pose fromJson(JsonObject o) {
		Double x = num(o, "x"), y = num(o, "y"), z = num(o, "z"), yaw = num(o, "yaw"), pitch = num(o, "pitch");
		if (x == null || y == null || z == null || yaw == null || pitch == null
                || !Float.isFinite(yaw.floatValue()) || !Float.isFinite(pitch.floatValue())) return null;
		return new Pose(x, y, z, yaw.floatValue(), pitch.floatValue());
	}

	private static Double num(JsonObject o, String key) {
		JsonElement e = o.get(key);
		if (e == null || !e.isJsonPrimitive() || !e.getAsJsonPrimitive().isNumber()) return null;
		double d = e.getAsDouble();
		return Double.isFinite(d) ? d : null;
	}
}
