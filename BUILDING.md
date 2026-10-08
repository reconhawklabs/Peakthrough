# Building Peakthrough

Players use Setup.cmd / Setup.sh. These steps are for contributors.

Requirements: Java 25 JDK, .NET SDK 10, Python 3, your own installed PEAK and BepInExPack_PEAK.

1. Build the PEAK plugin: `dotnet build peak-plugin/src/PeakCreativeMode.Plugin -c Release -p:PeakDir="YOUR PEAK FOLDER"`.
2. Build Minecraft: from `minecraft-bridge`, run `gradlew.bat build preparePrivateServer` on Windows, or `./gradlew build preparePrivateServer` on Linux.
3. Copy `Peakthrough.dll` and `PeakCreativeMode.Core.dll` from the plugin's `bin/Release/netstandard2.1` folder into the repository's `plugin` folder.
4. Copy the built Minecraft JAR into `fabric/Peakthrough-Bridge-1.0.5.jar`.

Keep game DLLs, extracted assets, runtime dependencies, saves and logs off Git. The old internal plugin/config ID is intentionally retained for upgrade compatibility.
