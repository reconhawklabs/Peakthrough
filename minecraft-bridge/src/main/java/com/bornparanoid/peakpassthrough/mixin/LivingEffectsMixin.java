package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.session.BridgePlayer;
import net.minecraft.world.entity.Entity;
import net.minecraft.world.entity.LivingEntity;
import net.minecraft.world.effect.MobEffectInstance;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
@Mixin(LivingEntity.class)
public abstract class LivingEffectsMixin {
 @Inject(method="onEffectAdded",at=@At("TAIL"))
 private void peakAdded(MobEffectInstance effect,Entity source,CallbackInfo ci){if((Object)this instanceof BridgePlayer player)player.bridgeEffect(effect);}
 @Inject(method="onEffectUpdated",at=@At("TAIL"))
 private void peakUpdated(MobEffectInstance effect,boolean refresh,Entity source,CallbackInfo ci){if((Object)this instanceof BridgePlayer player)player.bridgeEffect(effect);}
}
