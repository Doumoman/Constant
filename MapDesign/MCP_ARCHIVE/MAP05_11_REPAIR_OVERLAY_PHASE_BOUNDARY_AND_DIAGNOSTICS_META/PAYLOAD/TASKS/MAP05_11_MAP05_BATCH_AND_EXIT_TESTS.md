# MAP05_11 — MAP05 Batch and Exit Tests Repair v1.1

```yaml
status_control:
  task_key: MAP05_11_MAP05_BATCH_AND_EXIT_TESTS
  result_file: REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md
  repair_contract: OBSOLETE_OVERLAY_PHASE_BOUNDARY_AND_DIAGNOSTICS_META
```

## TASK TYPE

```text
TEST REPAIR / EXIT AUDIT RESUME / STRICT SCOPE POLICY
```

## Goal

MAP05_11의 신규 exit suite와 10,000-seed mandatory-route batch는 통과했다. 실패 원인은 production 구현이 아니라 아래 두 가지 gate mismatch다.

1. 기존 MAP05 regression tests 3개가 MAP05_10에서 완료된 `MandatoryRouteOverlay` symbols를 여전히 later-task symbol로 금지한다.
2. Unity force refresh가 기존 `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics` folder meta를 생성했지만, 기존 Task scope gate가 이 exact folder meta를 허용하지 않았다.

이번 repair는 MAP05_01~10 production, Graph, CSV, `SectorCell`, Authoring CSV, asmdef, Scene, Prefab, Packages, ProjectSettings를 수정하지 않는다. MAP06은 시작하지 않는다.

## Preconditions / Current Evidence

control → Master/Status → current MAP05_11 Task → current FAIL Result → allowlisted existing tests 순서로 읽는다.

```text
Current Task:
MapDesign/MCP/TASKS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS.md

Current Task SHA-256:
f0720d2df2f8807b2868b1c6074fb05efbe77ff0391bf3dd86a43c8d9957780f

Current FAIL Result:
MapDesign/MCP/REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md

Current FAIL Result SHA-256:
817d049e6f4ec5bec5641fb1de42cc561ecdf26578a4d16efca9c456e5a58863
```

Observed from the FAIL Result:

```text
New Map05ExitTests: 132/132 PASS
10,000-seed batch: 10000/10000 complete, retry/unresolved/invalid 0/0/0
Existing MAP05 focused aggregate: 1824/1827 PASS, failed/skipped 3/0
MAP05 phase aggregate: 1956/1959 PASS, failed/skipped 3/0
Failed existing assertions: 3 obsolete phase-boundary tests
Unexpected folder meta: Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics.meta
```

Current Task SHA나 FAIL Result SHA가 다르면 `BLOCKED`. MAP06_01 이후 Task body는 읽거나 생성하거나 실행하지 않는다.

## Root Cause / Authority

아래 existing tests는 각 작성 당시에는 올바른 later-task negative audit이었다. 그러나 MAP05_10이 PASS 완료된 이후 `MandatoryRouteOverlay` runtime diagnostics symbols는 현재 phase의 정식 output이다.

```text
HorizontalBackboneRouterTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_10PlusSymbols
VerticalGatewayPlannerTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_10PlusSymbols
UpDownConflictResolverTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_10PlusSymbols
```

수정 authority:

- forbidden symbol set에서 MAP05_10 overlay output symbols만 제거한다.
- test name/message는 `Map05_11Plus` 또는 `MAP06+` 경계가 드러나게 교정한다.
- MAP05_11+ 또는 MAP06+ production symbols는 계속 금지한다.
- test case 수를 줄이거나 skip/ignore/explicit/assumption으로 우회하지 않는다.

MAP05_10 allowed symbols:

```text
MandatoryRouteOverlayCell
MandatoryRouteOverlaySnapshot
MandatoryRouteOverlayGui
MandatoryRouteOverlay
```

## Type4 Contract — Preserve

Type4 규칙은 수정하지 않는다.

```text
Type4 requires U+D open.
L/R are independent and must preserve the actual route adjacency.
UD, LUD, RUD, LRUD are all legal.
No L/R canonicalization, forced-open, or forced-close is allowed.
```

## Read Allowlist

MCP:

```text
MapDesign/MCP/00_MCP_ENTRYPOINT.md
MapDesign/MCP/01_PROJECT_LOCKED_RULES.md
MapDesign/MCP/02_MCP_WORK_RULES.md
MapDesign/MCP/03_DATA_CSV_RULES.md
MapDesign/MCP/04_UNITY_MCP_RULES.md
MapDesign/MCP/05_CHANGE_CONTROL_RULES.md
MapDesign/MCP/07_PATCH_APPLY_RULES.md
MapDesign/MCP/08_STATUS_FINALIZE_RULES.md
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/TASKS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS.md
MapDesign/MCP/REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md
MapDesign/MCP/REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md
```

