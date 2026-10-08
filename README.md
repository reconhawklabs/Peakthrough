# Peakthrough

Minecraft blocks, tools, mobs, fishing and a dragon fight inside PEAK. Play alone or with up to four PEAK players.

## Before you start — everyone

1. Own **PEAK** and **Minecraft Java Edition**. This mod uses Minecraft **26.3**.
2. Install [BepInExPack_PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) first. Launch PEAK once, then close it.
3. Install [Java 25 **JDK**](https://adoptium.net/temurin/releases/?version=25) and [Python 3](https://www.python.org/downloads/). On Windows, enable **Add to PATH** during installation. Open **Command Prompt** and run `py -3 --version`; it must show a Python 3 version before you continue. If it fails, repair your Python installation and reopen Command Prompt.
4. [Download Peakthrough v1.0.5](https://github.com/reconhawklabs/Peakthrough/archive/refs/tags/v1.0.5.zip), then **extract the ZIP**. Keep the extracted folder.

## Host — solo or hosting friends

1. Double-click **Setup.cmd** on Windows. On Linux, open a terminal in the extracted folder and run `sh Setup.sh`.
2. Choose **host**. Paste your PEAK folder when asked: Steam → PEAK → Manage → Browse local files → copy the folder address.
3. Read and accept the Minecraft server EULA when prompted. Wait for **“Peakthrough installed”**. First setup downloads files and briefly opens Minecraft to extract your own assets.
4. Launch PEAK. Choose **Host Game** for friends, or **Play Offline** for solo. The background Minecraft server starts automatically.

## Clients — joining a friend in PEAK

1. Complete **Before you start**, then run **Setup.cmd** (Linux: `sh Setup.sh`).
2. Choose **client**, enter your PEAK folder, and wait for **“Peakthrough installed”**.
3. Launch PEAK and join your host’s room normally.

Everyone needs the same Peakthrough version.

Crash-site supplies include a pickaxe, fishing rod, iron sword, **3 ender pearls** and **3 golden apples**. Find or loot TNT; place it and use flint and steel to light it. The dragon campfire has **2 bows and 96 arrows**.

## Controls

**1–9 / wheel:** select items · **same number again:** empty hand · **0:** backpack · **I:** inventory · **left mouse:** mine/attack · **right mouse:** place/use/fish · **Q:** drop.

## Watch from Minecraft — optional

A Minecraft **26.3** player can use **Multiplayer → Direct Connection** and enter the host’s local-network IP followed by `:25565` (on the host’s computer: `127.0.0.1:25565`). They join as flying spectators to watch blocks, mobs and avatars. PEAK’s mountain scenery appears only in PEAK.

No client mod is required to watch. For a Fabric installation, put **fabric/Peakthrough-Bridge-1.0.5.jar** in Minecraft’s **mods** folder alongside Fabric API for 26.3, using [Fabric Loader](https://fabricmc.net/use/). This optional JAR provides the Minecraft bridge and the local model export used by setup. PEAK clients do not install this JAR manually. The host’s server permits LAN observers and uses offline authentication; keep it on a trusted network.

## If something goes wrong

Close PEAK and rerun setup. Keep the error message and `PEAK/BepInEx/LogOutput.log` when reporting a problem. Linux needs Steam launch option `WINEDLLOVERRIDES="winhttp=n,b" %command%` for BepInEx.

Updates: close PEAK, extract the new version and rerun setup with your original host/client choice. Uninstall: remove `BepInEx/plugins/Peakthrough` from your PEAK game folder.

[Build from source](BUILDING.md) · [Credits and license](CREDITS.md)
