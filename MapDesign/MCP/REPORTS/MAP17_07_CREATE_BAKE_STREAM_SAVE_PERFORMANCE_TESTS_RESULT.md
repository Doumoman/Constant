# MAP17_07 Create Bake Stream Save Performance Tests Result

TASK: MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP17_01~MAP17_06의 pure-data public API를 실제로 반복 호출하는 EditMode performance harness와 deterministic report 계약을 추가했다. Placement, logical layer bake, seam validation, collider cache, streaming window, shifted transition, modification storage, save manifest serialize/parse, regeneration apply, hash mismatch의 10개 operation group을 warmup 1회와 측정 3회로 관찰한다.

측정값은 실제 runtime frame budget이나 최적화 완료를 뜻하지 않는다. Unity Editor와 머신 상태에 따라 elapsed milliseconds가 흔들리므로 시간값은 진단 정보로만 보존하고 digest 및 PASS gate에서는 제외했다. 대신 cell/layer/seam/window/record/command 수, cache cold/warm/invalidate/evict 분리, atomic failure, retry 0, side effect 0 같은 구조 상한을 검증했다. 가장 큰 관찰값은 `layer_bake` max `3358.202900 ms`였지만 repeat/reverse/culture/warmup report digest와 모든 구조 count가 안정적이어서 focused proof를 무효화하는 spike로 판단하지 않았다. 이 Task에서는 최적화 rewrite나 broad regression을 시작하지 않았다.

기존 production digest와 MAP17 public API는 그대로 재사용했다. 다만 기존 MAP17 test의 reference-chain fixture builder가 모두 private이어서 새 harness 안에 fixture adapter 1개를 중복 구성했다. 여러 MAP17 test에 반복된 builder를 shared test fixture로 합치는 일은 consolidation 후보로만 기록하고 이번 Task에서는 수행하지 않았다. MAP17_08에는 stable performance report schema, report digest, 10개 operation의 구조 count와 timing 관찰을 넘기며 MAP17_08은 LOCKED / NOT STARTED로 유지했다.

