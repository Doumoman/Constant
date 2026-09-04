# MAP18_04 Place Hazards Enemies and Hierarchical Budgets Result

TASK: MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP18_03이 게시한 7개 occupied reservation과 5개 remaining candidate를 입력으로 받아 Hazard와 Enemy를 각각 하나씩 배치하는 pure-data logical placement planner를 추가했다. reference fixture의 결과는 `Hazard -> MAP16_SLOT_02`, `Enemy -> MAP16_SLOT_10`이며, 기존 content slot의 stable spawn ID와 reservation key를 그대로 사용한다.

이 placement는 실제 적이나 위험물의 runtime spawn 또는 combat 실행이 아니다. pool entry는 content key, namespace/version, biome allowlist, route clearance, safe/neighbor radius, pressure cost, maximum count만 선언하며 GameObject, Prefab, AI, Animator, Health/Damage, Collider, Rigidbody, Tilemap 또는 Physics2D를 생성하거나 변경하지 않는다.

MAP18_02의 RequiredProgressTrigger, MoonCore, CassiaSap, StarNuruk과 MAP18_03의 ShopInventory, OptionalResource, NeutralMapElement가 차지한 7개 reservation을 24개 pool-candidate proof에서 모두 occupied surface로 소비했다. 선택된 Hazard/Enemy의 upstream occupied slot 재사용은 0이고, MAP18_04의 2개 reservation을 더한 9개 occupied surface와 3개 remaining candidate를 MAP18_05에 넘긴다.

보호 projection은 mandatory route spine, traversal envelope, required landing, drop recovery floor, reward approach floor, special/village entry buffer, safe pocket, critical socket boundary와 safe/neighbor radius를 candidate마다 명시한다. 교차 후보는 deterministic proof에서 거절되고 선택된 두 entry의 route/reward/recovery violation은 모두 0이다.

hierarchical pressure budget은 `World -> Patch -> Sector -> Cluster -> Slot` 순서의 5개 scope를 모든 logical entry에 동시에 차감한다. Hazard cost 2와 Enemy cost 1을 각 scope에서 합계 3만큼 차감하며, 음수 잔액·중복 spend key는 atomic failure로 처리하고 partial entry 또는 partial budget spend를 게시하지 않는다.

선택은 content kind와 candidate의 stable order, lower-hex SHA-256 ticket만 사용한다. UnityEngine.Random, Random.Range, System.Random, hidden retry, implicit candidate 생성 또는 candidate mutation은 없다. production 중복 코드나 이름 없는 하드코딩 후보는 발견하지 않았다. starter budget 수치는 Task가 허용한 typed catalog에만 명시했다. 이전 Task의 private test fixture를 재사용할 수 없어 12-record source fixture를 focused test에 한 번 복제한 cleanup 후보 1개를 기록하며, 금지된 shared fixture consolidation은 수행하지 않았다.

회귀 트리거는 없었다. 실행한 세 번의 test selection은 모두 EditMode category `MAP18_04`만 사용했다. 첫 focused run의 1개 실패는 유효하지 않은 `Special` category pool로 occupied failure를 유도한 test fixture 문제였고 production planner failure가 아니었다. probe를 실제 occupied rejection evidence로 수정한 뒤 `10/10`, biome rejection과 digest mismatch proof를 보강한 최종 run도 `10/10` PASS했다. 이전 Task, legacy 19347, PlayMode, unfiltered 또는 full regression은 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedHazardEnemyPlacement.cs` | Hazard/Enemy typed pool, candidate protection projection/proof, 5-scope budget limit·spend·balance·ledger, logical placement entry, MAP18_05 occupied surface와 canonical digest를 정의한다. | 실제 spawn, combat, AI, damage, physics, Tilemap, GameObject, save 또는 content reward를 실행하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedHazardEnemyBudgetPlanner.cs` | MAP18_03 digest/count 선행조건, occupied exclusion, route/reward/recovery/radius filter, deterministic selection, hierarchical budget 차감, collision 검증과 atomic failure를 담당한다. | 실패 후보를 retry·보정하지 않고, partial plan/spend를 게시하거나 새 runtime object/candidate를 만들지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedHazardEnemyBudgetPlannerTests.cs` | 정확히 10개의 `MAP18_04` focused test로 두 group, 7개 exclusion, 보호 surface, 5-scope budget, 결정성, digest, atomic failure, 부작용 0과 MAP18_05 lock을 검증한다. | 이전 category, PlayMode, legacy 19347, unfiltered 또는 full regression을 선택하지 않는다. |
| `MapDesign/MCP/REPORTS/MAP18_04_PLACE_HAZARDS_ENEMIES_AND_HIERARCHICAL_BUDGETS_RESULT.md` | 실제 선택·budget·protection·digest·failure·Unity 검증과 runtime boundary를 기록한다. | MAP18_05를 열거나 실행하지 않는다. |
| `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | 이 Result의 PASS 검증 후 MAP18_04만 COMPLETE로 finalize하고 Current Task를 NONE으로 닫는다. | MAP18_05 또는 다른 LOCKED row를 변경하지 않는다. |

