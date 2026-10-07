package com.bornparanoid.peakpassthrough.inventory;
import com.google.gson.*;
import net.minecraft.server.level.ServerPlayer;
import net.minecraft.world.item.ItemStack;
/** Token placement rules. Never overwrites a Minecraft item. */
public final class PeakInventory {
 public static int choose(boolean[] empty,int preferred){if(preferred>=0&&preferred<36&&empty[preferred])return preferred;for(int i=0;i<36;i++)if(empty[i])return i;return -1;}
 public static int store(ServerPlayer p,PeakTokens.Token t,int preferred){var inv=p.getInventory();var empty=new boolean[36];for(int i=0;i<36;i++)empty[i]=inv.getItem(i).isEmpty();int slot=choose(empty,preferred);if(slot>=0)inv.setItem(slot,PeakTokens.stack(t));return slot;}
 public static int replace(ServerPlayer p,int slot,PeakTokens.Token t){var inv=p.getInventory();var cur=inv.getItem(slot);if(!cur.isEmpty()&&PeakTokens.of(cur)==null)return -1;inv.setItem(slot,t==null?ItemStack.EMPTY:PeakTokens.stack(t));return slot;}
 public static JsonArray clearAll(ServerPlayer p){var out=new JsonArray();var inv=p.getInventory();for(int i=0;i<36;i++){var t=PeakTokens.of(inv.getItem(i));if(t!=null){out.add(PeakTokens.json(t));inv.setItem(i,ItemStack.EMPTY);}}var carried=PeakTokens.of(p.inventoryMenu.getCarried());if(carried!=null){out.add(PeakTokens.json(carried));p.inventoryMenu.setCarried(ItemStack.EMPTY);}return out;}
 public static String ack(int ref,int slot,JsonArray tokens){var o=new JsonObject();o.addProperty("t","peak_ack");o.addProperty("ref",ref);o.addProperty("slot",slot);if(tokens!=null)o.add("tokens",tokens);return o.toString();}
}
