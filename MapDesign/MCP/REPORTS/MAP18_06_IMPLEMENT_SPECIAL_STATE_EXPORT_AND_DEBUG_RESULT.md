# MAP18_06 Implement Special State Export and Debug Result

TASK: MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG
STATUS: PASS

## User-Facing Implementation Report

이번 작업은 MAP13의 authoritative special persistence 계약과 MAP18_02~05의 placement/runtime surface를 하나의 pure-data export surface로 묶었다. 결과는 CoreResource, Forge, Boss, Village, ActivityEventRuntime, SpawnState 여섯 그룹과 18개의 typed row이며 실제 CSV 파일이나 save file을 만들지 않는다.

MoonCore, CassiaSap, StarNuruk은 MAP13 starter catalog의 긴 canonical persistence key와 MAP18_02 stable spawn ID를 함께 사용한다. Forge와 Boss는 public landmark catalog의 canonical key를 사용한다. 활성 Village plan은 현재 fixture에 없으므로 persistence key나 spawn ID를 임의 생성하지 않고 `AbsentButDeclared` 상태로 명시했다.

MAP18_05의 6개 runtime export record는 runtime ID와 `MAP18_RUNTIME_STATE/V1` save key를 그대로 보존한다. SpawnState는 MAP18_04의 occupied surface에서 CoreResource가 이미 소유한 3개 spawn ID를 제외한 6개를 게시해, export 범위의 stable spawn ID 9개가 중복되지 않게 했다. MAP18_05 runtime surface, MAP18_04 occupied surface와 budget ledger는 동일 객체 참조로 통과한다.

Generated CSV material은 11열 header와 18개 row를 메모리에서 stable order로 직렬화한다. 줄바꿈은 LF로 정규화하고 UTF-8 no-BOM byte material과 lower-hex SHA-256 digest를 제공한다. machine path, timestamp, Unity instance ID는 포함하지 않으며 실제 file read/write는 0이다.

Selection, Occupied, Budget, RuntimeState, Persistence 다섯 section의 debug snapshot은 MAP18_02~06 digest 9개를 모아 사람이 선택·점유·예산·runtime state·persistence 상태를 확인할 수 있게 한다. 이는 EditorWindow, overlay, screenshot 또는 파일 export가 아니다. MAP18_07이 소비할 audit surface digest만 게시하며 MAP18_07을 unlock하거나 실행하지 않았다.

정확히 10개의 EditMode `MAP18_06` test를 한 번 실행해 10/10 PASS했다. missing source, upstream digest mismatch 5종, missing/legacy persistence key, row/persistence/runtime/save/spawn ID 중복, CSV shape, file/save/runtime side-effect 요청을 모두 atomic failure로 확인했다. 이전 task fixture는 private이므로 허용되지 않은 shared-fixture refactor 대신 이번 focused test 안에 필요한 builder를 한정 복제했으며 별도 cleanup 후보로만 남긴다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExport.cs` | 여섯 export kind, declared-source 상태, typed export row, in-memory CSV material, 5-section debug snapshot, MAP18_07 audit surface와 digest를 정의한다. | CSV/save file I/O, runtime spawn, reward/state mutation, EditorWindow/overlay/scene 변경을 수행하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedSpecialStateExporter.cs` | MAP13 CoreResource/landmark source와 MAP18_05 surface를 검증·결합하고 canonical key, identity collision, CSV shape, side-effect 요청을 atomic failure로 처리한다. | legacy key를 보정하거나 optional source를 암묵적으로 만들지 않고 partial row/CSV/debug를 게시하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedSpecialStateExporterTests.cs` | 정확히 10개의 `MAP18_06` focused test로 row group, persistence key, CSV material, debug snapshot, passthrough, uniqueness, failure, determinism, side-effect 0, MAP18_07 lock을 검증한다. | 이전 category, PlayMode, legacy 19347, unfiltered/full regression을 선택하지 않는다. |
| `MapDesign/MCP/REPORTS/MAP18_06_IMPLEMENT_SPECIAL_STATE_EXPORT_AND_DEBUG_RESULT.md` | 실제 row/count/digest/failure/Unity 결과와 file I/O 경계를 기록한다. | MAP18_07을 시작하거나 상태를 변경하지 않는다. |
| `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | 이 PASS Result 검증 뒤 MAP18_06만 COMPLETE로 finalize하고 Current Task를 NONE으로 닫는다. | MAP18_07 및 다른 LOCKED row는 변경하지 않는다. |

