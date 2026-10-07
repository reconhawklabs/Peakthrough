package com.bornparanoid.peakpassthrough.session;
import com.google.gson.JsonObject;
public final class EffectFeed {
 public static String message(String id,double seconds,int amp){if(id==null||!id.matches("[a-z0-9_.-]+:[a-z0-9_/.-]+")||id.length()>128||!Double.isFinite(seconds)||seconds<=0)return null;var o=new JsonObject();o.addProperty("t","effect");o.addProperty("id",id);o.addProperty("seconds",Math.min(86400,seconds));o.addProperty("amplifier",Math.max(0,Math.min(4,amp)));return o.toString();}
}
