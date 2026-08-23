# MAP07_07 - Implement Microchunk Reachability Probe

```yaml
status_control:
  task_key: MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE
  result_file: REPORTS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md
```

## TASK TYPE

```text
RUNTIME MICROCHUNK LOCAL REACHABILITY PROBE + MANDATORY SOCKET PAIR PATH WITNESSES + PHASE-BOUNDARY TEST ADVANCE
```

## Objective

MAP07_06 PASS/finalize 뒤 complete 12x8 microchunk 안에서 mandatory no-tool socket pairs가 deterministic local traversal graph로 연결되는지 검증하는 reachability probe를 구현한다. 이 Task는 validated tile cells, socket definitions, socket band definitions, and movement policy를 입력으로 받아 socket entry cells와 shortest-path witness를 계산하는 범위까지만 연다.

Authoring grid/window, socket/slot editor UI, CSV import/export, preview/report, sector assembly, world-level traversal validation은 구현하지 않는다. MAP07_08 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP07_06_IMPLEMENT_96_CELL_VALIDATOR
STATUS: PASS
MAP07_06: COMPLETE ELIGIBLE
MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE: LOCKED / DO NOT START
SHA-256: 81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0
```

이 별도 patch가 적용된 뒤에만 MAP07_07을 실행한다. MAP07_08 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
New asmdef/asmref: NO
MAP07_06 Result SHA-256: 81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0
MAP07_06 Task SHA-256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
MAP07_06 96-cell validator model/API digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
MAP07_05 object-slot validator model/API digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
MAP07_04 socket-edge validator model/API digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
MAP07_03 transform model/API digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
MAP07_02 tile-layer rules model/API digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
Updated MAP07_01 model/API digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
MAP07_06 acceptance: 6705/6705 PASS
MAP07_06 failed/skipped: 0/0
MAP07_06 compile/Console/relevant warnings: 0/0/0
Assets meta: 3362
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP07_06: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP07_06: 0
Duplicate GUID groups: 0
```

Starter reachability facts from Map Package v1.0:

```text
Microchunk dimensions: 12x8 = 96
Complete coverage is now validated by MAP07_06
Socket side/band/outer clearance is now validated by MAP07_04
Tile blocking semantics are based on MAP07_02 occupancy rules
Mandatory socket pair reachability is local to a microchunk in this Task
World/sector/inter-microchunk traversal remains later MAP09/MAP13 ownership
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP07_MICROCHUNK_AUTHORING.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_tile_cells.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/socket_band_definitions.csv
04_CSV_STARTER/edge_signatures.csv
```

Reference는 local tile blocking, socket/band fields, mandatory no-tool socket meaning, and future handoff boundaries를 확인하는 용도다. Authoring CSV body를 수정하지 않는다. CSV import/export implementation은 하지 않는다.

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
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellRecord.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/Microchunk96CellValidator.cs
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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
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

금지: MAP07_08+ Task body, editor window/grid, CSV import/export, sector assembly, boundary chunk body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime microchunk reachability C# - exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTraversalNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkTraversalEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/MicrochunkReachabilityProbe.cs
```

### 신규 EditMode tests - exact 1

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkReachabilityProbeTests.cs
```

### 기존 phase-boundary test 수정 - exact up to 17

MAP07_07 production symbol `MicrochunkReachabilityProbe`를 허용하고 MAP07_08+ future symbols 금지를 유지하기 위해 필요한 경우 아래 existing test C#만 수정할 수 있다. matching `.cs.meta`는 수정하지 않는다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTileLayerRulesTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkTransformerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkSocketEdgeValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkObjectSlotValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/Microchunk96CellValidatorTests.cs
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
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
```

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md
```

## Required Implementation Contract

### Traversal graph model

- `MicrochunkTraversalNode` is immutable and stores microchunk ID and local coordinate.
- `MicrochunkTraversalEdge` is immutable and stores source coordinate, target coordinate, movement kind, and deterministic cost.
- Movement kind tokens are exact: `FLOOD`, `WALK`, `JUMP`, `DROP`, `CLIMB`, `SOCKET_ENTRY`.
- `MicrochunkReachabilityPolicy` is immutable and stores movement limits and marker sets:
  - maximum jump rise;
  - maximum jump horizontal span;
  - maximum drop distance;
  - optional climb marker codes;
  - deterministic neighbor ordering.
- Default policy is no-tool mandatory traversal. It must not reference player runtime physics, input, animation, or final MAP13 world traversal.

### Blocking and node rules

- The probe requires complete 96-cell coverage. Use MAP07_06 validator or an equivalent supplied successful coverage result as a gate.
- A local coordinate becomes a traversal node when its tile cell is in bounds and not blocked.
- Blocking layers for no-damage mandatory reachability are GroundSolid, Breakable, Hazard, and Liquid.
- OneWay, DecorationBack, DecorationFront, Marker, and `NONE` are not blockers.
- OneWay may be used as support metadata for movement policy, but it must not make the coordinate blocked.
- Tile-code FK validation and layer compatibility remain prior validators and are not reimplemented here.

### Socket entry resolution

For every socket row considered by the reachability probe:

- Mandatory no-tool socket means `mandatory_allowed == true` and `tool_requirement == NONE`.
- Sockets requiring tools are not part of the mandatory pair set in this Task.
- Socket side and band definitions are supplied in-memory. The probe does not import CSV.
- Entry candidates are the edge cells covered by the socket band:
  - `L`: `x = 0`, `y` in horizontal band range.
  - `R`: `x = 11`, `y` in horizontal band range.
  - `D`: `y = 0`, `x` in vertical band range.
  - `U`: `y = 7`, `x` in vertical band range.
