using BepInEx.Configuration;
using System.IO;
using PeakCreativeMode.Core;
using UnityEngine.InputSystem;

namespace PeakCreativeMode.Plugin
{
    internal sealed class KeyConfig
    {
        public readonly ConfigEntry<Key> ToggleMcMode, Inventory, CreativePicker, DebugPanel, QuickStart,Terraform,Backpack;
        public readonly ConfigEntry<int> HostileCap,PassiveCap,RunCap,BridgePort,HudScale,ChunksPerFrame,TerraformSize,ServerHeapMb,DragonOrbit;
        public readonly ConfigEntry<bool> PeakBarsTopLeft,LitBlocks,LootReplaceProtected,MobsEnabled,MobsGriefing,MobsSunBurn,NativeTerrainDigging,BlockDeposits,EnableAutomation,AutoStartServer,StopOwnedServer,EnableSounds,UnifiedHotbar,KeepMcItemsOnDeath;
        public readonly ConfigEntry<string> AssetCache,MapSeedOverride,ServerPath,StarterTool,DragonTriggerSegment;
        public readonly ConfigEntry<float> HeldItemScale,CreeperKnockbackMultiplier,LootReplaceChance,OpennessThreshold,TowerJumpSpeed,MobInjuryMultiplier,HungerMultiplier,KnockbackMultiplier,DragonMaxHealth;

