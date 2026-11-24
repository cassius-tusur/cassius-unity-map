# ANG GPO Map Unity Renderer

This UPM package renders a supplied complete `Map.Core.Model.MapSnapshot`. `MapGeneratorDemoBehaviour` is the explicit Unity host for the checked-in acceptance fixture: it generates a v1 snapshot, validates it, saves MessagePack beneath `Application.persistentDataPath`, and a fresh host reloads those same bytes for rendering. Core owns terrain, passage-projected roads, validation and serialization; the Unity package remains presentation and host wiring only and never references legacy scripts under `Assets/Scripts`.

## Setup

This package is self-contained for its managed model dependencies. `Runtime/Plugins` contains the complete `netstandard2.1` closure for `Map.Core`: `Map.Core.dll`, `BaseClasses.dll`, MessagePack, and the `System.*` / `Microsoft.*` libraries required by those assemblies. They are all referenced by the runtime assembly definition. Do not copy them into `Assets/Plugins`, add a registry dependency, or add the legacy scripts as a substitute.

Add this package through Package Manager using the project-local package path, then let Unity compile `Map.Unity.Runtime`. The package has no external or legacy package dependencies.

## Refreshing the checked-in DLLs

Whenever the Core model or serialization changes, restore the pinned MessagePack 3.1.12 graph, build every Unity-compatible target, and replace the package copies before opening the project:

```bash
dotnet restore src/map/Map.Core/Map.Core.csproj -p:TargetFramework=netstandard2.1 -m:1
dotnet build src/base/BaseClasses/BaseClasses.csproj --framework netstandard2.1 -m:1 -p:LangVersion=latest
dotnet build src/LocationGenerator/LocationGenerator.csproj --framework netstandard2.1 -m:1 -p:LangVersion=latest
dotnet build src/map/Map.Core/Map.Core.csproj --framework netstandard2.1 -m:1 -p:LangVersion=latest
cp src/map/Map.Core/bin/Debug/netstandard2.1/Map.Core.dll \
  src/map/Packages/com.ang-gpo.map.unity/Runtime/Plugins/Map.Core.dll
cp src/base/BaseClasses/bin/Debug/netstandard2.1/BaseClasses.dll \
  src/map/Packages/com.ang-gpo.map.unity/Runtime/Plugins/BaseClasses.dll
cp src/LocationGenerator/bin/Debug/netstandard2.1/LocationGenerator.dll \
  src/map/Packages/com.ang-gpo.map.unity/Runtime/Plugins/LocationGenerator.dll
```

The package contains the complete matching `netstandard2.1` runtime closure. From the restored assets for the pinned MessagePack 3.1.12 graph, replace these exact package DLLs as one set: `MessagePack.dll`, `MessagePack.Annotations.dll`, `Microsoft.NET.StringTools.dll`, `System.Collections.Immutable.dll`, `Microsoft.Bcl.AsyncInterfaces.dll`, `System.Buffers.dll`, `System.IO.Pipelines.dll`, `System.Memory.dll`, `System.Numerics.Vectors.dll`, `System.Runtime.CompilerServices.Unsafe.dll`, `System.Text.Encodings.Web.dll`, `System.Text.Json.dll`, and `System.Threading.Tasks.Extensions.dll`, plus freshly built `BaseClasses.dll`, `LocationGenerator.dll`, and `Map.Core.dll`. The matching versions are recorded in `src/map/Map.Core/obj/project.assets.json` under `.NETStandard,Version=v2.1`; copy each entry's `runtime` asset, never a `ref` asset. Keep the package `.meta` files (and add one for a newly introduced DLL), and keep `LocationGenerator.dll` in `Map.Unity.Runtime.asmdef`'s `precompiledReferences`. Reopen Unity after the copies are replaced and resolve any compiler errors before running the smoke test.

## Inspector workflow, generation, and loading

1. Open `Assets/Scenes/MapGeneratorDemo.unity`, select **Map Generator Demo Host**, and expand its `Pixel Profile`. The checked-in Inspector defaults reproduce the legacy 207×200 Pixel configuration. `BiomeTilePalette` maps the ten canonical profile IDs to the legacy colors.
2. To create a new map, adjust `Pixel Profile` if needed and invoke **Generate And Save Sample**. This generates a complete snapshot, validates it, saves `<SnapshotPath>.msgpack` under `Application.persistentDataPath`, and renders its cell texture, decorations, locations, labels, and stored road paths.
3. To display the saved picture later, use a fresh `MapGeneratorDemoBehaviour` with the same `SnapshotPath` and invoke **Load And Render**. Loading reads and renders the persisted snapshot only: it does not generate a new map, so saved dimensions and pixels are preserved even if that host's current Pixel Profile differs.
4. Enter Play mode and confirm that one generated root contains the map display plus the expected features, labels, and roads following their stored `GridPath` values.

`MapGenerationResult.ApplyTo` is optional and belongs after generation when a host needs the story-only `Plot` projection. It writes map context to compatible story locations and projects only roads whose endpoints are story locations; visual decoration roads remain part of the snapshot renderer.
