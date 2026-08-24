# MAP08_03 - Implement Boundary Chunk Resolver

```yaml
status_control:
  task_key: MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER
  result_file: REPORTS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER_RESULT.md
```

## TASK TYPE

```text
BOUNDARY CANDIDATE RESOLVER + REVERSIBLE REQUEST DIRECTION TESTS
```

## Objective

MAP08_02 PASS/finalize 뒤 boundary candidate index에서 요청 조건에 맞는 후보를 조회하고, deterministic weight/tie-break와 request direction을 적용해 선택 결과 또는 실패 이유를 반환하는 Runtime-only resolver를 구현한다.

이 Task는 후보 선택 정책까지만 연다. Mandatory route filter, warning marker rendering, pair-specific boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_04_FILTER_MANDATORY_BOUNDARIES`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX
STATUS: PASS
MAP08_02: COMPLETE ELIGIBLE
MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER: LOCKED / DO NOT START
SHA-256: 2a160c7bc32cf7177208bbb0d06c0e449ef7dd3e7904bb23060484509d893c54
```

이 별도 patch가 적용된 뒤에만 MAP08_03을 실행한다. MAP08_04 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_02 Result SHA-256: 2a160c7bc32cf7177208bbb0d06c0e449ef7dd3e7904bb23060484509d893c54
MAP08_02 Task SHA-256: 767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50
MAP08_02 patch receipt SHA-256: 7b39a6ad3c7690e86e4313fd801173083317c95d73d7192fb59c17f6cc40d693
MAP08_02 focused tests: 580/580 PASS
MAP08_01 focused tests: 400/400 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required total: 11107/11107 PASS
MAP08_02 failed/skipped: 0/0
MAP08_02 compile/Console/relevant warnings: 0/0/0
Assets meta: 3429
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP08_02: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_02: 0
Duplicate GUID groups: 0
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP08_BIOME_BOUNDARY_CHUNKS.md
03_CSV_SCHEMA/BIOME_BOUNDARY_FORMAT.md
04_CSV_STARTER/biome_boundary_profiles.csv
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/socket_band_definitions.csv
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 resolver ownership, candidate transform semantics, and MAP08_04 mandatory filtering boundary를 확인하는 용도다. MAP08_04+ Task 문서와 authored boundary chunk content body는 읽지 않는다.

## READ ALLOWLIST

### Existing MAP08 boundary contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/*
```

### Existing MAP07 microchunk/socket contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
```

읽기는 public API, transform/signature model, regression test selection, Authoring inventory hash, Assets meta count 확인에 한정한다.

금지: MAP08_04+ Task body, mandatory filter implementation body, authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# - exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryRequestDirection.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryResolveRequest.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryResolveIssue.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryResolveResult.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryResolvedCandidate.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryTransformPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryResolvePolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryChunkResolver.cs
```

Matching `.cs.meta` files are required. Existing `Boundaries` folders from MAP08_01 must be reused; new folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryChunkResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryTransformPolicyTests.cs
```

Matching `.cs.meta` files are required.

### 기존 phase-boundary tests - exact up to 24

MAP08_03 symbols를 허용하고 MAP08_04+ future symbols 금지를 유지하기 위해 필요한 경우 existing boundary/namespace guard test C#만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER_RESULT.md
```

## Required Implementation Contract

### Resolve request

Define an immutable request with:

```text
from_biome
to_biome
profile
orientation
route_role
edge_signature
selection_seed
```

`from_biome` and `to_biome` may be either canonical or reversed order. Self-pairs, unknown biomes, invalid profile, invalid orientation, invalid route role, empty signature, and missing index must fail with explicit issue codes.

### Direction and transform policy

The resolver must preserve request direction separately from canonical pair:

```text
Forward  = request direction matches canonical pair order
Reverse  = request direction is opposite canonical pair order
```

For reverse requests, the resolver returns a transform policy result that later systems can use for A-to-B/B-to-A content reversal. It must not mutate the candidate edge signature or silently rewrite candidate data. Actual transformed tile placement belongs to MAP09/MAP11.

### Candidate lookup and selection

- Query the MAP08_02 index by exact key.
- If exact key has no candidates, return a deterministic failure result with `NoCandidates`.
- Candidate selection must be deterministic for the same request, seed, and index content.
- Weighted selection must use only positive weights. Zero-weight candidates remain addressable but never win weighted selection while any positive candidate exists.
- Ties must be resolved by candidate ID, then candidate signature.
- Selection must not use process time, Unity random state, dictionary iteration order, filesystem order, or Editor state.
- The resolver returns the selected candidate, canonical pair, request direction, transform policy, and selected key.

### Ownership boundary

- This Task may choose among candidates already present in an in-memory index.
- It must not create, author, or mutate boundary candidate content.
- It must not enforce mandatory-route filtering. MAP08_04 owns that.
- It must not evaluate warning marker acceptance. MAP08_05 owns that.
- It must not emit generated CSV or sector assembly output.

## Required Tests

Run only task-relevant focused tests plus required MAP08/MAP07/MAP06/MAP05 regression gates.

```text
MoonpalaceBoundaryChunkResolverTests >=420 PASS
MoonpalaceBoundaryTransformPolicyTests >=260 PASS
MoonpalaceBoundaryCandidateIndexTests 360/360 PASS
MoonpalaceBoundaryCandidateKeyTests 220/220 PASS
MoonpalaceBiomePairCatalogTests 220/220 PASS
MoonpalaceBiomePairContractTests 180/180 PASS
MAP07 required total 5422/5422 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=11787 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3429 -> 3439
New Runtime production C#/matching meta 8/8
New Runtime test C#/matching meta 2/2
New Runtime folder meta 0
New Editor production C#/matching meta 0/0
New Editor test C#/matching meta 0/0
Task-local existing boundary test C# modified <=24
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta 50/50
Authoring manifest SHA-256 unchanged: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes 0
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0

MAP08_04+ production symbol hits 0
MAP09+ production symbol hits 0
Unapplied MCP patches 0
```

## Forbidden This Task

- MAP08_04 mandatory-boundary filtering behavior.
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
MapDesign/MCP/REPORTS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER_RESULT.md
```

Required header:

```text
TASK: MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER
STATUS: PASS | FAIL | BLOCKED
MAP08_03: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_04_FILTER_MANDATORY_BOUNDARIES: LOCKED / DO NOT START
```

Required evidence:

- Prior MAP08_02 Result SHA-256 and Task SHA-256.
- Applied patch receipt SHA-256.
- Resolve request field contract.
- Forward/reverse request direction evidence.
- Transform policy evidence.
- Weighted deterministic selection evidence.
- No-candidate, invalid request, zero-weight, and tie-break evidence.
- Focused test counts and required regression totals.
- Compile/Console/warning counts.
- Assets meta before/after.
- Authoring CSV/meta count and manifest SHA.
- Generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref change counts.
- Forbidden MAP08_04+/MAP09+ symbol scan result.

If any required gate fails, report `STATUS: FAIL` or `STATUS: BLOCKED` and do not open MAP08_04.
