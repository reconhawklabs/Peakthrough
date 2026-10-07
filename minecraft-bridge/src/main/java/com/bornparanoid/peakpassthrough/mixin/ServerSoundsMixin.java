package com.bornparanoid.peakpassthrough.mixin;
import com.bornparanoid.peakpassthrough.sound.SoundFeed;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.world.entity.Entity;
import net.minecraft.core.Holder;
import net.minecraft.sounds.*;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.*;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;
@Mixin(ServerLevel.class)
public abstract class ServerSoundsMixin {
 @Inject(method="playSeededSound(Lnet/minecraft/world/entity/Entity;DDDLnet/minecraft/core/Holder;Lnet/minecraft/sounds/SoundSource;FFJ)V",at=@At("HEAD"))
 private void positional(Entity except,double x,double y,double z,Holder<SoundEvent> sound,SoundSource source,float volume,float pitch,long seed,CallbackInfo ci){SoundFeed.emit((ServerLevel)(Object)this,x,y,z,sound.value(),volume,pitch);}
 @Inject(method="playSeededSound(Lnet/minecraft/world/entity/Entity;Lnet/minecraft/world/entity/Entity;Lnet/minecraft/core/Holder;Lnet/minecraft/sounds/SoundSource;FFJ)V",at=@At("HEAD"))
 private void entity(Entity except,Entity sourceEntity,Holder<SoundEvent> sound,SoundSource source,float volume,float pitch,long seed,CallbackInfo ci){SoundFeed.emit((ServerLevel)(Object)this,sourceEntity.getX(),sourceEntity.getY(),sourceEntity.getZ(),sound.value(),volume,pitch);}
}
