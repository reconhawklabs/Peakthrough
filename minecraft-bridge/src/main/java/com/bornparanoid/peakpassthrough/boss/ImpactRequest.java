package com.bornparanoid.peakpassthrough.boss;
import com.google.gson.*;
/** Validated physical inputs; clients cannot supply damage or non-player sources. */
public record ImpactRequest(boolean explosion,int entityId,String part,double x,double y,double z,double radius,double mass,double speed) {
 public static final String[] PARTS={"head","neck","body","tail1","tail2","tail3","wing1","wing2"};
 public static int partIndex(String part){for(int i=0;i<PARTS.length;i++)if(PARTS[i].equals(part))return i;return -1;}
 private static double number(JsonObject o,String k,double min,double max){var t=o.get(k);if(t==null||!t.isJsonPrimitive()||!t.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException(k);double n=t.getAsDouble();if(!Double.isFinite(n)||n<min||n>max)throw new IllegalArgumentException(k);return n;}
 public static ImpactRequest parse(JsonObject o){try{String source=o.get("source").getAsString();if(source.equals("explosion")){if(number(o,"power",20,20)!=20)return null;return new ImpactRequest(true,0,null,number(o,"x",-30000000,30000000),number(o,"y",-2032,2031),number(o,"z",-30000000,30000000),number(o,"radius",.01,32),0,0);}if(!source.equals("thrown"))return null;int id=com.bornparanoid.peakpassthrough.session.CombatMessages.entityId(o);String part=o.has("part")?o.get("part").getAsString():null;if(id<1||(part!=null&&partIndex(part)<0))return null;return new ImpactRequest(false,id,part,0,0,0,0,number(o,"mass",.001,100),number(o,"speed",0,100));}catch(RuntimeException e){return null;}}
}
