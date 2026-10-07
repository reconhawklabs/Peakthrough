package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.session.BridgePlayer;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.projectile.throwableitemprojectile.ThrownEnderpearl;
import net.minecraft.world.phys.HitResult;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
@Mixin(ThrownEnderpearl.class)
public abstract class EnderPearlMixin {
 @Inject(method="onHit",at=@At("HEAD"),cancellable=true)
 private void peakPearlHit(HitResult hit,CallbackInfo ci){var pearl=(ThrownEnderpearl)(Object)this;if(pearl.level() instanceof ServerLevel&&pearl.getOwner() instanceof BridgePlayer owner){if(!owner.isSpectator())owner.bridgeTeleport.accept(pearl.oldPosition());pearl.discard();ci.cancel();}}
}
