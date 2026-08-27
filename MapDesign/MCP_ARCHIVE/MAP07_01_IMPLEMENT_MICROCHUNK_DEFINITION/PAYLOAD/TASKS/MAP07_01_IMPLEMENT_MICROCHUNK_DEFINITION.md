# MAP07_01 — Implement Microchunk Definition

```yaml
status_control:
  task_key: MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION
  result_file: REPORTS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md
```

## TASK TYPE

```text
RUNTIME IMMUTABLE MICROCHUNK DEFINITION MODELS + STRUCTURAL TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP06 phase exit approved 뒤 MAP07의 첫 작업으로 12x8 microchunk를 표현하는 immutable runtime definition model을 만든다. 한 microchunk는 catalog metadata, exact 96 tile cell records, socket definitions, object slot definitions, local coordinate/value objects, and deterministic ordering contract로 구성된다.

이 Task는 data model과 structural creation semantics까지만 연다. CSV import/export, editor mutable painting state, transform application, tile layer collision rule matrix, socket edge validation, slot semantic validation, full standalone 96-cell validator pass, reachability probe, preview/report UI는 구현하지 않는다.

MAP06 generated world/optional-region artifacts는 read-only source-chain baseline으로만 확인한다. MAP07_02 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS
STATUS: PASS
MAP06_10: COMPLETE ELIGIBLE
MAP06 PHASE EXIT: APPROVED
MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: LOCKED / DO NOT START
SHA-256: 690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb
```

이 별도 patch가 적용된 뒤에만 MAP07_01을 실행한다. MAP07_02 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Editor assembly: MapAuthoring.Editor
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP06 phase exit: APPROVED
MAP06_10 Result SHA-256: 690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb
MAP06_10 repaired Task SHA-256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
MAP06 overlay digest: 9cbd3833bf5e8b771f46cc3442c1c60d63493d8ffb7e8aa3c9a701f6a097fefd
MAP06 acceptance: 4705/4705 PASS
MAP06 failed/skipped: 0/0
MAP06 compile/Console/relevant warnings: 0/0/0
Assets meta: 3323
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP06_10: 0
Boundary/recipe/microchunk/tile/socket/edge artifacts created by MAP06_10: 0
Duplicate GUID groups: 0
```

Starter microchunk authoring source facts from Map Package v1.0:

```text
Microchunk size: 12 x 8 = 96 cells
Logical layers: GroundSolid, OneWay, Breakable, Hazard, Liquid, DecorationBack, DecorationFront, Marker
microchunk_catalog rows: 14
microchunk_tile_cells rows: 1344 = 14 x 96
microchunk_sockets rows: 25
microchunk_object_slots rows: 9
Transforms enum: R0, MIRROR_X, MIRROR_Y, R180
Side enum: L, R, U, D
UsageClass enum: TRAVERSAL, BOUNDARY, FILLER, SPECIAL, VILLAGE, ADAPTER
TraversalKind enum: WALK, DROP, CLIMB, OPTIONAL_BREAK, HIDDEN, DECORATION
RouteLayer enum: MANDATORY, OPTIONAL, BOTH
SlotCategory enum: RESOURCE, MAP_ELEMENT, ENEMY, REWARD, NPC, SHOP_ITEM, EVENT_TRIGGER, SPECIAL_ITEM, DECORATION
ToolRequirement enum: NONE, PICKAXE, SHOVEL, ROPE, EXPLOSIVE, ENVIRONMENT
```

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/01_WORLD_FIXED_SPEC.md
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
01_FIXED_SPEC/05_IMPLEMENTATION_ORDER.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/microchunk_object_slots.csv
04_CSV_STARTER/tile_code_dictionary.csv
04_CSV_STARTER/socket_band_definitions.csv
04_CSV_STARTER/edge_signatures.csv
```

Reference는 definition field names, enum spelling, 12x8/96 contract, and future handoff boundaries를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

## READ ALLOWLIST

### Existing constants and CSV domain

```text
Assets/_Game/Map/Runtime/WorldGeneration/Domain/WorldGenConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Domain/SectorCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/OptionalRegionValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/OptionalRegionOverlayBuilder.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/OptionalRegionOverlayTests.cs
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

위 파일과 matching meta, approved Runtime/Test path-only inventory, Authoring CSV/meta count and aggregate hash, full Assets meta GUID, patch marker 이후 change-scope path만 확인할 수 있다.

금지: MAP07_02+ Task body, tile layer rule implementation, transform implementation, socket edge validator, object slot validator, reachability probe, editor window, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime microchunk definition C# — exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkLocalCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkDefinition.cs
```

### 신규 Runtime folder meta — exact if missing

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks.meta
```

이미 폴더와 matching meta가 존재하면 새로 만들지 않는다. 존재하지 않으면 이 Task에서만 생성할 수 있다.

### 신규 EditMode tests — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
```

