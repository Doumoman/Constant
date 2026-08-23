# MAP07_04 — Implement Socket Edge Validation

```yaml
status_control:
  task_key: MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION
  result_file: REPORTS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION_RESULT.md
```

## TASK TYPE

```text
RUNTIME MICROCHUNK SOCKET EDGE/BAND/SIGNATURE VALIDATOR + OUTER TILE OPENING TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_03 PASS/finalize 뒤 `MicrochunkDefinition`의 sockets가 실제 outer tile openings와 일치하는지 검증하는 deterministic validator를 구현한다. 이 Task는 socket `side`, `band_id`, `traversal_kind`, `edge_signature_id`, `mandatory_allowed`, `tool_requirement`, `minimum_safe_tiles`를 socket-band and edge-signature definitions 및 tile-cell outer clearance와 대조하는 범위까지만 연다.

Object slot semantic validation, standalone 96-cell validator, reachability probe, editor authoring UI, CSV import/export, preview/report는 구현하지 않는다. MAP07_05 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS
STATUS: PASS
MAP07_03: COMPLETE ELIGIBLE
MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION: LOCKED / DO NOT START
SHA-256: 062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505
```

이 별도 patch가 적용된 뒤에만 MAP07_04를 실행한다. MAP07_05 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_03 Result SHA-256: 062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505
MAP07_03 repaired/current Task SHA-256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
MAP07_03 acceptance: 5484/5484 PASS
MAP07_03 failed/skipped: 0/0
MAP07_03 compile/Console/relevant warnings: 0/0/0
Assets meta: 3344
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_03: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_03: 0
Duplicate GUID groups: 0
```

Starter socket facts from Map Package v1.0:

```text
Socket rows: 25
Socket band definitions: 6
Edge signatures: 9 including EDGE_SOLID
L/R sockets use HORIZONTAL_EDGE bands/signatures
U/D sockets use VERTICAL_EDGE bands/signatures
EDGE_SOLID has axis SOLID and must not appear as a socket row
Socket CSV is source; marker layer is debug/authoring aid only
L/R socket band range requires clear outer cells
U/D socket band range requires clear outer cells; actual reachability remains MAP07_07
```

## Map Package Reference

exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/socket_band_definitions.csv
04_CSV_STARTER/edge_signatures.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/tile_code_dictionary.csv
```

Reference는 socket field names, band/signature vocabulary, starter edge IDs, and future handoff boundaries를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

## READ ALLOWLIST

### Existing MAP07 runtime model/rules/transforms

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkConstants.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkLocalCoord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEnums.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerOccupancy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRuleResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTileLayerRules.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformOptions.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformUtility.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTransformer.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
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

금지: MAP07_05+ Task body, object slot semantic validator, standalone 96-cell validator, reachability probe, editor window, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime microchunk socket-edge validation C# — exact 5

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketBandDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEdgeSignatureDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidator.cs
```

### 신규 EditMode tests — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 17

MAP07_04 production symbol `MicrochunkSocketEdgeValidator`를 허용하고 MAP07_05+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
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
```

### Result report — exact 1

```text
MapDesign/MCP/REPORTS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION_RESULT.md
```

## Required Implementation Contract

### Definition objects

- `MicrochunkSocketBandDefinition` is immutable and stores `band_id`, axis, min/max local coordinate, recommended center, minimum clearance tiles, and description.
- `MicrochunkEdgeSignatureDefinition` is immutable and stores `edge_signature_id`, axis, optional band ID, traversal kind, ground entry height, clearance width/height, tool requirement, mandatory allowed flag, tags, and notes.
- The validator accepts definitions as in-memory objects or read-only dictionaries supplied by tests/runtime callers. It must not implement CSV import or mutate Authoring CSV.
- Axis tokens are exact: `HORIZONTAL_EDGE`, `VERTICAL_EDGE`, `SOLID`.

### Socket metadata validation

For every socket row in a `MicrochunkDefinition`:

- `band_id` must exist in the supplied band dictionary.
- `edge_signature_id` must exist in the supplied signature dictionary.
- `EDGE_SOLID` or any `SOLID` signature must not be referenced by a socket row.
- Socket side axis must match band/signature axis:
  - `L`/`R` require `HORIZONTAL_EDGE`.
  - `U`/`D` require `VERTICAL_EDGE`.
- Signature `band_id` must match socket `band_id` when the signature has a non-empty band ID.
- Signature `traversal_kind` must match socket `traversal_kind`.
- Signature `tool_requirement` must match socket `tool_requirement`.
- If socket `mandatory_allowed` is true, signature `mandatory_allowed` must also be true.
- Socket `minimum_safe_tiles` must be greater than or equal to band `minimum_clearance_tiles`.
- Band min/max must be ordered and in range for the side axis:
  - `HORIZONTAL_EDGE`: `0 <= min <= max < 8` because the band indexes local y on L/R edges.
  - `VERTICAL_EDGE`: `0 <= min <= max < 12` because the band indexes local x on U/D edges.

