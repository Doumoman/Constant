# MAP18_05 Instantiate Activity and Event Runtime States Result

TASK: MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES
STATUS: PASS

## User-Facing Implementation Report

이번 작업은 기존 `ActivityStructureContract`와 `EventOverlayContract`를 실제 오브젝트로 생성하지 않고, 저장과 재진입에서 참조할 수 있는 순수 데이터 runtime state record로 변환한다. Activity source 2개는 각각 `Cue`에서 시작하는 하나의 cycle record를 게시하고, Event source 2개는 각각 `Empty`와 `Active` variant를 게시해 총 6개의 state record를 만들었다.

Activity 전이는 `Cue -> Active -> Resolved -> Resettable -> Cue` 네 간선만 허용한다. 전이 자체를 실행하지 않고 허용표만 게시하며, 대표 금지 간선 네 개와 변조된 전이표는 focused test에서 거부했다. Event의 `Empty`는 runtime object를 게시하지 않으며, `Active`도 stable state identity만 게시한다. activation, resolution, reentry 정책은 각각 typed enum으로 고정되어 입력 순서나 culture에 영향을 받지 않는다.

모든 record는 world seed, generator/data version, sector, authoring source digest, state kind/variant를 lower-hex SHA-256으로 묶은 runtime state ID와 `MAP18_RUNTIME_STATE/V1` save key를 가진다. 이는 실제 저장 동작이 아니라 MAP18_06 이후가 참조할 identity 계약이다. PlayerPrefs, 파일, 플랫폼 저장소를 읽거나 쓰지 않았다.

MAP18_04 plan, occupied surface, budget ledger는 같은 객체 참조와 같은 digest로 그대로 통과한다. 9개의 occupied reservation과 3개의 remaining candidate는 변경되지 않았고 Activity/Event record는 새 reservation을 주장하지 않는다. MAP18_06 export surface는 6개 state identity와 save key를 게시하지만 MAP18_06을 unlock하거나 실행하지 않는다.

정상 경로와 missing source, digest mismatch, 잘못된 transition/variant, ID/key 중복, occupied conflict, runtime side-effect 요청을 모두 원자적으로 검증했다. 실패 결과에는 owner, reason, offending key, expected/actual이 있고 partial state, occupied mutation, budget mutation은 모두 0이다. 실행한 테스트는 EditMode category `MAP18_05` 하나뿐이며 10/10 PASS했다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedActivityEventRuntimeState.cs` | Activity phase/transition, Event variant/policy, stable runtime ID/save key, Activity/Event records, MAP18_04 passthrough와 MAP18_06 export surface를 정의한다. | GameObject/Prefab 생성, cue/event 실행, save I/O, reward/damage/AI/physics, Tilemap/Scene 변경을 수행하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedActivityEventRuntimeStateInstantiator.cs` | authoring source와 MAP18_04 digest/count를 검증하고 deterministic state records를 생성하며 충돌과 금지 side-effect 요청을 atomic failure로 보고한다. | source를 암묵적으로 만들거나 retry/보정하지 않고 partial surface를 게시하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedActivityEventRuntimeStateTests.cs` | 정확히 10개의 `MAP18_05` focused test로 state, transition, variant, identity, passthrough, export, failure, determinism, side-effect 0, MAP18_06 lock을 검증한다. | 이전 category, PlayMode, legacy 19347, unfiltered/full regression을 선택하지 않는다. |
| `MapDesign/MCP/REPORTS/MAP18_05_INSTANTIATE_ACTIVITY_AND_EVENT_RUNTIME_STATES_RESULT.md` | 실제 state 수, digest, 실패 probe, Unity focused 결과와 경계를 기록한다. | MAP18_06 작업을 시작하거나 상태를 변경하지 않는다. |
| `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | 이 PASS Result 검증 뒤 MAP18_05만 COMPLETE로 finalize하고 Current Task를 NONE으로 닫는다. | MAP18_06 및 다른 LOCKED row는 변경하지 않는다. |

## Activity Event Runtime State Summary

- MAP18_04 hazard/enemy plan digest reused: `003b2ddc329d736945eda48b8b03df128bd0891c40910aa89e97c965ed3222ac`
- MAP18_04 occupied surface digest reused: `39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688`
- MAP18_04 budget ledger digest reused: `08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d`
- MAP18_04 occupied surface entries reused: `9`
- MAP18_04 remaining candidate count reused: `3`

- Activity runtime state records: `2`
- Event runtime state records: `4`
- Empty Event variants: `2`
- Active Event variants: `2`
- total runtime state records: `6`
- unique runtime state IDs: `6`
- unique save keys: `6`
- duplicate runtime state IDs: `0`
- duplicate save keys: `0`
- save key namespace/version: `MAP18_RUNTIME_STATE/V1`

