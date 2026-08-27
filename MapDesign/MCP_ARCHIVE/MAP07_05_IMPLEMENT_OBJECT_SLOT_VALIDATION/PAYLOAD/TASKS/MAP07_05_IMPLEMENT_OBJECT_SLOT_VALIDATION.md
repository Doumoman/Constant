# MAP07_05 — Implement Object Slot Validation

```yaml
status_control:
  task_key: MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION
  result_file: REPORTS/MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION_RESULT.md
```

## TASK TYPE

```text
RUNTIME MICROCHUNK OBJECT SLOT SEMANTIC VALIDATOR + ANCHOR/POOL/MARKER/SAFETY-RADIUS TESTS + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_04 PASS/finalize 뒤 `MicrochunkDefinition`의 object slots가 실제 12x8 tile data와 slot policy에 맞는지 검증하는 deterministic validator를 구현한다. 이 Task는 object slot `anchor`, `category`, `allowed_pool_id`, `required`, `orientation`, `visible_from_route`, `forbidden_radius_tiles`, `required_marker_code` 의미 검증까지만 연다.

Standalone 96-cell completeness validator, reachability probe, editor authoring grid/window, socket/slot editor UI, CSV import/export, preview/report는 구현하지 않는다. MAP07_06 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION
STATUS: PASS
MAP07_04: COMPLETE ELIGIBLE
MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION: LOCKED / DO NOT START
SHA-256: 90bb39103282ad08d031ee710802abdeba0adc4799c754ba73eaede4a2b7ade5
```

이 별도 patch가 적용된 뒤에만 MAP07_05를 실행한다. MAP07_06 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_04 Result SHA-256: 90bb39103282ad08d031ee710802abdeba0adc4799c754ba73eaede4a2b7ade5
MAP07_04 Task SHA-256: a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_04 acceptance: 5816/5816 PASS
MAP07_04 failed/skipped: 0/0
MAP07_04 compile/Console/relevant warnings: 0/0/0
Assets meta: 3350
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_04: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_04: 0
Duplicate GUID groups: 0
```

Starter object slot facts from Map Package v1.0:

```text
Object slot rows: 9
Starter slot categories: RESOURCE, MAP_ELEMENT, REWARD, EVENT_TRIGGER, NPC
Starter slot orientations: NONE
Supported orientation tokens after MAP07_03 repair: NONE, L, R, U, D
Starter required_marker_code values: M_SLOT_RESOURCE, M_SLOT_HAZARD, M_SLOT_EVENT, M_SAFE
All starter required_marker_code values exist in tile_code_dictionary.csv as marker/debug tile codes
allowed_pool_id is a slot-pool selector, not item/spawn selection
Object slot rows are source authoring data; this Task does not import, export, or mutate CSV
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
03_CSV_SCHEMA/ENUM_REFERENCE.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_object_slots.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/tile_code_dictionary.csv
```

Reference는 object slot field names, slot category vocabulary, orientation vocabulary, marker-code FK, and future handoff boundaries를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

## READ ALLOWLIST

### Existing MAP07 runtime model/rules/transforms/socket validation

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
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

### Existing tests for style and boundary advance

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
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

금지: MAP07_06+ Task body, standalone 96-cell validator, reachability probe, editor window, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime microchunk object-slot validation C# — exact 5

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotPoolDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkObjectSlotValidator.cs
```

### 신규 EditMode tests — exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
```

### 기존 phase-boundary test 수정 — exact up to 17

MAP07_05 production symbol `MicrochunkObjectSlotValidator`를 허용하고 MAP07_06+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
```

### Result report — exact 1

```text
MapDesign/MCP/REPORTS/MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION_RESULT.md
```

## Required Implementation Contract

### Policy and pool definitions

- `MicrochunkObjectSlotPoolDefinition` is immutable and stores `pool_id`, allowed slot categories, required/optional allowance, and notes.
- `MicrochunkObjectSlotValidationPolicy` is immutable and stores the supplied pool definitions, allowed marker tile codes, and blocking-layer rules.
- Pool definitions are in-memory policy data supplied by tests/runtime callers. This Task must not implement CSV import, spawn-pool import, population selection, item selection, or prefab resolution.
- Slot category tokens remain the existing MAP07_01 enum vocabulary: `RESOURCE`, `MAP_ELEMENT`, `ENEMY`, `REWARD`, `NPC`, `SHOP_ITEM`, `EVENT_TRIGGER`, `SPECIAL_ITEM`, `DECORATION`.
- Orientation tokens remain the MAP07_03 repaired vocabulary: `NONE`, `L`, `R`, `U`, `D`.

### Slot metadata validation

For every object slot row in a `MicrochunkDefinition`:

