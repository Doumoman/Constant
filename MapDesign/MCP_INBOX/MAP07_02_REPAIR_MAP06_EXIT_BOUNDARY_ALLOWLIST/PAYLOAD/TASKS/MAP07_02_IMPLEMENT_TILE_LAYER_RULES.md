# MAP07_02 — Implement Tile Layer Rules

```yaml
status_control:
  task_key: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
  result_file: REPORTS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
```

## TASK TYPE

```text
RUNTIME MICROCHUNK TILE-LAYER COMPATIBILITY RULE MATRIX + CELL/DEFINITION RULE TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_01 PASS/finalize 뒤 `MicrochunkTileCell` and `MicrochunkDefinition`에 적용할 deterministic tile-layer compatibility rule matrix를 구현한다. 이 Task는 한 cell의 eight logical layers가 동시에 존재할 때 허용/금지 조합을 판정하고, definition 전체의 rule violations를 immutable report로 반환하는 범위까지만 연다.

`NONE`은 빈 레이어이며 error가 아니다. DecorationBack/DecorationFront는 non-colliding presentation layers로 모든 logical layer와 공존할 수 있다. Marker는 debug/authoring overlay이며 이 Task에서 명시 허용한 조합에만 공존할 수 있다.

Transform application, socket edge validation, object slot semantic validation, standalone 96-cell validator, reachability probe, editor authoring UI, CSV import/export, preview/report는 구현하지 않는다. MAP07_03 이후 Task body는 읽거나 시작하지 않는다.

## Repair v1.2 Note

MAP07_02 v1.0은 required API `MicrochunkTileLayerRules`와 existing `MicrochunkDefinitionTests.cs`의 obsolete absence assertion이 충돌했다. v1.1은 그 fixture의 boundary symbol만 교체할 수 있게 고쳤다. v1.2는 같은 obsolete symbol이 `Map06ExitTests.cs`에도 남아 있어 MAP06 aggregate gate를 막는 충돌만 교정한다.

v1.1에서 이미 승인한 `MicrochunkDefinitionTests.cs` replacement는 그대로 유지한다. v1.2의 추가 허용 수정은 `Map06ApprovedSourceChainAndPhaseExitRemainExact`의 obsolete `MicrochunkTileLayerRules` absence entry 1개를 MAP07_03+ forbidden production symbol `MicrochunkTransformer`로 교체하는 것뿐이다. `Map06ExitTests` case/parameterized-case 수와 모든 MAP06 exit assertion은 유지해야 하며, assertion weakening/delete/skip/ignore는 금지한다.

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
12. `REPORTS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION
STATUS: PASS
MAP07_01: COMPLETE ELIGIBLE
MAP07_02_IMPLEMENT_TILE_LAYER_RULES: LOCKED / DO NOT START
SHA-256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
```

이 별도 patch가 적용된 뒤에만 MAP07_02를 실행한다. MAP07_03 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_01 Result SHA-256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
MAP07_01 Task SHA-256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
MAP07_01 model inventory digest: 673f8a5057a28e6b2dbceac1a43f4eee4b30f0ec2a3738939107759e229cb7d5
MAP07_01 acceptance: 4851/4851 PASS
MAP07_01 failed/skipped: 0/0
MAP07_01 compile/Console/relevant warnings: 0/0/0
Assets meta: 3334
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_01: 0
Forbidden MAP07_02+ production hits in MAP07_01: 0
Duplicate GUID groups: 0
```

Starter tile authoring rule facts from Map Package v1.0:

```text
Microchunk size: 12 x 8 = 96 cells
Logical layers: GroundSolid, OneWay, Breakable, Hazard, Liquid, DecorationBack, DecorationFront, Marker
Allowed table: Ground+Marker, OneWay+Marker, Breakable+Marker, Hazard+Marker, Decoration+all logical layers
Forbidden table: Ground+Breakable, Ground+OneWay, Breakable+OneWay, Solid+Liquid default forbidden
Sparse cells: forbidden by future validator, but this task can validate any supplied cells independently
```

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/tile_code_dictionary.csv
04_CSV_STARTER/microchunk_tile_cells.csv
```

Reference는 rule matrix semantics와 layer vocabulary를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

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

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/OptionalRegionOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/OptionalRegionOverlaySceneDrawerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
```

