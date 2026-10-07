#!/bin/sh
set -eu
peak_dir=${1:-${PEAK_DIR:-}}
if [ -z "$peak_dir" ]; then echo 'Usage: deploy.sh "PEAK game folder"' >&2; exit 1; fi
if pgrep -x PEAK.exe >/dev/null 2>&1; then echo 'Close PEAK first.' >&2; exit 1; fi
build_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
dotnet build "$build_dir/src/PeakCreativeMode.Plugin" -c Release -t:Rebuild -p:DebugType=None -p:DebugSymbols=false "-p:PeakDir=$peak_dir"
test -f "$peak_dir/BepInEx/core/BepInEx.dll"
mkdir -p "$peak_dir/BepInEx/plugins/Peakthrough"
cp "$build_dir/src/PeakCreativeMode.Plugin/bin/Release/netstandard2.1/Peakthrough.dll" "$build_dir/src/PeakCreativeMode.Plugin/bin/Release/netstandard2.1/PeakCreativeMode.Core.dll" "$peak_dir/BepInEx/plugins/Peakthrough/"

for name in PeakCreativeMode.Plugin.dll PeakCreativeMode.Core.dll; do
 legacy="$peak_dir/BepInEx/plugins/PeakCreativeMode/$name"
 if [ -f "$legacy" ]; then mv "$legacy" "$legacy.previous"; fi
done
