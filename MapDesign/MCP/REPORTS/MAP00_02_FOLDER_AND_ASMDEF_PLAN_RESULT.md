# MAP00_02 Folder / Namespace / Assembly Boundary Plan Result

## 1. STATUS

```text
TASK: MAP00_02_FOLDER_AND_ASMDEF_PLAN
STATUS: PASS
DECISION: APPROVED
TASK TYPE: READ-ONLY ARCHITECTURE PLAN
```

The proposed layout is compatible with the audited project convention. This document freezes paths, namespaces, assembly ownership, dependency directions, and the exact structure-only scope that MAP00_03 may create.

Evidence confirmed for this decision:

- Unity version: `6000.3.8f1`
- Runtime assembly: `Game.Map.Runtime`, root namespace `StarNight.Map`, current references `NONE`
- Editor assembly: `MapAuthoring.Editor`, Editor-only, already references `Game.Map.Runtime`
- Runtime EditMode tests: `Game.Map.Tests.EditMode`, already references `Game.Map.Runtime`
- Runtime PlayMode tests: `Game.Map.Tests.PlayMode`, already references `Game.Map.Runtime`
- Editor tests: `MapAuthoring.Tests.EditMode`, already references `Game.Map.Runtime` and `MapAuthoring.Editor`
- Existing Stage and legacy generator names were reconfirmed from the Current Task allowlist.

No `Assets`, asmdef, C#, CSV, Scene, Prefab, Package, or ProjectSettings file was changed.

## 2. Runtime Folder Layout

Decision: `APPROVED`.