- Activity allowed transitions: `4/4` (`Cue -> Active`, `Active -> Resolved`, `Resolved -> Resettable`, `Resettable -> Cue`)
- Activity rejected transitions: `4/4` (`Cue -> Resolved`, `Active -> Cue`, `Resolved -> Active`, `Resettable -> Active`)
- invalid transition failure probes: `1/1`
- Event variant checks: `4/4` (`2 Empty`, `2 Active`)
- Event reentry policy checks: `4/4` (`RestoreSavedVariant`)
- activation policy deterministic: `YES` (`StableSourceMarker`)
- resolution policy deterministic: `YES` (`PersistVariantIdentity`)

- MAP18_04 occupied entries consumed: `9/9`
- MAP18_04 occupied digest exact passthrough: `YES`
- MAP18_04 budget ledger digest exact passthrough: `YES`
- MAP18_04 occupied surface reference reused: `YES`
- MAP18_04 budget ledger reference reused: `YES`
- occupied conflict count: `0`
- budget mutation count: `0`
- MAP18_06 export surface records: `6`

- runtime state surface digest lower-hex SHA-256: `YES`
- runtime state surface digest: `2774cc515c4531ad90055afb2bdabb4a73439a0e64162194b4c9dbdd51db0f72`
- save key set digest lower-hex SHA-256: `YES`
- save key set digest: `9c841116463551aff94fe77132c2b7b61d23b07840ee5aa29710799591b0d448`
- export surface digest lower-hex SHA-256: `YES`
- export surface digest: `2f2665b46b054f408f8e7a1fb97c128ca355b829aa74d4aa7811b2792b9f6d73`
- repeat/reverse/culture/candidate-order digest mismatches: `0/0/0/0`
- mutation sensitivity probes passed: `3/3` (`runtime surface`, `save-key set`, `export surface`)

- missing source failure probes: `2/2` (`Activity`, `Event`)
- digest mismatch failure probes: `2/2` (`occupied surface`, `budget ledger`)
- invalid Event variant failure probes: `1/1`
- duplicate runtime state ID failure probes: `1/1`
- duplicate save key failure probes: `1/1`
- occupied/budget mutation failure probes: `3/3` (`occupied digest`, `occupied claim conflict`, `budget digest`)
- attempted runtime spawn/event/save/reward/damage/physics/AI failure probes: `7/7` in one atomic request
- failure owner/reason/offending key/expected/actual: `PUBLISHED`
- atomic failure partial state records: `0`
- atomic failure occupied mutations: `0`
- atomic failure budget mutations: `0`
- atomic failure retry loops: `0`

## Runtime Object and Save Boundary Notes

- runtime Activity prefabs spawned: `0`
- runtime Event prefabs spawned: `0`
- actual cue VFX/SFX playback: `0/0`
- actual event activations executed: `0`
- actual state transitions executed: `0`
- actual save writes/reads: `0/0`
- PlayerPrefs writes/reads: `0/0`
- runtime objects spawned: `0`
- GameObject instantiate/enable/disable/destroy: `0/0/0/0`
- System.IO file write/read calls: `0/0`
- disk save/load files created: `0/0`
- actual user save slot writes: `0`
- platform save storage writes: `0`
- actual reward grants: `0`
- actual damage executions: `0`
- enemy AI/controller hookups: `0`
- Health/Damage/Hitbox/Hurtbox component creations: `0/0/0/0`
- Unity Tilemap component writes: `0`
- Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: `0/0/0/0`
- TilemapCollider2D/CompositeCollider2D/Collider2D creations: `0/0/0`
- Rigidbody2D creations: `0`
- Physics2D queries/simulations: `0/0`
- NavMesh/pathfinding setup: `0/0`
- Scene/Prefab/Tilemap mutation: `0/0/0`
- Camera reads/writes: `0/0`
- Addressables/Resources/AssetDatabase loads: `0/0/0`
- Authoring CSV edits: `0`
- Generated CSV/assets committed: `0/0`
- production seed approvals: `0`
- UnityEngine.Random/Random.Range/System.Random direct calls: `0/0/0`
- hidden retry loops: `0`
- implicit Activity/Event source creation: `0`
- candidate mutation: `0`
- MAP18_06 started: `NO`

## Preconditions and Installation

