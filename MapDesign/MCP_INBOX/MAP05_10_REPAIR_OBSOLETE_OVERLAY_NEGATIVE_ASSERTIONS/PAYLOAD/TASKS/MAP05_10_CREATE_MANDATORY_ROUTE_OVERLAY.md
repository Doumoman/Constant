# MAP05_10 — Mandatory Route Overlay Repair v1.1

```yaml
status_control:
  task_key: MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY
  result_file: REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md
  repair_contract: OBSOLETE_OVERLAY_NEGATIVE_ASSERTIONS
```

## Goal

MAP05_10 implementation은 compile되고 신규 focused suite `168/168`을 통과했다. 실패 원인은 기존 MAP05 regression negative-audit tests 4개가 MAP05_10의 정식 산출물인 `MandatoryRouteOverlay` runtime symbols 부재를 계속 요구하는 obsolete phase-boundary assertion이다.

이번 repair는 production과 MAP05_10 overlay implementation을 수정하지 않고, 기존 test의 later-task symbol audit만 MAP05_10 완료 상태에 맞게 교정한다.

MAP05_11은 시작하지 않는다.

## Preconditions / Prior Failure

control → Master/Status → current MAP05_10 Task → current BLOCKED Result → allowlisted existing tests 순서로 읽는다.

```text
Current Task SHA before repair:
b2ec466044db9a35cdb84bd691eb5f5c8c318db761947a1019c5634716642039

BLOCKED Result SHA:
601d11f6fe3ee15b094f5d17e9bd679dafe8682523c48bff31d80e93a6295e3f

Observed passing focused suite:
MAP05_10 focused 168/168 PASS

Observed failure:
Required regression attempt 1206 discovered; FAILED
Known obsolete negative-audit failures: 4
```

Current Task나 BLOCKED Result가 다르면 `BLOCKED`. MAP05_11 이후 Task body는 읽거나 시작하지 않는다.

## Root Cause / Authority

- MAP05_02/07/08/09 시점의 negative assertion은 각 당시에는 정상이었다. 그 시점에서는 `MandatoryRouteOverlay`가 later-task symbol이었기 때문이다.
- MAP05_10에서는 `MandatoryRouteOverlayCell`, `MandatoryRouteOverlaySnapshot`, `MandatoryRouteOverlayGui`, `MandatoryRouteOverlay`가 현재 Task의 required output type이다.
- 따라서 MAP05_10 검증 중에도 기존 회귀 테스트가 이 심볼의 부재를 요구하면 phase boundary가 자기모순이 된다.
- 수정 대상은 테스트의 forbidden symbol set과 test name/message뿐이다. Overlay production behavior, graph, route mask, CSV, `SectorCell`, validation report는 변경하지 않는다.

## Read / Write Allowlist

READ:

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
MapDesign/MCP/TASKS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY.md
MapDesign/MCP/REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md
```

Existing Assets read:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayCell.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlaySnapshot.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlayGui.cs
Assets/_Game/Map/Runtime/WorldGeneration/Diagnostics/MandatoryRouteOverlay.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

MODIFY:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteLoopPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteGraphValidatorTests.cs
MapDesign/MCP/REPORTS/MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY_RESULT.md
```

matching `.cs.meta`는 읽기만 하며 SHA/GUID를 보존한다.

금지:

- MAP05_10 production C# 수정
- MAP05_10 신규 focused runtime/editor tests 수정
- graph, route mask, `SectorCell`, generated CSV, Authoring CSV 수정
- Master/Status 수정
- asmdef/Scene/Prefab/Packages/ProjectSettings 수정
- test skip/ignore/assertion 삭제로 coverage 축소
- MAP05_11 이후 구현
- Git operation

## Required Test Repair

기존 test helper/name은 유지해도 된다. 의미는 아래로 교정한다.

```text
MAP05_10 current output symbols are allowed:
  MandatoryRouteOverlayCell
  MandatoryRouteOverlaySnapshot
  MandatoryRouteOverlayGui
  MandatoryRouteOverlay

MAP05_11+ production symbols remain forbidden.
```

허용되는 수정 예:

- forbidden symbol list에서 MAP05_10 overlay output symbols만 제거
- test 이름을 `RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols`처럼 MAP05_11+ 범위가 드러나게 조정
- assertion message를 MAP05_11+ later-task audit로 수정

허용되지 않는 수정:

- symbol audit test 전체 삭제
- MAP05_11+ forbidden symbols까지 허용
- 기존 test case 수를 줄이는 변경
- `Assert.Ignore`, `[Explicit]`, assumption skip, broad try/catch로 failure 숨김
- production 심볼 rename으로 test 우회

## Required Runs

수정 후 actual test gates:

```text
MAP05_10 focused overlay suite          168/168 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryRouteLoopPlannerTests          212/212 PASS
MandatoryRouteGraphBuilderTests         281/281 PASS
MandatoryRouteGraphValidatorTests       298/298 PASS
Required regression aggregate          1206/1206 PASS
Actually executed total                >=1374 PASS
failed/skipped                            0/0
visual Game/Scene checklist              18/18
```

forced refresh/compile/Console/relevant warning `0/0/0`.

Asset/scope gate:

```text
Assets meta 3245 -> 3245
modified existing test C# = 4
new Runtime/Test C#/meta = 0/0/0
production modifications = 0
MAP05_10 overlay production/focused tests modifications = 0
unexpected Assets changes = 0
Authoring CSV/meta = 50/50
duplicate GUID groups = 0
Scene/Prefab dirty = 0
```

Unity/Test Runner gate를 실제 실행하지 못하면 `BLOCKED`. test 실패를 production 변경, relaxed invariant, hidden skip, excluded test로 우회하지 않는다.

## Result / Finalize

현재 Result를 `<=150 lines`로 교체한다.

기록 필수:

```text
TASK / STATUS / SUMMARY
PATCH APPLY / READ / PRIOR FAILURE / REPAIR
CREATED / MODIFIED / PRESERVED
OVERLAY SNAPSHOT / TYPE4 / VALIDATION / VISUAL
TEST / UNITY / ASSET META / CHANGE SCOPE
OWNERSHIP AUDIT / OUT_OF_SCOPE_FINDINGS / DONE CONDITIONS / NEXT / Recommended Commit
```

PASS Result exact lines:

```text
STATUS: PASS
MAP05_10: COMPLETE ELIGIBLE
MAP05_11_MAP05_BATCH_AND_EXIT_TESTS: LOCKED / DO NOT START
```

모든 gate가 PASS일 때만 MAP05_10을 COMPLETE, Current Task NONE으로 finalize한다. MAP05_11은 LOCKED로 유지하며 별도 patch 없이는 생성/시작하지 않는다.

Recommended Commit: `test(map): allow route overlay symbols after MAP05_10`
