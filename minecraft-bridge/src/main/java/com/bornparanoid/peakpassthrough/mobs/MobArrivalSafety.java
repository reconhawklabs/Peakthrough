package com.bornparanoid.peakpassthrough.mobs;
public final class MobArrivalSafety {
 public static boolean spawn(boolean hostile,double distance,long elapsed){return !hostile||elapsed>=600&&distance>=16;}
 public static boolean clear(boolean hostile,double distance,long elapsed){return hostile&&elapsed<600&&distance<16;}
}
