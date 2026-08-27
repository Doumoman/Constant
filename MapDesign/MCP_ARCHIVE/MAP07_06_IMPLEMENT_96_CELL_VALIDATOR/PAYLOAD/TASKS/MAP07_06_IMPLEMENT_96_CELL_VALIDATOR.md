# MAP07_06 - Implement 96 Cell Validator

```yaml
status_control:
  task_key: MAP07_06_IMPLEMENT_96_CELL_VALIDATOR
  result_file: REPORTS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md
```

## TASK TYPE

```text
RUNTIME MICROCHUNK 12x8 CELL COVERAGE VALIDATOR + COMPLETE/PARTIAL RECORD TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_05 PASS/finalize 뒤 microchunk tile records가 12x8 logical coverage 계약을 만족하는지 검증하는 deterministic 96-cell validator를 구현한다. 이 Task는 complete chunk의 `0..11 x 0..7` 좌표 96개가 정확히 1회씩 존재하는지, 범위 초과와 중복이 없는지, empty tile도 explicit `NONE` row로 존재하는지만 검증한다.

Tile-layer compatibility, socket-edge validation, object-slot semantic validation, reachability probe, editor authoring grid/window, CSV import/export, preview/report는 구현하지 않는다. MAP07_07 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION
STATUS: PASS
MAP07_05: COMPLETE ELIGIBLE
MAP07_06_IMPLEMENT_96_CELL_VALIDATOR: LOCKED / DO NOT START
SHA-256: 4d805c6ff1702e4e8ecea3be7a337584e4e2856b7d5106d51d1e42c31954029c
```

이 별도 patch가 적용된 뒤에만 MAP07_06을 실행한다. MAP07_07 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_05 Result SHA-256: 4d805c6ff1702e4e8ecea3be7a337584e4e2856b7d5106d51d1e42c31954029c
MAP07_05 Task SHA-256: 141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc
MAP07_05 object-slot validator model/API digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_05 acceptance: 6299/6299 PASS
MAP07_05 failed/skipped: 0/0
MAP07_05 compile/Console/relevant warnings: 0/0/0
Assets meta: 3356
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_05: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_05: 0
Duplicate GUID groups: 0
```

Starter tile-cell facts from Map Package v1.0:

```text
Microchunk dimensions: 12x8 = 96
microchunk_tile_cells.csv columns: microchunk_id, local_x, local_y, 8 tile-layer code columns
All tile-layer code columns are required and use explicit NONE for empty layers
tile_data_complete=1 requires exactly 96 rows
Sparse row omission is forbidden for complete chunks
Empty cells must still be represented by explicit NONE rows
Tile code FK and layer compatibility are separate MAP01/MAP07_02 concerns
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/AUTHORING_ORDER.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
```

Reference는 `tile_data_complete`, 96-row coverage, explicit `NONE` row, and future handoff boundaries를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

## READ ALLOWLIST

### Existing MAP07 runtime model/rules/transforms/validators

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
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketBandDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkEdgeSignatureDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotPoolDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidator.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
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

금지: MAP07_07+ Task body, reachability probe, editor window, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime microchunk 96-cell validation C# - exact 5

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidator.cs
```

### 신규 EditMode tests - exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 17

MAP07_06 production symbol `Microchunk96CellValidator`를 허용하고 MAP07_07+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
```

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md
```

## Required Implementation Contract

### Record and policy model

- `Microchunk96CellRecord` is immutable and stores microchunk ID, source ordinal, raw local X, raw local Y, and optional normalized `MicrochunkTileCell` data.
- Raw X/Y integers are required so this validator can report out-of-range rows without relying on `MicrochunkLocalCoord` construction.
- A record represents the existence of a tile-cell row. A row with all eight tile-layer codes set to `NONE` is valid and still counts as an explicit record.
- `Microchunk96CellValidationPolicy` is immutable and defines whether complete 96-cell coverage is required.
- Default production policy requires complete coverage for tile-data-complete chunks.
- Draft/partial policy may allow missing coordinates but must still reject out-of-range and duplicate coordinates.
- The validator accepts in-memory records or existing complete `MicrochunkDefinition` projections supplied by tests/runtime callers. It must not implement CSV import or mutate Authoring CSV.

### Coverage validation

For each validated microchunk group:

- Valid coordinate domain is exact `0 <= local_x < 12` and `0 <= local_y < 8`.
- Complete coverage requires exactly 96 in-range coordinate records.
- Every coordinate in row-major order `y=0..7`, `x=0..11` must be present exactly once.
- Missing coordinates are reported as `MISSING_CELL_RECORD`.
- Missing coordinates include omitted empty cells that should have been represented by explicit `NONE` rows.
- Duplicate in-range coordinates are reported as `DUPLICATE_CELL_COORDINATE`.
- Out-of-range rows are reported as `CELL_COORDINATE_OUT_OF_RANGE`.
- Row-count mismatch is exposed in the result summary and backed by per-coordinate/per-row violations.
- In-range duplicate rows do not satisfy the missing coordinate they collide with.
- Out-of-range rows do not satisfy any in-range coordinate.
- Partial mode may report missing-count summary without failing on missing coordinates, but it must still report duplicates and out-of-range rows as failures.