## Special State Export Summary

- MAP18_05 runtime state surface digest reused: `2774cc515c4531ad90055afb2bdabb4a73439a0e64162194b4c9dbdd51db0f72`
- MAP18_05 save key set digest reused: `9c841116463551aff94fe77132c2b7b61d23b07840ee5aa29710799591b0d448`
- MAP18_05 export surface digest reused: `2f2665b46b054f408f8e7a1fb97c128ca355b829aa74d4aa7811b2792b9f6d73`
- MAP18_05 export surface records reused: `6`
- MAP18_04 occupied surface digest reused: `39e530dde3a98191aee290916d536b4952034aa2c758cb7c35050d4e2f74b688`
- MAP18_04 budget ledger digest reused: `08a4d128bb08324c99669c235101ead8d2c81d2f78d379b7b58fe59090bef52d`

- export groups published: `6/6`
- CoreResource export rows: `3`
- Forge export rows: `1`
- Boss export rows: `1`
- Village export rows: `1`
- ActivityEventRuntime export rows: `6`
- SpawnState export rows: `6`
- total export rows: `18`
- absent optional special sources declared: `1` (`Village`)
- unique export row keys: `18`
- unique persistence keys: `5`
- unique runtime state IDs: `6`
- unique save keys: `6`
- unique stable spawn IDs: `9`
- duplicate export row keys: `0`
- duplicate persistence/runtime/save/stable IDs: `0/0/0/0`

- MoonCore authoritative key accepted: `YES` (`SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD`)
- CassiaSap authoritative key accepted: `YES` (`SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD`)
- StarNuruk authoritative key accepted: `YES` (`SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD`)
- Forge/Boss canonical landmark keys accepted: `2/2`
- legacy short keys accepted: `0`
- missing required CoreResource key failure probes: `1/1`
- legacy key failure probes: `1/1`

- CSV material header columns: `11`
- CSV material row count: `18`
- CSV material LF normalized: `YES`
- CSV material UTF-8 no BOM: `YES`
- CSV material contains machine path/timestamp/Unity instance ID: `NO/NO/NO`
- CSV material digest lower-hex SHA-256: `YES`
- CSV material digest: `03711cb7dcc5f576ca6fb6ff16fcbdbbd295a3838c9061761c201f089f8473bf`
- actual CSV file writes/reads: `0/0`
- Generated CSV files committed: `0`

- debug snapshot sections: `5`
- Selection section present: `YES`
- Occupied section present: `YES`
- Budget section present: `YES`
- RuntimeState section present: `YES`
- Persistence section present: `YES`
- debug snapshot upstream digest count: `9`
- MAP18_02 placement digest included: `eda7bf7aedb660223927d6e0b36e63f5dbe041761febf91da6fb855f413f200f`
- MAP18_03 population digest included: `4fc87b1c2699802761b9956aaf58fdc9ebbfaf6f32f33bdc9b7a776752cd109e`
- debug snapshot digest lower-hex SHA-256: `YES`
- debug snapshot digest: `59efb7fd30df9ec62014cadd04a111b222e7dd13e298789dbab88a661bea22ed`

- special export surface digest lower-hex SHA-256: `YES`
- special export surface digest: `358ac8cfe78eec502db049f8940ed0c71458179b89bb451680e837b0797b77b5`
- MAP18_07 audit surface digest lower-hex SHA-256: `YES`
- MAP18_07 audit surface digest: `ca7cf633a9a2dbf7b5a85d5d847ece3f8fe2bf4c4071e546cbdb6f593397bc72`
- repeat/reverse/culture/candidate-order digest mismatches: `0/0/0/0`
- mutation sensitivity probes passed: `4/4` (`special surface`, `CSV`, `debug`, `audit`)

