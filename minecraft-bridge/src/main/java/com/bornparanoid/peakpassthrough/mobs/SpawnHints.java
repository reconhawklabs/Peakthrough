package com.bornparanoid.peakpassthrough.mobs;
import com.google.gson.*;
import java.util.*;
/** Input positions are Unity space, already offset to the run by RunSpace. */
public final class SpawnHints {
 public record Cell(double x,double y,double z,boolean dark,boolean open){}
 public static List<Cell> parse(JsonObject o,double px,double py,double pz){try{if(!(o.get("cells") instanceof JsonArray a)||a.size()>64)return List.of();var result=new ArrayList<Cell>();for(var token:a){if(!(token instanceof JsonArray p)||p.size()!=5)return List.of();double[] n=new double[5];for(int i=0;i<5;i++){if(!p.get(i).isJsonPrimitive()||!p.get(i).getAsJsonPrimitive().isNumber())return List.of();n[i]=p.get(i).getAsDouble();if(!Double.isFinite(n[i]))return List.of();}if(n[3]!=0&&n[3]!=1||n[4]!=0&&n[4]!=1)return List.of();double x=n[0],y=n[1],z=-n[2];if(Math.abs(x)>30000000||Math.abs(z)>30000000||y< -2030||y>2030||(x-px)*(x-px)+(y-py)*(y-py)+(z-pz)*(z-pz)>48*48)return List.of();result.add(new Cell(x,y,z,n[3]==1,n[4]==1));}return List.copyOf(result);}catch(RuntimeException e){return List.of();}}
}
