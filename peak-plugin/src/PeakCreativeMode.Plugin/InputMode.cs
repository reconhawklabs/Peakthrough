using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using HarmonyLib;
namespace PeakCreativeMode.Plugin
{
    internal sealed class InputMode:MonoBehaviour
    {
        public static InputMode Instance {get;private set;}
        public bool McMode {get;private set;}
        public bool PickerOpen {get;private set;}
        public string Selected {get;private set;}
        private float _nextPlace;
        private bool _using;
        private BlockPos? _pending;
        private string _pendingBlock;
        private float _pendingUntil;
        private JObject _pendingTarget;
        private BlockPos? _mining;
        private string _creative="minecraft:diamond_pickaxe";
        private static readonly string[] Creative={"minecraft:stone","minecraft:cobblestone","minecraft:oak_planks","minecraft:dirt","minecraft:bricks","minecraft:sandstone","minecraft:oak_log","minecraft:white_wool","minecraft:diamond_pickaxe","minecraft:diamond_sword","minecraft:fishing_rod","minecraft:cod","minecraft:campfire","minecraft:zombie_spawn_egg","minecraft:skeleton_spawn_egg","minecraft:cow_spawn_egg","minecraft:ender_dragon_spawn_egg"};
        private void Awake(){Instance=this;}
        public void Receive(JObject msg){Selected=msg?["item"]?.Type==JTokenType.String?(string)msg["item"]:null;}
        private bool Send(string kind,JObject target)
        {
            var b=BridgeBehaviour.Instance;
            return b!=null&&b.Client.Send(new JObject{["t"]="action",["kind"]=kind,["target"]=target}.ToString(Formatting.None));
        }
        private void Update()
        {
            Receive(BridgeBehaviour.Instance?.LastHeld);
            var ch=Character.localCharacter;var kb=Keyboard.current;
            if(!WorldDiagnostics.Playing(ch)){ReleaseUse();McMode=false;PickerOpen=false;_pending=null;return;}
            if(kb==null)return;
            if(!Plugin.Keys.UnifiedHotbar.Value&&!PickerOpen&&ItemUI.Instance?.Open!=true&&kb[Plugin.Keys.ToggleMcMode.Value].wasPressedThisFrame){McMode=!McMode;if(!McMode)PickerOpen=false;Plugin.Log.LogInfo("[input] Minecraft mode "+McMode);}
            if((McMode||UnifiedHotbar.Enabled)&&kb[Plugin.Keys.CreativePicker.Value].wasPressedThisFrame)PickerOpen=!PickerOpen;
            if(PickerOpen&&kb.escapeKey.wasPressedThisFrame)PickerOpen=false;
            if(!UnifiedHotbar.McInput||PickerOpen||ItemUI.Instance?.Open==true||BridgeBehaviour.Instance?.Client.Status!=BridgeStatus.Connected||BlockRenderer.Instance?.Ready!=true){_pending=null;StopMining();ReleaseUse();return;}
            if(_pendingBlock!=Selected)_pending=null;
            TryPending(ch);
            var mouse=Mouse.current;if(mouse==null)return;if(mouse.rightButton.wasReleasedThisFrame)ReleaseUse();
            Transform cam=MainCamera.instance!=null?MainCamera.instance.transform:Camera.main?.transform;if(cam==null)return;
            if(mouse.leftButton.wasPressedThisFrame)
            {
                HeldItemRenderer.Instance?.Swing();
                var hits=Physics.RaycastAll(cam.position,cam.forward,8f,~0,QueryTriggerInteraction.Collide);
                Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
                foreach(var rayHit in hits)
                {
                    if(rayHit.collider.GetComponentInParent<Character>()==ch)continue;
                    var mob=rayHit.collider.GetComponentInParent<MobHitbox>();if(mob!=null){if(MeleeTarget.CanHit(mob.Part,rayHit.distance)){StopMining();var attack=new JObject{["entityId"]=mob.EntityId};if(mob.Part!=null)attack["part"]=mob.Part;Send("attack",attack);return;}break;}if(rayHit.collider.isTrigger)continue;break;
                }
            }
            if(mouse.leftButton.wasPressedThisFrame)Send("attack",new JObject());
            bool hitSomething=Physics.Raycast(cam.position,cam.forward,out var hit,4.5f*Coords.BLOCK_SIZE,HelperFunctions.terrainMapMask,QueryTriggerInteraction.Ignore);
            BlockPos? targeted=null;JObject target=new JObject();
            if(hitSomething)
            {
                Vector3 inside=hit.point-hit.normal*.02f;var pos=Coords.UnityPointToMcBlock(inside.x,inside.y,inside.z);
                string face=Face(hit.normal);target=new JObject{["x"]=pos.X,["y"]=pos.Y,["z"]=pos.Z,["face"]=face};
                if(!NativeTerrainClipper.StaticTerrain(hit.collider)&&BlockRenderer.Instance.Grid.Get(pos)!=BlockGrid.Air)targeted=pos;
            }
            if(mouse.leftButton.isPressed&&targeted.HasValue&&Terraformer.Instance.ReadyForMining(targeted.Value))
            {
                HeldItemRenderer.Instance?.Swing();
                if(!_mining.HasValue||!_mining.Value.Equals(targeted.Value)){StopMining();if(Send("mine_start",target))_mining=targeted;}
            }
            else {StopMining();if(Plugin.Keys.NativeTerrainDigging.Value&&mouse.leftButton.isPressed&&hitSomething&&NativeTerrainClipper.StaticTerrain(hit.collider)&&MineGrid.CanDig(Selected,Terraformer.TerrainKind())){var inside=hit.point-hit.normal*.02f;Terraformer.Instance.RequestQuick(MineGrid.CellOrigin(new V3(inside.x,inside.y,inside.z)));MiningOverlay.Instance.ShowAt(hit.point,hit.normal);}}
            if(!mouse.rightButton.wasPressedThisFrame||Time.unscaledTime<_nextPlace)return;
            _nextPlace=Time.unscaledTime+.15f;
            if(!hitSomething){_using=Send("use",new JObject());return;}
            if(Selected==null||!IsBlock(Selected)){_using=Send("use",target);return;}
            var block=BlockTargets.Adjacent(new BlockPos((int)target["x"],(int)target["y"],(int)target["z"]),(string)target["face"]);
            _pending=block;_pendingBlock=Selected;_pendingTarget=target;_pendingUntil=Time.unscaledTime+.65f;
            TryPending(ch);
        }
        internal static bool IsBlock(string id){if(id==null||!id.StartsWith("minecraft:")||id.Contains(".."))return false;string name=id.Substring(10);return System.IO.File.Exists(System.IO.Path.Combine(Plugin.Keys.AssetCache.Value,"assets","minecraft","blockstates",name+".json"));}
        private static string Face(Vector3 n)
        {
            if(Mathf.Abs(n.y)>=Mathf.Abs(n.x)&&Mathf.Abs(n.y)>=Mathf.Abs(n.z))return n.y>0?"up":"down";
            if(Mathf.Abs(n.x)>=Mathf.Abs(n.z))return n.x>0?"east":"west";
            return n.z>0?"north":"south";
        }
        private void ReleaseUse(){if(_using){Send("use",new JObject{["release"]=true});_using=false;}}
        private void StopMining(){if(_mining.HasValue){Send("mine_stop",new JObject());_mining=null;}}

