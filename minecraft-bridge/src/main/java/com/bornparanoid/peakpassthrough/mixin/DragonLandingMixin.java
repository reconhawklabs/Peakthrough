package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.boss.*;
import net.minecraft.world.entity.boss.enderdragon.EnderDragon;
import net.minecraft.world.entity.boss.enderdragon.phases.*;
import net.minecraft.world.entity.player.Player;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.*;
import org.spongepowered.asm.mixin.injection.invoke.arg.Args;
@Mixin(DragonLandingApproachPhase.class)
public abstract class DragonLandingMixin extends AbstractDragonPhaseInstance {
 public DragonLandingMixin(EnderDragon dragon){super(dragon);}
 @ModifyArgs(method="findNewTarget",at=@At(value="INVOKE",target="Lnet/minecraft/world/entity/boss/enderdragon/EnderDragon;findClosestNode(DDD)I",ordinal=0))
 private void peakApproach(Args a){if(DragonArena.isArena(dragon)){var p=DragonNodes.approachTarget(a.get(0),a.get(1),a.get(2),dragon.getFightOrigin(),DragonArena.orbit(dragon));for(int i=0;i<3;i++)a.set(i,p[i]);}}
 @ModifyArgs(method="findNewTarget",at=@At(value="INVOKE",target="Lnet/minecraft/world/entity/boss/enderdragon/EnderDragon;findClosestNode(DDD)I",ordinal=1))
 private void peakFallback(Args a){if(DragonArena.isArena(dragon)){double scale=DragonArena.orbit(dragon)/60.0;a.set(0,dragon.getFightOrigin().getX()+(double)a.get(0)*scale);a.set(2,dragon.getFightOrigin().getZ()+(double)a.get(2)*scale);}}
 @Redirect(method="findNewTarget",at=@At(value="INVOKE",target="Lnet/minecraft/world/entity/player/Player;getX()D"))
 private double peakAimX(Player p){return p.getX()-(DragonArena.isArena(dragon)?dragon.getFightOrigin().getX():0);}
 @Redirect(method="findNewTarget",at=@At(value="INVOKE",target="Lnet/minecraft/world/entity/player/Player;getZ()D"))
 private double peakAimZ(Player p){return p.getZ()-(DragonArena.isArena(dragon)?dragon.getFightOrigin().getZ():0);}
}
