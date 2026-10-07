package com.bornparanoid.peakpassthrough.boss;
import net.minecraft.core.BlockPos;
/** Vanilla 24-node layout, re-centred on the fight origin and scaled by orbit radius. */
public final class DragonNodes {
 public static BlockPos node(int i,BlockPos origin,int orbit,int heightmapY){if(i<0||i>=24)throw new IllegalArgumentException("node");if(orbit<20||orbit>160)throw new IllegalArgumentException("orbit");double r;int count,base,lift;if(i<12){r=orbit;count=12;base=0;lift=5;}else if(i<20){r=orbit*2/3.0;count=8;base=12;lift=15;}else{r=orbit/3.0;count=4;base=20;lift=5;}double a=2*Math.PI*(i-base)/count+(i<12?0:i<20?Math.PI/8:Math.PI/4);int x=origin.getX()+(int)Math.floor(r*Math.cos(a)),z=origin.getZ()+(int)Math.floor(r*Math.sin(a));int floor=origin.getY()+(i<12?20:i<20?25:12);int y=heightmapY==Integer.MIN_VALUE?floor:Math.max(floor,heightmapY+lift);return new BlockPos(x,y,z);}
 public static double[] approachTarget(double vanillaX,double vanillaY,double vanillaZ,BlockPos origin,int orbit){double s=orbit/60.0;return new double[]{origin.getX()+vanillaX*s,origin.getY()+(vanillaY-64)*.5+20,origin.getZ()+vanillaZ*s};}
}
