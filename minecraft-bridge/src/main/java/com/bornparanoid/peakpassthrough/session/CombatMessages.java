package com.bornparanoid.peakpassthrough.session;
import com.google.gson.JsonObject;
public final class CombatMessages {
 public static int entityId(JsonObject o){try{return PlayerActions.integer(o,"entityId",1,Integer.MAX_VALUE);}catch(RuntimeException e){return -1;}}
 public static boolean withinReach(double distanceSquared){return withinReach(distanceSquared,6);}
 public static boolean withinReach(double distanceSquared,double radius){return Double.isFinite(radius)&&radius>=0&&Double.isFinite(distanceSquared)&&distanceSquared>=0&&distanceSquared<=radius*radius;}
 public static String damage(float amount,String source){return damage(amount,source,null);}
 public static String damage(float amount,String source,double[] knock){if(!Float.isFinite(amount)||amount<=0)return null;var o=new JsonObject();o.addProperty("t","damage");o.addProperty("amount",Math.min(amount,1000));o.addProperty("source",source);if(knock!=null&&knock.length==3&&java.util.Arrays.stream(knock).allMatch(Double::isFinite)){var a=new com.google.gson.JsonArray();for(double v:knock)a.add(v);o.add("knock",a);}return o.toString();}
}