### Deliberate non-goals

- Do not validate tile-code foreign keys.
- Do not validate tile-layer compatibility.
- Do not validate socket openings.
- Do not validate object-slot anchors.
- Do not validate reachability or movement.
- Do not create editor UI, preview, import, export, generated CSV, or sector assembly artifacts.

### Result and ordering

- `Microchunk96CellValidationViolation` records microchunk ID, optional source ordinal, optional raw coordinate, optional normalized local coordinate, and stable reason.
- `Microchunk96CellValidationResult` is immutable and exposes evaluated microchunk count, evaluated record count, in-range unique coordinate count, missing count, duplicate count, out-of-range count, issue count, success bool, and ordered violations.
- Violation ordering is deterministic: microchunk ID ordinal, reason category order, row-major coordinate when present, then source ordinal.
- `Microchunk96CellValidator.ValidateRecords(...)` must not mutate source records or supplied collections.
- `Microchunk96CellValidator.ValidateDefinition(...)` must preserve source `MicrochunkDefinition` and only validate its projected tile cells.
- Transformed definitions from MAP07_03 must preserve 96 unique coverage under all approved transforms.

## Forbidden Implementation

```text
MicrochunkReachabilityProbe
MicrochunkAuthoringWindow
MicrochunkSocketAndSlotEditor
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
BoundaryChunkResolver
SectorRecipeResolver
GeneratedSectorMicrochunkWriter
PopulationSlotIndex
StableSpawnId
```

## Required Tests

Create `Microchunk96CellValidatorTests.cs` with deterministic EditMode coverage:

- Immutable record, policy, violation, and result snapshots.
- Exact 12x8 dimension constants and row-major expected coordinate sequence.
- Valid complete record set with 96 rows.
- Valid all-`NONE` complete record set.
- Every single-coordinate omission among all 96 coordinates.
- Multiple missing coordinates in deterministic row-major order.
- Every legal coordinate duplicated once and detected as duplicate.
- Duplicate records do not mask missing coordinates.
- Out-of-range X below/above and Y below/above cases.
- Out-of-range rows do not count toward complete coverage.
- Row-count mismatch summary for missing, duplicate, and out-of-range combinations.
- Partial/draft policy permits missing coordinates while still rejecting duplicates and out-of-range rows.
- Complete policy rejects sparse data even when all present rows are otherwise valid.
- Existing `MicrochunkDefinition` projections validate without mutation.
- Existing MAP07_02 layer compatibility remains out of scope.
- Existing MAP07_04 socket-edge and MAP07_05 object-slot validators remain out of scope.
- All four MAP07_03 transforms preserve complete 96-cell coverage.
- Source record collections, definitions, and normalized tile cells are not mutated.

Required actual gates:

```text
Microchunk96CellValidatorTests >=384 PASS
MicrochunkObjectSlotValidatorTests 483/483 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=6683 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3356 -> 3362
new Runtime C#/meta 5/5
new Test C#/meta 1/1
new folder meta 0
existing boundary test C# modified <=17
matching existing test meta modified 0
Authoring CSV/meta 50/50 and manifest unchanged
Generated CSV files created 0
Scene/Prefab tracked changes 0/0
ProjectSettings/Packages changes 0/0
asmdef/asmref changes 0/0
MAP07_01 production source changes 0
MAP07_02 production source changes 0
MAP07_03 production source changes 0
MAP07_04 production source changes 0
MAP07_05 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_07+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md` containing:

```text
TASK: MAP07_06_IMPLEMENT_96_CELL_VALIDATOR
STATUS: PASS | FAIL | BLOCKED
MAP07_06: COMPLETE ELIGIBLE only if PASS
MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_05 Result SHA-256 `4d805c6ff1702e4e8ecea3be7a337584e4e2856b7d5106d51d1e42c31954029c`.
- MAP07_05 Task SHA-256 `141ba64ee4fadee918c69daa94693a89aac21efb10d14f65576c04c4e66515fc`.
- MAP07_06 Task SHA-256 from this file.
- 96-cell validator deterministic model/API digest.
- Preserved object-slot, socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and manifest hash proving no source CSV mutation.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_07+ production symbols remain absent.

PASS finalization may only mark MAP07_06 COMPLETE and set Current Task to NONE. MAP07_07 remains LOCKED until a separate MAP07_07 patch is applied.
