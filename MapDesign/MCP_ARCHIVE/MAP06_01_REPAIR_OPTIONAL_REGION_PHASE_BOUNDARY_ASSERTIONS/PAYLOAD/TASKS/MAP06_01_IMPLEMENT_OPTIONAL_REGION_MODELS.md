# MAP06_01 — Optional Region Models Repair v1.1

```yaml
status_control:
  task_key: MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS
  result_file: REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
  repair_contract: OPTIONAL_REGION_PHASE_BOUNDARY_ASSERTIONS
```

## TASK TYPE

```text
TEST-BOUNDARY REPAIR / MAP06_01 EXIT RESUME
```

## Goal

MAP06_01 optional region model implementation itself passed. 신규 `OptionalRegionModelsTests`는 `194/194 PASS`였고 compile/Console/asset scope도 통과했다.

실패 원인은 기존 MAP05 phase-boundary tests 6개가 MAP06_01에서 정식으로 생성된 `OptionalRegion*` model symbols의 존재를 여전히 금지하기 때문이다.

이번 repair는 MAP06_01 model production code를 수정하지 않고, 기존 phase-boundary tests의 forbidden symbol set만 MAP06_01 완료 경계에 맞게 교정한다. MAP06_02는 시작하지 않는다.

## Preconditions / Current Evidence

control → Master/Status → current MAP06_01 Task → current FAIL Result → allowlisted existing tests 순서로 읽는다.

```text
Current Task:
MapDesign/MCP/TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md

Current Task SHA-256:
79b806802dab4a86f3cdc0b6193be4c8f5c97a2e6a9cc8bcc023259752b49a62

Current FAIL Result:
MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md

Current FAIL Result SHA-256:
254092c80abdec87d20c9276854539ca7225e33738dfbe2419384a48710fb553
```

Observed from the FAIL Result:

```text
OptionalRegionModelsTests: 194/194 PASS
Existing MAP05 aggregate: 1953/1959 PASS, failed/skipped 6/0
Actually executed total: 2147/2153 PASS, failed/skipped 6/0
Compile/Console/warnings: 0/0/0
Assets meta: 3254
Authoring CSV/meta: 50/50
```

Current Task SHA나 FAIL Result SHA가 다르면 `BLOCKED`. MAP06_02 이후 Task body는 읽거나 생성하거나 실행하지 않는다.

## Root Cause / Authority

아래 existing tests는 MAP05 완료 시점에는 올바른 negative audit이었다. 그러나 MAP06_01이 열리고 optional region model files가 생성된 뒤에는 MAP06_01 model symbols가 current output이다.

```text
HorizontalBackboneRouterTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols
MandatoryRouteGraphValidatorTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols
MandatoryRouteMaskLookupBuilderTests.Map05_11PlusProductionSymbolsAreAbsent("OptionalRegion")
Map05ExitTests.RuntimePhaseBoundaryAllowsOverlayAndForbidsMap06PlusSurface
UpDownConflictResolverTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols
VerticalGatewayPlannerTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols
```

수정 authority:

- MAP06_01 model symbols만 later-task forbidden set에서 제거한다.
- test name/message는 `MAP06_02Plus` 또는 `MAP06 later implementation` 경계가 드러나게 교정한다.
- MAP06_02+ production symbols는 계속 금지한다.
- test case 수와 safety audit coverage를 줄이지 않는다.

MAP06_01 allowed symbols:

```text
OptionalRegionId
OptionalRegionEnums
OptionalRegionAccessRule
OptionalRewardTier
OptionalReturnPolicy
OptionalRegionDepth
OptionalRegionAttachment
OptionalRegionCell
OptionalRegion
OptionalRegionSnapshot
OptionalRegionTokenCodec
```

MAP06_02+ still forbidden examples:

```text
OptionalAttachmentEnumerator
OptionalAttachmentCandidate
OptionalRegionGrower
Type0RouteMaskAssigner
OptionalRouteMaskLookup
OptionalAccessRuleAssigner
OptionalClueAssigner
OptionalRewardTierCalculator
OptionalReturnPolicyResolver
InactiveBufferAssigner
OptionalRegionValidator
OptionalRegionOverlay
OptionalRegionOverlaySnapshot
```

## Type4 / Mandatory Route Contract — Preserve

MAP05 output은 수정하지 않는다.

```text
graph nodes/directed/undirected/route cells = 47/96/48/47
mask T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD = 20/4/4/17/0/0/2
Type4 requires U+D open.
L/R are independent and preserve actual mandatory graph adjacency.
UD, LUD, RUD, LRUD are all legal.
```

