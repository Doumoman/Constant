# MAP08_02 - Implement Boundary Candidate Index

```yaml
status_control:
  task_key: MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX
  result_file: REPORTS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX_RESULT.md
```

## TASK TYPE

```text
BOUNDARY CANDIDATE INDEX MODEL + DETERMINISTIC LOOKUP TESTS
```

## Objective

MAP08_01 PASS/finalize 뒤 MoonPalace boundary candidates를 deterministic key로 색인하는 Runtime-only contract를 구현한다. 이 Task는 candidate source를 실제 Authoring CSV로 만들지 않고, in-memory candidate definitions를 입력받아 pair/profile/orientation/route/signature key별 read-only index를 생성하고 조회하는 모델과 테스트까지만 연다.

Boundary chunk resolver, mandatory-boundary filtering, warning implementation, pair-specific boundary content authoring, generated CSV writer, sector assembly, Scene/Prefab output은 구현하지 않는다. `MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER`와 이후 Task body는 읽거나 시작하지 않는다.

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
12. `REPORTS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS_RESULT.md`

Prior Result exact gate:

```text
TASK: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS
STATUS: PASS
MAP08_01: COMPLETE ELIGIBLE
MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX: LOCKED / DO NOT START
SHA-256: bc9298f3e51615b4d9724bcd2d7c8809b1ba8d3455aa30e8436f6a25ab6d5970
```

이 별도 patch가 적용된 뒤에만 MAP08_02를 실행한다. MAP08_03 이후 Task body는 읽거나 시작하지 않는다.

## Approved Input Baseline

```text
Unity: 6000.3.8f1
Runtime assembly: Game.Map.Runtime
Runtime namespace boundary: StarNight.Map.WorldGeneration.*
Runtime EditMode assembly: Game.Map.Tests.EditMode
Editor assembly: MapAuthoring.Editor
Editor EditMode assembly: MapAuthoring.Tests.EditMode
New asmdef/asmref: NO
MAP08_01 Result SHA-256: bc9298f3e51615b4d9724bcd2d7c8809b1ba8d3455aa30e8436f6a25ab6d5970
MAP08_01 Task SHA-256: 19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d
MAP08_01 patch receipt SHA-256: b82282016e5d352cb6adbd0605ed474698bf4c569ce32d40473984c1ead56858
MAP08_01 focused tests: 400/400 PASS
MAP07 required total: 5422/5422 PASS
MAP06 required total: 2746/2746 PASS
MAP05 required total: 1959/1959 PASS
Actually executed required total: 10527/10527 PASS
MAP08_01 failed/skipped: 0/0
MAP08_01 compile/Console/relevant warnings: 0/0/0
Assets meta: 3419
Authoring CSV/meta: 50 / 50
Authoring manifest SHA-256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Generated CSV files created by MAP08_01: 0
Scene/Prefab/ProjectSettings/Packages/asmdef/asmref changes by MAP08_01: 0
Duplicate GUID groups: 0
```

## Map Package Reference

Exact installed Map Package v1.0이 있으면 아래만 읽는다.

```text
01_FIXED_SPEC/03_DATA_OWNERSHIP_AND_PASS_BOUNDARIES.md
02_PHASE_ROADMAP/MAP08_BIOME_BOUNDARY_CHUNKS.md
03_CSV_SCHEMA/TILE_AUTHORING_FORMAT.md
03_CSV_SCHEMA/BIOME_BOUNDARY_FORMAT.md
04_CSV_STARTER/biome_definitions.csv
04_CSV_STARTER/biome_boundary_profiles.csv
04_CSV_STARTER/microchunk_catalog.csv
04_CSV_STARTER/microchunk_sockets.csv
04_CSV_STARTER/socket_band_definitions.csv
06_CHECKLISTS/VALIDATOR_MATRIX.md
```

Reference는 index key semantics, Authoring/generated ownership, MAP07 socket/signature contract, and MAP08_03 resolver ownership을 확인하는 용도다. MAP08_03+ Task 문서와 authored boundary chunk content body는 읽지 않는다.

## READ ALLOWLIST

