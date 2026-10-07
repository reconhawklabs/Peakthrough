#!/bin/sh
set -eu
if [ "$#" -ne 2 ]; then
 echo 'Usage: export_assets.sh "Minecraft client jar" "asset cache folder"' >&2
 exit 1
fi
export_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec java "$export_dir/ExportAssets.java" "$1" "$2"
