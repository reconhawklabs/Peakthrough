package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.boss.*;
import net.minecraft.world.entity.boss.enderdragon.EnderDragon;
import net.minecraft.world.level.pathfinder.Node;
import net.minecraft.world.level.levelgen.Heightmap;
import org.spongepowered.asm.mixin.*;
import org.spongepowered.asm.mixin.injection.*;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
@Mixin(EnderDragon.class)
public abstract class DragonNodesMixin {
 @org.spongepowered.asm.mixin.Unique private net.minecraft.core.BlockPos peakNodeOrigin;
 @Shadow @Final private Node[] nodes;@Shadow @Final private int[] nodeAdjacency;
 @Inject(method="addAdditionalSaveData",at=@At("TAIL"))
 private void peakSaveOrigin(net.minecraft.world.level.storage.ValueOutput output,CallbackInfo ci){var d=(EnderDragon)(Object)this;if(!DragonArena.isArena(d))return;var p=d.getFightOrigin();output.putInt("PeakFightX",p.getX());output.putInt("PeakFightY",p.getY());output.putInt("PeakFightZ",p.getZ());}
 @Inject(method="readAdditionalSaveData",at=@At("TAIL"))
 private void peakLoadOrigin(net.minecraft.world.level.storage.ValueInput input,CallbackInfo ci){var d=(EnderDragon)(Object)this;if(!DragonArena.isArena(d))return;var x=input.getInt("PeakFightX");var y=input.getInt("PeakFightY");var z=input.getInt("PeakFightZ");if(x.isPresent()&&y.isPresent()&&z.isPresent())d.setFightOrigin(new net.minecraft.core.BlockPos(x.get(),y.get(),z.get()));}
 @Inject(method="findClosestNode()I",at=@At("HEAD"))
 private void peakNodes(org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable<Integer> ci){var d=(EnderDragon)(Object)this;if(!DragonArena.isArena(d))return;if(!d.getFightOrigin().equals(peakNodeOrigin)){java.util.Arrays.fill(nodes,null);peakNodeOrigin=d.getFightOrigin();}if(nodes[0]!=null)return;for(int i=0;i<24;i++){var p=DragonNodes.node(i,d.getFightOrigin(),DragonArena.orbit(d),Integer.MIN_VALUE);int top=d.level().hasChunkAt(p)?d.level().getHeightmapPos(Heightmap.Types.MOTION_BLOCKING_NO_LEAVES,p).getY():Integer.MIN_VALUE;p=DragonNodes.node(i,d.getFightOrigin(),DragonArena.orbit(d),top);nodes[i]=new Node(p.getX(),p.getY(),p.getZ());}int[] masks={6146,8197,8202,16404,32808,32848,65696,131392,131712,263424,526848,525313,1581057,3166214,2138120,6373424,4358208,12910976,9044480,9706496,15216640,13688832,11763712,8257536};System.arraycopy(masks,0,nodeAdjacency,0,24);}
}
