```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS
  task_file: TASKS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS.md
  requires_current_task: NONE
  requires_completed_task: MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS
  requires_result:
    path: REPORTS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS_RESULT.md
    status: PASS
    sha256: 0fc8a5da0d08f2eccdd5e183a2b8653cfc75a703e09441388a1682fa9fceef96
  requires_installed_task:
    path: TASKS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS.md
    sha256: a09d234a5fc837f972dd24d3983fb634c09bc2c91223f8f1db640223c1090a6d
  requires_handoff:
    name: MAP21_06 handoff digest
    sha256: 854cf1a08510898a1570676ce4ea30f1c0b3b3883c7694be49092f1fae88733b
  sets_current_task: MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS
```

# MAP21_06 - Expand All Six Boundary Pools

```text
TASK: MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS
PHASE: MAP21 - MoonPalace Vertical Slice
STATUS: CURRENT
NEXT: MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP08에서 승인된 6개 biome pair 경계 계약을 월궁 vertical slice의 production boundary pool로 확장한다.

이번 Task는 **MoonPalace boundary candidate authoring, H/V route profile, directional socket projection, warning evidence, digest manifest**만 소유한다. MAP08 원본 경계 CSV/코드/테스트는 읽기 전용 기준선이며, sector/world placement와 final 12x8 slice, Tilemap bake는 아직 시작하지 않는다.

| Area | This task owns | This task must not do |
|---|---|---|
| Pair inventory | MAP08의 정확한 6 pair를 모두 포함하는 production pool | pair 추가/삭제, PDF의 누락된 표 사용 |
| Candidate pool | pair마다 8개, 총 48개 boundary candidate | MAP08 31개 baseline rewrite |
| H/V split | pair마다 H 4개 / V 4개 | 방향 없는 generic edge |
| Direction projection | 각 candidate의 A->B/B->A projection, 총 96개 | world neighbor placement |
| Warning evidence | 각 directional projection의 Tile/Background/Resource/Audio evidence | warning 자동 무시 또는 삭제 |

핵심 원칙:

```text
This task expands boundary data, not generated maps.
MAP08 remains the read-only source of truth for pair identity and warning rules.
MAP21_06 writes only MoonPalace production boundary authoring and generated manifests.
No sector/world generation, final chunk slice, Tilemap, renderer, runtime object, or validation runner is executed.
```

## 1. 사용자 보고 의무

Result 첫 부분에는 반드시 아래 두 섹션을 둔다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`User-Facing Implementation Report`에는 아래 질문에 답한다.

```text
이번 작업이 MAP08 6 pair 경계를 production pool로 어떻게 확장했는가?
기존 31 candidate 기준선과 새 48 candidate production pool의 차이는 무엇인가?
각 pair의 H/V 후보 수량은 얼마인가?
directional projection 96개와 socket 96개는 무엇을 보증하는가?
warning evidence는 어떤 category로 보존되었는가?
이번 작업이 MAP08 원본, MAP21_03/04 cluster, MAP21_05 Activity/Event를 rewrite하지 않았다는 증거는 무엇인가?
이번 Task에서 추가한 script/CSV/JSON과 각 책임은 무엇인가?
legacy regression과 prior-category rerun을 하지 않았다는 증거는 무엇인가?
MAP21_07은 왜 아직 시작하지 않았는가?
```

`Responsibility and Added Scripts`는 반드시 표로 작성한다.

```text
| Script or file | Added or changed responsibility | Explicit non-ownership |
```

파일별 책임이 구체적이지 않거나, 사용자가 “그래서 뭘 했는지” 알 수 없으면 FAIL이다.

## 2. 선행조건

작업 시작 전에 다음을 확인한다.

```text
MapDesign/MCP/REPORTS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS_RESULT.md exists
MAP21_05 Result STATUS: PASS
MAP21_05 Result SHA-256:
0fc8a5da0d08f2eccdd5e183a2b8653cfc75a703e09441388a1682fa9fceef96

