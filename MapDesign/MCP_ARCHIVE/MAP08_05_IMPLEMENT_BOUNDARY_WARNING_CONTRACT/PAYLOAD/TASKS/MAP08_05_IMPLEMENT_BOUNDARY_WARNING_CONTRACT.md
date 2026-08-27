# MAP08_05 - Implement Boundary Warning Contract

```yaml
status_control:
  task_key: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
  result_file: REPORTS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT_RESULT.md
```

## TASK TYPE

```text
BOUNDARY WARNING LENGTH + DISTINCT MARKER CONTRACT VALIDATOR
```

## Objective

MAP08_04 PASS/finalize 뒤 boundary candidate가 실제 authored content로 확장되기 전에, 다음 biome 경고 조건을 Runtime-only contract와 validator로 고정한다.

이 Task는 warning length와 marker category sufficiency까지만 연다. Pair-specific boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_04_FILTER_MANDATORY_BOUNDARIES
STATUS: PASS
MAP08_04: COMPLETE ELIGIBLE
MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT: LOCKED / DO NOT START
SHA-256: f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd
```

이 별도 patch가 적용된 뒤에만 MAP08_05를 실행한다. MAP08_06 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_04 Result SHA-256: f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd
MAP08_04 Task SHA-256: 9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
MAP08_04 patch receipt SHA-256: 11ef33b9315b643f470229dc23547dda3cd5233d4ada73347024bc255bfea3d9
MAP08_04 focused tests: 520/520 PASS
MAP08_03 focused tests: 680/680 PASS
MAP08_02 focused tests: 580/580 PASS
MAP08_01 focused tests: 400/400 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required total: 12307/12307 PASS
MAP08_04 failed/skipped: 0/0
MAP08_04 compile/Console/relevant warnings: 0/0/0
Assets meta: 3447
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP08_04: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_04: 0
Duplicate GUID groups: 0
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP08_BOUNDARY_CHUNKS.md
03_CSV_SCHEMA/CSV_SCHEMA_REFERENCE.md
04_CSV_STARTER/biome_boundary_profiles.csv
04_CSV_STARTER/biome_boundary_pair_rules.csv
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 warning length, biome marker minimum, and MAP08 ownership 확인 용도다. MAP08_06+ Task 문서와 authored boundary chunk content body는 읽지 않는다.

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

금지: MAP08_06+ Task body, authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# - exact 6

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningMarkerCategory.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningRequirement.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningProbeRequest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningProbeResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningProbe.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningContractTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningProbeTests.cs
```

Matching `.cs.meta` files are required.

### Existing MAP08 boundary files - exact up to 18

기존 MAP08_01~MAP08_04 boundary Runtime/Test C#는 warning contract integration을 위해 필요한 최소 범위만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### 기존 phase-boundary tests - exact up to 28

