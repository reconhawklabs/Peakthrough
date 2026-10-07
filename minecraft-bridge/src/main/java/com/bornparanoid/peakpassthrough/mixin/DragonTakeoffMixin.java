package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.boss.*;
import net.minecraft.world.entity.boss.enderdragon.EnderDragon;
import net.minecraft.world.entity.boss.enderdragon.phases.*;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.*;
import org.spongepowered.asm.mixin.injection.invoke.arg.Args;
@Mixin(DragonTakeoffPhase.class)
public abstract class DragonTakeoffMixin extends AbstractDragonPhaseInstance {
 public DragonTakeoffMixin(EnderDragon dragon){super(dragon);}
 @ModifyArgs(method="findNewTarget",at=@At(value="INVOKE",target="Lnet/minecraft/world/entity/boss/enderdragon/EnderDragon;findClosestNode(DDD)I"))
 private void peakApproach(Args a){if(DragonArena.isArena(dragon)){var p=DragonNodes.approachTarget(a.get(0),a.get(1),a.get(2),dragon.getFightOrigin(),DragonArena.orbit(dragon));for(int i=0;i<3;i++)a.set(i,p[i]);}}
}
