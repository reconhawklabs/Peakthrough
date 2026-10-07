package com.bornparanoid.peakpassthrough.session;
public final class CombatKnockback {
 public static double[] vector(double playerX,double playerZ,double sourceX,double sourceZ,double damage){
  return vector(playerX,playerZ,sourceX,sourceZ,damage,false);
 }
 public static double[] vector(double playerX,double playerZ,double sourceX,double sourceZ,double damage,boolean creeper){
  if(!Double.isFinite(playerX)||!Double.isFinite(playerZ)||!Double.isFinite(sourceX)||!Double.isFinite(sourceZ)||!Double.isFinite(damage)||damage<=0)return new double[]{0,0,0};
  double dx=playerX-sourceX,dz=playerZ-sourceZ,n=Math.hypot(dx,dz),force=Math.min(4,.4*damage);
  return n>0?new double[]{dx/n*force,creeper?Math.max(.4,force*.3):.2,dz/n*force}:new double[]{0,creeper?.4:.2,0};
 }
}
