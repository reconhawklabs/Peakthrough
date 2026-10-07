package com.bornparanoid.peakpassthrough.world;
/** Old natural drops alone use both no gravity and Minecraft's unlimited-age sentinel. */
public final class LadderReplacement {
 public static int pearls(String item,int count,boolean pinned,int age){return "minecraft:ladder".equals(item)&&count>0&&count<=64&&pinned&&age==-32768?(count+7)/8:0;}
}
