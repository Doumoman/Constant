# MAP08_04 - Filter Mandatory Boundaries

```yaml
status_control:
  task_key: MAP08_04_FILTER_MANDATORY_BOUNDARIES
  result_file: REPORTS/MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md
```

## TASK TYPE

```text
MANDATORY ROUTE BOUNDARY NO-TOOL FILTER + RESOLVER INPUT GATE TESTS
```

## Objective

MAP08_03 PASS/finalize 뒤 mandatory route boundary 요청에서 `tool_requirement=NONE`이고 `mandatory_route_allowed=true`인 후보만 resolver 입력으로 전달하는 Runtime-only filter를 구현한다.

이 Task는 mandatory boundary filter까지만 연다. Warning marker/length contract, pair-specific boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER
STATUS: PASS
MAP08_03: COMPLETE ELIGIBLE
MAP08_04_FILTER_MANDATORY_BOUNDARIES: LOCKED / DO NOT START
SHA-256: 43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445
```

이 별도 patch가 적용된 뒤에만 MAP08_04를 실행한다. MAP08_05 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_03 Result SHA-256: 43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445
MAP08_03 Task SHA-256: 1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63
MAP08_03 patch receipt SHA-256: cf65fbb4444f4a08d67129b185f2c567cc767bc5413500edcf2e5f2f5fd60a26
MAP08_03 focused tests: 680/680 PASS
MAP08_02 focused tests: 580/580 PASS
MAP08_01 focused tests: 400/400 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required total: 11787/11787 PASS
MAP08_03 failed/skipped: 0/0
MAP08_03 compile/Console/relevant warnings: 0/0/0
Assets meta: 3439
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP08_03: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_03: 0
Duplicate GUID groups: 0
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP08_BIOME_BOUNDARY_CHUNKS.md
03_CSV_SCHEMA/BIOME_BOUNDARY_FORMAT.md
04_CSV_STARTER/biome_boundary_profiles.csv
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 mandatory-route ownership and no-tool policy 확인 용도다. MAP08_05+ Task 문서와 authored boundary chunk content body는 읽지 않는다.

## READ ALLOWLIST

### Existing MAP08 boundary contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/*
```

### Required regression scope

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
```

금지: MAP08_05+ Task body, warning implementation body, authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# - exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryToolRequirement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMandatoryBoundaryFilterRequest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMandatoryBoundaryFilterIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMandatoryBoundaryFilterResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMandatoryBoundaryFilterPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceMandatoryBoundaryFilter.cs
```

Matching `.cs.meta` files are required.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceMandatoryBoundaryFilterTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryToolRequirementTests.cs
```

Matching `.cs.meta` files are required.

### Existing MAP08 boundary files - exact up to 16

기존 MAP08_01~MAP08_03 boundary Runtime/Test C#는 `MoonpalaceBoundaryToolRequirement` typed contract와 filter integration을 위해 필요한 최소 범위만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### 기존 phase-boundary tests - exact up to 26

MAP08_04 symbols를 허용하고 MAP08_05+ future symbols 금지를 유지하기 위해 필요한 경우 existing boundary/namespace guard test C#만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md
```

## Required Implementation Contract

### Tool requirement

Define a deterministic tool requirement contract. Required accepted values:

```text
NONE
Pickaxe
Rope
Bomb
KeyItem
```

`NONE` is the only requirement allowed for mandatory route boundaries. Unknown, empty, padded, or case-variant tokens must fail parsing unless an existing strict parser already enforces that behavior.

### Filter request

The filter request must carry:

```text
resolve_request
candidate_index
mandatory_route_boundary
```

When `mandatory_route_boundary=false`, the filter may pass through candidates unchanged. When `true`, it must remove every candidate whose `mandatory_route_allowed` is false or whose `tool_requirement` is not `NONE`.

### Filter result

The result must include:

```text
original_candidate_count
accepted_candidate_count
rejected_candidate_count
accepted_candidates
rejection_summary_by_reason
issue_list
```

The accepted candidate order must remain deterministic and match the resolver/index ordering contract.

### Rejection reasons

At minimum, distinguish:

```text
ToolRequired
MandatoryRouteNotAllowed
InvalidRequest
NoCandidatesAfterFilter
```

Candidates rejected for both tool and mandatory-route reasons must be counted deterministically with both reasons or a documented stable priority. The priority must be tested.

### Resolver boundary

- The filter may prepare a filtered candidate list or filtered temporary index for the resolver.
- It must not choose a winner when multiple candidates remain. MAP08_03 resolver owns final selection.
- It must not change candidate weights, pair keys, route roles, orientation, edge signature, or request direction.
- It must not evaluate warning marker sufficiency. MAP08_05 owns that.

## Required Tests

Run only task-relevant focused tests plus required MAP08/MAP07/MAP06/MAP05 regression gates.

```text
MoonpalaceMandatoryBoundaryFilterTests >=320 PASS
MoonpalaceBoundaryToolRequirementTests >=200 PASS
MoonpalaceBoundaryChunkResolverTests 420/420 PASS
MoonpalaceBoundaryTransformPolicyTests 260/260 PASS
MoonpalaceBoundaryCandidateIndexTests 360/360 PASS
MoonpalaceBoundaryCandidateKeyTests 220/220 PASS
MoonpalaceBiomePairCatalogTests 220/220 PASS
MoonpalaceBiomePairContractTests 180/180 PASS
MAP07 required total 5422/5422 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=12307 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3439 -> 3447
New Runtime production C#/matching meta 6/6
New Runtime test C#/matching meta 2/2
New Runtime folder meta 0
New Editor production C#/matching meta 0/0
New Editor test C#/matching meta 0/0
Existing MAP08 boundary production/test C# modified <=16
Matching existing boundary production/test meta modified 0
Task-local existing boundary test C# modified <=26
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta 50/50
Authoring manifest SHA-256 unchanged: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes 0
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0

MAP08_05+ production symbol hits 0
MAP09+ production symbol hits 0
Unapplied MCP patches 0
```

## Forbidden This Task

- MAP08_05 warning length/marker renderer.
- MAP08_06~MAP08_11 authored boundary chunk CSV/content rows.
- MAP08_12 coverage validator, MAP08_13 preview window, MAP08_14 exit tests.
- MAP09 sector assembly, recipe resolver, compatibility solver.
- Generated CSV output.
- Authoring CSV mutation.
- ScriptableObject assets.
- Scene, Prefab, ProjectSettings, Packages, asmdef, asmref changes.
- Legacy/Stage/P6/P11 generator dependency as implementation base.
- Git commit/push.

## Result Report Requirements

Create exactly:

```text
MapDesign/MCP/REPORTS/MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md
```

Required header:

```text
TASK: MAP08_04_FILTER_MANDATORY_BOUNDARIES
STATUS: PASS | FAIL | BLOCKED
MAP08_04: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT: LOCKED / DO NOT START
```

Required evidence:

- Prior MAP08_03 Result SHA-256 and Task SHA-256.
- Applied patch receipt SHA-256.
- Tool requirement accepted/rejected tokens.
- Mandatory and non-mandatory filter behavior.
- Accepted/rejected counts and rejection priority.
- No-candidate-after-filter behavior.
- Resolver boundary evidence proving no winner selection in the filter.
- Focused test counts and required regression totals.
- Compile/Console/warning counts.
- Assets meta before/after.
- Authoring CSV/meta count and manifest SHA.
- Generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref change counts.
- Forbidden MAP08_05+/MAP09+ symbol scan result.

If any required gate fails, report `STATUS: FAIL` or `STATUS: BLOCKED` and do not open MAP08_05.
