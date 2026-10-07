using System;
using System.Linq;
namespace PeakCreativeMode.Core
{
 public readonly struct Loot{public readonly string Item;public readonly int Count;public readonly string Potion;public Loot(string item,int count,string potion=null){Item=item;Count=count;Potion=potion;}}
 /// Deterministic per run+spawn key, so reloads/replays decide identically.
 public static class LootTable
 {
  public static readonly string[] Protected={"rope","backpack","flare","guidebook","book","compass","piton","chain","bugle","passport","anchor","shelf"};
  private static readonly Loot[][] Tables={
   /*Beach*/   new[]{new Loot("minecraft:potion",1,"minecraft:swiftness"),new Loot("minecraft:golden_apple",1),new Loot("minecraft:potion",1,"minecraft:leaping"),new Loot("minecraft:torch",8),new Loot("minecraft:bread",3)},
   /*Tropics*/ new[]{new Loot("minecraft:ender_pearl",2),new Loot("minecraft:cooked_porkchop",3),new Loot("minecraft:golden_apple",1),new Loot("minecraft:potion",1,"minecraft:leaping"),new Loot("minecraft:potion",1,"minecraft:slow_falling")},
   /*Alpine+*/ new[]{new Loot("minecraft:ender_pearl",3),new Loot("minecraft:potion",1,"minecraft:slow_falling"),new Loot("minecraft:potion",1,"minecraft:swiftness"),new Loot("minecraft:golden_apple",1),new Loot("minecraft:iron_sword",1),new Loot("minecraft:arrow",16)}};
  public static bool Eligible(string peakItemName)=>!string.IsNullOrEmpty(peakItemName)&&!Protected.Any(p=>peakItemName.IndexOf(p,StringComparison.OrdinalIgnoreCase)>=0);
  public static double Roll(string runKey,string spawnKey){unchecked{uint h=2166136261;foreach(char c in runKey+"\0"+spawnKey){h^=c;h*=16777619;}return (h&0xFFFFFF)/(double)0x1000000;}}
  public static bool ShouldReplace(string runKey,string spawnKey,string peakItemName,double chance,bool replaceProtected=false)=>(replaceProtected?!string.IsNullOrEmpty(peakItemName):Eligible(peakItemName))&&chance>0&&Roll(runKey,spawnKey)<chance;
  public static Loot For(int segment,string runKey,string spawnKey){if(segment<=0){double roll=Roll(runKey,spawnKey);if(roll<.05)return new Loot(roll<.025?"minecraft:cobblestone":"minecraft:oak_planks",4);}var t=Tables[Math.Max(0,Math.Min(Tables.Length-1,segment<=0?0:segment==1?1:2))];return t[(int)(Roll(spawnKey,runKey)*t.Length)];}
  public static string SpawnKey(int x,int y,int z,int index)=>x+","+y+","+z+"#"+index;
 }
}