- `slot_id` must be non-empty.
- `slot_id` must be unique within the same microchunk definition.
- `local_x/local_y` must be in 12x8 range.
- If tile data is partial and the anchor tile cell is missing, report `MISSING_TILE_CELL_FOR_SLOT_ANCHOR`. This Task does not require complete 96-cell tile data.
- `slot_category` must be a defined object-slot category, not a default/unknown sentinel.
- `allowed_pool_id` must be non-empty and must exist in the supplied pool dictionary.
- The referenced pool must allow the slot category.
- A `required` slot must be allowed by the referenced pool's required-slot policy.
- `orientation` must be a defined orientation token and must survive MAP07_03 transforms consistently.
- `visible_from_route` is stored and reported but does not trigger route visibility/reachability probing in this Task.
- `forbidden_radius_tiles` must be greater than or equal to zero.
- Duplicate slot anchors in the same microchunk are reported deterministically as `DUPLICATE_SLOT_ANCHOR`.
- Duplicate `slot_id` values in the same microchunk are reported deterministically as `DUPLICATE_SLOT_ID`.

### Marker and tile safety validation

- `required_marker_code` may be empty.
- If `required_marker_code` is non-empty, it must exist in the supplied marker code set.
- If `required_marker_code` is non-empty and the anchor tile cell exists, the anchor cell's marker layer must match the required marker code.
- Marker-layer mismatch is reported as `REQUIRED_MARKER_MISMATCH`.
- A slot anchor is blocking when GroundSolid, Breakable, Hazard, or Liquid is occupied.
- DecorationBack, DecorationFront, Marker, and empty `NONE` layers do not make the anchor blocking.
- OneWay is not solid interior for object-slot anchors in this Task; reachability and standability remain MAP07_07.
- `forbidden_radius_tiles` validates a Manhattan-distance safety radius around the anchor, clipped to in-bounds cells.
- Within the safety radius, GroundSolid, Breakable, Hazard, and Liquid are blockers and must be reported with exact coordinates.
- Other object-slot anchors must not appear within the subject slot's forbidden radius. Report pair collisions once using stable slot ID ordering.
- Missing cells inside the safety radius are reported only when tile data is marked complete; partial definitions may report the missing anchor cell but do not become MAP07_06.

### Result and ordering

- `MicrochunkObjectSlotValidationViolation` records microchunk ID, slot ID, category, pool ID, optional local coordinate, optional compared slot ID, and stable reason.
- `MicrochunkObjectSlotValidationResult` is immutable, exposes evaluated slot count, issue count, success bool, and ordered violations.
- Violation ordering is deterministic: slot order by slot ID ordinal, then reason, then row-major coordinate when present, then compared slot ID.
- `MicrochunkObjectSlotValidator.ValidateDefinition(...)` must not mutate source definitions, supplied pool definitions, supplied marker code set, or tile cells.
- Transformed definitions from MAP07_03 must validate under transformed anchor coordinates and orientation when the same slot safety semantics are preserved.

## Forbidden Implementation

```text
Microchunk96CellValidator
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

Create `MicrochunkObjectSlotValidatorTests.cs` with deterministic EditMode coverage:

- Immutable pool definition and policy snapshots.
- Valid starter-compatible slots for all starter categories and marker codes.
- All supported categories and orientation tokens.
- Non-empty slot ID and allowed pool ID failures.
- Unknown/missing pool failure.
- Category/pool mismatch failure.
- Required slot rejected by pool policy.
- In-bounds anchor boundaries and out-of-bounds failures.
- Missing anchor cell behavior for partial definitions.
- Duplicate slot ID and duplicate anchor ordering.
- Required marker exists/missing/mismatch behavior.
- Blocking anchor failures for GroundSolid, Breakable, Hazard, and Liquid.
- Non-blocking anchor behavior for OneWay, DecorationBack, DecorationFront, Marker, and NONE.
- Forbidden-radius negative failure.
- Manhattan-radius blocker checks, clipped world bounds, and deterministic coordinate ordering.
- Pair spacing failures reported once per pair.
- No standalone 96-cell completeness enforcement for partial definitions.
- MAP07_03 transforms preserve valid slots and remap orientation/anchor deterministically.
- Source definitions, pools, marker set, and tile cells are not mutated.

Required actual gates:

```text
MicrochunkObjectSlotValidatorTests >=300 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=6116 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3350 -> 3356
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
MAP06 production source changes 0
Forbidden MAP07_06+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION_RESULT.md` containing:

```text
TASK: MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION
STATUS: PASS | FAIL | BLOCKED
MAP07_05: COMPLETE ELIGIBLE only if PASS
MAP07_06_IMPLEMENT_96_CELL_VALIDATOR: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_04 Result SHA-256 `90bb39103282ad08d031ee710802abdeba0adc4799c754ba73eaede4a2b7ade5`.
- MAP07_04 Task SHA-256 `a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6`.
- MAP07_05 Task SHA-256 from this file.
- Object-slot validator deterministic model/API digest.
- Preserved socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and manifest hash proving no source CSV mutation.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_06+ production symbols remain absent.

PASS finalization may only mark MAP07_05 COMPLETE and set Current Task to NONE. MAP07_06 remains LOCKED until a separate MAP07_06 patch is applied.