## Hazard Enemy Budget Summary

- MAP18_03 population plan digest reused: `4fc87b1c2699802761b9956aaf58fdc9ebbfaf6f32f33bdc9b7a776752cd109e`
- MAP18_03 occupied surface digest reused: `f5556c9e609de1b71195c45473582009f99b5799cb03052da75682ed9c43e422`
- MAP18_03 occupied surface entries reused: `7`
- MAP18_03 remaining candidate count reused: `5`
- MAP18_02+MAP18_03 occupied slots excluded: `7/7`
- logical hazard/enemy groups published: `2/2`
- Hazard entries: `1`
- Enemy entries: `1`
- total logical hazard/enemy entries: `2`
- unique content keys: `2`
- unique stable spawn IDs: `2`
- unique reservation keys: `2`
- occupied slot reuse count: `0`
- MAP18_04 reservation collisions: `0`
- MAP18_04 stable spawn ID collisions: `0`
- MAP18_05 occupied surface entries: `9` (`MAP18_02+03=7`, `MAP18_04=2`)
- remaining unoccupied candidate count: `3`
- pool entries published: `2`
- pool namespace/version checks: `2/2`
- hazard pool accepted/rejected: `1/11`
- enemy pool accepted/rejected: `1/11`
- biome allowlist accepted/rejected: `12/12`
- route protection accepted/rejected: `12/12`
- reward protection accepted/rejected: `20/4`
- recovery protection accepted/rejected: `18/6`
- safe radius accepted/rejected: `22/2`
- neighbor radius accepted/rejected: `22/2`
- occupied surface accepted/rejected: `9/15`
- critical route violation count: `0`
- critical reward violation count: `0`
- critical recovery violation count: `0`

Selected logical placement entries:

| Group | Source slot | Biome | Pool | Pressure cost |
|---|---|---|---|---|
| `Hazard` | `MAP16_SLOT_02` | `AbandonedMill` | `POPULATION_HAZARD@V1` | `2` |
| `Enemy` | `MAP16_SLOT_10` | `AbandonedMill` | `POPULATION_ENEMY@V1` | `1` |

- budget scopes published: `5/5`
- World budget initial/spent/remaining: `12/3/9`
- Patch budget initial/spent/remaining: `10/3/7`
- Sector budget initial/spent/remaining: `8/3/5`
- Cluster budget initial/spent/remaining: `6/3/3`
- Slot budget initial/spent/remaining: `4/3/1`
- budget spend entries: `2` (one per logical entry)
- hierarchical scope spend records: `10` (five per logical entry)
- duplicate budget spend keys: `0`
- budget overflow failure probes: `1/1`
- duplicate budget spend failure probes: `1/1`
- budget rollback after failure: `2/2`
- negative budget values: `0`

- selection uses stable order: `YES`
- deterministic hash/ticket selections: `2`
- input order dependency detected: `NO`
- UnityEngine.Random/Random.Range calls: `0/0`
- System.Random direct usage: `0`
- hidden retry loop count: `0`
- implicit candidate creation count: `0`
- candidate mutation count: `0`

