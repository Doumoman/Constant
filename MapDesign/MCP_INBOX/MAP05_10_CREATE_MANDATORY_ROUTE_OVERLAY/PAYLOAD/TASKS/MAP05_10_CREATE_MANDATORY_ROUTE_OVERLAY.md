# MAP05_10 — Create Mandatory Route Overlay

```yaml
status_control:
  task_key: MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY
  result_file: REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md
```

## TASK TYPE

```text
RUNTIME + EDITOR MANDATORY ROUTE OVERLAY + EDITMODE TESTS
```

## Objective

`MAP05_09`에서 PASS 확정된 `MandatoryRouteGraph`, route-stamped `GeneratedWorldData`, generated edge records/CSV bytes, `MandatoryRouteValidationReport`를 읽어 mandatory route 상태를 Game View와 Scene View에서 확인하는 overlay를 만든다. 이번 Task는 diagnostics/visualization only다. graph, route mask, `SectorCell`, generated CSV bytes, Authoring CSV, root/pass pipeline은 수정하지 않는다.

Type4 규칙은 계속 고정한다. Type4는 U+D가 반드시 열려야 하며 L/R은 실제 graph adjacency를 보존한다. `UD`, `LUD`, `RUD`, `LRUD` 네 조합은 모두 합법이고 overlay는 이 차이를 숨기거나 `LRUD` 하나로 canonicalize하지 않는다.

```text
input graph nodes/directed edges/undirected edges/route cells = 47 / 96 / 48 / 47
input mask counts T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
mandatory terminals reachable from Start = 7/7
accepted loops represented = 2/2
validation rules registered/evaluated/passed = 12/12/12
generated sectors CSV bytes = 16838
generated edges CSV bytes/rows = 7094 / 96
output = overlay snapshot + Game GUI + Scene drawer only
```

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
11. this Task
12. `REPORTS/MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH
STATUS: PASS
VALIDATION RULES REGISTERED / EVALUATED / PASSED: 12 / 12 / 12
VIOLATIONS / ERRORS / WARNINGS: 0 / 0 / 0
GRAPH NODES / DIRECTED EDGES / UNDIRECTED EDGES / ROUTE CELLS: 47 / 96 / 48 / 47
MASK COUNTS T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: 20/4/4/17/0/0/2
MANDATORY TERMINALS REACHABLE FROM START: 7/7
ACCEPTED LOOPS REPRESENTED: 2/2
GENERATED SECTORS CSV BYTES: 16838
GENERATED EDGES CSV BYTES / ROWS: 7094 / 96
TEST INVOCATIONS: 2414/2414 PASS
ASSET META: 3238
SHA-256: 72df536b5d51c7db7ff364e74e7bd7141f0399465e38b3a75d366640a1d3b33a
DONE CONDITIONS: PASS
```

## READ ALLOWLIST

```text
Assets/_Game/Map/Runtime/WorldGeneration/Generation/SectorCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldData.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/GeneratedWorldEdgesCsvSerializer.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteMaskFamily.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNodeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdgeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphNode.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphEdge.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraph.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuildResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteTerminalSet.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteLoopPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationRuleId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationSeverity.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationViolation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationSummary.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationReport.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationDiagnostics.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteValidationResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryRouteGraphValidator.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/WorldTopologyOverlay.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/SiteReservationOverlay.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/BiomePatchOverlay.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawer.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/SiteReservationOverlaySceneDrawer.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/BiomePatchOverlaySceneDrawer.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryTerminalBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/GeneratedWorldDataTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/WorldTopologyOverlayTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/SiteReservationOverlayTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/BiomePatchOverlayTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/WorldTopologyOverlaySceneDrawerTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/SiteReservationOverlaySceneDrawerTests.cs
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/BiomePatchOverlaySceneDrawerTests.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Editor/MapAuthoring/MapAuthoring.Editor.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/MapAuthoring.Tests.EditMode.asmdef
```

matching meta, approved `Diagnostics`/`Preview`/test 직계 path-only inventory, Authoring CSV/meta count/hash, generated output schema header if present, 전체 meta GUID와 task-marker 이후 change-scope만 확인한다. MAP05_11+ Task body와 unrelated production/Scene/Prefab YAML은 읽지 않는다.

## WRITE ALLOWLIST

신규 Runtime production C# exact 4:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlay.cs
```

신규 Editor production C# exact 1:

```text
Assets/_Game/Editor/MapAuthoring/WorldGeneration/Preview/MandatoryRouteOverlaySceneDrawer.cs
```

신규 Runtime/EditMode test C# exact 1:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics/MandatoryRouteOverlayTests.cs
```

신규 Editor/EditMode test C# exact 1:

```text
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/Preview/MandatoryRouteOverlaySceneDrawerTests.cs
```

신규 C# 7개와 matching `.cs.meta` 7개, Result 1개만 생성한다. 기존 production/test/CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings는 수정하지 않는다.

## Namespace / Assembly

```text
Runtime namespace: StarNight.Map.WorldGeneration.Diagnostics
Editor namespace:  StarNight.MapAuthoring.Editor.WorldGeneration.Preview
Runtime test namespace: StarNight.Map.Tests.WorldGeneration.Diagnostics
Editor test namespace:  StarNight.MapAuthoring.Tests.WorldGeneration.Preview
Runtime assembly:       Game.Map.Runtime
Editor assembly:        MapAuthoring.Editor
Runtime test assembly:  Game.Map.Tests.EditMode
Editor test assembly:   MapAuthoring.Tests.EditMode
```

