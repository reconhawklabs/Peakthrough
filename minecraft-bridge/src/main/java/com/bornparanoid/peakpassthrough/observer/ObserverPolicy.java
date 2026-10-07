package com.bornparanoid.peakpassthrough.observer;
import java.util.Collection;
import java.util.Comparator;
import java.util.Objects;
public final class ObserverPolicy {
 private ObserverPolicy(){}
 public record Anchor(int client,String seed,boolean active,boolean guest,double x,double y,double z){}
 public static Anchor choose(Collection<Anchor> anchors){return anchors.stream().filter(Anchor::active).min(Comparator.comparing(Anchor::guest).thenComparingInt(Anchor::client)).orElse(null);}
 public static boolean move(String seenSeed,String dimensionSeed,double x,double y,double z,Anchor target){if(target==null)return false;double dx=x-target.x(),dy=y-target.y(),dz=z-target.z();return !Objects.equals(seenSeed,target.seed())||!Objects.equals(dimensionSeed,target.seed())||dx*dx+dy*dy+dz*dz>128*128;}
}