### Existing MAP08_01 boundary contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomeId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomePair.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryOrientation.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryWarningMarker.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomePairDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBiomePairCatalog.cs
```

### Existing MAP07 microchunk/socket contracts

```text
Assets/_Game/Map/Runtime/WorldGeneration/Microchunks/*
Assets/_Game/Map/Runtime/Data/Definitions/*
Assets/_Game/Map/Runtime/Data/Registry/*
Assets/_Game/Map/Runtime/Data/Csv/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/*
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/*
Assets/_Game/Editor/MapAuthoring/Tests/EditMode/WorldGeneration/*
```

읽기는 public API, signature model, regression test selection, Authoring inventory hash, Assets meta count 확인에 한정한다.

금지: MAP08_03+ Task body, boundary resolver implementation body, authored boundary content body, generated output body, MAP09 sector assembly implementation body, Legacy/Stage/P6/P11 generator body, Scene/Prefab YAML.

## WRITE ALLOWLIST

### 신규 Runtime production C# - exact 8

```text
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryProfileId.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryRouteRole.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryEdgeSignature.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateKey.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateDefinition.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateIndexEntry.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateIndex.cs
Assets/_Game/Map/Runtime/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateIndexer.cs
```

Matching `.cs.meta` files are required. The `Boundaries` runtime folder already exists from MAP08_01; new runtime folder meta must remain `0`.

### 신규 Runtime EditMode tests - exact 2

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateIndexTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Boundaries/MoonpalaceBoundaryCandidateKeyTests.cs
```

Matching `.cs.meta` files are required. The `Boundaries` test folder already exists from MAP08_01; new test folder meta must remain `0`.

### 기존 phase-boundary tests - exact up to 22

MAP08_02 symbols를 허용하고 MAP08_03+ future symbols 금지를 유지하기 위해 필요한 경우 existing boundary/namespace guard test C#만 수정할 수 있다. Matching existing `.cs.meta`는 수정하지 않는다.

### Result report - exact 1

```text
MapDesign/MCP/REPORTS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX_RESULT.md
```

## Required Implementation Contract

### Candidate key

Define a stable key with exactly these semantic fields:

```text
MoonpalaceBiomePair pair
MoonpalaceBoundaryProfileId profile
MoonpalaceBoundaryOrientation orientation
MoonpalaceBoundaryRouteRole route_role
MoonpalaceBoundaryEdgeSignature edge_signature
```

The key must be value-equal, hash-stable, culture-invariant, and deterministically formatted. Pair input must be canonicalized through MAP08_01 pair semantics. Invalid pair, empty profile, invalid orientation, empty route role, and empty signature must be rejected.

### Candidate definition

Define candidate records as immutable data. Required fields:

```text
candidate_id
pair
profile
orientation
route_role
edge_signature
weight
mandatory_route_allowed
tool_requirement
warning_markers
```

This Task may use in-memory fixture definitions only. It must not create Authoring CSV rows, generated CSV rows, ScriptableObject assets, Scene objects, Prefabs, or tilemap content.

### Index build

- Candidate IDs must be globally unique.
- Duplicate candidate IDs fail.
- Unknown biome pairs fail.
- Pair/orientation combinations not allowed by MAP08_01 fail.
- Empty candidate source produces a valid empty index with zero keys.
- Duplicate keys are allowed and must map to a deterministic candidate list.
- Candidate lists must be sorted by stable candidate ID, then weight, then signature.
- Key enumeration must be sorted by pair order, profile, orientation, route role, and signature.
- Public collections must be immutable or copy-safe.

### Lookup behavior

Support exact lookup by full key and filtered lookup by:

```text
pair
pair + orientation
pair + profile + orientation
pair + route_role
```

Lookup must not select or weight-randomize a candidate. Candidate choice belongs to MAP08_03. Mandatory route filtering belongs to MAP08_04.

### Reversal behavior

The index must accept reversed pair lookup input and resolve it to the same canonical pair key. It must not silently reverse edge signatures or transforms; A->B/B->A transform resolution belongs to MAP08_03.

## Required Tests

Run only task-relevant focused tests plus required MAP08_01/MAP07/MAP06/MAP05 regression gates.

```text
MoonpalaceBoundaryCandidateIndexTests >=360 PASS
MoonpalaceBoundaryCandidateKeyTests >=220 PASS
MoonpalaceBiomePairCatalogTests 220/220 PASS
MoonpalaceBiomePairContractTests 180/180 PASS
MAP07 required total 5422/5422 PASS
MAP06 required total 2746/2746 PASS
MAP05 required total 1959/1959 PASS
Actually executed required total >=11107 PASS
Required failed/skipped 0/0
Unity compile errors 0
Final Console errors/warnings 0/0
Relevant warnings 0
```

## Static Gates

```text
Assets meta 3419 -> 3429
New Runtime production C#/matching meta 8/8
New Runtime test C#/matching meta 2/2
New Runtime folder meta 0
New Editor production C#/matching meta 0/0
New Editor test C#/matching meta 0/0
Task-local existing boundary test C# modified <=22
Matching existing boundary-test meta modified 0
Assets duplicate GUID groups 0

Authoring CSV/matching meta 50/50
Authoring manifest SHA-256 unchanged: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
Authoring CSV tracked changes 0
Generated CSV files created 0

Scene/Prefab tracked changes 0/0
ProjectSettings/Packages tracked changes 0/0
asmdef/asmref tracked changes 0/0

MAP08_03+ production symbol hits 0
MAP09+ production symbol hits 0
Unapplied MCP patches 0
```

## Forbidden This Task

- MAP08_03 boundary candidate selection/resolver.
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
MapDesign/MCP/REPORTS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX_RESULT.md
```

Required header:

```text
TASK: MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX
STATUS: PASS | FAIL | BLOCKED
MAP08_02: COMPLETE ELIGIBLE | REPAIR REQUIRED | BLOCKED
MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER: LOCKED / DO NOT START
```

Required evidence:

- Prior MAP08_01 Result SHA-256 and Task SHA-256.
- Applied patch receipt SHA-256.
- Candidate key field contract.
- Candidate definition field contract.
- Exact lookup/filter lookup behavior.
- Reversed pair canonical lookup evidence.
- Duplicate ID, invalid pair, invalid orientation, and duplicate key behavior.
- Empty index behavior.
- Focused test counts and required regression totals.
- Compile/Console/warning counts.
- Assets meta before/after.
- Authoring CSV/meta count and manifest SHA.
- Generated CSV, Scene/Prefab, ProjectSettings/Packages, asmdef/asmref change counts.
- Forbidden MAP08_03+/MAP09+ symbol scan result.

If any required gate fails, report `STATUS: FAIL` or `STATUS: BLOCKED` and do not open MAP08_03.