MapDesign/MCP/TASKS/MAP21_05_PRODUCTIONIZE_ACTIVITIES_AND_EVENT_OVERLAYS.md exists
MAP21_05 installed Task SHA-256:
a09d234a5fc837f972dd24d3983fb634c09bc2c91223f8f1db640223c1090a6d

MAP21_06 handoff digest:
854cf1a08510898a1570676ce4ea30f1c0b3b3883c7694be49092f1fae88733b

MAP08 phase exit Result exists:
MapDesign/MCP/REPORTS/MAP08_14_MAP08_EXIT_TESTS_RESULT.md
MAP08_14 Result STATUS: PASS
MAP08_14 Result SHA-256:
5d0b2f0d478ef8479b93e1b9163445f6e736022b533dee77f81690b8670cf2d1
MAP08_14 installed Task SHA-256:
6fffc0ed3f8ca333cf7d74d44c437ab6e4193871ce8b2a7a254405e4bcaa5e8e

MAP08 accepted boundary aggregate digest:
f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
MAP08 accepted authoring manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb

MAP21_04 all-biome cluster manifest digest:
d30baf04fcd6eceeabd7b6f467b24ce11ce7c5f44833887bf7b0457e6fbdda72

MAP21_05 activity/event digest manifest:
645f95b8c58757179223b8e5b219b60b5a732cb898a69f99594178c76f166a1e

MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS: LOCKED
```

불일치 시:

```text
STATUS: BLOCKED
reason: precondition mismatch
created/changed code files: 0
MAP21_07 started: NO
STOP
```

## 3. 허용 범위

허용 파일:

```text
Assets/_Game/Map/Runtime/WorldGeneration/MoonPalace/MoonPalaceBoundaryProduction.cs
Assets/_Game/Editor/MapAuthoring/WorldGeneration/MoonPalace/MoonPalaceBoundaryPoolPublisher.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceBoundaryProductionTests.cs
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/**
MapDesign/MCP/GENERATED/MAP21_06/**
MapDesign/MCP/TASKS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS.md
MapDesign/MCP/REPORTS/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS_RESULT.md
MapDesign/MCP_ARCHIVE/MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

조건부 허용:

```text
Existing MAP08 public boundary contracts may be referenced read-only.
Existing MAP21_01 profile/tile shell may be referenced read-only for MoonPalace biome/material vocabulary.
Existing MAP21_03/04 cluster manifests may be referenced read-only for biome identity coverage.
Existing MAP21_05 Activity/Event manifests may be referenced read-only only to prove no ownership overlap.
Authoring CSV writes are allowed only under Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06.
Generated JSON writes are allowed only under MapDesign/MCP/GENERATED/MAP21_06.
```

금지:

```text
MAP08 source CSV, production C#, or test rewrite
MAP21_01 profile/tile shell rewrite
MAP21_02 MicroPattern rewrite
MAP21_03/04 cluster pool rewrite or regeneration
MAP21_05 Activity/Event rewrite or regeneration
cluster placement into sector/world
sector/world generation
final 12x8 slice generation
Tilemap bake or Tilemap mutation
pattern renderer execution or behavior change
validation runner execution
actual replay execution
rollback execution
save/runtime state generation
Activity/Event placement or execution
SpecialRegion placement or execution
Collider generation
Scene / Prefab / ProjectSettings / Packages changes
sprite/audio/background asset import
Addressables group/build change
runtime GameObject instantiate/enable/disable/destroy during tooling actions
CSV authoring edit outside MAP21_06 authoring folder
auto-fix / auto-repair
production seed approval
external process launch
manual gameplay test
automatic MAP21_07 start
broad refactor or optimization rewrite
```

회귀 금지:

```text
MAP08 category rerun
MAP09_01 baseline rerun
MAP19_09 scale audit rerun
MAP20_01 generator run rerun
MAP20_02 overlay sample regeneration
MAP20_03 detail sample regeneration
MAP20_04 navigation sample regeneration
MAP20_05 replay/export sample regeneration
MAP20_06 exit audit regeneration
MAP21_01 profile tile shell regeneration
MAP21_02 MicroPattern regeneration
MAP21_03 Crater/Root cluster regeneration
MAP21_04 Mill/Dough cluster regeneration
MAP21_05 Activity/Event regeneration
legacy 19347 regression
prior task category selection
PlayMode selection
unfiltered test run
full regression run
```

## 4. Boundary Pair 계약

MAP21_06 must use MAP08's exact six pair IDs. Do not use the incomplete PDF boundary table as source of truth.

Required pair inventory:

| Pair ID | Required candidates | Horizontal | Vertical |
|---|---:|---:|---:|
| `PAIR_CRATER_ROOT` | 8 | 4 | 4 |
| `PAIR_CRATER_MILL` | 8 | 4 | 4 |
| `PAIR_CRATER_DOUGH` | 8 | 4 | 4 |
| `PAIR_ROOT_MILL` | 8 | 4 | 4 |
| `PAIR_ROOT_DOUGH` | 8 | 4 | 4 |
| `PAIR_MILL_DOUGH` | 8 | 4 | 4 |
| **TOTAL** | **48** | **24** | **24** |

Required boundary candidate fields:

```text
candidate_id
schema_version
pair_id
biome_a
biome_b
orientation
variant_index
route_profile_id
tile_profile_id
background_profile_id
resource_profile_id
audio_profile_id
source_map08_pair_digest
source_map08_warning_policy_digest
tile_digest
socket_digest
warning_digest
canonical_digest
```

Rules:

```text
There must be exactly 48 production boundary candidates.
Every pair must have exactly 8 candidates: 4 Horizontal and 4 Vertical.
Every candidate represents a 12x8 MicroChunk-sized boundary slice with exactly 96 tile cells.
Every candidate has exactly two directional projections: A_TO_B and B_TO_A.
Every candidate has exactly two socket rows, one per directional projection.
Every directional projection keeps tool_requirement NONE.
No candidate may reference a biome outside MoonCrater, CassiaRoot, AbandonedMill, MoonDough.
No candidate may duplicate candidate_id, projection key, tile coordinate, or socket key.
No candidate may be written back into MAP08 authoring folders.
```

Recommended stable ID shape:

```text
BND_<PAIR_TOKEN>_H_01..04
BND_<PAIR_TOKEN>_V_01..04
```

For example:

```text
BND_CRATER_ROOT_H_01
BND_CRATER_ROOT_V_01
BND_MILL_DOUGH_H_04
BND_MILL_DOUGH_V_04
```

## 5. Route, Socket, and Warning 계약

Required route profile fields:

```text
candidate_id
projection_id
pair_id
direction
orientation
entry_side
exit_side
socket_signature
route_intent
movement_tokens
access_class
mandatory_route
tool_requirement
profile_digest
canonical_digest
```

Required socket rules:

```text
Horizontal candidates must use left/right edge-compatible socket profiles.
Vertical candidates must use up/down edge-compatible socket profiles.
A_TO_B and B_TO_A may differ only by approved transform-policy reversal fields.
Socket signatures must preserve MAP08's EDGE_H_MID_WALK or EDGE_V_CENTER_CLIMB compatibility family.
Each projection must preserve route/profile identity enough for MAP14+ planners to consume it later.
This task does not place sockets into actual sectors.
```

Required warning evidence fields:

```text
projection_id
candidate_id
pair_id
direction
category
evidence_key
entering_biome
source_policy
severity
canonical_digest
```

Required warning rules:

```text
There must be exactly 96 directional projections.
Every projection must have warning evidence from at least two distinct categories.
Allowed categories are Tile, Background, Resource, Audio.
Minimum warning evidence rows: 192.
Warning evidence is not failure; it is transition context for downstream tooling.
Entering-biome references must match the projection direction.
No warning evidence may be dropped when direction reverses.
```

## 6. Required Authoring Files

Create exactly these MAP21_06 authoring CSVs unless a narrower equivalent is strongly justified in the Result.

```text
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/moonpalace_boundary_candidates.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/moonpalace_boundary_tiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/moonpalace_boundary_sockets.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/moonpalace_boundary_route_profiles.csv
Assets/_Game/Map/Authoring/WorldGeneration/MoonPalace/MAP21_06/moonpalace_boundary_warning_evidence.csv
```

Authoring rules:

```text
CSV must use UTF-8 without BOM, LF, one final LF, stable header order, stable row order.
Tile coordinates are local 12x8 boundary-slice coordinates only.
No generated world coordinates may be stored.
No file outside MAP21_06 authoring may be written.
```

## 7. Required Generated Artifacts

Create exactly these generated JSON artifacts.

```text
MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_pool_manifest.json
MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_projection_manifest.json
MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_warning_manifest.json
MapDesign/MCP/GENERATED/MAP21_06/moonpalace_boundary_digest_manifest.json
```

Generated artifact rules:

```text
JSON must be deterministic and canonical enough to hash repeatedly.
created_utc may exist only if excluded from canonical digest.
The digest manifest must include MAP08 aggregate/authoring digests, MAP21_05 digest manifest, every MAP21_06 CSV digest, every MAP21_06 JSON digest, and MAP21_07 handoff digest.
The generated artifacts are static publication samples, not world/sector/slice outputs.
```

## 8. Required Focused Tests

Add exactly one focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/MoonPalace/MoonPalaceBoundaryProductionTests.cs
```

Required test category:

```text
MAP21_06
```

Required focused test cases:

```text
MoonPalaceBoundaryPoolContainsExactSixPairsAndFortyEightCandidates
MoonPalaceBoundaryPoolHasFourHorizontalAndFourVerticalCandidatesPerPair
MoonPalaceBoundaryTilesHaveExactNinetySixCellsPerCandidateAndNoDuplicates
MoonPalaceBoundarySocketsPublishTwoDirectionalProjectionsPerCandidate
MoonPalaceBoundaryRouteProfilesPreserveHorizontalAndVerticalCompatibilityFamilies
MoonPalaceBoundaryWarningEvidencePublishesAtLeastTwoCategoriesPerProjection
MoonPalaceBoundaryWarningEvidencePreservesEnteringBiomeOnDirectionReversal
MoonPalaceBoundaryPublisherReadsMap08AndMap21SourcesWithoutRewriting
MoonPalaceBoundaryPublisherWritesOnlyMap21_06AuthoringAndGeneratedRoots
MoonPalaceBoundaryDigestsAreDeterministicReverseOrderRepeatAndCultureStable
MoonPalaceBoundaryRejectsDuplicateIdsMissingPairsBadCellsWrongSocketsAndInsufficientWarnings
MoonPalaceBoundaryDoesNotRunGenerationRendererValidationReplayRollbackPlayModeOrLegacyRegression
MoonPalaceBoundaryPublishesMap21_07HandoffOnlyAfterFocusedPass
```

Only run:

```text
EditMode category MAP21_06
```

Expected:

```text
Discovered: 13
Executed: 13
Passed: 13
Failed: 0
Skipped: 0
Inconclusive: 0
```

Do not run MAP08 categories, PlayMode, or any previous task category.

## 9. Result Required Evidence

The Result must include these sections.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
## Boundary Pair and Candidate Summary
## Tile Socket and Route Profile Summary
## Warning Evidence Summary
## Static Safety and Non-Execution Summary
## Snapshot and Digest Summary
## Focused Validation Summary
## No Legacy Regression Boundary Notes
## Final Status Evidence
```

Minimum required evidence:

```text
MAP08 baseline candidates / projections: 31 / 62
MAP21_06 production candidates / projections: 48 / 96
pair rows: 6 / 6
candidate rows per pair: 8 / 8 / 8 / 8 / 8 / 8
horizontal candidates per pair: 4 / 4 / 4 / 4 / 4 / 4
vertical candidates per pair: 4 / 4 / 4 / 4 / 4 / 4
tile rows: 4608
unique tile cells per candidate: 96
socket rows: 96
route profile rows: 96
directional projections: 96 / 96
tool_requirement NONE: 96 / 96
warning evidence rows: at least 192
minimum distinct warning categories per projection: 2
unknown pair IDs: 0
unknown biome IDs: 0
duplicate candidate IDs: 0
duplicate tile coordinates per candidate: 0
duplicate socket/projection keys: 0
MAP08 source modifications: 0
MAP21_03/04 cluster artifact modifications: 0
MAP21_05 Activity/Event modifications: 0
generated world count: 0
Tilemap writes: 0
runtime GameObject spawns: 0
Scene/Prefab changes: 0
```

## 10. Focused Validation Boundaries

Validation must stay focused.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
MAP08 CATEGORY RERUNS: 0
MAP09_01 BASELINE RERUNS: 0
MAP19_09 SCALE AUDIT RERUNS: 0
MAP20_01 GENERATOR RUN RERUNS: 0
MAP20_02 OVERLAY SAMPLE REGENERATION RUNS: 0
MAP20_03 DETAIL SAMPLE REGENERATION RUNS: 0
MAP20_04 NAVIGATION SAMPLE REGENERATION RUNS: 0
MAP20_05 REPLAY EXPORT SAMPLE REGENERATION RUNS: 0
MAP20_06 EXIT AUDIT REGENERATION RUNS: 0
MAP21_01 PROFILE TILE SHELL REGENERATION RUNS: 0
MAP21_02 MICROPATTERN REGENERATION RUNS: 0
MAP21_03 CRATER ROOT CLUSTER REGENERATION RUNS: 0
MAP21_04 MILL DOUGH CLUSTER REGENERATION RUNS: 0
MAP21_05 ACTIVITY EVENT REGENERATION RUNS: 0
VALIDATION RUNNER EXECUTIONS: 0
REPLAY EXECUTIONS: 0
GENERATOR EXECUTIONS: 0
RENDERER EXECUTIONS: 0
ROLLBACK EXECUTIONS: 0
CSV AUTHORING WRITES OUTSIDE MAP21_06 AUTHORING FOLDER: 0
RUNTIME OBJECT SPAWNS: 0
SCENE PREFAB CHANGES: 0
```

If a real compile or focused test failure occurs, only the MAP21_06 focused category may be rerun after a local fix. Do not broaden test selection unless the Result explicitly reports a real regression trigger and stops for review.

## 11. Finalize and Commit Rules

Finalize only if all MAP21_06 checks pass.

Status expectations before finalize:

```text
MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS: CURRENT
MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS: LOCKED
```

Status expectations after finalize:

```text
MAP21_06_EXPAND_ALL_SIX_BOUNDARY_POOLS: COMPLETE
Current Task: NONE
MAP21_07_COMPLETE_THREE_CORE_RESOURCE_REGIONS: LOCKED
```

The Result must state whether Status Finalize and atomic commit were actually performed. If the local protocol intentionally writes Result before finalize, say so explicitly.

Atomic commit scope:

```text
MAP21_06 task file/archive
MAP21_06 Result
MAP21_06 authoring CSV/meta files
MAP21_06 generated JSON files
MoonPalaceBoundaryProduction.cs and .meta if new
MoonPalaceBoundaryPoolPublisher.cs and .meta if new
MoonPalaceBoundaryProductionTests.cs and .meta if new
06_IMPLEMENTATION_STATUS.md finalize change
```

Commit subject:

```text
MAP21_06 expand all six boundary pools
```

Do not stage unrelated files. Do not push.

## 12. Stop Rule

After Result and commit:

```text
STOP
Do not create MAP21_07 files.
Do not run MAP21_07.
Do not rerun MAP08 categories.
Do not run world generation, renderer, validation runner, replay, rollback, PlayMode, legacy regression, or full regression.
```
