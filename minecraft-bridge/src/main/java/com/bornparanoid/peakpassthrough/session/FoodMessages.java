package com.bornparanoid.peakpassthrough.session;
import com.google.gson.JsonObject;
public final class FoodMessages {
 public static String eat(int nutrition,float saturation){if(nutrition<=0||nutrition>100||!Float.isFinite(saturation)||saturation<0)return null;var o=new JsonObject();o.addProperty("t","eat");o.addProperty("nutrition",nutrition);o.addProperty("saturation",saturation);return o.toString();}
}