- hazard/enemy plan digest lower-hex SHA-256: `YES`
- hazard/enemy plan digest: `003b2ddc329d736945eda48b8b03df128bd0891c40910aa89e97c965ed3222ac`
- occupied surface digest lower-hex SHA-256: `YES`
- occupied surface digest: `39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688`
- budget ledger digest lower-hex SHA-256: `YES`
- budget ledger digest: `08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d`
- repeat/reverse/culture/candidate-order digest mismatches: `0/0/0/0`
- mutation sensitivity probes passed: `3/3` (`plan`, `occupied`, `budget`)

## Route Protection and Runtime Boundary Notes

- mandatory route spine intersection: explicit reject proof
- traversal envelope intersection: explicit reject proof
- required landing intersection: explicit reject proof
- drop recovery floor intersection: explicit reject proof
- reward approach floor intersection: explicit reject proof
- special/village entry buffer intersection: explicit reject proof
- safe pocket intersection: explicit reject proof
- critical socket boundary intersection: explicit reject proof
- selected route/reward/recovery violations: `0/0/0`
- runtime hazard placements performed: `0`
- runtime enemy placements performed: `0`
- actual damage executions: `0`
- actual combat encounters started: `0`
- enemy AI/controller hookups: `0`
- Health/Damage/Hitbox/Hurtbox component creations: `0/0/0/0`
- runtime objects spawned: `0`
- GameObject instantiate/enable/disable/destroy: `0/0/0/0`
- System.IO file write/read calls: `0/0`
- disk save/load files created: `0/0`
- actual user save slot writes: `0`
- platform save storage writes: `0`
- Unity Tilemap component writes: `0`
- Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: `0/0/0/0`
- TilemapCollider2D/CompositeCollider2D/Collider2D creations: `0/0/0`
- Rigidbody2D creations: `0`
- Physics2D queries/simulations: `0/0`
- NavMesh/pathfinding setup: `0/0`
- Scene/Prefab/Tilemap mutation: `0/0/0`
- Camera reads/writes: `0/0`
- Addressables/Resources/AssetDatabase loads: `0/0/0`
- shop transactions/reward grants/inventory mutations/resource pickup grants: `0/0/0/0`
- Authoring CSV edits: `0`
- Generated CSV/assets committed: `0/0`
- production seed approvals: `0`
- optimization rewrites/broad refactors: `0/0`
- MAP18_05 started: `NO`

## Validation and Atomic Failure Evidence

- missing population plan failure probes: `1/1`
- missing candidate failure probes: `2/2 logical groups` in the protected-candidate probe
- digest mismatch failure probes: `5/5` (`MAP18_03 plan`, `MAP18_03 occupied`, `MAP18_04 plan`, `MAP18_05 occupied`, `budget ledger`)
- occupied reuse failure probes: `1/1`
- route/reward/recovery protection failure probes: `1/1/1`
- budget overflow failure probes: `1/1`
- duplicate budget spend failure probes: `1/1`
- invalid/missing pool failure probes: `1/1` / `1/1`
- reservation collision failure probes: `1/1`
- stable spawn ID collision failure probes: `1/1`
- attempted runtime spawn/damage/physics/AI/combat failure probes: `1/1`
- failure owner/reason/offending key/expected/actual: `PUBLISHED`
- atomic failure partial entries: `0`
- atomic failure partial budget spends: `0`
- atomic failure partial mutations: `0`
- atomic failure retry loops: `0`

## Preconditions and Installation

- MAP18_03 Result exists: `YES`
- MAP18_03 Result independent PASS line count: `1`
- MAP18_03 Result SHA-256 required/actual: `35cd66c535d908683df3fe90ccfcfc55a362e19891ecf078c165f7d5c29a9a92` / `35cd66c535d908683df3fe90ccfcfc55a362e19891ecf078c165f7d5c29a9a92`
- MAP18_03 installed Task SHA-256 required/actual: `f24861e47cdeed27ec98650a3f8ea871ec53242f4ef0af33626a8756aa53c512` / `f24861e47cdeed27ec98650a3f8ea871ec53242f4ef0af33626a8756aa53c512`
- MAP18_04 inbox/install/archive SHA-256: `621ffbfe1c6f38a5f7548278e0e92d3f5166c3d360937e3d1a2c606d4a65b1e1`
- inbox candidates validated: `1`
- legacy inbox candidates: `0`
- installed/archive byte equality: `YES`
- Current Task before apply: `NONE`
- MAP18_03 before apply: `COMPLETE`
- MAP18_04 before apply/task execution: `LOCKED / CURRENT`
- MAP18_05 before/after task execution: `LOCKED / LOCKED`
- Status row count and apply delta: `216`, `COMPLETE 0 / CURRENT +1 / LOCKED -1`
- unrelated staged files before apply: `0`
- Master task membership count: `1`
- Master task list edited: `NO`
- protocol-required Archive path is the only Phase A path outside the Task body write list: `YES`

