# ANG GPO Map Unity Renderer

This UPM package renders a supplied complete `Map.Core.Model.MapSnapshot`. `MapGeneratorDemoBehaviour` is the explicit Unity host for the checked-in acceptance fixture: it generates a v1 snapshot, validates it, saves MessagePack beneath `Application.persistentDataPath`, and a fresh host reloads those same bytes for rendering. Core owns terrain, passage-projected roads, validation and serialization; the Unity package remains presentation and host wiring only and never references legacy scripts under `Assets/Scripts`.

## Setup

This package is self-contained for its managed model dependencies. `Runtime/Plugins` contains the complete `netstandard2.1` closure for `Map.Core`: `Map.Core.dll`, `BaseClasses.dll`, MessagePack, and the `System.*` / `Microsoft.*` libraries required by those assemblies. They are all referenced by the runtime assembly definition. Do not copy them into `Assets/Plugins`, add a registry dependency, or add the legacy scripts as a substitute.

Add this package through Package Manager using the project-local package path, then let Unity compile `Map.Unity.Runtime`. The package has no external or legacy package dependencies.

## Refreshing the checked-in DLLs

Initialize this repository’s submodules recursively and run `bash refresh-dependencies.sh` from the repository root. This builds the pinned Map and ProceduralGenerators sources and updates the packaged DLLs. Run the editor tests in a Unity project after refreshing.

## Inspector workflow, generation, and loading

1. Open `Assets/Scenes/MapGeneratorDemo.unity`, select **Map Generator Demo Host**, and expand its `Pixel Profile`. The checked-in Inspector defaults reproduce the legacy 207×200 Pixel configuration. `BiomeTilePalette` maps the ten canonical profile IDs to the legacy colors.
2. To create a new map, adjust `Pixel Profile` if needed and invoke **Generate And Save Sample**. This generates a complete snapshot, validates it, saves `<SnapshotPath>.msgpack` under `Application.persistentDataPath`, and renders its cell texture, decorations, locations, labels, and stored road paths.
3. To display the saved picture later, use a fresh `MapGeneratorDemoBehaviour` with the same `SnapshotPath` and invoke **Load And Render**. Loading reads and renders the persisted snapshot only: it does not generate a new map, so saved dimensions and pixels are preserved even if that host's current Pixel Profile differs.
4. Enter Play mode and confirm that one generated root contains the map display plus the expected features, labels, and roads following their stored `GridPath` values.

`MapGenerationResult.ApplyTo` is optional and belongs after generation when a host needs the story-only `Plot` projection. It writes map context to compatible story locations and projects only roads whose endpoints are story locations; visual decoration roads remain part of the snapshot renderer.