No L/R canonicalization, forced-open, forced-close, graph repair, CSV rewrite, or `SectorCell` mutation is allowed.

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
MapDesign/MCP/TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md
MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
```

Existing tests by exact class/file:

```text
HorizontalBackboneRouterTests.cs
MandatoryRouteGraphValidatorTests.cs
MandatoryRouteMaskLookupBuilderTests.cs
Map05ExitTests.cs
UpDownConflictResolverTests.cs
VerticalGatewayPlannerTests.cs
```

Resolve each file only under the approved EditMode map test tree. If both legacy `_Game` layout and compact `Assets/Tests/EditMode/MapDesign` layout exist, modify only the path that contains the exact failing class.

Observed MAP06_01 created model/test files may be read for symbol verification only:

```text
OptionalRegionId.cs
OptionalRegionEnums.cs
OptionalRegionAttachment.cs
OptionalRegionCell.cs
OptionalRegion.cs
OptionalRegionSnapshot.cs
OptionalRegionModelsTests.cs
```

Do not read MAP06_02+ Task body, unrelated production/test C# body, Authoring CSV body, generated CSV body, Scene/Prefab YAML, Legacy/Stage/P6/P11 generator body.

## Write Allowlist

Modify exactly these six existing test C# files, resolved by class name under the approved EditMode map test tree:

```text
HorizontalBackboneRouterTests.cs
MandatoryRouteGraphValidatorTests.cs
MandatoryRouteMaskLookupBuilderTests.cs
Map05ExitTests.cs
UpDownConflictResolverTests.cs
VerticalGatewayPlannerTests.cs
```

Current Result:

```text
MapDesign/MCP/REPORTS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
```

Preserve all existing `.cs.meta` GUIDs. Do not modify optional region production files, optional region model tests, MAP05 production graph/CSV/SectorCell, Authoring CSV, generated CSV, asmdef, Scene, Prefab, Packages, ProjectSettings, Master, or Status during execution.

## Required Test Repair

For each failing boundary test:

- keep the runtime-surface audit
- remove only MAP06_01 optional region model symbols from forbidden later-task list
- keep `UnityEditor`, mutable static state, filesystem, RNG/cache, root/pass adapter, batch runner, generated writer, and MAP06_02+ symbols forbidden
- rename the test/message only if needed to clarify the new boundary
- preserve test count; do not delete assertions

Allowed naming/message examples:

```text
RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap06_02PlusSymbols
MAP06_01 model symbols are allowed after MAP06_01; MAP06_02+ implementation symbols remain forbidden.
```

Disallowed:

```text
Assert.Ignore / [Explicit] / Assume / inconclusive
catch-all passing test
symbol audit removal
production rename or API hiding to satisfy tests
allowing MAP06_02+ symbols
```

## Required Runs

After repair, run the actual gates:

```text
OptionalRegionModelsTests            194/194 PASS
HorizontalBackboneRouterTests        142/142 PASS
MandatoryRouteGraphValidatorTests    298/298 PASS
MandatoryRouteMaskLookupBuilderTests 127/127 PASS
Map05ExitTests                       132/132 PASS
UpDownConflictResolverTests          194/194 PASS
VerticalGatewayPlannerTests          156/156 PASS
Existing MAP05 aggregate             1959/1959 PASS
Actually executed total              2153/2153 PASS or higher if repair adds tests
failed/skipped                       0/0
```

Forced import/domain reload/compile/Console/relevant warnings:

```text
0/0/0
```

Unity/Test Runner 접근 불가능으로 actual gate를 완료하지 못하면 `BLOCKED`.

## Asset / Scope Gate

```text
Final Assets meta = 3254
OptionalRegion model/test C#/meta preserved = 7/7
modified existing test C# = 6
modified existing test meta = 0
new production/test C#/meta = 0/0
production model modifications = 0
unexpected existing/folder meta = 0
Authoring CSV/meta = 50/50
duplicate GUID groups = 0
generated CSV files = 0
Scene/Prefab/asmdef/Packages/ProjectSettings = 0
```

Approved MAP05_11 `Diagnostics.meta` baseline is preserved and must not be deleted/recreated by this repair.

## Result / Finalize

Replace the current Result in `<=170 lines`.

Required sections:

```text
TASK / STATUS / SUMMARY
PATCH APPLY / PRIOR FAILURE / REPAIR
MODIFIED / PRESERVED
TEST / UNITY / ASSET META / CHANGE SCOPE
DONE CONDITIONS / NEXT / Recommended Commit
```

PASS Result exact lines:

```text
STATUS: PASS
MAP06_01: COMPLETE ELIGIBLE
MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS: LOCKED / DO NOT START
```

PASS일 때만 MAP06_01 COMPLETE, Current Task NONE으로 finalize한다. MAP06_02는 LOCKED로 유지하고 자동 생성/시작하지 않는다.

Recommended Commit:

```text
test(map): allow optional region model symbols after MAP06_01
```
