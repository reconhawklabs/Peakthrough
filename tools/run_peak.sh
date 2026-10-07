#!/bin/sh
set -eu
if [ "$#" -ne 1 ]; then echo 'Usage: run_peak.sh "PEAK game folder"' >&2; exit 1; fi
"$1/BepInEx/plugins/Peakthrough/data/server/server_start.sh" --owner peak-wrapper
steam steam://rungameid/3527290
