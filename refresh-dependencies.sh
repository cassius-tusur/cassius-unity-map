#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

map_project=external/Map/src/map/Map.Core/Map.Core.csproj
location_project=external/ProceduralGenerators/src/LocationGenerator/LocationGenerator.csproj
plugins=src/map/Packages/com.ang-gpo.map.unity/Runtime/Plugins

if [[ ! -f "$map_project" || ! -f "$location_project" ]]; then
  echo 'Initialize submodules first: git submodule update --init --recursive' >&2
  exit 1
fi

dotnet build "$map_project" --framework netstandard2.1 -m:1 -p:CopyLocalLockFileAssemblies=true
dotnet build "$location_project" --framework netstandard2.1 -m:1 -p:CopyLocalLockFileAssemblies=true
map_output=external/Map/src/map/Map.Core/bin/Debug/netstandard2.1
location_output=external/ProceduralGenerators/src/LocationGenerator/bin/Debug/netstandard2.1

sources=()
destinations=()
for existing in "$plugins"/*.dll; do
  name=${existing##*/}
  if [[ "$name" == LocationGenerator.dll ]]; then
    source="$location_output/$name"
  elif [[ -f "$map_output/$name" ]]; then
    source="$map_output/$name"
  elif [[ -f "$location_output/$name" ]]; then
    source="$location_output/$name"
  else
    echo "No build output for $name; verify the runtime dependency closure." >&2
    exit 1
  fi
  sources+=("$source")
  destinations+=("$existing")
done
for index in "${!sources[@]}"; do
  cp "${sources[$index]}" "${destinations[$index]}"
done
echo 'Updated Unity plugin DLLs. Reopen Unity and run its editor tests.'
