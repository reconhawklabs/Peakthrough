package com.bornparanoid.peakpassthrough.mirror;
import com.bornparanoid.peakpassthrough.Coords;
import com.google.gson.*;
import java.util.*;
/** Terrain origin and voxel offsets are Unity-space; only this decoder invokes Coords. */
public final class TerrainSamples {
 public record Cell(int x,int y,int z){}
 public record Batch(List<Cell> solid,List<Cell> water){}
 public static int[] cell(double ox,double oy,double oz,int x,int y,int z){double[] p=Coords.unityToMc(ox+x+.5,oy+y+.5,oz+z+.5);return new int[]{(int)Math.floor(p[0]),(int)Math.floor(p[1]),(int)Math.floor(p[2])};}
 public static Batch parse(JsonObject o){try{
  var origin=o.getAsJsonArray("origin");var size=o.getAsJsonArray("size");if(origin.size()!=3||size.size()!=3)return null;
  double[] base=new double[3];for(int i=0;i<3;i++){base[i]=number(origin.get(i));double v=number(size.get(i));if(v<=0||v!=Math.rint(v))return null;}
  var solid=read(o.getAsJsonArray("solid"),base);var water=read(o.getAsJsonArray("water"),base);
  if(solid.size()+water.size()>2048)return null;return new Batch(List.copyOf(solid),List.copyOf(water));
 }catch(RuntimeException e){return null;}}
 private static List<Cell> read(JsonArray a,double[] base){if(a==null||a.size()>2048)throw new IllegalArgumentException();var result=new ArrayList<Cell>();for(var v:a){var xyz=v.getAsJsonArray();if(xyz.size()!=3)throw new IllegalArgumentException();int[] off=new int[3];for(int i=0;i<3;i++){double n=number(xyz.get(i));if(n!=Math.rint(n))throw new IllegalArgumentException();off[i]=(int)n;}int[] p=cell(base[0],base[1],base[2],off[0],off[1],off[2]);result.add(new Cell(p[0],p[1],p[2]));}return result;}
 private static double number(JsonElement e){if(!e.isJsonPrimitive()||!e.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException();double n=e.getAsDouble();if(!Double.isFinite(n)||Math.abs(n)>30000000)throw new IllegalArgumentException();return n;}
}