`UnityEditor` dependency는 editor-only file에만 둔다. Runtime production은 Unity object/lifecycle, `System.IO`, RNG, clock, record/record struct, `required`, `init`, nullable-reference directive, reflection factory, singleton/static mutable state를 도입하지 않는다.

## Overlay Contract

`MandatoryRouteOverlayCell` stores immutable display data:

- sector coordinate/index and grid coordinate
- route mask family and exact side flags L/R/U/D
- display type token `T1`, `T2`, `T3`, `T4-UD`, `T4-LUD`, `T4-RUD`, or `T4-LRUD`
- BFS distance from Start, terminal role token, loop marker, directed edge count
- validation state and deterministic warning token

`MandatoryRouteOverlaySnapshot` stores immutable ordered cells/edges and summary:

- graph nodes/directed edges/undirected edges/route cells `47/96/48/47`
- mask family counts `20/4/4/17/0/0/2`
- terminals reachable `7/7`, accepted loops represented `2/2`
- validation rules `12/12/12`, violations/errors/warnings `0/0/0`
- generated sectors/edges byte sizes `16838/7094`

`MandatoryRouteOverlay` builds a snapshot from existing graph/data/report objects without mutation. It must preserve all Type4 L/R combinations exactly as input and must fail closed if validation report status is not PASS.

`MandatoryRouteOverlayGui` draws a deterministic 13x13 overlay:

- color by Type1/2/3/4 family
- arrows or side glyphs for L/R/U/D
- terminal labels for Start, Core resources, Forge, Boss, and Village entries
- BFS distance heat labels
- accepted loop markers
- validation summary banner

`MandatoryRouteOverlaySceneDrawer` draws the same information in Scene View using editor-only Handles/Gizmos style helpers. It may create transient visual fixtures during tests but must not save Scene/Prefab assets.

## Required Tests

`MandatoryRouteOverlayTests.cs` actual NUnit cases minimum `130`:

- snapshot immutability, ordering, equality/hash and culture-stable labels
- exact starter vector counts from MAP05_09
- Type1/2/3 display tokens and all four Type4 tokens `UD/LUD/RUD/LRUD`
- Type4 U+D required display assertion and actual L/R preservation
- BFS distance heat labels, terminal labels, loop markers, edge side glyphs
- validation summary PASS banner and zero violation rendering
- generated CSV byte/row numbers surfaced without reading/writing files
- no graph/data/report mutation, no RNG/filesystem/clock/static mutable state
- deterministic GUI layout rectangles for 13x13 and compact mobile-sized rects

`MandatoryRouteOverlaySceneDrawerTests.cs` actual NUnit cases minimum `24`:

- Scene drawer can build draw commands from the snapshot without saving assets
- world coordinate mapping for 13x13 sectors is deterministic
- Type4 side arrows and terminal/loop labels match runtime snapshot
- disabled/null/failed validation input is handled without exceptions

Actually run:

```text
MandatoryRouteOverlayTests              >=130 PASS
MandatoryRouteOverlaySceneDrawerTests    >=24 PASS
MandatoryRouteGraphValidatorTests        298/298 PASS
MandatoryRouteGraphBuilderTests          281/281 PASS
MandatoryRouteLoopPlannerTests           212/212 PASS
MandatoryRouteMaskLookupBuilderTests     127/127 PASS
MandatoryTerminalBuilderTests            120/120 PASS
GeneratedWorldDataTests                   56/56 PASS
Actually executed total                 >=1248 PASS
failed/skipped                             0/0
Game.Map targeted discovery             >=7160
Full EditMode discovery                  >=7248
forced refresh/compile/Console/warnings   0/0/0
visual Game/Scene checklist               18/18
```

If visual capture/checklist cannot be produced, mark `BLOCKED` and do not infer success from code tests alone.

## Asset / Meta / Change Gate

```text
baseline Authoring CSV/meta = 50/50; Assets meta = 3238; duplicate GUID = 0
new Runtime production C# = 4; new Editor production C# = 1
new Runtime test C# = 1; new Editor test C# = 1
new matching cs.meta = 7
modified existing production C# = 0; modified existing test C# = 0
final Assets meta = 3245; task-marker Assets changes = 14
Authoring CSV/body/meta modifications = 0
generated CSV/body/meta modifications = 0
Scene/Prefab modifications = 0
asmdef/asmref modifications = 0
Package/ProjectSettings modifications = 0
unexpected Assets changes = 0
duplicate GUID groups = 0
```

## Failure Policy

If any precondition, prior Result SHA, Type4 rule, write allowlist, visual checklist, asset/meta count, compile, Console, or test condition fails:

```text
STATUS: BLOCKED
CHANGED / CREATED: list exact files or NONE
TEST / UNITY: exact executed counts or NOT RUN
STATUS FINALIZE: MUST NOT RUN
NEXT TASK: MUST NOT START
```

## Done / Next

On PASS only:

```text
MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY -> COMPLETE
Current Task -> NONE
MAP05_11_MAP05_BATCH_AND_EXIT_TESTS -> LOCKED
recommended commit: feat(map): add mandatory route overlay
```