MAP08_05 symbols를 허용하고 MAP08_06+ future symbols 금지를 유지하기 위해 필요한 경우 existing boundary/namespace guard test C#만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT_RESULT.md
```

## Required Implementation Contract

### Marker categories

Define a deterministic marker category contract. Required accepted values:

```text
Tile
Background
Resource
Audio
```

Accepted marker categories are case-sensitive canonical values. Unknown, empty, whitespace-only, padded, duplicated, and null inputs must be rejected or normalized only when the normalization is explicitly documented and tested.

### Warning requirement

The warning requirement must be derived from the resolved boundary profile and pair/orientation contract:

```text
boundary_profile_id
orientation
warning_microchunks_min
required_distinct_marker_categories
allowed_marker_categories
```

`warning_microchunks_min` must come from the boundary profile contract and must be positive. For the Moonpalace pair profiles currently used by `biome_boundary_pair_rules.csv`, the accepted transition must require at least two distinct marker categories from:

```text
Tile
Background
Resource
Audio
```

`BOUND_HARD_STARSTONE` is not a pair-rule boundary profile in the current Moonpalace pair set; do not use it to weaken the pair warning contract.

### Probe request

The warning probe request must carry enough immutable input to validate a candidate without authoring content:

```text
resolve_request
candidate
warning_requirement
warning_microchunk_count
observed_marker_categories
target_biome
```

The probe may accept synthetic test evidence and future authored evidence through the same API. It must not read or write boundary CSV rows in this Task.

### Probe result

The result must include:

```text
accepted
warning_microchunk_count
required_warning_microchunks
observed_distinct_marker_category_count
required_distinct_marker_category_count
observed_marker_categories
missing_marker_category_count
issue_list
```

Issue ordering must be deterministic. The result must preserve request/candidate identity and must not mutate the candidate, resolver request, weights, edge signature, route role, pair key, profile id, or orientation.

### Issues

At minimum, distinguish:

```text
InvalidRequest
MissingBoundaryProfile
InvalidWarningLength
InsufficientWarningLength
InsufficientMarkerCategories
UnknownMarkerCategory
DuplicateMarkerCategory
TargetBiomeMismatch
```

If multiple issues apply, report all applicable structural issues and keep deterministic ordering. Tests must cover the ordering.

### Ownership boundary

- The probe may validate warning evidence for a boundary candidate.
- It must not select a winner when multiple candidates remain. MAP08_03 resolver owns final selection.
- It must not filter mandatory candidates by tool requirement. MAP08_04 owns that.
- It must not author pair-specific boundary microchunks. MAP08_06~MAP08_11 own that.
- It must not write generated CSV or sector recipe output. Later MAP phases own that.
- It must not create Scene, Prefab, ScriptableObject, Tilemap, asmdef, or asmref assets.

## Required Tests

Run only task-relevant focused tests plus required MAP08/MAP07/MAP06/MAP05 regression gates.

```text
MoonpalaceBoundaryWarningContractTests >=260 PASS
MoonpalaceBoundaryWarningProbeTests >=260 PASS
MoonpalaceMandatoryBoundaryFilterTests 320/320 PASS
MoonpalaceBoundaryToolRequirementTests 200/200 PASS
MoonpalaceBoundaryChunkResolverTests 420/420 PASS
MoonpalaceBoundaryTransformPolicyTests 260/260 PASS
MoonpalaceBoundaryCandidateIndexTests 360/360 PASS
MoonpalaceBoundaryCandidateKeyTests 220/220 PASS
MoonpalaceBiomePairCatalogTests 220/220 PASS
MoonpalaceBiomePairContractTests 180/180 PASS
MAP07 required total 5422/5422 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=12827 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3447 -> 3455
New Runtime production C#/matching meta 6/6
New Runtime test C#/matching meta 2/2
New Runtime folder meta 0
New Editor production C#/matching meta 0/0
New Editor test C#/matching meta 0/0
Existing MAP08 boundary production/test C# modified <=18
Matching existing boundary production/test meta modified 0
Task-local existing boundary test C# modified <=28
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta 50/50
Authoring manifest SHA-256 unchanged: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes 0
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0
MAP08_06+ / MAP09+ forbidden production symbol hits 0 / 0
Unapplied MCP patches 0
```

## Commit Requirement

After all implementation and validation gates pass, create one git commit for this Task when the target project is a valid git repository.

Required commit behavior:

```text
Stage only task-owned files and the MAP08_05 Result report.
Do not stage unrelated pre-existing worktree items.
Do not run git push.
Use a detailed commit body describing implementation details, validation gates, and preserved ownership boundaries.
If git is unavailable, report the exact blocker and do not fake a commit hash.
```

Suggested commit subject:

```text
MAP08_05: implement boundary warning contract
```

The Result report must include the final `git status --short` scope and either the created commit hash or the precise reason no commit was created.

## Forbidden

Do not implement or mutate:

- MAP08_06~MAP08_11 authored boundary chunk CSV/content rows.
- MAP08_12 coverage validator.
- MAP08_13 preview window.
- MAP08_14 exit tests.
- MAP09 sector recipe or assembly logic.
- Generated CSV writer/output.
- Scene, Prefab, ScriptableObject, Tilemap, ProjectSettings, Packages, asmdef, asmref.
- Legacy/Stage/P6/P11 generator body.

## Required Result Header

```text
TASK: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
STATUS: PASS | FAIL | BLOCKED
MAP08_05: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES: LOCKED / DO NOT START
```

## Required Result Evidence

The Result must include:

- Prior MAP08_04 Result SHA-256 and Task SHA-256.
- Applied MAP08_05 patch receipt SHA-256.
- Warning requirement fields and accepted marker categories.
- Evidence that every active Moonpalace pair profile requires sufficient warning length and at least two distinct marker categories.
- Warning probe pass/fail issue examples and deterministic issue ordering.
- Resolver/filter ownership preservation evidence.
- Exact Unity test counts.
- Compile/Console/warning counts.
- Assets meta counts and duplicate GUID scan.
- Authoring CSV manifest unchanged proof.
- Generated CSV / Scene / Prefab / ProjectSettings / Packages / asmdef / asmref unchanged proof.
- Forbidden MAP08_06+/MAP09+ symbol scan result.
- Commit hash or exact no-commit blocker.

If any required gate fails, report `STATUS: FAIL` or `STATUS: BLOCKED` and do not open MAP08_06.