- At least one unblocked entry candidate must exist per mandatory no-tool socket.
- Invalid or missing band definitions are reported by this probe only as reachability input issues; MAP07_04 remains the owner of full socket-edge validation.

### Path validation

- Build a deterministic local traversal graph over the 12x8 nodes.
- Evaluate every unordered pair of mandatory no-tool sockets.
- For each pair, run BFS or equivalent deterministic shortest-path search from any source entry candidate to any target entry candidate.
- Store the chosen shortest path witness as ordered local coordinates.
- Tie-break paths by deterministic movement kind order, then row-major coordinate order, then socket ID order.
- If no path exists, report `MANDATORY_SOCKET_PAIR_UNREACHABLE`.
- If a mandatory socket has no valid entry node, report `MANDATORY_SOCKET_ENTRY_UNREACHABLE`.
- If the complete coverage gate fails, report `CELL_COVERAGE_INVALID` and do not produce false path success.
- A chunk with fewer than two mandatory no-tool sockets succeeds with evaluated pair count `0` if all input gates pass.

### Result and ordering

- `MicrochunkReachabilityViolation` records microchunk ID, socket ID, optional paired socket ID, optional coordinate, and stable reason.
- `MicrochunkReachabilityResult` is immutable and exposes evaluated socket count, evaluated pair count, reachable pair count, issue count, success bool, ordered violations, and ordered path witnesses.
- Violation ordering is deterministic: microchunk ID ordinal, socket ID ordinal, paired socket ID ordinal, reason, then row-major coordinate.
- `MicrochunkReachabilityProbe.ValidateDefinition(...)` must not mutate definitions, tile cells, socket definitions, band definitions, policies, or prior validator results.
- Transformed definitions from MAP07_03 must preserve reachability when geometry and socket remapping preserve the same local paths.

## Forbidden Implementation

```text
MicrochunkAuthoringWindow
MicrochunkAuthoringGrid
MicrochunkSocketAndSlotEditor
MicrochunkCsvImporter
MicrochunkCsvExporter
MicrochunkPreviewReport
BoundaryChunkResolver
SectorRecipeResolver
GeneratedSectorMicrochunkWriter
PopulationSlotIndex
StableSpawnId
WorldTraversalValidator
```

## Required Tests

Create `MicrochunkReachabilityProbeTests.cs` with deterministic EditMode coverage:

- Immutable node, edge, policy, violation, result, and witness snapshots.
- Exact 12x8 local node construction from complete tile cells.
- Blocking behavior for GroundSolid, Breakable, Hazard, and Liquid.
- Non-blocking behavior for OneWay, DecorationBack, DecorationFront, Marker, and `NONE`.
- Mandatory no-tool socket filtering and tool-required socket exclusion.
- L/R/D/U socket entry coordinate derivation from supplied band definitions.
- Missing band and blocked entry diagnostics.
- Pair enumeration for zero, one, two, and multiple mandatory sockets.
- Deterministic BFS path witnesses and tie-breaking.
- Flood/walk, jump, drop, and climb movement edges under supplied policy.
- Unreachable pair diagnostics.
- Coverage gate failure prevents false reachability success.
- Prior validators remain source gates and are not reimplemented.
- All four MAP07_03 transforms preserve reachable fixtures.
- Source definitions, tiles, sockets, bands, policies, and prior results are not mutated.

Required actual gates:

```text
MicrochunkReachabilityProbeTests >=480 PASS
Microchunk96CellValidatorTests 406/406 PASS
MicrochunkObjectSlotValidatorTests 483/483 PASS
MicrochunkSocketEdgeValidatorTests 332/332 PASS
MicrochunkTransformerTests 483/483 PASS
MicrochunkTileLayerRulesTests 150/150 PASS
MicrochunkDefinitionTests 146/146 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed total >=7185 PASS
failed/skipped 0/0
compile/Console/relevant warnings 0/0/0
```

## Static and Change-Scope Gates

```text
Assets meta 3362 -> 3369
new Runtime C#/meta 6/6
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
MAP07_06 production source changes 0
MAP06 production source changes 0
Forbidden MAP07_08+ production hits 0
Assets duplicate GUID groups 0
Unapplied MCP patches 0
```

## Result Report Requirements

Write `MapDesign/MCP/REPORTS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md` containing:

```text
TASK: MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE
STATUS: PASS | FAIL | BLOCKED
MAP07_07: COMPLETE ELIGIBLE only if PASS
MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID: LOCKED / DO NOT START
```

The report must include:

- Applied patch receipt SHA-256.
- MAP07_06 Result SHA-256 `81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0`.
- MAP07_06 Task SHA-256 `38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa`.
- MAP07_07 Task SHA-256 from this file.
- Reachability probe deterministic model/API digest.
- Preserved 96-cell, object-slot, socket-edge, transform, tile-layer, and MAP07_01 model/API digests.
- Required test execution counts and failed/skipped totals.
- Unity compile/Console/relevant warning counts.
- Assets meta before/after, new C#/meta counts, and duplicate GUID groups.
- Authoring CSV/meta count and manifest hash proving no source CSV mutation.
- Generated CSV, Scene, Prefab, ProjectSettings, Packages, asmdef, asmref change counts.
- Confirmation that MAP07_08+ production symbols remain absent.

PASS finalization may only mark MAP07_07 COMPLETE and set Current Task to NONE. MAP07_08 remains LOCKED until a separate MAP07_08 patch is applied.