```text
Assets/_Game/Map/Runtime/WorldGeneration/
|-- Domain/
|-- Data/
|-- Generation/
|-- Validation/
|-- Random/
`-- Diagnostics/
```

| folder | frozen responsibility | forbidden content |
|---|---|---|
| `Domain` | Scene-independent world constants, integer coordinate value objects, sector/microchunk identity, route and direction domain types | MonoBehaviour ownership, Scene objects, Stage room types, duplicated `GridCell` implementation |
| `Data` | Typed read models consumed after CSV import | Authoring truth in ScriptableObject, CSV schema definitions before their Task |
| `Generation` | Ordered world generation passes and solvers for world grid, site reservation, biome patches, mandatory routes, optional overlay, and sector assembly | `StageMapGenerator`, `P6RoomGraphGenerator`, post-generation tunnel carving or other auto-fix |
| `Validation` | Invariant checks over generated data | Silent repair, mutation that hides generation failure |
| `Random` | Explicit seeds, per-stage RNG streams, stable-ID candidate selection | One shared global RNG, row-order-dependent selection |
| `Diagnostics` | Pure diagnostic records, snapshots, and replay metadata | Editor rendering, EditorWindow, Scene-owned debug objects |

The expected type names in the Task are design examples, not files authorized for MAP00_03.

## 3. Editor Folder Layout

Decision: `APPROVED`.

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/
|-- Import/
|-- Validation/
|-- Preview/
`-- Windows/
```

| folder | frozen responsibility |
|---|---|
| `Import` | UTF-8/invariant-culture CSV parsing adapter, typed-data import, foreign-key validation bridge |
| `Validation` | Authoring-data validation commands and batch-seed validation entry points; delegates runtime invariants to Runtime Validation |
| `Preview` | 13x13 sector visualization, biome/debug overlays, microchunk preview adapters |
| `Windows` | Future WorldGeneration, MicroChunkAuthoring, and SeedReplay editor windows |

All code under this root belongs to `MapAuthoring.Editor`. No Editor type may be placed in Runtime folders.

## 4. Test Folder Layout

Decision: `APPROVED`.

### Runtime pure/EditMode

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
|-- Domain/
|-- Data/
|-- Generation/
|-- Validation/
`-- Determinism/
```

Assembly: `Game.Map.Tests.EditMode`.

### Runtime adapter/PlayMode

```text
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/
```

Assembly: `Game.Map.Tests.PlayMode`. Tests are added here only when a Unity runtime adapter genuinely requires PlayMode.

### Editor tools/EditMode

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/
|-- Import/
|-- Validation/
`-- Preview/
```

Assembly: `MapAuthoring.Tests.EditMode`.

No new test asmdef is approved.

## 5. Authoring Data Folder Layout

Decision: `APPROVED`.

```text
Assets/_Game/Map/Data/WorldGeneration/
|-- Authoring/
|   |-- World/
|   |-- Route/
|   |-- Biome/
|   |-- SpecialMap/
|   |-- Village/
|   |-- MicroChunk/
|   |-- Boundary/
|   |-- Population/
|   `-- Items/
|-- Imported/
`-- GeneratedDebug/
```

Frozen meanings:

- `Authoring`: human-edited CSV source of truth. Authoring schema and files are not created by MAP00_03.
- `Imported`: generated import cache only when a later Task explicitly permits it. Never hand-edited.
- `GeneratedDebug`: seed replay, QA CSV, and diagnostic snapshots. Never reused as Authoring input.

Authoring CSV root: `Assets/_Game/Map/Data/WorldGeneration/Authoring`.

## 6. Namespace Matrix

| path scope | approved namespace |
|---|---|
| Runtime root | `StarNight.Map.WorldGeneration` |
| Runtime `Domain` | `StarNight.Map.WorldGeneration.Domain` |
| Runtime `Data` | `StarNight.Map.WorldGeneration.Data` |
| Runtime `Generation` | `StarNight.Map.WorldGeneration.Generation` |
| Runtime `Validation` | `StarNight.Map.WorldGeneration.Validation` |
| Runtime `Random` | `StarNight.Map.WorldGeneration.Random` |
| Runtime `Diagnostics` | `StarNight.Map.WorldGeneration.Diagnostics` |
| Editor root | `StarNight.MapAuthoring.Editor.WorldGeneration` |
| Editor `Import` | `StarNight.MapAuthoring.Editor.WorldGeneration.Import` |
| Editor `Validation` | `StarNight.MapAuthoring.Editor.WorldGeneration.Validation` |
| Editor `Preview` | `StarNight.MapAuthoring.Editor.WorldGeneration.Preview` |
| Editor `Windows` | `StarNight.MapAuthoring.Editor.WorldGeneration.Windows` |
| Runtime tests root | `StarNight.Map.Tests.WorldGeneration` |
| Runtime test subfolders | `StarNight.Map.Tests.WorldGeneration.<Role>` |
| Editor tests root | `StarNight.MapAuthoring.Tests.WorldGeneration` |
| Editor test subfolders | `StarNight.MapAuthoring.Tests.WorldGeneration.<Role>` |

The namespace boundary is frozen at `StarNight.Map.WorldGeneration.*`. Existing `StarNight.Map`, `StarNight.Stage.*`, `StarNight.Grid`, `StarNight.Generation.P6`, and `StarNight.MapHarness.P11` types remain separate.

## 7. Assembly Matrix

| scope | existing assembly to reuse | new asmdef? | allowed compile references for new WorldGeneration content |
|---|---|---:|---|
| Runtime Domain/Data/Generation/Validation/Random/Diagnostics | `Game.Map.Runtime` | NO | No new assembly reference; current asmdef references remain `NONE` |
| Editor Import/Validation/Preview/Windows | `MapAuthoring.Editor` | NO | `Game.Map.Runtime`; existing assembly references remain unchanged |
| Runtime pure/EditMode tests | `Game.Map.Tests.EditMode` | NO | `Game.Map.Runtime` and existing Unity test runner references |
| Runtime adapter/PlayMode tests | `Game.Map.Tests.PlayMode` | NO | `Game.Map.Runtime`; existing `Game.Stage.Runtime` reference remains but is not a basis for Runtime WorldGeneration code |
| Editor tool tests | `MapAuthoring.Tests.EditMode` | NO | `Game.Map.Runtime`, `MapAuthoring.Editor`, and existing Unity test runner references |

Final decision: reuse all five existing assemblies. Create or modify no asmdef.

## 8. Dependency Rules

### Allowed assembly directions

```text
MapAuthoring.Tests.EditMode
    -> MapAuthoring.Editor
    -> Game.Map.Runtime / WorldGeneration