        private void TryPending(Character ch)
        {
            if(!_pending.HasValue)return;
            if(!TowerPlacement.Pending(Time.unscaledTime,_pendingUntil)){_pending=null;return;}
            var block=_pending.Value;
            // Reject a cube overlapping the local body (avoids wedging the player in a new collider).
            var min=Coords.McBlockMinToUnity(block.X,block.Y,block.Z);var bounds=new Bounds(new Vector3(min.X+.5f,min.Y+.5f,min.Z+.5f),Vector3.one*.98f);
            // Below both feet, unrelated arm/body AABBs must not veto safe tower placement.
            if(!TowerPlacement.FeetClear(WorldDiagnostics.Feet(ch).y,min.Y+Coords.BLOCK_SIZE))
                foreach(var col in ch.GetComponentsInChildren<Collider>())if(col.enabled&&!col.isTrigger&&bounds.Intersects(col.bounds))return;
            if(Send("use",_pendingTarget))Plugin.Log.LogInfo($"[input] place requested {_pendingBlock} at {block.X},{block.Y},{block.Z}");
            _pending=null;
        }
        private void OnGUI()
        {
            var ch=Character.localCharacter;if(!WorldDiagnostics.Playing(ch))return;
            var b=BridgeBehaviour.Instance;string status=b?.Client.Status.ToString().ToLowerInvariant()??"offline";
            var inventory=ItemUI.Instance?.State;string heldName=inventory?.Slots[inventory.Selected].Peak?.Name??Selected?.Replace("minecraft:","").Replace('_',' ')??"Empty hand";
            GUI.Box(new Rect(10,Screen.height-65,520,55),$"MC: {status}\n{heldName}");
            if(!PickerOpen)return;
            GUI.Box(new Rect(20,150,350,645),"Creative items (testing) — F7 / Esc closes");
            for(int i=0;i<Creative.Length;i++)
            {
                string block=Creative[i];
                if(GUI.Button(new Rect(35,180+i*32,295,28),block.Substring("minecraft:".Length))&&Send("creative_give",new JObject{["block"]=block}))PickerOpen=false;
            }
            _creative=GUI.TextField(new Rect(35,715,220,28),_creative);
            if(GUI.Button(new Rect(260,715,90,28),"Give")&&Send("creative_give",new JObject{["item"]=_creative}))PickerOpen=false;
        }
    }
    // Suppress only item use; PEAK movement and climbing remain active.
    [HarmonyPatch(typeof(CharacterItems),"DoUsing")]
    internal static class SuppressPeakItemUse
    {
        private static bool Prefix(Character ___character)=>!(UnifiedHotbar.McInput&&___character==Character.localCharacter);
    }
    [HarmonyPatch(typeof(CharacterItems),"DoDropping")]
    internal static class SuppressPeakDropping
    {private static bool Prefix(Character ___character)=>!(UnifiedHotbar.McInput&&___character==Character.localCharacter);}
    [HarmonyPatch(typeof(CharacterItems),"DoSwitching")]
    internal static class SuppressPeakSwitching
    {private static bool Prefix(Character ___character)=>!((UnifiedHotbar.Enabled||UnifiedHotbar.McInput)&&___character==Character.localCharacter);}
    [HarmonyPatch(typeof(CursorHandler),"Update")]
    internal static class PickerCursor
    {
        private static bool Prefix()
        {
            if(InputMode.Instance?.PickerOpen!=true&&ItemUI.Instance?.Open!=true)return true;
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return false;
        }
    }
    [HarmonyPatch(typeof(Character),"CanDoInput")]
    internal static class PickerBlocksBodyInput
    {
        private static void Postfix(Character __instance,ref bool __result)
        {if(__instance==Character.localCharacter&&(InputMode.Instance?.PickerOpen==true||ItemUI.Instance?.Open==true))__result=false;}
    }
}
