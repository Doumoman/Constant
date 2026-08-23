# MAP07_03 — Implement Microchunk Transforms

```yaml
status_control:
  task_key: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS
  result_file: REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
```

## TASK TYPE

```text
RUNTIME MICROCHUNK R0/MIRROR_X/MIRROR_Y/R180 TRANSFORMER + TRANSFORM TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_02 PASS/finalize 뒤 `MicrochunkDefinition`에 적용할 deterministic transform layer를 구현한다. 이 Task는 12x8 microchunk의 tile cells, sockets, and object slots를 `R0`, `MIRROR_X`, `MIRROR_Y`, `R180`으로 변환하고, 90-degree rotations을 명시적으로 거부하는 범위까지만 연다.

Transform은 model-level coordinate and direction projection이다. Socket edge compatibility, object slot semantic validation, standalone 96-cell validator, reachability probe, editor authoring UI, CSV import/export, preview/report는 구현하지 않는다. MAP07_04 이후 Task body는 읽거나 시작하지 않는다.

Repair v1.1 note: v1.0 was blocked because MAP07_01's `MicrochunkObjectOrientation` enum exposed only `None`, while this Task requires exact orientation transforms for package tokens `NONE`, `L`, `R`, `U`, and `D`. This repair authorizes exactly one MAP07_01 model-file change: extending that enum in `MicrochunkEnums.cs` with the four directional values required by the package. No other MAP07_01 model edit is allowed.

## Mandatory Read Order

1. `00_MCP_ENTRYPOINT.md`
2. `01_PROJECT_LOCKED_RULES.md`
3. `02_MCP_WORK_RULES.md`
4. `03_DATA_CSV_RULES.md`
5. `04_UNITY_MCP_RULES.md`
6. `05_CHANGE_CONTROL_RULES.md`
7. `07_PATCH_APPLY_RULES.md`
8. `08_STATUS_FINALIZE_RULES.md`
9. `MASTER_IMPLEMENTATION_TASK_LIST.md`
10. `06_IMPLEMENTATION_STATUS.md`
11. 이 Task
12. `REPORTS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
STATUS: PASS
MAP07_02: COMPLETE ELIGIBLE
MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: LOCKED / DO NOT START
SHA-256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
```

이 별도 patch가 적용된 뒤에만 MAP07_03을 실행한다. MAP07_04 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_02 Result SHA-256: 98240add84d955ffdc50c3e22e18eb3a0255d9a1d397e9d6c2039e2488dafc4e
MAP07_02 repaired/current Task SHA-256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
MAP07_02 acceptance: 5001/5001 PASS
MAP07_02 failed/skipped: 0/0
MAP07_02 compile/Console/relevant warnings: 0/0/0
Assets meta: 3339
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_02: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_02: 0
Duplicate GUID groups: 0
```

Starter transform facts from Map Package v1.0:

```text
Microchunk size: 12 x 8 = 96 cells
Transforms enum: R0, MIRROR_X, MIRROR_Y, R180
Forbidden rotations: R90, R270, arbitrary 90-degree rotation
Transform applies to: cell coordinates, direction-facing tile code, socket side and band, object slot position and orientation, route markers
Side enum: L, R, U, D
Object orientation token: NONE, L, R, U, D
```

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/microchunk_object_slots.csv
04_CSV_STARTER/socket_band_definitions.csv
04_CSV_STARTER/tile_code_dictionary.csv
```

Reference는 transform vocabulary, coordinate dimensions, side/orientation spelling, and future handoff boundaries를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

## READ ALLOWLIST

### Existing MAP07_01 microchunk model

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkLocalCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkDefinition.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing MAP07_02 tile-layer rules

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerOccupancy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRules.cs
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
```

위 파일과 matching meta, approved Runtime/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_04+ Task body, socket edge validator, object slot semantic validator, standalone 96-cell validator, reachability probe, editor window, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 기존 MAP07_01 model enum repair — exact 1

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
```

이 파일에서는 `MicrochunkObjectOrientation` enum만 수정할 수 있다. 허용 변경은 package tokens `L`, `R`, `U`, `D`에 대응하는 directional values 추가와 required parser/string conversion test support에 필요한 최소 enum-facing helper adjustment뿐이다. `None`은 보존한다. 다른 enum, constants, tile cell, socket, slot, definition semantics는 수정하지 않는다.

### 신규 Runtime microchunk transform C# — exact 4

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformOptions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformUtility.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformer.cs
```

