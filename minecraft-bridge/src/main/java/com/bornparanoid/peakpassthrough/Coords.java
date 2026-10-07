package com.bornparanoid.peakpassthrough;

/**
 * The only Unity→Minecraft conversion on the Minecraft side.
 * Unity: Y-up, left-handed, +Z forward. Minecraft: Y-up, +Z south. One axis (Z) is flipped.
 */
public final class Coords {
	/** Unity units per Minecraft block. Validated at the Phase 2 checkpoint. */
	public static final double BLOCK_SIZE = 1.0;

	private Coords() {}

	public static double[] unityToMc(double ux, double uy, double uz) {
		return new double[] {ux / BLOCK_SIZE, uy / BLOCK_SIZE, -uz / BLOCK_SIZE};
	}

	/** Unity yaw (degrees clockwise from +Z, seen from above) → Minecraft yRot. */
	public static float unityYawToMc(float unityYaw) {
		return wrapDegrees(unityYaw + 180f);
	}

	/** Unity pitch (eulerAngles.x, 0..360, positive = down) → Minecraft xRot (-90..90, positive = down). */
	public static float unityPitchToMc(float unityPitch) {
		return wrapDegrees(unityPitch);
	}

	/** Wraps to [-180, 180). */
	public static float wrapDegrees(float deg) {
		float d = deg % 360f;
		if (d >= 180f) d -= 360f;
		if (d < -180f) d += 360f;
		return d;
	}
}
