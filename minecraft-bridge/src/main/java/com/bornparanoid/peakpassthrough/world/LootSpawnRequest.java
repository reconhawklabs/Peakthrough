package com.bornparanoid.peakpassthrough.world;
import com.google.gson.*;
public record LootSpawnRequest(String key,String item,int count,String potion,double x,double y,double z) {
 private static double number(JsonElement e){if(e==null||!e.isJsonPrimitive()||!e.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException();double n=e.getAsDouble();if(!Double.isFinite(n))throw new IllegalArgumentException();return n;}
 private static String id(JsonElement e){String s=e.getAsString();if(!s.matches("[a-z0-9_.-]+:[a-z0-9_/.-]+")||s.length()>128)throw new IllegalArgumentException();return s;}
 public static LootSpawnRequest parse(JsonObject o){try{String key=o.get("key").getAsString();if(key.isBlank()||key.length()>256)return null;double count=number(o.get("count"));if(count!=Math.rint(count)||count<1||count>64)return null;var p=o.getAsJsonArray("pos");if(p.size()!=3)return null;double x=number(p.get(0)),y=number(p.get(1)),z=-number(p.get(2));if(Math.abs(x)>30000000||Math.abs(z)>30000000||y< -2030||y>2030)return null;String potion=o.has("components")&&o.getAsJsonObject("components").has("potion")?id(o.getAsJsonObject("components").get("potion")):null;return new LootSpawnRequest(key,id(o.get("item")),(int)count,potion,x,y,z);}catch(RuntimeException e){return null;}}
}