### 신규 EditMode tests — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 17

MAP07_03 production symbol `MicrochunkTransformer`를 허용하고 MAP07_04+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
```

### Result report — exact 1

```text
MapDesign/MCP/REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
```

## Required Implementation Contract

### Transform kinds and rejection

- Use the existing MAP07_01 transform enum if it already exposes `R0`, `MirrorX`, `MirrorY`, and `R180` semantics.
- Do not add `R90`, `R270`, `Rotate90`, or arbitrary rotation values.
- If a public parse/try-parse helper is added, it must accept only exact package tokens `R0`, `MIRROR_X`, `MIRROR_Y`, `R180`; `R90`, `R270`, empty, and unknown tokens fail deterministically.
- `R0` must return a definition that is value-equivalent to the source but still passes through the same immutable construction/canonicalization path.

### Coordinate projection

Use exact 12x8 formulas:

```text
R0:       (x, y) -> (x, y)
MIRROR_X: (x, y) -> (11 - x, y)
MIRROR_Y: (x, y) -> (x, 7 - y)
R180:     (x, y) -> (11 - x, 7 - y)
```

- Transform every tile cell coordinate and every object slot anchor coordinate.
- Preserve row-major canonical ordering after transform.
- Reject null definitions and null cells/slots through existing model semantics or explicit argument checks.
- Preserve `TileDataComplete`; a complete 96-cell source must produce a complete 96-cell result.

### Side, orientation, and marker projection

Use exact side/orientation formulas:

```text
MIRROR_X: L <-> R, U and D unchanged
MIRROR_Y: U <-> D, L and R unchanged
R180:     L <-> R and U <-> D
R0:       unchanged
```

- Apply the same projection to socket side and object slot orientation when the orientation token is `L`, `R`, `U`, or `D`.
- `NONE` orientation remains `NONE`.
- `MicrochunkObjectOrientation` must support exactly the package object-orientation tokens `NONE`, `L`, `R`, `U`, and `D` after this repair.
- The enum extension must be mechanical and backwards-compatible: existing `NONE` object slots and MAP07_01 tests must still pass unchanged in meaning.
- Marker layer tile-code values move with their owning tile cells and may be optionally remapped by the tile-code remapper described below.
- This Task does not verify whether marker codes match sockets or object slots; that remains later authoring/validation work.

### Direction-facing tile-code handling

- Do not guess direction-facing tile-code renames from string suffixes.
- `MicrochunkTransformOptions` may expose an optional deterministic remapper for tile-code IDs.
- If no tile-code remapper is supplied, preserve exact tile-code strings on all eight layers.
- If a remapper is supplied, apply it consistently per layer and transform kind, including marker layer values.
- Remapping must never convert `NONE` to another value unless the caller explicitly maps it; default behavior preserves `NONE`.

### Socket band handling

- Always transform socket side by the formulas above.
- Do not validate socket openings, edge signatures, or clearance.
- If socket definitions store only `band_id`, preserve exact `band_id` by default.
- `MicrochunkTransformOptions` may expose an optional deterministic socket-band remapper that receives original side, transformed side, original band ID, and transform kind.
- If a socket-band remapper is supplied, use its returned band ID exactly and preserve deterministic ordering.
- If local numeric band ranges are exposed by the existing model, provide utility formulas for range projection, but do not require CSV import or `socket_band_definitions.csv` mutation.

### Definition-level transformation

- `MicrochunkTransformer.Transform(MicrochunkDefinition definition, transform, options)` returns an immutable transform result or transformed definition.
- Preserve definition identity fields except where transform semantics require an explicit derived transform marker; do not rewrite `MicrochunkId` unless a caller-supplied ID projection is provided.
- Preserve sockets, slots, allowed transform metadata, usage class, biome IDs, route roles, weights, budgets, prefab ID, active flag, and notes unless a field is directly transformed by this contract.
- Preserve MAP07_02 tile-layer rule behavior; transformed definitions must still be accepted/rejected by `MicrochunkTileLayerRules` according to the same occupied-layer combinations.
- Applying `MIRROR_X` twice, `MIRROR_Y` twice, or `R180` twice must return a value-equivalent definition.
- `MIRROR_X` followed by `MIRROR_Y` must be value-equivalent to `R180` for coordinates, sides, orientations, and default-preserved codes/bands.

## Forbidden Implementation

```text
MicrochunkSocketEdgeValidator
MicrochunkObjectSlotValidator
Microchunk96CellValidator
MicrochunkReachabilityProbe
MicrochunkAuthoringWindow
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
BoundaryChunkResolver
SectorRecipeResolver
SectorAssembly
GeneratedSectorMicrochunks writer
Generated CSV export writer
Scene/Prefab/ProjectSettings/asmdef/asmref changes
```

Do not modify MAP07_01 model files except the exact `MicrochunkEnums.cs` enum repair allowed above. Do not modify MAP07_02 tile-layer rule files unless a compile-only signature issue makes it unavoidable. If any other MAP07_01/MAP07_02 production edit is required, stop and report `BLOCKED`.

## Required Tests

Create `MicrochunkTransformerTests` with at least these coverage groups:

```text
R0 preserves all coordinates, sides, orientations, tile codes, sockets, and slots
MIRROR_X maps all 96 coordinates to x = 11 - x and swaps L/R only
MIRROR_Y maps all 96 coordinates to y = 7 - y and swaps U/D only
R180 maps all 96 coordinates to both axes and swaps both side pairs
Each transform preserves 96 unique cells for complete definitions
Partial definitions remain partial and duplicate-free after transform
Tile cell row-major ordering is canonical after transform
Object slot anchors transform with the same coordinate formulas
Object slot orientation L/R/U/D/NONE transforms exactly
Socket side transforms exactly
Socket band IDs preserve by default
Socket band remapper is called deterministically when supplied
Tile-code IDs preserve by default on all eight layers
Tile-code remapper applies consistently per layer and transform
NONE remains NONE by default
MIRROR_X twice, MIRROR_Y twice, and R180 twice are involutions
MIRROR_X followed by MIRROR_Y equals R180 under default options
Unknown/R90/R270 transform tokens are rejected when parsing is exposed
Transformed definitions still pass/fail MAP07_02 tile-layer rules identically
No forbidden MAP07_04+ production symbols exist
```

Required actual gates:

```text
MicrochunkTransformerTests >=180 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=5181 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

