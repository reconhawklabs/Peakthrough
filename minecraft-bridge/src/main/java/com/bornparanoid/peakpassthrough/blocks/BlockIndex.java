package com.bornparanoid.peakpassthrough.blocks;
import com.google.gson.*;
import java.io.IOException;
import java.nio.file.*;
import java.util.*;
/** Only tracks owned coordinates; block state always comes from the Minecraft level. */
public final class BlockIndex {
    public record Pos(int x,int y,int z) {}
    private final Set<Pos> positions=new HashSet<>();
    public Set<Pos> positions(){return Set.copyOf(positions);}
    public boolean contains(Pos p){return positions.contains(p);}
    public void add(Pos p){positions.add(p);}
    public void save(Path path)throws IOException {
        JsonArray data=new JsonArray();
        positions.stream().sorted(Comparator.comparingInt(Pos::x).thenComparingInt(Pos::y).thenComparingInt(Pos::z)).forEach(p->{
            JsonArray row=new JsonArray();row.add(p.x());row.add(p.y());row.add(p.z());data.add(row);
        });
        Files.createDirectories(path.toAbsolutePath().getParent());Path tmp=path.resolveSibling(path.getFileName()+".tmp");
        Files.writeString(tmp,data.toString());
        try{Files.move(tmp,path,StandardCopyOption.REPLACE_EXISTING,StandardCopyOption.ATOMIC_MOVE);}
        catch(AtomicMoveNotSupportedException e){Files.move(tmp,path,StandardCopyOption.REPLACE_EXISTING);}
    }
    public static BlockIndex load(Path path)throws IOException {
        BlockIndex index=new BlockIndex();if(!Files.exists(path))return index;
        try{
            for(JsonElement e:JsonParser.parseString(Files.readString(path)).getAsJsonArray()){
                JsonArray a=e.getAsJsonArray();if(a.size()!=3)throw new IllegalArgumentException("coordinate row");
                index.add(new Pos(a.get(0).getAsInt(),a.get(1).getAsInt(),a.get(2).getAsInt()));
            }
        }catch(RuntimeException e){throw new IOException("Invalid placed-block index "+path,e);}
        return index;
    }
}