Existing Assets read:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map05ExitTests.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlay.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics.meta
```

Folder/path-only inventory is allowed only for exact change-scope and meta counting under `Assets/_Game`.

## Write Allowlist

Existing tests allowed for repair:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/HorizontalBackboneRouterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/VerticalGatewayPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/UpDownConflictResolverTests.cs
```

Current Result:

```text
MapDesign/MCP/REPORTS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md
```

Diagnostics folder meta policy:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics.meta
```

Preferred gate is cleanup to final Assets meta `3246`. The exact `Diagnostics.meta` may be removed only if it is a Unity-regenerated folder meta with no asset-body change and Unity force refresh does not regenerate it.

If Unity deterministically regenerates the same exact folder meta during forced refresh because the folder exists, do not delete/recreate it in a loop. Treat exactly this one folder meta as an approved pre-existing Unity folder meta only if:

- path is exactly `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics.meta`
- GUID is unique
- no asset body, production file, test file other than the 3 allowlisted tests, CSV, asmdef, Scene, Prefab, Packages, or ProjectSettings changed because of it
- final Result records the policy branch and final Assets meta explicitly

No other `.meta`, folder, asset, C#, CSV, asmdef, Scene, Prefab, Package, or ProjectSettings changes are allowed.

## Required Test Repair

For each of the three existing tests:

- keep the runtime-surface audit coverage
- remove only `MandatoryRouteOverlay*` from the forbidden later-task list
- keep `UnityEditor`, mutable static state, filesystem, RNG/cache, root adapter, retry/batch runner, generated writer, and MAP06+ symbols forbidden
- keep or increase test count; do not delete assertions
- preserve test meta GUIDs

Allowed naming/message examples:

```text
RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols
Runtime surface must not expose MAP06+ optional-region symbols before MAP06.
```

Disallowed:

```text
Assert.Ignore / [Explicit] / Assume / inconclusive
catch-all passing tests
symbol audit removal
production rename or API hiding to satisfy tests
MAP05_11+ or MAP06+ symbol relaxation
```

## Exit Audit Rerun

Re-run the full MAP05_11 exit gate after the repair. Do not cite stale MAP05_10 captures as current visual evidence.

Required actual gates:

```text
HorizontalBackboneRouterTests      142/142 PASS
VerticalGatewayPlannerTests        156/156 PASS
UpDownConflictResolverTests        194/194 PASS
New Map05ExitTests                 132/132 PASS, or >=120 if test count legitimately increases
Existing MAP05 focused aggregate   1827/1827 PASS
MAP05 phase aggregate              1959/1959 PASS, or higher only if added repair tests are counted
failed/skipped                     0/0
```

10,000-seed batch must remain:

```text
world seeds 0..9999 / attempt ordinal 0
completed/retry/unresolved/invalid = 10000/0/0/0
terminal failures / route-mask mismatch = 0/0
Type4 U+D missing / L-R preservation mismatch = 0/0
edge reciprocity / generated-edge bijection failures = 0/0
validation failures = 0
overlay snapshots = 10000/10000
```

Known vector must remain:

```text
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
terminals reachable = 7/7
loops represented = 2/2
generated sector/edge bytes/edge rows = 16838/7094/96
validation rules/violations/errors/warnings = 12/0/0/0
```

Visual actual:

```text
Game checklist 9/9
Scene checklist 9/9
combined 18/18
```

Forced import/domain reload/compile/Console/relevant warning:

```text
0/0/0
```

Unity/Test Runner/visual access가 없어 actual gate를 완료하지 못하면 `BLOCKED`.

## Asset / Scope Gate

Strict cleanup branch:

```text
Final Assets meta = 3246
Map05ExitTests.cs/meta preserved = 1/1
modified existing test C# = 3
unexpected existing/folder meta = 0
Authoring CSV/meta = 50/50
duplicate GUID groups = 0
generated CSV files = 0
production modifications = 0
Scene/Prefab/asmdef/Packages/ProjectSettings = 0
```

Approved-regenerated-meta branch, only if force refresh requires it:

```text
Final Assets meta = 3247
approved regenerated folder meta = 1 exact path
unexpected existing/folder meta = 0
all other scope counts identical to strict branch
```

If any other path changes, report `FAIL` or `BLOCKED`; do not finalize.

## Result / Finalize

Replace the current Result in `<=180 lines`.

Required sections:

```text
TASK / STATUS / SUMMARY
PATCH APPLY / PRIOR FAILURE / REPAIR
CREATED / MODIFIED / PRESERVED
TEST / 10,000-SEED BATCH / TYPE4 / KNOWN VECTOR
VISUAL / UNITY / ASSET META / CHANGE SCOPE
EXIT DECISION / NEXT / Recommended Commit
```

PASS Result exact lines:

```text
STATUS: PASS
MAP05 EXIT: APPROVED
MAP06 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS: LOCKED / DO NOT START
```

PASS일 때만 MAP05_11 COMPLETE, Current Task NONE, Last Completed/Result를 MAP05_11로 finalize한다. MAP06_01은 LOCKED로 유지하고 자동 생성/시작하지 않는다.

Recommended Commit:

```text
test(map): repair MAP05 exit boundary audits
```