Game.Map.Tests.EditMode
    -> Game.Map.Runtime / WorldGeneration

Game.Map.Tests.PlayMode
    -> Game.Map.Runtime / WorldGeneration
```

### Forbidden assembly and ownership directions

```text
Game.Map.Runtime
    -X-> Game.Stage.Runtime

Game.Map.Runtime
    -X-> StarNight.Runtime (legacy)

Game.Map.Runtime
    -X-> MapAuthoring.Editor

Game.Map.Runtime
    -X-> UnityEditor

WorldGeneration.Domain
    -X-> MonoBehaviour or Scene object ownership

WorldGeneration.Generation
    -X-> StageMapGenerator

WorldGeneration.Generation
    -X-> P6RoomGraphGenerator or P11MapStageHarness2D
```

Existing downstream assemblies that already reference `Game.Map.Runtime` remain unchanged and are outside this Task.

### Logical Runtime layer directions inside `Game.Map.Runtime`

```text
Domain       -> no WorldGeneration layer
Data         -> Domain
Random       -> Domain only when domain seed/value types are needed
Generation   -> Domain, Data, Random
Validation   -> Domain, Data, generated result contracts
Diagnostics  -> immutable outputs from Domain/Data/Generation/Validation
```

No layer may call a later pass to repair an earlier pass failure.

## 9. Naming Collision Rules

The following existing names are reserved and must not be reused for the broad-world system:

```text
GridWorld
StageMapGenerator
StageMapProfile
StageGeneratedLayout
RoomTemplate
RoomGridTransform
P6RoomGraphGenerator
TileMutationService
P11MapStageHarness2D
```

Required naming guardrails:

- New types use explicit `World`, `Sector`, `MicroChunk`, or `WorldGeneration` domain language.
- Existing `StarNight.Map.GridCell` is not copied, renamed, or treated as the broad-world coordinate authority.
- Existing `RoomGridTransform` is not used as World/Sector/MicroChunk conversion logic.
- Existing `GridWorld` is not used as the 624x416 world model.
- Existing `StageMapGenerator` and legacy P6/P11 logic are not implementation bases.
- Existing `12x8 Micro room` and new `12x8 MicroChunk` are unrelated concepts.
- `RoomTemplate != MicroChunkTemplate`.
- `RoomSizeCatalog.Micro != MicroChunk size authority`.
- A MicroChunk is exactly 12x8 logical tiles and 96 cells under the locked world rules, never a Stage room alias.

## 10. Files/Folders MAP00_03 May Create

MAP00_03 may create only the missing directories listed below and their Unity-generated folder `.meta` files.

### Runtime directories

```text
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Map/Runtime/WorldGeneration/Domain/
Assets/_Game/Map/Runtime/WorldGeneration/Data/
Assets/_Game/Map/Runtime/WorldGeneration/Generation/
Assets/_Game/Map/Runtime/WorldGeneration/Validation/
Assets/_Game/Map/Runtime/WorldGeneration/Random/
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/
```

### Editor directories

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Import/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Validation/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Windows/
```

### Runtime test directories

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Domain/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Determinism/
Assets/_Game/Tests/PlayMode/Map/WorldGeneration/
```

### Editor test directories

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Import/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Validation/
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/
```

### Authoring-data structure directories

