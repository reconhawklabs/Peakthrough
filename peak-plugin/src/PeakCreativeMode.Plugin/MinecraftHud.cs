using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace PeakCreativeMode.Plugin {
 /// Uses only the user's privately exported native sprites and ASCII atlas.
 internal static class MinecraftHud {
  private static readonly Dictionary<string,Texture2D> Textures=new Dictionary<string,Texture2D>();private static int[] _widths;
  internal static Texture2D Texture(string relative){if(Textures.TryGetValue(relative,out var t))return t;string p=Path.Combine(Plugin.Keys.AssetCache.Value,"assets/minecraft/textures",relative+".png");if(!File.Exists(p)){Textures[relative]=null;return null;}t=new Texture2D(2,2,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};if(!t.LoadImage(File.ReadAllBytes(p))){UnityEngine.Object.Destroy(t);return null;}Textures[relative]=t;return t;}
  private static int Width(char c){if(_widths==null){_widths=new int[128];var t=Texture("font/ascii");if(t==null){for(int i=0;i<128;i++)_widths[i]=6;_widths[32]=4;return 6;}for(int code=32;code<127;code++){int max=0;for(int x=0;x<8;x++)for(int y=0;y<8;y++)if(t.GetPixel(code%16*8+x,t.height-1-(code/16*8+y)).a>.1f)max=Math.Max(max,x+1);_widths[code]=code==32?4:max+1;}}return c>=32&&c<127?_widths[c]:6;}
  internal static float Measure(string text,float scale){float width=0;foreach(char c in text)width+=Width(c)*scale;return width;}
  internal static void Text(float x,float y,string text,float scale,Color? tint=null){var atlas=Texture("font/ascii");if(atlas==null){GUI.Label(new Rect(x,y,400,40),text,new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(8*scale)});return;}var saved=GUI.color;for(int pass=0;pass<2;pass++){float px=x+(pass==0?scale:0),py=y+(pass==0?scale:0);GUI.color=pass==0?new Color(0,0,0,.85f):tint??Color.white;foreach(char original in text){char c=original>=32&&original<127?original:'?';GUI.DrawTextureWithTexCoords(new Rect(px,py,8*scale,8*scale),atlas,new Rect(c%16/16f,1-(c/16+1)/16f,1/16f,1/16f));px+=Width(c)*scale;}}GUI.color=saved;}
  internal static void Fill(Rect rect,Color color){var saved=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=saved;}
  internal static void Slot(Rect r,bool selected){Fill(r,new Color(.12f,.12f,.12f,.85f));float p=Mathf.Max(1,r.width/20);var light=selected?Color.white:new Color(.55f,.55f,.55f);Fill(new Rect(r.x,r.y,r.width,p),light);Fill(new Rect(r.x,r.y,p,r.height),light);Fill(new Rect(r.x,r.yMax-p,r.width,p),Color.black);Fill(new Rect(r.xMax-p,r.y,p,r.height),Color.black);}
  internal static void Clear(){foreach(var t in Textures.Values)if(t!=null)UnityEngine.Object.Destroy(t);Textures.Clear();_widths=null;}
 }
}
