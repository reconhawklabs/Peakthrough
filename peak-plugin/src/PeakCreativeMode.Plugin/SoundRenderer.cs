using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.Networking;
namespace PeakCreativeMode.Plugin
{
 internal sealed class SoundRenderer:MonoBehaviour
 {
  public static SoundRenderer Instance{get;private set;}
  private JObject _events;private readonly Queue<SoundMessage> _pending=new Queue<SoundMessage>();
  private readonly Dictionary<string,AudioClip> _clips=new Dictionary<string,AudioClip>();
  private readonly List<AudioSource> _sources=new List<AudioSource>();private int _loading,_generation;private bool _warned,_played;
  private void Awake(){Instance=this;try{_events=JObject.Parse(File.ReadAllText(Path.Combine(Plugin.Keys.AssetCache.Value,"sounds.json")));}catch(Exception e){Plugin.Log.LogWarning("[sound] Local sound export unavailable: "+e.Message);}}
  public void Receive(JObject message){if(!Plugin.Keys.EnableSounds.Value||_events==null||_pending.Count>=64)return;if(SoundMessage.TryParse(message,out var sound))_pending.Enqueue(sound);}
  private string Resolve(string id,int depth=0)
  {
   if(depth>8||!id.StartsWith("minecraft:",StringComparison.Ordinal))return null;
   var sounds=_events[id.Substring(10)]?["sounds"] as JArray;if(sounds==null||sounds.Count==0)return null;
   var entry=sounds[UnityEngine.Random.Range(0,sounds.Count)];string name=entry.Type==JTokenType.String?(string)entry:(string)entry["name"];
   if(entry.Type==JTokenType.Object&&(string)entry["type"]=="event")return Resolve(name.Contains(":")?name:"minecraft:"+name,depth+1);
   if(entry.Type==JTokenType.Object&&(bool?)entry["stream"]==true)return null;
   name=name?.Replace("minecraft:","");if(name==null||name.Contains("..")||name.Contains("\\")||name.StartsWith("/")||name.Contains(":"))return null;
   return name;
  }
  private void Update()
  {
   if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||!WorldDiagnostics.Playing(Character.localCharacter)){if(_pending.Count>0||_loading>0||_sources.Count>0)Clear();return;}
   for(int i=_sources.Count-1;i>=0;i--)if(_sources[i]==null||!_sources[i].isPlaying){if(_sources[i]!=null)Destroy(_sources[i].gameObject);_sources.RemoveAt(i);}
   while(_loading<4&&_pending.Count>0&&_sources.Count<32){var sound=_pending.Dequeue();string name=Resolve(sound.Id);if(name!=null)StartCoroutine(Play(sound,name,_generation));}
  }
  private IEnumerator Play(SoundMessage sound,string name,int generation)
  {
   _loading++;
   if(!_clips.TryGetValue(name,out var clip))
   {
    string path=Path.Combine(Plugin.Keys.AssetCache.Value,"sounds",name+".ogg");
    using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.OGGVORBIS))
    {
     yield return request.SendWebRequest();
     if(request.result!=UnityWebRequest.Result.Success){if(!_warned){_warned=true;Plugin.Log.LogWarning("[sound] "+request.error);} _loading--;yield break;}
     clip=DownloadHandlerAudioClip.GetContent(request);
    }
    if(generation!=_generation){Destroy(clip);_loading--;yield break;}
    if(_clips.TryGetValue(name,out var existing)){Destroy(clip);clip=existing;}
    else{if(_clips.Count>=64){string evict=null;foreach(var pair in _clips){if(!_sources.Exists(s=>s!=null&&s.clip==pair.Value)){evict=pair.Key;break;}}if(evict!=null){Destroy(_clips[evict]);_clips.Remove(evict);}else{Destroy(clip);_loading--;yield break;}}_clips[name]=clip;}
   }
   _loading--;
   if(generation!=_generation||_sources.Count>=32)yield break;
   var go=new GameObject("Minecraft sound "+sound.Id);go.transform.SetParent(transform,false);var p=Coords.McToUnity(sound.Position.X,sound.Position.Y,sound.Position.Z);go.transform.position=new Vector3(p.X,p.Y,p.Z);
   var source=go.AddComponent<AudioSource>();source.clip=clip;source.spatialBlend=sound.Id.StartsWith("minecraft:ui.",StringComparison.Ordinal)?0:1;source.minDistance=1;source.maxDistance=Mathf.Max(16,sound.Volume*16);source.rolloffMode=AudioRolloffMode.Linear;source.volume=Mathf.Clamp01(sound.Volume);source.pitch=sound.Pitch;source.Play();_sources.Add(source);if(!_played){_played=true;Plugin.Log.LogInfo("[sound] Decoded and playing native OGG: "+sound.Id);}
  }
  public void Clear(){_generation++;_pending.Clear();foreach(var source in _sources)if(source!=null)Destroy(source.gameObject);_sources.Clear();}
  private void OnDestroy(){Clear();foreach(var clip in _clips.Values)Destroy(clip);_clips.Clear();}
 }
}
