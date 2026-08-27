# MAP05_03 — Minimum Connector Tree Repair v1.1

```yaml
status_control:
  task_key: MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE
  result_file: REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md
  repair_contract: OBSOLETE_ROUTE_MASK_NEGATIVE_ASSERTION
```

## Goal

MAP05_03 implementation은 compile되고 신규 focused suite `MandatoryConnectorTreeBuilderTests 129/129`를 통과했다. 실패 원인은 MAP05_02 회귀 테스트가 MAP05_03의 정식 산출물인 `MandatoryConnectorTree` 심볼 부재를 계속 요구하는 obsolete negative assertion이다.

이번 repair는 production과 MAP05_03 implementation을 수정하지 않고, MAP05_02 회귀 테스트의 later-task symbol audit만 MAP05_03 완료 상태에 맞게 교정한다.

MAP05_04는 시작하지 않는다.

## Preconditions / Prior Failure

control → Master/Status → current MAP05_03 Task → current FAIL Result → allowlisted MAP05_02 test 순서로 읽는다.

```text
Current Task SHA before repair:
dd54e1c01ee8a248dd2a480f39ab664d3a83e3d88927b419fa9da661656168f5

FAIL Result SHA:
635b0c792b01ad2413ad65faa2c7d06290f9c263e71ef93a39d8ae3320dcf35a

Observed failure:
MandatoryRouteMaskLookupBuilderTests 126/127 PASS
LaterTaskProductionSymbolsAreAbsent("MandatoryConnectorTree")
```

Current Task나 FAIL Result가 다르면 `BLOCKED`. MAP05_04 이후 Task body는 읽거나 시작하지 않는다.

## Root Cause / Authority

- MAP05_02의 negative assertion은 MAP05_02 당시에는 정상이었다. 그 시점에서는 connector tree가 later-task symbol이었기 때문이다.
- MAP05_03에서는 `MandatoryConnectorTree`가 현재 Task의 required output type이다.
- 따라서 MAP05_03 검증 중에도 MAP05_02 회귀 테스트가 `MandatoryConnectorTree` 부재를 요구하면 phase boundary가 자기모순이 된다.
- 수정 대상은 테스트의 forbidden symbol set뿐이다. MAP05_02 route mask lookup contract, production code, public API, route mask behavior는 변경하지 않는다.

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
MapDesign/MCP/TASKS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE.md
MapDesign/MCP/REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md
```

Existing Assets read:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryConnectorTreeBuilderTests.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTree.cs
Assets/_Game/Map/Runtime/WorldGeneration/Generation/MandatoryConnectorTreeBuilder.cs
Assets/_Game/Map/Runtime/Game.Map.Runtime.asmdef
Assets/_Game/Tests/EditMode/Map/Game.Map.Tests.EditMode.asmdef
```

MODIFY:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/MandatoryRouteMaskLookupBuilderTests.cs
MapDesign/MCP/REPORTS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE_RESULT.md
```

`MandatoryRouteMaskLookupBuilderTests.cs.meta`는 읽기만 하며 SHA/GUID를 보존한다.

금지:

- MAP05_03 production C# 수정
- MAP05_03 신규 focused test 수정
- MAP05_01/MAP05_02 production 수정
- Master/Status 수정
- Authoring CSV/meta 수정
- asmdef/Scene/Prefab/Packages/ProjectSettings 수정
- test skip/ignore/assertion 완화로 coverage 삭제
- MAP05_04 이후 구현
- Git operation

## Required Test Repair

기존 test helper/name은 유지해도 된다. 의미는 아래로 교정한다.

```text
MAP05_02 regression still forbids MAP05_04+ production symbols.
MAP05_03 current output symbols are allowed:
  MandatoryConnectorEdgeId
  MandatoryConnectorEdgeCost
  MandatoryConnectorCandidateEdge
  MandatoryConnectorTree
  MandatoryConnectorTreeBuildError
  MandatoryConnectorTreeDiagnostics
  MandatoryConnectorTreeBuildResult
  MandatoryConnectorTreeBuilder
```

허용되는 수정 예:

- forbidden symbol list에서 MAP05_03 output symbols만 제거
- test 이름을 `LaterRouteTaskProductionSymbolsAreAbsent`처럼 MAP05_04+ 범위가 드러나게 조정
- assertion message를 MAP05_04+ later-task audit로 수정

허용되지 않는 수정:

- symbol audit test 전체 삭제
- MAP05_04+ forbidden symbols까지 허용
- `MandatoryRouteMaskLookupBuilderTests` case 수를 줄이는 변경
- `Assert.Ignore`, `[Explicit]`, assumption skip, broad try/catch로 failure 숨김
- production 심볼 rename으로 test 우회

## Required Runs

수정 후 actual test gates:

```text
MandatoryConnectorTreeBuilderTests      >=118 PASS
MandatoryRouteMaskLookupBuilderTests    127/127 PASS
MandatoryTerminalBuilderTests           120/120 PASS
SiteReservationValidatorTests           268/268 PASS
BiomePatchValidatorTests                196/196 PASS
Map04ExitTests                          110/110 PASS
Actually executed total                 >=939 PASS
failed/skipped                            0/0
```

large suites discovery-only under reduced profile:

```text
Game.Map targeted discovery >=5730
Full EditMode discovery      >=5841
```

forced refresh/compile/Console/relevant warning `0/0/0`.

Asset/scope gate:

```text
Assets meta 3179 -> 3179
modified existing test C# = 1
new Runtime/Test C#/meta = 0/0/0
production modifications = 0
MAP05_03 implementation/test modifications = 0
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
TREE NODES / CANDIDATE EDGES / TREE EDGES / COST MODEL
TEST / UNITY / ASSET META / CHANGE SCOPE
OWNERSHIP AUDIT / OUT_OF_SCOPE_FINDINGS / DONE CONDITIONS / NEXT / Recommended Commit
```

PASS Result exact lines:

```text
STATUS: PASS
MAP05_03: COMPLETE ELIGIBLE
MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER: LOCKED / DO NOT START
```

모든 gate가 PASS일 때만 MAP05_03을 COMPLETE, Current Task NONE으로 finalize한다. MAP05_04는 LOCKED로 유지하며 별도 patch 없이는 생성/시작하지 않는다.

Recommended Commit: `test(map): allow connector tree symbol after MAP05_03`
