package com.bornparanoid.peakpassthrough.blocks;
import com.google.gson.*;
import java.util.*;
import java.util.function.Function;
public final class BlockMessages {
    public static final List<String> PICKER=List.of("minecraft:stone","minecraft:cobblestone","minecraft:oak_planks","minecraft:dirt","minecraft:bricks","minecraft:sandstone","minecraft:oak_log","minecraft:white_wool");
    public record Chunk(int x,int y,int z){}
    public record Place(BlockIndex.Pos pos,String block){}
    public static Place parsePlace(JsonObject target){
        try{
            if(target==null||!target.has("block")||!target.get("block").getAsJsonPrimitive().isString())return null;
            String block=target.get("block").getAsString();if(!PICKER.contains(block))return null;
            return new Place(new BlockIndex.Pos(integer(target,"x"),integer(target,"y"),integer(target,"z")),block);
        }catch(RuntimeException e){return null;}
    }
    private static int integer(JsonObject o,String name){
        JsonElement e=o.get(name);if(e==null||!e.isJsonPrimitive()||!e.getAsJsonPrimitive().isNumber())throw new IllegalArgumentException();
        double n=e.getAsDouble();if(!Double.isFinite(n)||n!=Math.rint(n)||Math.abs(n)>30000000)throw new IllegalArgumentException();return (int)n;
    }
    public static boolean withinReach(Place p,double x,double y,double z){
        double dx=p.pos().x()+.5-x,dy=p.pos().y()+.5-(y+1.6),dz=p.pos().z()+.5-z;
        return dx*dx+dy*dy+dz*dz<=36; // camera ray 4.5 plus body/eye and target-centre tolerance
    }
    public static String block(BlockIndex.Pos p,String state){
        JsonObject o=new JsonObject();o.addProperty("t","block");o.addProperty("x",p.x());o.addProperty("y",p.y());o.addProperty("z",p.z());o.addProperty("state",state);return o.toString();
    }
    public static List<String> snapshot(Set<BlockIndex.Pos> positions,Function<BlockIndex.Pos,String> state){
        Set<Chunk> chunks=new HashSet<>();for(var p:positions)chunks.add(new Chunk(Math.floorDiv(p.x(),16),Math.floorDiv(p.y(),16),Math.floorDiv(p.z(),16)));
        List<String> result=new ArrayList<>();
        for(var c:chunks){
            List<String> palette=new ArrayList<>();palette.add("minecraft:air");JsonArray data=new JsonArray();
            for(int y=0;y<16;y++)for(int z=0;z<16;z++)for(int x=0;x<16;x++){
                var p=new BlockIndex.Pos(c.x()*16+x,c.y()*16+y,c.z()*16+z);
                String id=positions.contains(p)?state.apply(p):"minecraft:air";
                if(!palette.contains(id))palette.add(id);data.add(palette.indexOf(id));
            }
            JsonObject o=new JsonObject();o.addProperty("t","chunk");o.addProperty("cx",c.x());o.addProperty("cy",c.y());o.addProperty("cz",c.z());
            JsonArray pal=new JsonArray();palette.forEach(pal::add);o.add("palette",pal);o.add("data",data);result.add(o.toString());
        }
        return result;
    }
    /** Only for a fresh receiver after welcome clears its grid; existing receivers need full clearing chunks. */
    public static List<String> initialSnapshot(Set<BlockIndex.Pos> positions,Function<BlockIndex.Pos,String> state){
        var groups=new HashMap<Chunk,Set<BlockIndex.Pos>>();var states=new HashMap<BlockIndex.Pos,String>();
        for(var p:positions){var id=state.apply(p);if(id.equals("minecraft:air")||id.startsWith("minecraft:barrier"))continue;states.put(p,id);groups.computeIfAbsent(new Chunk(Math.floorDiv(p.x(),16),Math.floorDiv(p.y(),16),Math.floorDiv(p.z(),16)),c->new HashSet<>()).add(p);}
        var result=new ArrayList<String>();for(var cells:groups.values())if(cells.size()<=48){for(var p:cells)result.add(block(p,states.get(p)));}else result.addAll(snapshot(cells,states::get));return result;
    }
}
