package com.bornparanoid.peakpassthrough.boss;
public final class Impacts {
 public static float damage(double power,double distance,double radius){if(!Double.isFinite(power)||!Double.isFinite(distance)||!Double.isFinite(radius)||power<=0||distance<0||radius<=0||distance>=radius)return 0;return (float)Math.min(40,power*(1-distance/radius));}
 public static float thrown(double massKg,double speed){if(!Double.isFinite(massKg)||!Double.isFinite(speed)||massKg<=0||speed<6)return 0;return (float)Math.min(12,.5*massKg*speed*speed*.02);}
}