- missing source failure probes: `1/1`
- digest mismatch failure probes: `5/5` (`runtime surface`, `save key set`, `runtime export`, `occupied`, `budget`)
- legacy key failure probes: `1/1`
- duplicate row/id/key failure probes: `5/5` (`row`, `persistence`, `runtime`, `save`, `spawn`)
- CSV shape failure probes: `1/1`
- attempted file/save/runtime side-effect failure probes: `10/10` in one atomic request
- failure owner/reason/offending key/expected/actual: `PUBLISHED`
- atomic failure partial export rows: `0`
- atomic failure partial CSV material: `0`
- atomic failure partial debug snapshot: `0`
- atomic failure retry loops: `0`

## Debug Snapshot and File IO Boundary Notes

- actual save writes/reads: `0/0`
- PlayerPrefs writes/reads: `0/0`
- runtime SpecialRegion/Village/resource/Forge/Boss spawns: `0/0/0/0/0`
- runtime Activity/Event prefabs spawned: `0/0`
- actual event activations executed: `0`
- actual reward grants: `0`
- actual inventory/resource mutations: `0/0`
- actual damage executions: `0`
- enemy AI/controller hookups: `0`
- runtime objects spawned: `0`
- GameObject instantiate/enable/disable/destroy: `0/0/0/0`
- System.IO file write/read calls: `0/0`
- disk save/load files created: `0/0`
- Unity Tilemap component writes: `0`
- Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: `0/0/0/0`
- TilemapCollider2D/CompositeCollider2D/Collider2D creations: `0/0/0`
- Rigidbody2D creations: `0`
- Physics2D queries/simulations: `0/0`
- NavMesh/pathfinding setup: `0/0`
- Scene/Prefab/Tilemap mutation: `0/0/0`
- Camera reads/writes: `0/0`
- Addressables/Resources/AssetDatabase loads: `0/0/0`
- production seed approvals: `0`
- UnityEngine.Random/Random.Range/System.Random direct calls: `0/0/0`
- hidden retry loops: `0`
- implicit SpecialRegion/Village/Forge/Boss source creation: `0`
- MAP18_07 started: `NO`

## Preconditions and Installation

- MAP18_05 Result exists: `YES`
- MAP18_05 Result independent PASS line count: `1`
- MAP18_05 Result SHA-256 required/actual: `b76e46388bd2db9043d313dde29c000fec11105fb873ad8967d315d3c8fbf5ed` / `b76e46388bd2db9043d313dde29c000fec11105fb873ad8967d315d3c8fbf5ed`
- MAP18_05 installed Task SHA-256 required/actual: `2f53f0d8ec57c3f57bf604990c314d9a931a4709cf4b103c46edb8bae4581f54` / `2f53f0d8ec57c3f57bf604990c314d9a931a4709cf4b103c46edb8bae4581f54`
- MAP18_06 inbox/install/archive SHA-256: `63c5dfa646565bcd74a1a4631f7c8e3868668ace81b2a1cd0aed5d606d48a6cc`
- inbox candidates validated: `1`
- legacy inbox candidates: `0`
- installed/archive byte equality: `YES`
- Current Task before apply: `NONE`
- MAP18_05 before apply: `COMPLETE`
- MAP18_06 before apply/task execution: `LOCKED / CURRENT`
- MAP18_07 before/after task execution: `LOCKED / LOCKED`
- Status row count and apply delta: `216`, `COMPLETE 0 / CURRENT +1 / LOCKED -1`
- unrelated staged files before apply: `0`
- Master task membership count: `1`
- Master task list edited: `NO`
- protocol-required Archive path is the only Phase A path outside the Task body write list: `YES`

## Focused Validation

Final authoritative run:

```text
mode: EditMode
category_names: [MAP18_06]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 2.1439173
job id: 5c5ed36352b3442fb61e336b2d74d541
```

Exact focused test names:

1. `SpecialStateExporterCreatesResourceForgeBossVillageRuntimeAndSpawnRows`
2. `SpecialStateExportUsesAuthoritativePersistenceKeysAndRejectsLegacyShortKeys`
3. `GeneratedSpawnStateCsvMaterialIsDeterministicLfUtf8AndDoesNotWriteFiles`
4. `SelectionBudgetDebugSnapshotIncludesRequiredSectionsAndUpstreamDigests`
5. `SpecialStateExporterPreservesMap18_05RuntimeSurfaceAndMap18_04BudgetReferences`
6. `SpecialStateExportIdsSaveKeysAndRowsAreUniqueStableAndMutationSensitive`
7. `SpecialStateExportFailuresAreAtomicAndReportOwnerReasonExpectedActual`
8. `SpecialStateExportDigestsAreStableAcrossRepeatReverseCultureAndCandidateOrder`
9. `SpecialStateExporterDoesNotSpawnObjectsWriteSavesMutateScenesOrRunRegressions`
10. `Map18HandoffKeepsMap18_07Locked`

- focused MAP18_06 selections: `1` (`10/10`)

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
- non-task MCP domain-reload transport diagnostic before authoritative run: `1` (`NetworkStream` disposed; cleared after recording)
- non-task TestRunner persistence diagnostic: `1` (`Saving results to .../TestResults.xml`, no failed test or compile error)
- non-task package/test framework warnings: `3` (automated-mode advisory and performance-test pre/post setup)
- EditMode Tests: `MAP18_06 10/10 PASS`
- authoritative test result source: `Unity MCP test job 5c5ed36352b3442fb61e336b2d74d541`
- PlayMode Tests: `NOT RUN`
- Scene/Prefab/Tilemap Changes: `NONE`

## Static Gates

- required `[Test]` methods present: `10/10`
- `MAP18_06` category declarations: `1`
- new matching Unity meta files: `3/3`
- new meta GUID uniqueness: `3/3`
- production runtime API calls (`UnityEngine`, `System.IO`, PlayerPrefs, GameObject, Tilemap, Physics2D, NavMesh): `0`
- production random calls: `0`
- task-owned source roots only: `YES`
- CSV files/assets, Scene, Prefab, Tilemap, asmdef, ProjectSettings and package changes: `0`
- task-owned implementation/test/Result `git diff --check`: `PASS`
- byte-identical installed/archive Task body trailing-space findings: `2` each (same supplied Markdown hard-breaks at lines 32 and 229; Task bytes preserved and not auto-corrected)

## Completion Gate

- MAP18_06 focused tests pass: `PASS`
- all six export groups represented: `PASS`
- CoreResource canonical keys accepted and legacy short keys rejected: `PASS`
- deterministic in-memory LF/UTF-8 no-BOM CSV material created without file I/O: `PASS`
- five-section debug snapshot includes required upstream digests: `PASS`
- MAP18_05 runtime and MAP18_04 occupied/budget references preserved: `PASS`
- export row, persistence, runtime, save and spawn identities unique: `PASS`
- MAP18_07 audit surface created: `PASS`
- atomic failures leave no partial row, CSV material or debug snapshot: `PASS`
- actual CSV/save I/O, spawn, event execution, reward, inventory/resource mutation, damage, AI, physics, Tilemap, Collider, GameObject and NavMesh work absent: `PASS`
- optimization rewrite, broad refactor and regression runs absent: `PASS`
- Scene/Prefab/Tilemap mutation absent: `PASS`
- MAP18_07 remains LOCKED / NOT STARTED: `PASS`

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. 이전 MAP18 focused test의 private source builder 중복은 이번 write boundary에서 공용화할 수 없으므로 이번 test 안에만 한정했고, shared fixture consolidation이나 별도 cleanup은 수행하지 않았다.