```text
Assets/_Game/Map/Data/WorldGeneration/
Assets/_Game/Map/Data/WorldGeneration/Authoring/
Assets/_Game/Map/Data/WorldGeneration/Authoring/World/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Route/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Biome/
Assets/_Game/Map/Data/WorldGeneration/Authoring/SpecialMap/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Village/
Assets/_Game/Map/Data/WorldGeneration/Authoring/MicroChunk/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Boundary/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Population/
Assets/_Game/Map/Data/WorldGeneration/Authoring/Items/
Assets/_Game/Map/Data/WorldGeneration/Imported/
Assets/_Game/Map/Data/WorldGeneration/GeneratedDebug/
```

Placeholder decision:

- `.gitkeep`: NOT ALLOWED.
- Placeholder README: NOT ALLOWED.
- Unity-generated folder `.meta`: ALLOWED and required for created `Assets` directories.
- C# placeholder or empty class: NOT ALLOWED.
- CSV placeholder or schema: NOT ALLOWED.
- New asmdef: NOT ALLOWED.

## 11. Files MAP00_03 Must Not Touch

MAP00_03 must not create, edit, move, or delete any of the following:

```text
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Tests/PlayMode/Map/Game.Map.Tests.PlayMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
Assets/_Game/Map/Runtime/Core/GridCell.cs
Assets/_Game/Stage/**
Assets/StarNight/**
Packages/**
ProjectSettings/**
```

Also forbidden in MAP00_03:

- Any existing C# or asmdef file.
- Any new C# or asmdef file.
- Any CSV or CSV schema.
- Any ScriptableObject, Scene, Prefab, Tile, Tile Palette, Animator, or Addressables asset.
- Any folder outside the explicit allowlist in section 10.
- Any deletion, rename, migration, or cleanup of existing Stage/Map/legacy systems.

## 12. Risks Remaining

| risk | impact | required control in later Tasks |
|---|---|---|
| `Game.Map.Runtime` has `noEngineReferences: false` | Assembly settings cannot enforce a Unity-free Domain | Keep Domain types free of MonoBehaviour/Scene ownership through code review and EditMode tests |
| Existing Editor and PlayMode test assemblies already reference `Game.Stage.Runtime` | Assembly-level references cannot prove new WorldGeneration code is Stage-independent | Enforce namespace/type dependency tests and review imports in later implementation Tasks |
| Existing 12x8 room catalog and new 12x8 MicroChunk share dimensions | Accidental semantic reuse could compile while violating the design | Use explicit MicroChunk coordinate/size authority and boundary tests |
| Existing generator and coordinate types remain in the project | Unqualified names and convenience reuse can reintroduce legacy logic | Use frozen namespaces and reserved-name list; do not add legacy references |
| Empty Unity folders require `.meta` files to persist | A filesystem-only folder plan can disappear from version control | MAP00_03 must create directories through Unity or otherwise verify Unity-generated folder `.meta` files |
| CSV schemas and imported-cache representation are not defined yet | Data files cannot be safely created in MAP00_03 | Keep data directories empty until the relevant MAP01 Task opens |

No risk blocks MAP00_03 structure creation under the frozen allowlist.

## 13. DONE CONDITIONS

- [x] Runtime folder layout confirmed
- [x] Editor folder layout confirmed
- [x] EditMode/PlayMode/Editor test locations confirmed
- [x] Authoring CSV root confirmed
- [x] `StarNight.Map.WorldGeneration.*` namespace boundary confirmed
- [x] Existing assembly reuse confirmed
- [x] New asmdef requirement confirmed as `NO`
- [x] Runtime to Stage reverse reference explicitly forbidden
- [x] Legacy generator reference explicitly forbidden
- [x] Existing 12x8 Micro room and new MicroChunk semantics explicitly separated
- [x] MAP00_03 creation allowlist written
- [x] Project implementation files modified: 0
- [x] Result documents created: 1

## 14. NEXT TASK READY

```text
MAP00_03_CREATE_MAP_MODULE_STRUCTURE = YES
AUTO_START = NO
```

MAP00_03 was not started. The implementation status file remains unchanged by this Task.

Recommended commit message:

```text
docs(map): freeze world generation module boundaries
```
