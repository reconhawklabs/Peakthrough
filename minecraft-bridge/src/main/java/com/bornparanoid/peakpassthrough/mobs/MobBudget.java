package com.bornparanoid.peakpassthrough.mobs;
/** Pure spawn and cleanup boundaries, in server ticks. */
public final class MobBudget {
 public static boolean allow(int hostileNear,int passiveNear,int total,long lastSpawnTick,long tick,boolean hostile){return allow(hostileNear,passiveNear,total,lastSpawnTick,tick,hostile,6,6,24);}
 public static boolean allow(int hostileNear,int passiveNear,int total,long lastSpawnTick,long tick,boolean hostile,int hostileCap,int passiveCap,int runCap){return hostileCap>=0&&hostileCap<=64&&passiveCap>=0&&passiveCap<=64&&runCap>=0&&runCap<=128&&tick-lastSpawnTick>=100&&total<runCap&&(hostile?hostileNear<hostileCap:passiveNear<passiveCap);}
 public static boolean despawn(boolean hostile,double nearestPlayerDist,boolean day,boolean open,long aliveTicks){return nearestPlayerDist>64||(hostile&&day&&open&&aliveTicks>600);}
}