Static/change-scope gates:

```text
Assets meta 3339 -> 3344
approved MAP07_01 model source modification 1/1: MicrochunkEnums.cs only
MicrochunkObjectOrientation values support NONE/L/R/U/D and no unrelated enum changes
new Runtime Microchunks C#/meta 4/4
new MicrochunkTransformerTests C#/meta 1/1
new folder meta 0
existing boundary test C# modified <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV body changes 0
Generated CSV files created 0
Scene/Prefab changes 0/0
ProjectSettings/Packages changes 0/0
asmdef/asmref changes 0/0
MAP07_01 production source changes exact 1 approved enum file
MAP07_02 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_04+ production hits 0
duplicate GUID groups 0
```

## Result Report Requirements

Write `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md` with:

```text
TASK: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS
STATUS: PASS | FAIL | BLOCKED
MAP07_03: COMPLETE ELIGIBLE only when PASS
MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION: LOCKED / DO NOT START
```

Include exact SHA-256 values for:

```text
MAP07_02 Result input
MAP07_02 repaired/current Task input
MAP07_03 Task file
MAP07_03 v1.1 repair receipt
MicrochunkEnums.cs before/after
Updated MAP07_01 model/API digest
Microchunk transform model/API digest
MAP07_02 tile-layer rules model/API digest
Authoring manifest
```

Include created/changed file list, test counts, compile/Console/warning counts, Assets meta before/after, GUID duplicate count, Authoring CSV unchanged proof, and explicit forbidden-symbol scan for MAP07_04+.

PASS finalize rule: MAP07_03만 COMPLETE로 전환하고 Current Task는 NONE으로 둔다. MAP07_04는 별도 patch가 오기 전까지 LOCKED다.