## Focused Validation

Final authoritative run:

```text
mode: EditMode
category_names: [MAP18_04]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 2.286498
job id: 2aff2bb8c1684280b5a54e191bf23a53
```

Exact focused test names:

1. `HazardEnemyPlanCreatesLogicalHazardAndEnemyGroups`
2. `HazardEnemyPlannerConsumesPopulationOccupiedSurfaceAndNeverReusesSlots`
3. `MandatoryRouteRewardRecoveryAndSafeFloorAreProtected`
4. `HierarchicalBudgetsSpendTopDownAndRejectNegativeOrDuplicateSpends`
5. `HazardEnemySelectionUsesStableOrderAndDeterministicTicketsWithoutRandom`
6. `HazardEnemyPlanPublishesOccupiedAndBudgetSurfaceForMap18_05`
7. `HazardEnemyFailuresAreAtomicAndReportOwnerReasonExpectedActual`
8. `HazardEnemyDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder`
9. `HazardEnemyPlannerDoesNotSpawnObjectsMutatePhysicsScenesOrRunRegressions`
10. `Map18HandoffKeepsMap18_05Locked`

- focused MAP18_04 selections: `3` (`9/10`, `10/10`, final `10/10`)
- first-run failure owner: `test-only invalid occupied-failure fixture`
- production failure detected by first run: `NO`

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
- non-task package/test framework warnings observed: `3` (automated-mode advisory and performance-test pre/post setup)
- EditMode Tests: `MAP18_04 10/10 PASS`
- authoritative test result source: `Unity MCP test job 2aff2bb8c1684280b5a54e191bf23a53`
- PlayMode Tests: `NOT RUN`
- Scene/Prefab/Tilemap Changes: `NONE`

## Static Gates

- required `[Test]` methods present: `10/10`
- `MAP18_04` category declarations: `1`
- new matching Unity meta files: `3/3`
- new meta GUID uniqueness: `3/3`
- production `UnityEngine.Random` / `Random.Range` / `System.Random` direct calls: `0/0/0`
- production UnityEngine/System.IO/runtime object API calls: `0`
- task-owned source roots only: `YES`
- CSV, Scene, Prefab, Tilemap, asmdef, ProjectSettings and package changes: `0`
- task-owned implementation/test/Result `git diff --check`: `PASS`
- byte-identical installed/archive Task body trailing-space findings: `2` (same supplied Markdown hard-break at line 41; Task bytes preserved, not auto-corrected)

## Completion Gate

- MAP18_04 focused tests pass: `PASS`
- Hazard and Enemy logical groups created: `PASS`
- MAP18_02+MAP18_03 occupied reservations excluded 7/7: `PASS`
- mandatory route/reward/recovery/safe surfaces protected: `PASS`
- five hierarchical budget scopes represented and tested: `PASS`
- deterministic stable-order selection without random/retry/invention: `PASS`
- stable IDs/reservations/spend keys unique: `PASS`
- atomic failure leaves no partial placement or budget spend: `PASS`
- actual spawn, damage, combat, AI, physics, Tilemap, Collider, GameObject and NavMesh work absent: `PASS`
- transaction, reward, inventory, resource pickup and disk save work absent: `PASS`
- optimization rewrite, broad refactor and regression runs absent: `PASS`
- Scene/Prefab/Tilemap mutation absent: `PASS`
- MAP18_05 remains LOCKED / NOT STARTED: `PASS`

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. MAP18_01~03의 private test fixture와 중복되는 source builder는 후속 승인된 cleanup 후보 1개로만 기록했고 공용화하지 않았다. MAP18_05는 시작하지 않았다.
