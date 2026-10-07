package com.bornparanoid.peakpassthrough.mirror;
import java.util.Set;
import java.util.HashSet;
/** Owns bridge-dimension tickets, including those restored from the native world save. */
public final class ChunkLeases {
 public record Delta(Set<Long> added,Set<Long> removed){}
 private Set<Long> current;
 public ChunkLeases(Set<Long> restored){current=new HashSet<>(restored);}
 public Delta replace(Set<Long> desired){var added=new HashSet<>(desired);added.removeAll(current);var removed=new HashSet<>(current);removed.removeAll(desired);current=new HashSet<>(desired);return new Delta(Set.copyOf(added),Set.copyOf(removed));}
}
