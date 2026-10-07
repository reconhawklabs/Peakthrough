package com.bornparanoid.peakpassthrough.boss;
import java.io.IOException;
/** Publish the killed state only after durable storage succeeds. */
public final class DragonVictory {
 @FunctionalInterface public interface Save {void write(DragonState state)throws IOException;}
 public static DragonState persist(DragonState state,Save save)throws IOException{if(state.killed())return state;var killed=DragonState.parse(state.json());killed.markKilled();save.write(killed);return killed;}
}
