using System;
using System.Collections.Generic;
using System.Text;
namespace PeakCreativeMode.Core
{
 public sealed class RelayFrames
 {
  public const int FragmentBytes=16384,MaxBytes=1048576;
  private sealed class Pending{public byte[][] Parts;public int Count,Bytes;}
  private readonly Dictionary<long,Pending> _pending=new Dictionary<long,Pending>();
  public string Token{get;private set;}private long _last=-1;
  public void Reset(string token){Token=token;_last=-1;_pending.Clear();}
  public static string PacketToken(object packet)=>packet is object[] a&&a.Length==6&&a[0] is int version&&version==1&&a[1] is string token&&token.Length>0&&token.Length<=64?token:null;
  public static object[][] Split(string token,long sequence,string text)
  {
   if(string.IsNullOrEmpty(token)||token.Length>64||sequence<0)throw new ArgumentException("Relay identity");
   byte[] bytes=Encoding.UTF8.GetBytes(text);if(bytes.Length>MaxBytes)throw new ArgumentException("Relay frame too large");
   int count=Math.Max(1,(bytes.Length+FragmentBytes-1)/FragmentBytes);var parts=new object[count][];
   for(int i=0;i<count;i++){var piece=new byte[Math.Min(FragmentBytes,bytes.Length-i*FragmentBytes)];Buffer.BlockCopy(bytes,i*FragmentBytes,piece,0,piece.Length);parts[i]=new object[]{1,token,sequence,i,count,piece};}return parts;
  }
  public string Accept(object packet)
  {
   string token=PacketToken(packet);if(token==null)return null;var a=(object[])packet;
   if(!(a[2] is long sequence)||sequence<0||sequence<=_last||!(a[3] is int part)||!(a[4] is int count)||!(a[5] is byte[] bytes)||count<1||count>64||part<0||part>=count||bytes.Length>FragmentBytes)return null;
   if(Token==null)Token=token;if(token!=Token)return null;
   if(!_pending.TryGetValue(sequence,out var p)){if(_pending.Count>=8)return null;p=new Pending{Parts=new byte[count][]};_pending[sequence]=p;}
   if(p.Parts.Length!=count)return null;
   if(p.Parts[part]!=null)return null;p.Parts[part]=(byte[])bytes.Clone();p.Count++;p.Bytes+=bytes.Length;
   if(p.Bytes>MaxBytes){_pending.Remove(sequence);return null;}if(p.Count!=count)return null;
   var whole=new byte[p.Bytes];int at=0;foreach(var piece in p.Parts){Buffer.BlockCopy(piece,0,whole,at,piece.Length);at+=piece.Length;}
   _last=sequence;foreach(long old in new List<long>(_pending.Keys))if(old<=sequence)_pending.Remove(old);
   try{return new UTF8Encoding(false,true).GetString(whole);}catch(DecoderFallbackException){return null;}
  }
 }
 public static class RelayAuthority
 {
  public static bool Request(int sender,int local,bool host,bool peer)=>host&&peer&&sender>0&&sender!=local;
  public static bool Response(int sender,int master,string token,string expected)=>sender>0&&sender==master&&token==expected;
  public static string PlayerId(string authenticatedId,int actor)=>!string.IsNullOrWhiteSpace(authenticatedId)&&authenticatedId.Length<=110?"photon:"+authenticatedId:"photon:actor:"+actor;
 }
}