위 파일과 matching meta, approved Runtime/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_03+ Task body, transform implementation, socket edge validator, object slot semantic validator, standalone 96-cell validator, reachability probe, editor window, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime microchunk tile-layer rules C# — exact 4

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerOccupancy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRules.cs
```

### 신규 EditMode tests — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
```

### 기존 MAP07_01 microchunk boundary test 수정 — exact 1

MAP07_02 production symbol을 허용하고 MAP07_03+ future symbols 금지를 유지하기 위해 아래 existing test C#을 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
```

허용되는 수정은 exact one boundary-symbol replacement뿐이다. `Map0702PlusProductionSymbolsAreAbsent`의 obsolete `MicrochunkTileLayerRules` absence case를 MAP07_03+ forbidden production symbol로 교체한다. `MicrochunkDefinitionTests` total case count는 `146`으로 유지하고, 다른 assertions/delete/skip/ignore는 금지한다.

### 기존 phase-boundary test 수정 — exact up to 16

MAP07_02 production symbols를 허용하고 MAP07_03+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionModelsTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAttachmentEnumeratorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionGrowerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Type0RouteMaskAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalAccessRuleAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRewardTierCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalReturnPolicyResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/InactiveBufferAssignerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/OptionalRegionValidatorTests.cs
```

`Map06ExitTests.cs`에는 v1.2에서만 exact one boundary-symbol replacement를 허용한다. `Map06ApprovedSourceChainAndPhaseExitRemainExact`의 obsolete `MicrochunkTileLayerRules` entry만 `MicrochunkTransformer`로 교체한다. 해당 fixture의 case count, aggregate denominator `2746`, 그리고 MAP06 exit-approved assertion은 변경하지 않는다.

### Result report — exact 1

```text
MapDesign/MCP/REPORTS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
```

## Required Implementation Contract

### Occupancy extraction

- `MicrochunkTileLayerOccupancy` derives occupied logical layers from a `MicrochunkTileCell`.
- A layer is occupied when its code is not exactly `NONE` and not null/empty.
- `NONE` values are preserved by `MicrochunkTileCell`; this rule layer only treats them as unoccupied.
- Null/empty tile-code values in existing model construction should remain invalid through MAP07_01 model semantics.

### Rule result and violations

- `MicrochunkTileLayerRuleViolation` records local coordinate, first layer, second layer, first code, second code, and deterministic reason code/string.
- `MicrochunkTileLayerRuleResult` is immutable, exposes total evaluated cells, violation count, success bool, and read-only ordered violations.
- Violation ordering is deterministic: row-major coordinate, then layer priority order, then reason.

### Allowed combinations

Allowed exact combinations:

```text
Single occupied logical layer: allowed
DecorationBack with any layer: allowed
DecorationFront with any layer: allowed
DecorationBack + DecorationFront: allowed
GroundSolid + Marker: allowed
OneWay + Marker: allowed
Breakable + Marker: allowed
Hazard + Marker: allowed
Marker alone: allowed
Empty cell: allowed
```

### Forbidden combinations

Forbidden exact combinations:

```text
GroundSolid + Breakable
GroundSolid + OneWay
Breakable + OneWay
GroundSolid + Liquid
Breakable + Liquid
Hazard + GroundSolid
Hazard + OneWay
Hazard + Breakable
Hazard + Liquid
Liquid + Marker
Liquid + OneWay
```

Rule for unlisted non-decoration pairs:

```text
If two occupied non-decoration layers are not in the allowed table, they are forbidden.
```

This keeps `Solid + Liquid` default-forbidden and prevents accidental composite gameplay semantics from leaking into MAP07_02.

### Definition validation

