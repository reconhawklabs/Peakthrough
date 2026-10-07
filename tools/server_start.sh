#!/bin/sh
# Wine start /unix launches this bounded native supervisor without Gradle.
server_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
exec python3 "$server_dir/server_runtime.py" "$@"
