using System;
namespace PeakCreativeMode.Core {public static class DeathFx {public static int Beams(int tick)=>tick<=0||tick>200?0:Math.Min(64,1+tick*63/200);public static bool Burst(int tick)=>tick>=180&&tick<=200;public static bool Xp(int tick)=>tick>150&&tick<=200&&tick%5==0;}}