검증은 EditMode category `MAP17_07` 한 번만 선택했고 정확히 10개가 발견·실행되어 10/10 PASS했다. 구현 오류나 회귀 trigger가 없었으므로 MAP09~MAP16, 이전 MAP17 category, legacy 19347, PlayMode, unfiltered 및 full regression은 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTerrainPerformanceReport.cs` | Count-based budget, per-iteration sample, invariant min/median/max aggregate, deterministic failure와 timing-independent stable report digest를 정의한다. | Runtime profiler, optimization rewrite, frame-time guarantee, disk report 저장 또는 Unity object 작업을 수행하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainPerformanceHarness.cs` | MAP17_01~06 public API를 warmup/measure하고 10개 operation의 구조 metric, elapsed ticks/ms와 output digest를 report sample로 조립한다. | Full generator hidden retry, disk save/load, PlayMode, Scene/Prefab/Tilemap/GameObject 조작을 수행하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedTerrainPerformanceTests.cs` | 정확한 MAP17_07 focused test 10개로 locked counts, budget failure, deterministic report, timing 관찰과 side-effect 부재를 검증한다. | Strict millisecond gate, prior category, legacy 또는 broad regression을 선택하지 않는다. |

## Performance Observation Summary

- performance warmup iterations: 1
- performance measured iterations: 3
- performance operation groups reported: 10
- performance report digest lower-hex SHA-256: YES
- performance report digest: `c153ac3f76cb5aa64abeaad2c0091279a027de02c4a3817c9335e74b79cbce2f`
- repeat/reverse/culture/warmup digest mismatches: 0/0/0/0
- elapsed values included in report digest: NO
- strict millisecond PASS threshold: NONE
- observed diagnostic spike: `layer_bake` max `3358.202900 ms`
- spike invalidates focused structural proof: NO

| Operation | Operation count | Min/median/max ms |
|---|---:|---:|
| `placement` | 10752 | 2224.881700 / 2684.617700 / 2729.698000 |
| `layer_bake` | 10752 | 2613.687400 / 3293.941300 / 3358.202900 |
| `seam_validation` | 1376 | 396.036900 / 492.785300 / 737.758900 |
| `collider_cache` | 4 | 900.061000 / 903.703500 / 956.689700 |
| `stream_window` | 93 | 123.360800 / 125.407000 / 225.321000 |
| `transition` | 63 | 85.006000 / 85.842700 / 93.728600 |
| `modification_storage` | 5 | 9.692700 / 13.375500 / 13.971000 |
| `save_manifest` | 20518 | 6.480200 / 9.980000 / 18.411800 |
| `regen_apply` | 5 | 1.696300 / 8.108600 / 42.701200 |
| `hash_mismatch` | 6 | 8.452100 / 8.599600 / 11.153100 |

## Duplication and Hardcoding Observation

- existing helpers reused: 19 — `ReferenceFinalRouteRecoveryFixture`, `BakingCanonicalDigest`, geometry/registry/export primitives와 MAP17 placement, bake, seam, collider, handle, window, modification, manifest, regeneration public API
- new duplicate helper count: 1
- duplicated helper: reference-chain fixture adapter; 기존 단계별 helper가 private이고 broad test refactor가 금지되어 harness 한 곳에만 한정
- hardcoded count constants added: 22
- hardcoded count constants justified: 22/22 — locked 13x13/48x32 counts, MAP17_02 seam counts, MAP17_04 window counts, MAP17_05/06 record counts, iteration 수와 diagnostic structural upper bounds를 이름 있는 budget constant로 게시
- consolidation candidates observed: 1 — MAP17 EditMode tests의 반복 reference-chain fixture builder
- consolidation work performed: 0
- optimization rewrites performed: 0
- broad refactors performed: 0

## Preconditions and Installation

- MAP17_06 Result exists: YES
- MAP17_06 Result independent PASS line count: 1
- MAP17_06 Result SHA-256 required/actual: `de743b24661e061544e4d3e032d8fdaca399eb413429da542469d2ede7932968` / `de743b24661e061544e4d3e032d8fdaca399eb413429da542469d2ede7932968`
- MAP17_06 installed Task SHA-256 required/actual: `52e97516e909f8c5580d6832b67eb1fdc206d85376b9b4f2cef12b65aae1619b` / `52e97516e909f8c5580d6832b67eb1fdc206d85376b9b4f2cef12b65aae1619b`
- MAP17_07 inbox/install/archive SHA-256: `935957fbc6e563fdda87a1121329c8d81a91951e27ee2f109069eb97d42ae658`
- inbox candidates validated: 1
- legacy inbox candidates: 0
- installed/archive byte equality: YES
- MAP17_06 manifest digest reused: `18bb9bd0ada73c2c84b9b400675d792a0e9c206f4ee5bd5eec897468154cd27a`
- MAP17_06 canonical payload digest reused: `af88b4751877d4a03b0854eefea089bab70c542717d676d8eb52655b67ebac04`
- MAP17_06 regeneration apply digest reused: `13a1d61f92382f05460e7bc5c39f75b39c8e24850918bd7b94e8ace330504568`

## Structural Count Evidence

- placement cells measured: 1536
- placement layer refs measured: 10752
- placement sector/world coordinate counts: 1536/1536
- layer bake logical layers measured: 7
- layer bake gap/overlap/stale asset counts: 0/0/0
- seam 4x4/12x8/4x4-only counts: 688/240/448
- collider cache cold misses/warm hits/invalidates/evicts: 1/1/1/1
- collider rebuild command count: 55
- stream center preload/active: 49/25
- stream edge preload/active: 28/15
- stream corner preload/active: 16/9
- stream active subset preload: YES
- transition shifted-window diff count: 63
- transition batch count: 24
- transition duplicate handle changes: 0
- modification modified sectors/records/dirty revision: 1/5/5
- modification compact idempotent: YES
- modification apply command count: 5
- save manifest payload bytes: 20518
- save manifest modified sector entries: 1
- save manifest unmodified sector entries: 0
- save manifest unmodified sectors omitted: 168
- save manifest serialized records: 5
- regen apply modified sector plans: 1
- regen apply command count: 5
- DestroyTile/ReplaceTile/CollectPickup/ChangeDeviceState/ConsumeSlot: 1/1/1/1/1
- hash mismatch failure probes: 6
- hash mismatch retry loops: 0
- atomic failure partial mutations: 0
- count budget rejection probes: 1/1
- retry-loop structural budget rejection probes: 1/1

## Structural Upper Bounds and Side Effects

- full 169-sector tile serialization as save data: 0
- unmodified manifest sector entries: 0
- Unity object ids serialized: 0
- file paths/timestamps/frame counts serialized: 0/0/0
- population/content spawn ids serialized: 0
- hidden full generator executions for performance fixture: 0
- automatic broad regression selections: 0
- retry loops after deterministic hash mismatch: 0
- System.IO file write/read calls: 0/0
- disk save/load files created: 0/0
- actual user save slot writes: 0
- platform save storage writes: 0
- Unity Tilemap component writes: 0
- Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
- TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
- Rigidbody2D creations: 0
- Physics2D queries/simulations: 0/0
- Scene/Prefab/Tilemap mutation: 0/0/0
- GameObject instantiate/enable/disable/destroy: 0/0/0/0
- Camera reads/writes: 0/0
- Addressables/Resources/AssetDatabase loads: 0/0/0
- Authoring CSV edits: 0
- Generated CSV/assets committed: 0/0
- runtime objects spawned: 0
- production seed approvals: 0
- MAP17_08 started: NO

## Focused Validation

```text
mode: EditMode
category_names: [MAP17_07]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 101.51
```

Exact focused test names:

1. `BakePlacementPerformanceReportsStableCellLayerAndCoordinateCounts`
2. `LayerBakeAndSeamPerformanceReportsExpectedCountsWithoutTilemapWrites`
3. `ColliderCachePerformanceSeparatesColdWarmInvalidateAndEvictPaths`
4. `StreamingWindowPerformanceReportsCenterEdgeCornerAndActiveSubsetBudgets`
5. `TransitionPerformancePublishesShiftedWindowDiffWithoutDuplicateHandleChanges`
6. `ModificationStoragePerformanceReportsDirtyRevisionCompactAndApplyCounts`
7. `SaveManifestReloadPerformanceSerializesModifiedOnlyAndAppliesFiveRecords`
8. `HashMismatchPerformanceFailsAtomicallyWithoutRetryStorm`
9. `PerformanceReportsAreDeterministicAcrossRepeatReverseCultureAndWarmup`
10. `Map17HandoffKeepsMap17_08Locked`

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## Unity Verification

- Unity Version: 6000.3.8f1
- final Editor state: ready, compiling false, domain reload false, PlayMode stopped
- successful explicit recompiles: 2
- Compile Errors: 0
- Relevant Console Errors: 0
- Relevant Warnings: 0
- EditMode Tests: MAP17_07 10/10 PASS
- Test result XML: `C:/Users/user/AppData/LocalLow/DefaultCompany/별을 물어오는 밤/TestResults.xml`
- Test result XML timestamp: `2026-09-04T22:36:07.4149556+09:00`
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: NONE
- Tilemap Changes: NONE

## Completion Gate

- bake/stream/save performance report fixture created: PASS
- structural count budgets and digests deterministic: PASS
- diagnostic timing recorded without flaky ms threshold: PASS
- save/reload remains in-memory manifest parse plus regeneration apply only: PASS
- hash mismatch atomic with retry storm absent: PASS
- disk save/load and Unity object side effects absent: PASS
- optimization rewrite and broad refactor absent: PASS
- focused-only policy preserved: PASS
- MAP17_08 remains LOCKED / NOT STARTED: PASS

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. Master task list는 수정하지 않았다. 반복 reference-chain test builder consolidation은 후보로만 보고했으며 수정하지 않았다.
