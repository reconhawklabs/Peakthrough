package com.bornparanoid.peakpassthrough.blocks;
import com.bornparanoid.peakpassthrough.PassPeakThrough;
import com.bornparanoid.peakpassthrough.bridge.BridgeServer;
import com.google.gson.*;
import java.io.IOException;
import java.nio.file.Path;
import java.util.*;
import net.minecraft.core.BlockPos;
import net.minecraft.core.registries.BuiltInRegistries;
import net.minecraft.core.registries.Registries;
import net.minecraft.resources.ResourceKey;
import net.minecraft.resources.Identifier;
import net.minecraft.server.MinecraftServer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.level.Level;
import net.minecraft.world.level.storage.LevelResource;
/** Native Minecraft blocks; coordinate index only identifies content belonging to the bridge. */
public final class BlockWorld {
    public static final ResourceKey<Level> DIMENSION=ResourceKey.create(Registries.DIMENSION,PassPeakThrough.id("run_dev"));
    private final BridgeServer bridge;
    private final Path indexPath;
    private final BlockIndex index;
    private final com.bornparanoid.peakpassthrough.terraform.Volumes volumes;private final Path volumePath;private boolean volumeDirty;
    private final ServerLevel level;
    private final Map<BlockIndex.Pos,String> known=new HashMap<>();
    private boolean dirty;
    private boolean warnedSave;
    public BlockWorld(MinecraftServer server,BridgeServer bridge){
        this.bridge=bridge;level=server.getLevel(DIMENSION);
        if(level==null)throw new IllegalStateException("Missing passpeakthrough:run_dev dimension; restart with mod data enabled");
        indexPath=server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-blocks.json");
        try{index=BlockIndex.load(indexPath);}catch(IOException e){throw new IllegalStateException("Cannot load placed block index; refusing to overwrite it",e);}
        volumePath=server.getWorldPath(LevelResource.ROOT).resolve("passpeakthrough-volumes.json");try{volumes=com.bornparanoid.peakpassthrough.terraform.Volumes.load(volumePath);}catch(IOException e){throw new IllegalStateException("Cannot load terraform boundaries",e);}
        for(long chunk:level.getForceLoadedChunks().toLongArray())level.setChunkForced(net.minecraft.world.level.ChunkPos.getX(chunk),net.minecraft.world.level.ChunkPos.getZ(chunk),false);
        PassPeakThrough.LOGGER.info("Block world {} height {}..{}, {} indexed positions",DIMENSION.identifier(),level.getMinY(),level.getMaxY(),index.positions().size());
    }
    public ServerLevel level(){return level;}
    public boolean owns(BlockPos p){return index.contains(new BlockIndex.Pos(p.getX(),p.getY(),p.getZ()));}
    public void track(BlockPos p){var pos=new BlockIndex.Pos(p.getX(),p.getY(),p.getZ());index.add(pos);dirty=true;}
    public String blockState(BlockPos p){return state(new BlockIndex.Pos(p.getX(),p.getY(),p.getZ()));}
    private BlockPos nativePos(BlockIndex.Pos p){return new BlockPos(p.x(),p.y(),p.z());}
    private String state(BlockIndex.Pos p){var state=level.getBlockState(nativePos(p));String properties=state.getValues().map(Object::toString).sorted().collect(java.util.stream.Collectors.joining(","));return BuiltInRegistries.BLOCK.getKey(state.getBlock()).toString()+(properties.isEmpty()?"":"["+properties+"]");}
    public void snapshot(int client,com.bornparanoid.peakpassthrough.dimension.RunRegions.Region region){
        var positions=index.positions().stream().filter(p->region.owns(p.x(),p.z())).collect(java.util.stream.Collectors.toSet());
        var lines=BlockMessages.snapshot(positions,p->{String current=state(p);known.put(p,current);return current;});
        for(int i=0;i<lines.size();i++){var msg=JsonParser.parseString(lines.get(i)).getAsJsonObject();if(i==lines.size()-1){var list=new JsonArray();volumes.all().stream().filter(v->region.owns(v.x(),v.z())).forEach(v->list.add(v.json()));msg.add("terraform",list);}bridge.send(client,msg.toString());}
    }
    public void quickSnapshot(int client,com.bornparanoid.peakpassthrough.terraform.Volumes.Volume volume){
        var touched=com.bornparanoid.peakpassthrough.terraform.Volumes.touchedChunks(volume.x(),volume.y(),volume.z(),volume.sx(),volume.sy(),volume.sz());
        var positions=index.positions().stream().filter(p->touched.contains(new BlockMessages.Chunk(Math.floorDiv(p.x(),16),Math.floorDiv(p.y(),16),Math.floorDiv(p.z(),16)))).collect(java.util.stream.Collectors.toSet());
        var lines=BlockMessages.snapshot(positions,p->{String current=state(p);known.put(p,current);return current;});
        for(int i=0;i<lines.size();i++){var msg=JsonParser.parseString(lines.get(i)).getAsJsonObject();if(i==lines.size()-1){var list=new JsonArray();list.add(volume.json());msg.add("terraform",list);}bridge.send(client,msg.toString());}
    }
    public boolean convert(com.bornparanoid.peakpassthrough.terraform.TerrainConversion conversion,com.bornparanoid.peakpassthrough.dimension.RunRegions.Region region){
        if(conversion==null)return false;var v=conversion.volume();if(!region.owns(v.x(),v.z())||!region.owns(v.x()+v.sx()-1,v.z()+v.sz()-1))return false;
        if(volumes.all().stream().anyMatch(old->old.id().equals(v.id())))return true;
        if(volumes.all().stream().filter(old->region.owns(old.x(),old.z())).count()>=com.bornparanoid.peakpassthrough.terraform.Volumes.MAX_PER_RUN)return false;
        var material=BuiltInRegistries.BLOCK.getValue(Identifier.parse(v.material())).defaultBlockState();
        for(int x=v.x();x<v.x()+v.sx();x++)for(int y=v.y();y<v.y()+v.sy();y++)for(int z=v.z();z<v.z()+v.sz();z++){
            var pos=new BlockPos(x,y,z);if(owns(pos))continue;
            var wanted=conversion.solid().contains(new com.bornparanoid.peakpassthrough.terraform.Volumes.Cell(x,y,z))?material:net.minecraft.world.level.block.Blocks.AIR.defaultBlockState();
            level.setBlock(pos,wanted,3);track(pos);
        }
        volumes.add(v);volumeDirty=true;save();PassPeakThrough.LOGGER.info("Converted native terrain {} {}",v.id(),v.material());return true;
    }
    public boolean deposit(String seed,int x,int y,int z){
        var cells=com.bornparanoid.peakpassthrough.world.BlockDeposits.layout(seed,x,y,z);
        // Refuse the whole outcrop if any cell was already placed/mined or contains native content.
        if(cells.stream().anyMatch(c->{var p=new BlockPos(c.x(),c.y(),c.z());return level.isOutsideBuildHeight(p)||owns(p)||!level.getBlockState(p).isAir()&&!level.getBlockState(p).is(net.minecraft.world.level.block.Blocks.BARRIER);}))return false;
        for(var c:cells){var p=new BlockPos(c.x(),c.y(),c.z());var block=BuiltInRegistries.BLOCK.getValue(Identifier.parse(c.block()));level.setBlock(p,block.defaultBlockState(),3);track(p);}save();return true;
    }
    public String place(BlockMessages.Place action,ServerPlayer player,String selected){
        if(action==null||!action.block().equals(selected)||!BlockMessages.withinReach(action,player.getX(),player.getY(),player.getZ()))return null;
        var p=action.pos();var pos=nativePos(p);
        if(level.isOutsideBuildHeight(pos)||!level.getBlockState(pos).isAir())return null;
        var block=BuiltInRegistries.BLOCK.getValue(Identifier.parse(action.block()));
        if(block==null||!level.setBlock(pos,block.defaultBlockState(),3))return null;
        index.add(p);dirty=true;String current=state(p);known.put(p,current);
        PassPeakThrough.LOGGER.info("Placed {} at {},{},{}",current,p.x(),p.y(),p.z());
        return BlockMessages.block(p,current);
    }
    public List<String> changes(java.util.Collection<com.bornparanoid.peakpassthrough.dimension.RunRegions.Region> regions){
        List<String> result=new ArrayList<>();
        for(var p:index.positions()){
            if(regions.stream().noneMatch(r->r.owns(p.x(),p.z())))continue;
            String current=state(p);if(!current.equals(known.put(p,current)))result.add(BlockMessages.block(p,current));
        }
        return result;
    }
    public void save(){
        if(volumeDirty){try{volumes.save(volumePath);volumeDirty=false;}catch(IOException e){PassPeakThrough.LOGGER.error("Cannot save terraform boundaries",e);}}
        if(!dirty)return;
        try{index.save(indexPath);dirty=false;warnedSave=false;}
        catch(IOException e){if(!warnedSave){warnedSave=true;PassPeakThrough.LOGGER.error("Cannot save placed coordinate index",e);}}
    }
}
