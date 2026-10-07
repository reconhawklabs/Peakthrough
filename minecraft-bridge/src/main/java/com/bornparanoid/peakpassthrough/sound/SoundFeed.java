package com.bornparanoid.peakpassthrough.sound;
import net.minecraft.server.level.ServerLevel;
import net.minecraft.sounds.SoundEvent;
import com.google.gson.*;
/** Native event hook configured only while the bridge server lives. */
public final class SoundFeed {
 public interface Sink {void sound(ServerLevel level,double x,double y,double z,SoundEvent sound,float volume,float pitch);}
 public static Sink sink;
 public static void emit(ServerLevel level,double x,double y,double z,SoundEvent event,float volume,float pitch){if(sink!=null)sink.sound(level,x,y,z,event,volume,pitch);}
 public static String message(double x,double y,double z,SoundEvent event,float volume,float pitch){var o=new JsonObject();o.addProperty("t","sound");o.addProperty("id",event.location().toString());var pos=new JsonArray();pos.add(x);pos.add(y);pos.add(z);o.add("pos",pos);o.addProperty("volume",volume);o.addProperty("pitch",pitch);return o.toString();}
}
