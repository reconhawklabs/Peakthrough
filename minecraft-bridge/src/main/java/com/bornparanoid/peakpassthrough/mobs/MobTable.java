package com.bornparanoid.peakpassthrough.mobs;
import java.util.*;
/** Which native mob a PEAK spawn hint gets. roll in [0,1). Returns null for no spawn. */
public final class MobTable {
 public enum Biome{SHORE,TROPICS,ROOTS,ALPINE,MESA,VOLCANO,OTHER;public static Biome of(String s){try{return valueOf(s.toUpperCase(Locale.ROOT));}catch(RuntimeException e){return switch(s==null?"":s){case "Caldera","TheKiln","Volcano"->VOLCANO;default->OTHER;};}}}
 private record W(String type,double w){}
 private static final List<W> HOSTILE=List.of(new W("minecraft:zombie",.35),new W("minecraft:skeleton",.30),new W("minecraft:spider",.20),new W("minecraft:creeper",.15));
 private static final Map<Biome,List<W>> DAY=Map.of(
  Biome.SHORE,List.of(new W("minecraft:chicken",.5),new W("minecraft:pig",.5)),
  Biome.TROPICS,List.of(new W("minecraft:pig",.3),new W("minecraft:chicken",.3),new W("minecraft:cow",.15),new W("minecraft:spider",.15),new W("minecraft:creeper",.10)),
  Biome.ROOTS,List.of(new W("minecraft:spider",.4),new W("minecraft:creeper",.2),new W("minecraft:cow",.2),new W("minecraft:pig",.2)),
  Biome.ALPINE,List.of(new W("minecraft:cow",.5),new W("minecraft:creeper",.25),new W("minecraft:spider",.25)),
  Biome.MESA,List.of(new W("minecraft:creeper",.3),new W("minecraft:spider",.3),new W("minecraft:cow",.2),new W("minecraft:chicken",.2)));
 public static String pick(Biome b,boolean night,boolean dark,double roll){if(!(roll>=0&&roll<1))return null;List<W> t=dark||night?HOSTILE:DAY.get(b);if(t==null||(b==Biome.VOLCANO&&!dark))return null;double acc=0;for(var w:t){acc+=w.w();if(roll<acc)return w.type();}return t.get(t.size()-1).type();}
 public static boolean hostile(String type){return HOSTILE.stream().anyMatch(w->w.type().equals(type));}
}