### Outer tile opening validation

- Use existing MAP07_01 tile cells and MAP07_02 occupancy semantics.
- A tile is blocking for socket clearance when GroundSolid, Breakable, Hazard, or Liquid is occupied.
- DecorationBack, DecorationFront, Marker, and empty `NONE` layers do not block socket clearance.
- OneWay is not a clearance blocker for this Task; reachability and movement semantics remain MAP07_07.
- For `L`, validate cells at `x = 0..minimum_safe_tiles-1` for `y` in the band range.
- For `R`, validate cells at `x = 11-minimum_safe_tiles+1..11` for `y` in the band range.
- For `D`, validate cells at `y = 0..minimum_safe_tiles-1` for `x` in the band range.
- For `U`, validate cells at `y = 7-minimum_safe_tiles+1..7` for `x` in the band range.
- Missing cells in partial definitions are reported as `MISSING_TILE_CELL_FOR_SOCKET_CLEARANCE`; this Task does not become the standalone 96-cell validator.
- The validator reports every blocking or missing cell deterministically.

### Result and ordering

- `MicrochunkSocketEdgeValidationViolation` records microchunk ID, socket ID, side, band ID, edge signature ID, optional local coordinate, and stable reason.
- `MicrochunkSocketEdgeValidationResult` is immutable, exposes evaluated socket count, issue count, success bool, and ordered violations.
- Violation ordering is deterministic: socket order by socket ID ordinal, then reason, then row-major coordinate when present.
- `MicrochunkSocketEdgeValidator.ValidateDefinition(...)` must not mutate source definitions or supplied dictionaries.
- Transformed definitions from MAP07_03 must validate under transformed socket side/band/signature data when the same clear-edge geometry is preserved.

## Forbidden Implementation

```text
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

Do not modify MAP07_01 model files, MAP07_02 tile-layer rule files, or MAP07_03 transform files unless a compile-only signature issue makes it unavoidable. If any production edit outside this Task's exact write allowlist is required, stop and report `BLOCKED`.

## Required Tests

Create `MicrochunkSocketEdgeValidatorTests` with at least these coverage groups:

```text
Band definitions validate horizontal and vertical axis ranges
Invalid band min/max and out-of-range values fail
Edge signature dictionary rejects missing and SOLID socket references
L/R sockets require HORIZONTAL_EDGE
U/D sockets require VERTICAL_EDGE
Signature band/traversal/tool/mandatory fields must match socket fields
Socket minimum_safe_tiles must satisfy band minimum_clearance_tiles
L/R/D/U outer clearance rectangles are checked at exact 12x8 edges
GroundSolid, Breakable, Hazard, and Liquid block clearance
Decoration, Marker, OneWay, and NONE do not block clearance for this Task
Partial definitions report missing cells for socket clearance without enforcing all 96 cells
Multiple violations are ordered deterministically by socket/reason/coordinate
Starter-style 25 socket rows with supplied six bands and nine edge signatures validate when edge cells are open
EDGE_SOLID has no socket row and is rejected when referenced
MAP07_02 tile-layer rules still pass/fail unchanged
MAP07_03 transformed definitions preserve socket validation when geometry is transformed consistently
No forbidden MAP07_05+ production symbols exist
```

Required actual gates:

```text
MicrochunkSocketEdgeValidatorTests >=260 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=5744 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

Static/change-scope gates:

```text
Assets meta 3344 -> 3350
new Runtime Microchunks C#/meta 5/5
new MicrochunkSocketEdgeValidatorTests C#/meta 1/1
new folder meta 0
existing boundary test C# modified <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Authoring CSV body changes 0
Generated CSV files created 0
Scene/Prefab changes 0/0
ProjectSettings/Packages changes 0/0
asmdef/asmref changes 0/0
MAP07_01 production source changes 0
MAP07_02 production source changes 0
MAP07_03 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_05+ production hits 0
duplicate GUID groups 0
```

## Result Report Requirements

Write `MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION_RESULT.md` with:

```text
TASK: MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION
STATUS: PASS | FAIL | BLOCKED
MAP07_04: COMPLETE ELIGIBLE only when PASS
MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION: LOCKED / DO NOT START
```

Include exact SHA-256 values for:

```text
MAP07_03 Result input
MAP07_03 repaired/current Task input
MAP07_04 Task file
Microchunk socket-edge validator model/API digest
MAP07_03 transform model/API digest
MAP07_02 tile-layer rules model/API digest
Updated MAP07_01 model/API digest
Authoring manifest
```

Include created/changed file list, test counts, compile/Console/warning counts, Assets meta before/after, GUID duplicate count, Authoring CSV unchanged proof, and explicit forbidden-symbol scan for MAP07_05+.

PASS finalize rule: MAP07_04만 COMPLETE로 전환하고 Current Task는 NONE으로 둔다. MAP07_05는 별도 patch가 오기 전까지 LOCKED다.
