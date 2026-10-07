package com.bornparanoid.peakpassthrough.inventory;
import com.google.gson.*;
import java.util.Base64;
import java.util.regex.Pattern;
import net.minecraft.core.component.DataComponents;
import net.minecraft.nbt.CompoundTag;
import net.minecraft.network.chat.Component;
import net.minecraft.world.item.*;
import net.minecraft.world.item.component.CustomData;
/** PEAK item stored as an unstackable paper stack. PEAK owns the bytes; Minecraft only stores them. */
public final class PeakTokens {
 public static final int MAX_DATA=4096;private static final Pattern NAME=Pattern.compile("^[A-Za-z0-9 _().,'!-]{1,64}$");
 public record Token(int id,String name,String data){}
 public static Token parse(JsonElement e){try{if(e==null||!e.isJsonObject())return null;var o=e.getAsJsonObject();var idE=o.get("id");if(idE==null||!idE.isJsonPrimitive()||!idE.getAsJsonPrimitive().isNumber())return null;double id=idE.getAsDouble();if(id!=Math.rint(id)||id<0||id>=65535)return null;String name=o.get("name").getAsString(),data=o.get("data").getAsString();if(!NAME.matcher(name).matches()||Base64.getDecoder().decode(data).length>MAX_DATA)return null;return new Token((int)id,name,data);}catch(RuntimeException ex){return null;}}
 public static JsonObject json(Token t){var o=new JsonObject();o.addProperty("id",t.id());o.addProperty("name",t.name());o.addProperty("data",t.data());return o;}
 public static ItemStack stack(Token t){var s=new ItemStack(Items.PAPER);var tag=new CompoundTag();var peak=new CompoundTag();peak.putInt("id",t.id());peak.putString("name",t.name());peak.putString("data",t.data());tag.put("peak",peak);s.set(DataComponents.CUSTOM_DATA,CustomData.of(tag));s.set(DataComponents.MAX_STACK_SIZE,1);s.set(DataComponents.CUSTOM_NAME,Component.literal(t.name()));return s;}
 public static Token of(ItemStack s){if(s.isEmpty()||!s.is(Items.PAPER))return null;var data=s.get(DataComponents.CUSTOM_DATA);if(data==null)return null;var tag=data.copyTag();if(!tag.contains("peak"))return null;var p=tag.getCompoundOrEmpty("peak");var o=new JsonObject();o.addProperty("id",p.getIntOr("id",-1));o.addProperty("name",p.getStringOr("name",""));o.addProperty("data",p.getStringOr("data",""));return parse(o);}
}
