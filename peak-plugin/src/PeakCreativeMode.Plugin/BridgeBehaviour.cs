using Newtonsoft.Json.Linq;
using PeakCreativeMode.Core;
using Photon.Pun;
using UnityEngine;

namespace PeakCreativeMode.Plugin
{
    /// Owns the BridgeClient: sends the local player's pose at 20 Hz, drains incoming messages each frame.
    internal sealed class BridgeBehaviour : MonoBehaviour
    {
        private const float PoseInterval = 0.05f;

        public static BridgeBehaviour Instance { get; private set; }
        public IBridgeTransport Client { get; private set; }
        public JObject LastDebugState { get; private set; }
        public JObject LastHeld { get; private set; }
        public int PosesSent { get; private set; }

        private float _nextPose, _nextIdentity;
        public string MapSeed {get;private set;}
        private string _hello,_role,_requestedSeed;
        public string PlayerUuid {get;private set;}
        private static string Role()=>!PhotonNetwork.OfflineMode&&!PhotonNetwork.IsMasterClient?"guest:"+(PhotonNetwork.MasterClient?.ActorNumber??0):"host";

        private void Awake()
        {
            Instance = this;
            Connect(RunIdentity.Current());
        }

        private void Connect(string seed)
        {
            Client?.Stop();
            MapSeed=seed;_requestedSeed=seed;
            _hello=Protocol.Hello(PlayerId(),seed,RunIdentity.Biomes());
            string hello=_hello;
            _role=Role();
            Client=_role.StartsWith("guest:")?(IBridgeTransport)new PhotonGuestBridge(hello):new BridgeClient("127.0.0.1",Plugin.Keys.BridgePort.Value,()=>hello,line=>Plugin.Log.LogInfo(line));
            Client.Start();
        }

        private static string PlayerId()
        {
            string id = PhotonNetwork.LocalPlayer?.UserId;
            return string.IsNullOrEmpty(id) ? SystemInfo.deviceUniqueIdentifier : id;
        }

        private void Update()
        {
            if(Time.unscaledTime>=_nextIdentity){_nextIdentity=Time.unscaledTime+1f;string seed=RunIdentity.Current();if(seed!=_requestedSeed||Role()!=_role)Connect(seed);}
            if(Client is PhotonGuestBridge guest && guest.NeedsReconnect)Connect(_requestedSeed);
            Client.Tick();
            var receiveBudget=System.Diagnostics.Stopwatch.StartNew();
            for(int received=0;received<96&&receiveBudget.Elapsed.TotalMilliseconds<2&&Client.TryReceive(out var msg);received++)
            {
                if (Client.Status != BridgeStatus.Connected) continue;
                switch ((string)msg["t"])
                {
                    case "welcome": EmbeddedArrows.Instance?.Clear(); MapSeed=Protocol.WelcomeSeed(msg,_requestedSeed);WorldEventLink.Instance?.Replay(); BossHud.Instance?.Clear(); EffectLink.Instance?.Clear(); LootReplacer.Instance?.Replay(); HeldItemRenderer.Instance?.Clear(); Terraformer.Instance?.Clear(); PlayerUuid=(string)msg["playerUuid"]; BlockRenderer.Instance?.Clear(); LastDebugState=null; LastHeld=null; ItemUI.Instance?.Clear();ItemEntities.Instance?.Clear();EntityRenderer.Instance?.Clear();FishingRenderer.Instance?.Clear(); break;
                    case "debug_state": LastDebugState=msg; break;
                    case "chunk": case "block": BlockRenderer.Instance?.Receive(msg);Terraformer.Instance?.Receive(msg); break;
                    case "avatar_held":HeldItemRenderer.Instance?.Receive(msg);break;
                    case "held": LastHeld=msg; break;
                    case "peak_ack": UnifiedHotbar.Instance?.Ack(msg);break;
                    case "inventory": ItemUI.Instance?.Receive(msg);break;
                    case "entity_add":case "entity_update":case "entity_remove":ItemEntities.Instance?.Receive(msg);EntityRenderer.Instance?.Receive(msg);FishingRenderer.Instance?.Receive(msg);break;
                    case "sound":SoundRenderer.Instance?.Receive(msg);break;
                    case "damage":StatusLink.Receive(msg);break;
                    case "eat":StatusLink.Eat(msg);break;
                    case "teleport":TeleportLink.Receive(msg);break;
                    case "boss":case "boss_remove":BossHud.Instance?.Receive(msg);break;
                    case "effect":EffectLink.Instance?.Receive(msg);break;
                    case "break_progress":MiningOverlay.Instance?.Receive(msg);break;
                }
            }

            if (Client.Status != BridgeStatus.Connected || Time.unscaledTime < _nextPose) return;
            _nextPose = Time.unscaledTime + PoseInterval;
            var ch = Character.localCharacter;
            if(!WorldDiagnostics.Playing(ch)){Client.Send("{\"t\":\"pose\",\"active\":false}");return;}
            Vector3 pos = WorldDiagnostics.Feet(ch);
            Transform cam = MainCamera.instance != null ? MainCamera.instance.transform : Camera.main?.transform;
            Vector3 euler = cam != null ? cam.eulerAngles : ch.transform.eulerAngles;
            if (Client.Send(Protocol.Pose(pos.x, pos.y, pos.z, euler.y, euler.x))) PosesSent++;
        }

        private void OnDestroy() => Client?.Stop();
    }
}