### 신규 Test folder meta — exact if missing

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks.meta
```

이미 폴더와 matching meta가 존재하면 새로 만들지 않는다. 존재하지 않으면 이 Task에서만 생성할 수 있다.

### 기존 phase-boundary test 수정 — exact up to 15

MAP07_01 production symbols를 허용하고 MAP07_02+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
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

### Result report — exact 1

```text
MapDesign/MCP/REPORTS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md
```

## Required Implementation Contract

### Core constants and coordinates

- `MicrochunkConstants.WidthTiles = 12`, `HeightTiles = 8`, `CellCount = 96`, `LayerCount = 8`.
- `MicrochunkLocalCoord` is an immutable value type with `X`, `Y`, equality/hash/string, `TryCreate`, and strict constructor or factory range checks.
- Valid coordinates are `0 <= x < 12`, `0 <= y < 8`; row-major index is `y * 12 + x`.
- No dependency on sector/world coordinate conversion is introduced beyond constant reuse where appropriate.

### Identity and enums

- `MicrochunkId` is an immutable non-empty ID value object. It preserves exact CSV spelling and rejects null/empty/whitespace.
- Enums mirror Map Package spelling semantically but use C# identifiers safely: usage class, transform, side, traversal kind, route layer, slot category, tool requirement, object orientation, and tile layer.
- Do not introduce new enum values that are not represented by the package reference, except an internal `None` where needed for safe default construction.

### Tile cells

- `MicrochunkTileCell` stores exactly one local coordinate and eight tile-code IDs: ground, one-way, breakable, hazard, liquid, decor back, decor front, marker.
- `NONE` is stored as data, not omitted.
- This Task does not validate layer collision compatibility. It only preserves layer identity and coordinate placement.

### Sockets

- `MicrochunkSocketDefinition` stores `socket_id`, side, band ID, traversal kind, direction token, mandatory flag, tool requirement, edge signature ID, route layer, minimum safe tiles, and notes.
- Side and route-layer values must be strongly typed. `band_id`, `edge_signature_id`, and `direction` can remain validated IDs/tokens at this step.
- This Task does not compare socket bands to outer tile openings and does not resolve edge-signature compatibility.

### Object slots

- `MicrochunkObjectSlotDefinition` stores slot ID, local coordinate anchor, slot category, allowed pool ID, required flag, orientation token, visible-from-route flag, forbidden radius, required marker code, and notes.
- This Task does not check solid overlap, marker presence, pool existence, or gameplay spawn semantics.

### Definition aggregate

- `MicrochunkDefinition` is immutable and exposes read-only collections for tile cells, sockets, and object slots.
- Construction canonicalizes tile cells by row-major coordinate and rejects duplicate coordinates.
- If `TileDataComplete` is true, construction requires exactly 96 unique tile cells.
- If `TileDataComplete` is false, partial cell sets are allowed but coordinates must still be in range and duplicate-free.
- Definition metadata stores display name, width/height, usage class, biome IDs, route roles, allowed transforms, selection weight, threat, cognitive, chain, prefab ID, active, and notes.
- Negative numeric weights/scores/radii are rejected at model-construction level only when the field belongs to this definition aggregate.

## Forbidden Implementation

```text
MicrochunkTileLayerRules
TileLayerRuleMatrix
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

Create `MicrochunkDefinitionTests` with at least these coverage groups:

```text
Constants are exactly 12/8/96/8
Local coordinates accept all 96 valid coordinates
Local coordinates reject x/y below/above bounds
Row-major index and ordering are stable
MicrochunkId rejects null/empty/whitespace and preserves exact spelling
Definition accepts complete 96-cell set and orders cells row-major
Definition rejects duplicate tile cell coordinates
Definition rejects complete definitions with missing cells
Partial definition allowed only when TileDataComplete is false
Tile cell preserves all eight layer codes including NONE
Socket definition preserves side/traversal/route/tool/min-safe fields
Object slot definition preserves anchor/category/pool/required/radius fields
Negative selection weight/threat/cognitive/chain/radius/min-safe rejected where applicable
Allowed transforms preserve exact R0/MIRROR_X/MIRROR_Y/R180 set semantics
No forbidden MAP07_02+ production symbols exist
```

Required actual gates:

```text
MicrochunkDefinitionTests >=128 PASS
Map06ExitTests 180/180 PASS
OptionalRegionOverlayTests 180/180 PASS
OptionalRegionOverlaySceneDrawerTests 40/40 PASS
OptionalRegionValidatorTests 321/321 PASS
InactiveBufferAssignerTests 281/281 PASS
OptionalReturnPolicyResolverTests 289/289 PASS
OptionalRewardTierCalculatorTests 279/279 PASS
OptionalAccessRuleAssignerTests 289/289 PASS
Type0RouteMaskAssignerTests 257/257 PASS
OptionalRegionGrowerTests 234/234 PASS
OptionalAttachmentEnumeratorTests 202/202 PASS
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total >=4833 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

Static/change-scope gates:

```text
Assets meta 3323 -> 3332..3334 depending on preexisting Microchunks folders
new Runtime Microchunks C#/meta 8/8
new MicrochunkDefinitionTests C#/meta 1/1
new Microchunks folder meta 0..2 only
existing boundary test C# modified <=15
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV body changes 0
Generated CSV files created 0
Scene/Prefab changes 0/0
ProjectSettings/Packages changes 0/0
asmdef/asmref changes 0/0
boundary/recipe/socket-edge/generated-sector artifacts 0
MAP06 production source changes 0
duplicate GUID groups 0
```

## Result Report Requirements

Write `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md` with:

```text
TASK: MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION
STATUS: PASS | FAIL | BLOCKED
MAP07_01: COMPLETE ELIGIBLE only when PASS
MAP07_02_IMPLEMENT_TILE_LAYER_RULES: LOCKED / DO NOT START
```

Include exact SHA-256 values for:

```text
MAP06_10 Result input
MAP06_10 repaired Task input
MAP07_01 Task file
Microchunk definition model digest or public API inventory digest
Authoring manifest
```

Include created/changed file list, test counts, compile/Console/warning counts, Assets meta before/after, GUID duplicate count, Authoring CSV unchanged proof, and explicit forbidden-symbol scan for MAP07_02+.

PASS finalize rule: MAP07_01만 COMPLETE로 전환하고 Current Task는 NONE으로 둔다. MAP07_02는 별도 patch가 오기 전까지 LOCKED다.