- `MicrochunkTileLayerRules.ValidateCell(MicrochunkTileCell cell)` validates one cell.
- `MicrochunkTileLayerRules.ValidateDefinition(MicrochunkDefinition definition)` validates every tile cell in the definition.
- This validation does not enforce complete 96-cell coverage beyond what `MicrochunkDefinition` already enforced.
- This validation does not inspect sockets, object slots, transforms, edge signatures, reachability, tile asset prefabs, or CSV rows.

## Forbidden Implementation

```text
MicrochunkTransformer
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

## Required Tests

Create `MicrochunkTileLayerRulesTests` with at least these coverage groups:

```text
NONE-only and empty logical cells pass
Each single logical layer passes
DecorationBack and DecorationFront coexist with every logical layer
Ground/OneWay/Breakable/Hazard marker combinations pass exactly as allowed
Ground+Breakable, Ground+OneWay, Breakable+OneWay fail
Solid+Liquid defaults fail for Ground+Liquid and Breakable+Liquid
Hazard with blocking/liquid layers fails unless only marker/decoration is present
Liquid+Marker and Liquid+OneWay fail as unlisted non-decoration pairs
Multiple violations in one cell are reported deterministically
Definition-level validation aggregates row-major ordered violations
MAP07_01 MicrochunkDefinitionTests still pass 146/146 after exact boundary-symbol replacement only
No forbidden MAP07_03+ production symbols exist
```

Required actual gates:

```text
MicrochunkTileLayerRulesTests >=128 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 aggregate 2746/2746 PASS
MAP05 aggregate 1959/1959 PASS
Actually executed total >=4979 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

Static/change-scope gates:

```text
Assets meta 3334 -> 3339 on the original MAP07_02 attempt; repair resume must remain 3339
Runtime Microchunks C#/meta inventory 4/4, unchanged by v1.2
MicrochunkTileLayerRulesTests C#/meta inventory 1/1, unchanged by v1.2
new folder meta 0
MicrochunkDefinitionTests C# modified exact 1 only for obsolete boundary-symbol replacement
Map06ExitTests C# modified exact 1 only for obsolete boundary-symbol replacement
Generation boundary test C# modified <=16
existing boundary test C# modified total <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV body changes 0
Generated CSV files created 0
Scene/Prefab changes 0/0
ProjectSettings/Packages changes 0/0
asmdef/asmref changes 0/0
MAP07_01 production source changes 0 except reading existing model API
MAP06 production source changes 0
Forbidden MAP07_03+ production hits 0, with MicrochunkTileLayerRules allowed as MAP07_02 production
duplicate GUID groups 0
```

## Result Report Requirements

Write `MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md` with:

```text
TASK: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
STATUS: PASS | FAIL | BLOCKED
MAP07_02: COMPLETE ELIGIBLE only when PASS
MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: LOCKED / DO NOT START
```

Include exact SHA-256 values for:

```text
MAP07_01 Result input
MAP07_01 Task input
MAP07_02 Task file
MAP07_02 repair Task file if applicable
Tile-layer rules model/API digest
Authoring manifest
```

Include created/changed file list, test counts, compile/Console/warning counts, Assets meta before/after, GUID duplicate count, Authoring CSV unchanged proof, and explicit forbidden-symbol scan for MAP07_03+.

PASS finalize rule: MAP07_02만 COMPLETE로 전환하고 Current Task는 NONE으로 둔다. MAP07_03은 별도 patch가 오기 전까지 LOCKED다.

## Repair Acceptance Addendum

When resuming from `MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md` with `STATUS: BLOCKED`, implementation must record both repair-authorized boundary-symbol replacements:

```text
MicrochunkDefinitionTests.cs:
  MicrochunkTileLayerRules -> MicrochunkPreviewReport
  fixture remains 146/146 PASS

Map06ExitTests.cs:
  MicrochunkTileLayerRules -> MicrochunkTransformer
  fixture case count remains unchanged and MAP06 aggregate is 2746/2746 PASS
```

No other pre-existing test C# changes are authorized by v1.2 beyond the v1.1 replacements already recorded in the Result.
