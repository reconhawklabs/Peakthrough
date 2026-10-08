package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.session.BridgePlayer;
import net.minecraft.world.entity.projectile.arrow.AbstractArrow;
import net.minecraft.world.phys.EntityHitResult;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
@Mixin(AbstractArrow.class)
public abstract class ArrowHitMixin {
 @Inject(method="onHitEntity",at=@At("HEAD"))
 private void peakArrowBegin(EntityHitResult hit,CallbackInfo ci){if(hit.getEntity() instanceof BridgePlayer p)p.arrowImpact=hit.getLocation();}
 @Inject(method="onHitEntity",at=@At("RETURN"))
 private void peakArrowEnd(EntityHitResult hit,CallbackInfo ci){if(hit.getEntity() instanceof BridgePlayer p)p.arrowImpact=null;}
}
