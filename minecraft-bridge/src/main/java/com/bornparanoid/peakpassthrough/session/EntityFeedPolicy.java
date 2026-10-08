package com.bornparanoid.peakpassthrough.session;
public final class EntityFeedPolicy { public static boolean update(long tick,boolean first){return first||tick%2==0;} }