- MAP18_04 Result exists: `YES`
- MAP18_04 Result independent PASS line count: `1`
- MAP18_04 Result SHA-256 required/actual: `2d601a8aa25670187c642b15e079c9662af31749d5d00f994120fc75f5085e98` / `2d601a8aa25670187c642b15e079c9662af31749d5d00f994120fc75f5085e98`
- MAP18_04 installed Task SHA-256 required/actual: `621ffbfe1c6f38a5f7548278e0e92d3f5166c3d360937e3d1a2c606d4a65b1e1` / `621ffbfe1c6f38a5f7548278e0e92d3f5166c3d360937e3d1a2c606d4a65b1e1`
- MAP18_05 inbox/install/archive SHA-256: `2f53f0d8ec57c3f57bf604990c314d9a931a4709cf4b103c46edb8bae4581f54`
- inbox candidates validated: `1`
- legacy inbox candidates: `0`
- installed/archive byte equality: `YES`
- Current Task before apply: `NONE`
- MAP18_04 before apply: `COMPLETE`
- MAP18_05 before apply/task execution: `LOCKED / CURRENT`
- MAP18_06 before/after task execution: `LOCKED / LOCKED`
- Status row count and apply delta: `216`, `COMPLETE 0 / CURRENT +1 / LOCKED -1`
- unrelated staged files before apply: `0`
- Master task membership count: `1`
- Master task list edited: `NO`
- protocol-required Archive path is the only Phase A path outside the Task body write list: `YES`

## Focused Validation

Final authoritative run:

```text
mode: EditMode
category_names: [MAP18_05]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 2.0933474
job id: af43ad24327d4c939d062ca87db93719
```

Exact focused test names:

1. `ActivityEventRuntimeStateCreatesActivityAndEventRecords`
2. `ActivityRuntimeTransitionsAllowOnlyCueActiveResolvedResettableCycle`
3. `EventOverlayRuntimePublishesEmptyAndActiveVariantsWithExplicitReentry`
4. `RuntimeStateIdsAndSaveKeysAreUniqueStableAndMutationSensitive`
5. `RuntimeStateInstantiatorPreservesMap18_04OccupiedAndBudgetSurfaces`
6. `ActivityEventRuntimeSurfacePublishesExportInputForMap18_06`
7. `RuntimeStateFailuresAreAtomicAndReportOwnerReasonExpectedActual`
8. `RuntimeStateDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder`
9. `RuntimeStateInstantiatorDoesNotSpawnObjectsWriteSavesMutateScenesOrRunRegressions`
10. `Map18HandoffKeepsMap18_06Locked`

- focused MAP18_05 selections: `1` (`10/10`)

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## Unity Verification

- Unity Version: `6000.3.8f1`
- final Editor state: compiling `false`, domain reload pending `false`, PlayMode `stopped`
- final production and test assembly compile errors: `0`
- final relevant Console errors: `0`
- final relevant Console warnings: `0`
- non-task TestRunner persistence diagnostic: `1` (`Saving results to .../TestResults.xml`, no failed test or compile error)
- EditMode Tests: `MAP18_05 10/10 PASS`
- authoritative test result source: `Unity MCP test job af43ad24327d4c939d062ca87db93719`
- PlayMode Tests: `NOT RUN`
- Scene/Prefab/Tilemap Changes: `NONE`

## Static Gates

- required `[Test]` methods present: `10/10`
- `MAP18_05` category declarations: `1`
- new matching Unity meta files: `3/3`
- new meta GUID uniqueness: `3/3`
- production runtime API calls (`UnityEngine`, `System.IO`, PlayerPrefs, GameObject, Tilemap, Physics2D, NavMesh): `0`
- production random calls: `0`
- task-owned source roots only: `YES`
- CSV, Scene, Prefab, Tilemap, asmdef, ProjectSettings and package changes: `0`
- task-owned implementation/test/Result `git diff --check`: `PASS`
- byte-identical installed/archive Task body trailing-space findings: `1` each (same supplied Markdown hard-break at line 32; Task bytes preserved and not auto-corrected)

## Completion Gate

- MAP18_05 focused tests pass: `PASS`
- Activity and Event runtime state records created: `PASS`
- Activity allowed/rejected transitions represented and tested: `PASS`
- Event Empty/Active variants and explicit reentry represented and tested: `PASS`
- stable runtime IDs and save keys unique: `PASS`
- MAP18_04 occupied/budget surfaces preserved exactly: `PASS`
- MAP18_06 export surface created: `PASS`
- atomic failures leave no partial state or mutation: `PASS`
- actual spawn, cue/event execution, save I/O, reward, damage, AI, physics, Tilemap, Collider, GameObject and NavMesh work absent: `PASS`
- optimization rewrite, broad refactor and regression runs absent: `PASS`
- Scene/Prefab/Tilemap mutation absent: `PASS`
- MAP18_06 remains LOCKED / NOT STARTED: `PASS`

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. 이전 MAP18 test fixture의 source builder 중복은 허용되지 않은 shared fixture consolidation을 피하기 위해 이번 focused test 안에만 한정했고, 별도 cleanup이나 다음 Task를 시작하지 않았다.
