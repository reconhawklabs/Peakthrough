using BepInEx;
using BepInEx.Logging;
using PeakCreativeMode.Core;

namespace PeakCreativeMode.Plugin
{
    [BepInPlugin(BuildInfo.PluginGuid, BuildInfo.PluginName, BuildInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static Plugin Instance;
        internal static KeyConfig Keys;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{BuildInfo.PluginName} loaded v{BuildInfo.Version}");
            Keys = new KeyConfig(Config);
            BindingDump.Install(Keys);
            gameObject.AddComponent<QuickStart>();gameObject.AddComponent<ScriptedChecks>();
            gameObject.AddComponent<BlockRenderer>();
            gameObject.AddComponent<InputMode>();
            gameObject.AddComponent<ItemUI>();gameObject.AddComponent<PeakItemBridge>();gameObject.AddComponent<UnifiedHotbar>();gameObject.AddComponent<HotbarHud>();gameObject.AddComponent<HeldItemRenderer>();gameObject.AddComponent<BowFov>();
            gameObject.AddComponent<TerrainSampler>();gameObject.AddComponent<DragonTerrainSampler>();gameObject.AddComponent<SpawnHintSampler>();gameObject.AddComponent<WorldEventLink>();gameObject.AddComponent<LootReplacer>();gameObject.AddComponent<EffectLink>();
            gameObject.AddComponent<ItemEntities>();
            gameObject.AddComponent<EntityRenderer>();gameObject.AddComponent<BossHud>();gameObject.AddComponent<FishingRenderer>();
            gameObject.AddComponent<MiningOverlay>();
            gameObject.AddComponent<WorldDiagnostics>();gameObject.AddComponent<NativeHudLayout>();
            new HarmonyLib.Harmony(BuildInfo.PluginGuid).PatchAll();
            gameObject.AddComponent<NativeTerrainClipper>();gameObject.AddComponent<Terraformer>();
            gameObject.AddComponent<PhotonRelay>();gameObject.AddComponent<PhotonRelayCheck>();
            gameObject.AddComponent<BridgeBehaviour>();
            gameObject.AddComponent<ServerLauncher>();
            gameObject.AddComponent<SoundRenderer>();
            gameObject.AddComponent<DebugPanel>();
        }
    }
}
