using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
namespace PeakCreativeMode.Plugin
{
 internal sealed class DropInteraction:MonoBehaviour,IInteractible
 {
  public int EntityId;public string Item;private float _next;
  public bool IsInteractible(Character interactor)=>BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected&&interactor==Character.localCharacter;
  public void Interact(Character interactor){if(!IsInteractible(interactor)||Time.unscaledTime<_next)return;_next=Time.unscaledTime+.2f;BridgeBehaviour.Instance.Client.Send(new JObject{["t"]="action",["kind"]="pickup",["target"]=new JObject{["entityId"]=EntityId}}.ToString(Formatting.None));}
  public void HoverEnter(){}public void HoverExit(){}public Vector3 Center()=>transform.position;public Transform GetTransform()=>transform;public string GetInteractionText()=>"Pick up";public string GetName()=>Item?.Replace("minecraft:","").Replace('_',' ')??"Minecraft item";
 }
 internal sealed class ItemEntities:MonoBehaviour
 {
  private sealed class Drop{public GameObject Root;public Transform Visual;public string Item;public Vector3 Position,Previous;public bool Projectile,Ready;public float Received;public float Attract=-1;public Vector3 From;}
  public static ItemEntities Instance {get;private set;}
  private readonly Dictionary<int,Drop> _items=new Dictionary<int,Drop>();private bool _warned;
  private void Awake(){Instance=this;}
  public void Clear(){foreach(var drop in _items.Values)Destroy(drop.Root);_items.Clear();}
  public void Receive(JObject o)
  {
   if(o["id"]?.Type!=JTokenType.Integer)return;int id=(int)o["id"];string type=(string)o["t"];
   if(type=="entity_remove"){if(_items.TryGetValue(id,out var old)){Destroy(old.Root);_items.Remove(id);}return;}
   bool projectile=o["projectile"]?.Type==JTokenType.Boolean&&(bool)o["projectile"];
   if(type=="entity_add"&&((string)o["type"]=="minecraft:item"||projectile))
   {
    if(_items.ContainsKey(id))return;GameObject go=null;
    try{string item=(string)o["item"]?["item"]??"minecraft:stone";var model=HeldItemRenderer.Instance.CreateDrop(item);go=new GameObject("MC item "+id);DontDestroyOnLoad(go);model.transform.SetParent(go.transform,false);go.layer=0;if(!projectile){var collider=go.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=.35f;var interaction=go.AddComponent<DropInteraction>();interaction.EntityId=id;interaction.Item=item;}go.SetActive(false);_items[id]=new Drop{Root=go,Visual=model.transform,Item=item,Projectile=projectile};}
    catch(Exception e){if(go!=null)Destroy(go);if(!_warned){_warned=true;Plugin.Log.LogWarning("[drops] "+e.Message);}}
   }
   if(type=="entity_update"&&_items.TryGetValue(id,out var drop)&&o["pos"] is JArray p&&p.Count==3){string item=(string)o["item"]?["item"];if(item!=null&&item!=drop.Item){try{var model=HeldItemRenderer.Instance.CreateDrop(item);model.transform.SetParent(drop.Root.transform,false);Destroy(drop.Visual.gameObject);drop.Visual=model.transform;drop.Item=item;var interaction=drop.Root.GetComponent<DropInteraction>();if(interaction!=null)interaction.Item=item;}catch(Exception e){if(!_warned){_warned=true;Plugin.Log.LogWarning("[drops] "+e.Message);}}}var v=Coords.McToUnity((float)p[0],(float)p[1],(float)p[2]);var next=new Vector3(v.X,v.Y+(drop.Projectile?0:.2f),v.Z);drop.Previous=drop.Ready?drop.Position:next;drop.Position=next;drop.Received=Time.unscaledTime;drop.Ready=true;}
  }
  private void Update(){bool active=BridgeBehaviour.Instance?.Client.Status==BridgeStatus.Connected&&WorldDiagnostics.Playing(Character.localCharacter);if(BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected){Clear();return;}
   foreach(var drop in _items.Values){drop.Root.SetActive(active&&drop.Ready);if(!active||!drop.Ready)continue;
    if(drop.Projectile){drop.Root.transform.position=Vector3.Lerp(drop.Previous,drop.Position,Mathf.Clamp01((Time.unscaledTime-drop.Received)/.05f));drop.Visual.localRotation=Quaternion.Euler(0,Time.time*180,0);continue;}var target=Character.localCharacter.Center;bool near=Vector3.Distance(drop.Position,WorldDiagnostics.Feet(Character.localCharacter))<1.5f;
    if(near&&drop.Attract<0){drop.Attract=Time.unscaledTime;drop.From=drop.Root.transform.position;}if(!near)drop.Attract=-1;
    drop.Root.transform.position=drop.Attract>=0?Vector3.Lerp(drop.From,target,Mathf.Clamp01((Time.unscaledTime-drop.Attract)/.15f)):drop.Position;
    drop.Visual.localPosition=Vector3.up*(Mathf.Sin(Time.time*Mathf.PI*2)*.05f);drop.Visual.localRotation=Quaternion.Euler(0,Time.time*90,0);
   }
  }
  private void OnDestroy(){Clear();Instance=null;}
 }
}
