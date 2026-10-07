package com.bornparanoid.peakpassthrough.client.export;
import com.google.gson.*;
import java.lang.reflect.Field;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;
import net.minecraft.client.model.geom.*;
/** Export baked vanilla model data from the user's running client; no game data shipped. */
public final class EntityExporter {
 private static final Gson GSON=new GsonBuilder().setPrettyPrinting().create();
 private static JsonArray vector(float... v){var a=new JsonArray();for(float n:v)a.add(n);return a;}
 @SuppressWarnings("unchecked") private static JsonObject part(String name,ModelPart p)throws ReflectiveOperationException{
  var o=new JsonObject();o.addProperty("name",name);o.add("pivot",vector(p.x,p.y,p.z));o.add("rot",vector(p.xRot,p.yRot,p.zRot));o.add("scale",vector(p.xScale,p.yScale,p.zScale));var faces=new JsonArray();
  Field cubes=ModelPart.class.getDeclaredField("cubes");cubes.setAccessible(true);
  for(var cube:(List<ModelPart.Cube>)cubes.get(p))for(var polygon:cube.polygons){var q=new JsonArray();for(var v:polygon.vertices())q.add(vector(v.x(),v.y(),v.z(),v.u(),v.v()));faces.add(q);}o.add("faces",faces);
  Field children=ModelPart.class.getDeclaredField("children");children.setAccessible(true);var out=new JsonArray();for(var entry:((Map<String,ModelPart>)children.get(p)).entrySet().stream().sorted(Map.Entry.comparingByKey()).toList())out.add(part(entry.getKey(),entry.getValue()));o.add("children",out);return o;
 }
 public static void export()throws Exception{
  Path root=Path.of(System.getProperty("peak.assets.cache",System.getenv("PEAK_ASSETS_CACHE")!=null?System.getenv("PEAK_ASSETS_CACHE"):System.getProperty("user.home")+"/.local/share/PeakCreativeMode/asset-cache/26.3"));Path dir=root.resolve("entity-models");Files.createDirectories(dir);var set=EntityModelSet.vanilla();
  var layers=Map.of("zombie",ModelLayers.ZOMBIE,"skeleton",ModelLayers.SKELETON,"cow",ModelLayers.COW,"arrow",ModelLayers.ARROW,"spider",ModelLayers.SPIDER,"creeper",ModelLayers.CREEPER,"pig",ModelLayers.PIG,"chicken",ModelLayers.CHICKEN,"ender_dragon",ModelLayers.ENDER_DRAGON);var textures=Map.of("zombie","zombie/zombie","skeleton","skeleton/skeleton","cow","cow/cow_temperate","arrow","projectiles/arrow","spider","spider/spider","creeper","creeper/creeper","pig","pig/pig_temperate","chicken","chicken/chicken_temperate","ender_dragon","enderdragon/dragon");var digest=MessageDigest.getInstance("SHA-256");
  for(String name:new TreeSet<>(layers.keySet())){var o=new JsonObject();o.addProperty("mcVersion","26.3");o.addProperty("type","minecraft:"+name);o.addProperty("texture","assets/minecraft/textures/entity/"+textures.get(name)+".png");o.add("root",part("root",set.bakeLayer(layers.get(name))));byte[] data=GSON.toJson(o).getBytes(java.nio.charset.StandardCharsets.UTF_8);Files.write(dir.resolve(name+".json"),data);digest.update(name.getBytes(java.nio.charset.StandardCharsets.UTF_8));digest.update(data);}
  var manifest=new JsonObject();manifest.addProperty("mcVersion","26.3");manifest.addProperty("models",layers.size());manifest.addProperty("hash",HexFormat.of().formatHex(digest.digest()));Files.writeString(dir.resolve("manifest.json"),GSON.toJson(manifest));System.out.println("[PeakAssets] exported 9 baked entity models to "+dir);
 }
}
