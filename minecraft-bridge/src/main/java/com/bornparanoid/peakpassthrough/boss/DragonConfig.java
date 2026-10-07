package com.bornparanoid.peakpassthrough.boss;
import com.google.gson.*;
public record DragonConfig(int triggerSegment,float maxHealth,int orbit) {
 public static DragonConfig parse(JsonObject o){if(o==null)return new DragonConfig(1,120,60);return new DragonConfig((int)number(o,"triggerSegment",1,1,4,true),(float)number(o,"maxHealth",120,20,1000,false),(int)number(o,"orbit",60,20,160,true));}
 private static double number(JsonObject o,String k,double def,double min,double max,boolean integer){if(!o.has(k))return def;var t=o.get(k);if(!t.isJsonPrimitive()||!t.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException(k);double n=t.getAsDouble();if(!Double.isFinite(n)||n<min||n>max||(integer&&n!=Math.rint(n)))throw new IllegalArgumentException(k);return n;}
}
