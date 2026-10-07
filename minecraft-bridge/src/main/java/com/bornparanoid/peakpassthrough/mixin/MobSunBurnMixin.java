package com.bornparanoid.peakpassthrough.mixin;
import net.minecraft.world.entity.Mob;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfoReturnable;
@Mixin(Mob.class)
public abstract class MobSunBurnMixin {
 @Inject(method="isSunBurnTick",at=@At("HEAD"),cancellable=true)
 private void peakSunBurn(CallbackInfoReturnable<Boolean> cir){if(!com.bornparanoid.peakpassthrough.mobs.MobDirector.sunBurn)cir.setReturnValue(false);}
}