        public KeyConfig(ConfigFile cfg)
        {
            PeakBarsTopLeft=cfg.Bind("HUD","PeakBarsTopLeft",true,"Move native PEAK health, status and stamina bars to the top-left, clear of the Minecraft hotbar.");
            HudScale=cfg.Bind("HUD","Scale",4,new ConfigDescription("Minecraft hotbar pixel scale at 1080p; adapts down to fit smaller displays",new AcceptableValueRange<int>(2,6)));
            HeldItemScale=cfg.Bind("Rendering","HeldItemScale",1.5f,new ConfigDescription("Size multiplier for Minecraft items held by PEAK players; drops and mobs retain native size",new AcceptableValueRange<float>(.5f,3)));
            LitBlocks=cfg.Bind("Rendering","LitBlocks",true,"Placed Minecraft blocks receive PEAK directional, ambient and local lighting and shadows.");
            CreeperKnockbackMultiplier=cfg.Bind("Status","CreeperKnockbackMultiplier",2f,new ConfigDescription("Extra knockback for actual native creeper explosions; ordinary attacks unchanged",new AcceptableValueRange<float>(0,6)));
            DragonTriggerSegment=cfg.Bind("Dragon","TriggerSegment","Tropics",new ConfigDescription("Campfire destination segment triggering the finale; Tropics is the first summit",new AcceptableValueList<string>("Tropics","Alpine","Caldera","TheKiln")));
            DragonMaxHealth=cfg.Bind("Dragon","MaxHealth",120f,new ConfigDescription("Native dragon maximum health",new AcceptableValueRange<float>(20,1000)));
            DragonOrbit=cfg.Bind("Dragon","Orbit",60,new ConfigDescription("Native flight orbit radius around the summit",new AcceptableValueRange<int>(20,160)));
            HostileCap=cfg.Bind("Mobs","HostileCap",6,new ConfigDescription("Maximum hostiles within 48 blocks",new AcceptableValueRange<int>(0,64)));
            PassiveCap=cfg.Bind("Mobs","PassiveCap",6,new ConfigDescription("Maximum passive mobs within 48 blocks",new AcceptableValueRange<int>(0,64)));
            RunCap=cfg.Bind("Mobs","RunCap",24,new ConfigDescription("Maximum mobs per run",new AcceptableValueRange<int>(0,128)));
            MobsEnabled=cfg.Bind("Mobs","Enabled",true,"Spawn native Minecraft mobs from host terrain/light surveys.");
            MobsGriefing=cfg.Bind("Mobs","Griefing",false,"Allow native mobs to destroy Minecraft builds.");
            MobsSunBurn=cfg.Bind("Mobs","SunBurn",false,"Allow native sun burning; default off because PEAK owns shade.");
            OpennessThreshold=cfg.Bind("Mobs","OpennessThreshold",.20f,new ConfigDescription("Minimum light-volume openness for exposed daytime cells",new AcceptableValueRange<float>(0,1)));
            StarterTool=cfg.Bind("World","StarterTool","minecraft:iron_pickaxe","Floating starter item near the crash site.");
            NativeTerrainDigging=cfg.Bind("World","NativeTerrainDigging",false,"Opt in to experimental LMB conversion of original PEAK terrain. Natural block deposits are the normal resource source.");
            BlockDeposits=cfg.Bind("World","BlockDeposits",true,"Distribute small Minecraft resource outcrops along the mountain without changing PEAK terrain.");
            ServerHeapMb=cfg.Bind("Server","MaxHeapMb",4096,new ConfigDescription("Native Minecraft heap budget in MB for the raised world",new AcceptableValueRange<int>(1024,8192)));
            UnifiedHotbar=cfg.Bind("Gameplay","UnifiedHotbar",true,"One 9-slot PEAK and Minecraft hotbar; false restores B mode.");
            KeepMcItemsOnDeath=cfg.Bind("Gameplay","KeepMinecraftItemsOnDeath",true,"Keep Minecraft items on death; PEAK items drop at the body.");
            Backpack=cfg.Bind("Keys","Backpack",Key.Digit0,"Select native PEAK backpack.");
            Terraform=cfg.Bind("Keys","Terraform",Key.F6,"Convert nearby original terrain to mineable Minecraft blocks (MC mode).");
            TerraformSize=cfg.Bind("Terraform","RegionSize",8,new ConfigDescription("Local cubic conversion size",new AcceptableValueRange<int>(4,8)));
            ChunksPerFrame=cfg.Bind("Rendering","ChunksPerFrame",4,new ConfigDescription("Maximum chunk mesh rebuilds per frame",new AcceptableValueRange<int>(1,32)));
            EnableSounds=cfg.Bind("Audio","MinecraftSounds",true,"Play nearby native Minecraft sounds from the private local export.");
            AutoStartServer=cfg.Bind("Server","AutoStart",true,"Start the host-only private native server in the Airport.");
            StopOwnedServer=cfg.Bind("Server","StopOnExit",true,"Stop only this PEAK instance's server on exit.");
            ServerPath=cfg.Bind("Server","Path",Path.Combine(BepInEx.Paths.PluginPath,"Peakthrough","data","server"),"Private native runtime installed by tools/install_server.py.");
            MapSeedOverride=cfg.Bind("Bridge","MapSeedOverride","","Optional diagnostic seed; empty uses the actual PEAK daily map. dev accesses legacy builds.");
            EnableAutomation=cfg.Bind("Diagnostics","EnableAutomation",false,"Enable bounded one-shot test commands in BepInEx/config/PeakCreativeMode.test-command.json. Disable for normal play.");
            LootReplaceChance=cfg.Bind("Loot","ReplaceChance",1f,new ConfigDescription("Fraction of natural PEAK spawn candidates replaced by Minecraft loot",new AcceptableValueRange<float>(0,1)));
            LootReplaceProtected=cfg.Bind("Loot","ReplaceProtected",true,"Replace all natural items, including ropes/backpacks/flares. False restores the legacy protected list.");
            KnockbackMultiplier=cfg.Bind("Status","KnockbackMultiplier",6f,new ConfigDescription("PEAK force scale for native mob hits and blasts",new AcceptableValueRange<float>(0f,20f)));
            HungerMultiplier=cfg.Bind("Status","HungerMultiplier",.025f,new ConfigDescription("PEAK Hunger relief per native Minecraft nutrition point",new AcceptableValueRange<float>(0f,1f)));
            MobInjuryMultiplier=cfg.Bind("Status","MobInjuryMultiplier",.025f,new ConfigDescription("PEAK Injury per native Minecraft mob damage point",new AcceptableValueRange<float>(0f,1f)));
            TowerJumpSpeed = cfg.Bind("Building", "TowerJumpSpeed", 8f, new ConfigDescription("Minimum upward speed for a local jump while looking down with a block selected in MC mode. 0 disables assistance.", new AcceptableValueRange<float>(0f,12f)));
            AssetCache = cfg.Bind("Assets", "CachePath", Path.Combine(BepInEx.Paths.PluginPath,"Peakthrough","data","asset-cache","26.3"), "Your private Minecraft 26.3 asset export.");
            ToggleMcMode   = cfg.Bind("Keys", "ToggleMcMode",   Key.B,   "Switch between PEAK mode and Minecraft mode");
            Inventory      = cfg.Bind("Keys", "Inventory",      Key.I,   "Open the Minecraft inventory (Minecraft mode)");
            CreativePicker = cfg.Bind("Keys", "CreativePicker", Key.F7,  "Creative item picker (testing)");
            DebugPanel     = cfg.Bind("Keys", "DebugPanel",     Key.F8,  "Show/hide the bridge debug panel");
            QuickStart     = cfg.Bind("Keys", "QuickStart",     Key.F10, "In the Airport: start a run immediately (host only, testing)");
            BridgePort     = cfg.Bind("Bridge", "Port", 47655, "TCP port of the Minecraft bridge server on 127.0.0.1");
        }

        public static string PathOf(Key key) => "<Keyboard>/" + (key>=Key.Digit1&&key<=Key.Digit0?key.ToString().Substring(5):key.ToString().ToLowerInvariant());

        public (string name, string path)[] All() => new[]
        {
            ("ToggleMcMode", PathOf(ToggleMcMode.Value)), ("Inventory", PathOf(Inventory.Value)),
            ("CreativePicker", PathOf(CreativePicker.Value)), ("DebugPanel", PathOf(DebugPanel.Value)),
            ("QuickStart", PathOf(QuickStart.Value)),("Terraform",PathOf(Terraform.Value)),("Backpack",PathOf(Backpack.Value)),
        };
    }
}
