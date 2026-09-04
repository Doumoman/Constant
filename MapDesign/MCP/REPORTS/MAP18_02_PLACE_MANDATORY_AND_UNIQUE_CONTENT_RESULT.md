# MAP18_02 Place Mandatory and Unique Content Result

TASK: MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP18_01이 게시한 immutable content slot index의 검증된 mandatory/unique 후보 5개를 입력으로 받아, required progress trigger와 MoonCore, CassiaSap, StarNuruk를 각각 정확히 하나의 기존 slot에 예약하는 pure-data logical preplacement 계약을 추가했다. 실제 선택은 content key 순서, category preference, sector, slice, cell, source owner, source slot, pool 순서로 결정되며 random roll, retry 또는 암묵적 slot 생성 없이 동일 입력에서 동일한 plan을 만든다.

logical preplacement는 런타임 GameObject를 spawn하거나 reward를 지급하는 동작이 아니다. 이번 결과는 `RequiredProgressTrigger -> MAP16_SLOT_07`, `MoonCore -> MAP16_SLOT_08`, `CassiaSap -> MAP16_SLOT_11`, `StarNuruk -> MAP16_SLOT_05`라는 논리적 배치와 기존 stable spawn ID/reservation key의 연결만 게시한다. reward grant, inventory 변경, device 실행, pool roll, budget spend, save I/O, Unity scene/asset 작업은 전부 0이다.

각 규칙은 required, exactly-one, world-unique, max-world-count 1을 선언한다. 선택된 4개 reservation key와 stable spawn ID는 서로 고유하며, 충돌 또는 중복 규칙은 부분 결과 없이 원자적으로 거절된다. CoreResource 3종의 persistence identity는 MAP13의 `SpecialPersistenceKey.ForSlot(regionId, Reward, slotId)` 결과를 그대로 사용하고 `MOON_CORE` 같은 legacy short key는 허용하지 않는다.

MAP18_03에는 선택된 4개 reservation/stable-ID/content-key exclusion entry와 남은 미예약 후보 `MAP16_SLOT_06`을 넘긴다. MAP18_03은 이번 Task에서 열거나 시작하지 않았고 `LOCKED / NOT STARTED`로 유지했다.

공유 production 코드의 새 중복 또는 이름 없는 hardcoding 후보는 발견하지 않았다. 다만 MAP18_01 test helper가 private이므로 동일한 12-record 검증 fixture를 MAP18_02 focused test에 한 번 복제한 test-only cleanup 후보 1개를 기록한다. 이 Task에서 fixture 공용화나 broad refactor는 수행하지 않았다. 회귀 트리거는 없었고 오직 EditMode category `MAP18_02`만 실행했다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedMandatoryUniquePlacement.cs` | 네 개 mandatory content key, authoritative CoreResource identity, world-unique 규칙, logical placement entry/plan, MAP18_03 exclusion surface와 canonical digest를 정의한다. | 실제 spawn, reward 지급, inventory/device 실행, pool roll, slot 생성 또는 Unity object 작업을 하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedMandatoryUniqueContentPreplacer.cs` | MAP18_01 digest/count 선행조건, stable candidate selection, reservation/stable-ID collision 검증, structured atomic failure와 plan 생성을 담당한다. | 실패 시 대체 후보 retry, 입력 후보 수정, legacy key 보정 또는 부분 commit을 하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedMandatoryUniquePlacementTests.cs` | 정확히 10개의 `MAP18_02` focused test로 배치 수량, MAP13 key, 결정성, mutation sensitivity, atomic rejection, 부작용 0과 MAP18_03 lock을 검증한다. | 이전 Task, PlayMode, legacy 19347, unfiltered 또는 full regression을 선택하지 않는다. |
| `MapDesign/MCP/REPORTS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT_RESULT.md` | 실제 선택 slot, digest, failure probe, side-effect, focused Unity 결과와 handoff 상태를 기록한다. | MAP18_03을 열거나 실행하지 않는다. |
| `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | PASS 확정 후 MAP18_02만 COMPLETE로 finalize하고 Current Task를 NONE으로 되돌린다. | MAP18_03 또는 다른 LOCKED task의 상태를 변경하지 않는다. |

## Mandatory Unique Placement Summary

- MAP18_01 slot index digest reused: `889c25815c9d0bffe6c6ea785b66c55e79f0e8e93631771f0ec30a0b39c2b6bd`
- MAP18_01 stable id set digest reused: `bfc341e0c62a62d8846580b9455874df9e30573bd4c5f6cc450d719c89464b8a`
- MAP18_01 slot source records reused: `12`
- MAP18_01 mandatory/unique candidate count reused: `5`
- required content keys published: `4`
- required trigger placements: `1`
- core resource placements: `3`
- MoonCore/CassiaSap/StarNuruk placements: `1/1/1`
- mandatory unique placement entries: `4`
- unique content keys: `4`
- unique stable spawn IDs: `4`
- unique reservation keys: `4`
- world unique max count rules: `4`
- remaining unreserved candidate count: `1`
- MAP18_03 exclusion entries: `4`
- CoreResource authoritative identity checks: `3/3`
- legacy short persistence keys accepted: `0`
- candidate selection uses stable order: `YES`
- input order dependency detected: `NO`
- random roll count: `0`
- retry loop count: `0`
- implicit slot creation count: `0`
- candidate mutation count: `0`
- placement plan digest lower-hex SHA-256: `YES`
- placement plan digest: `eda7bf7aedb660223927d6e0b36e63f5dbe041761febf91da6fb855f413f200f`
- placement stable id set digest lower-hex SHA-256: `YES`
- placement stable id set digest: `c4c1948c17d8e75e821e3eec4402832635e7773693c4b956bc18a53d7ca15a09`
- repeat/reverse/culture/candidate-order digest mismatches: `0/0/0/0`
- mutation sensitivity probes passed: `1/1`

Selected logical placements:

| Content key | Source slot | Reservation key | Stable spawn ID | MAP13 authoritative persistence key |
|---|---|---|---|---|
| `REQUIRED_PROGRESS_TRIGGER` | `MAP16_SLOT_07` | `fc7b8af575a02f8a3bfc335b2e102fb128944cd2f423f7464b00bfe18280f7f1` | `73e0fce865dddd7e105d9c1ec31e40ba55f1ffb5afd0fee098141e38e2dda98c` | none |
| `MOONCORE` | `MAP16_SLOT_08` | `091d4690e3c52899c6238f2011169ac401aee9227bfcb54ff2ece8f04fde156f` | `c69385426d4f989800ca217d26a74a569d35cf32950e89a7dcdd059e19b6fb77` | `SR_STATE_MOON_CORE_SITE_5_REWARD_MOON_CORE_REWARD` |
| `CASSIASAP` | `MAP16_SLOT_11` | `c1ac5175b8fd0680e19a1fc0791d5c5a1240328fc8157e9fcac74d70e171fd4c` | `e7d4077c28f835da78418d377b983e1a01a06bd05c124c4c7bb4dcb8e70a0dad` | `SR_STATE_CASSIA_SAP_SITE_5_REWARD_CASSIA_SAP_REWARD` |
| `STARNURUK` | `MAP16_SLOT_05` | `b7b88bc0fb636489745c5807f4f29fe9626365d023636b5001a501205c4c9c04` | `43f73923c8b976e64e3a150590fa6bd46b1e4957a47bb5fb6def5da8dcf3f91c` | `SR_STATE_STAR_NURUK_SITE_5_REWARD_STAR_NURUK_REWARD` |

- remaining unreserved candidate: `MAP16_SLOT_06`
- stable spawn ID namespace reused: `POPULATION_STABLE_SPAWN_V1`
- MAP18_03 exclusion lookup probes: `4/4`

## Runtime Spawn Boundary and Risk Notes

- logical preplacement entries created: `4`
- runtime content placements performed: `0`
- weighted pool rolls performed: `0`
- budget spends performed: `0`
- reward grants performed: `0`
- inventory mutations performed: `0`
- device executions performed: `0`
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
- Scene/Prefab/Tilemap mutation: `0/0/0`
- Camera reads/writes: `0/0`
- Addressables/Resources/AssetDatabase loads: `0/0/0`
- Authoring CSV edits: `0`
- Generated CSV/assets committed: `0/0`
- production seed approvals: `0`
- optimization rewrites/broad refactors: `0/0`
- shared production helper duplication candidates: `0`
- test-only fixture duplication/cleanup candidates: `1/1`
- unnamed hardcoding candidates: `0`
- MAP18_03 started: `NO`

The test-only fixture duplication is deliberately reported rather than consolidated because the reviewed MAP18_01 helper is private and fixture refactoring is outside this focused Task. Its cleanup owner is `LATER_APPROVED_CLEANUP_TASK`; it does not affect production runtime or placement identity.

## Validation and Atomic Failure Evidence

- missing candidate failure probes: `2/2` (`below minimum`, `missing MoonCore-compatible candidate`)
- digest mismatch failure probes: `3/3` (`slot index`, `stable ID set`, `placement plan`)
- duplicate unique key failure probes: `1/1`
- max count exceeded failure probes: `1/1`
- reservation collision failure probes: `1/1`
- stable spawn ID collision failure probes: `1/1`
- legacy short key rejection probes: `1/1`
- failure owner/reason/offending key/expected/actual: `PUBLISHED`
- atomic failure partial placement entries: `0`
- atomic failure partial mutations: `0`
- atomic failure retry loops: `0`

## Preconditions and Installation

- MAP18_01 Result exists: `YES`
- MAP18_01 Result independent PASS line count: `1`
- MAP18_01 Result SHA-256 required/actual: `18ce7c28e876d40e9c40c2e89e2dd984e315cb84c1e979374036375ab303452b` / `18ce7c28e876d40e9c40c2e89e2dd984e315cb84c1e979374036375ab303452b`
- MAP18_01 installed Task SHA-256 required/actual: `d678721d2cfc42b809ccc36335e84657a7312d8dddacbe2233c0b7ba1a28b211` / `d678721d2cfc42b809ccc36335e84657a7312d8dddacbe2233c0b7ba1a28b211`
- MAP18_02 inbox/install/archive SHA-256: `19f75c5068c7c8ed0ab17bbc1e288ebee07be547fa74bd3f8aa54ec6579c2264`
- inbox candidates validated: `1`
- legacy inbox candidates: `0`
- installed/archive byte equality: `YES`
- Current Task before apply: `NONE`
- MAP18_01 before apply: `COMPLETE`
- MAP18_02 before apply/task execution: `LOCKED / CURRENT`
- MAP18_03 before/after task execution: `LOCKED / LOCKED`
- unrelated staged files before apply: `0`
- Master task list edited: `NO`
- protocol-required archive is the only Phase A path outside the Task body allowlist: `YES`

## Focused Validation

```text
mode: EditMode
category_names: [MAP18_02]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 1.13
```

Exact focused test names:

1. `MandatoryUniquePreplacementCreatesRequiredTriggerAndThreeCoreResources`
2. `PreplacementUsesSlotIndexStableIdsAndReservationKeysWithoutPoolRolls`
3. `CoreResourceKeysMatchMap13AuthoritativeRewardDefinitions`
4. `WorldUniqueAndMaxCountRulesRejectDuplicatesAtomically`
5. `PreplacementIsStableAcrossRepeatReverseCultureAndCandidateOrder`
6. `SelectionUsesStableSlotOrderAndDoesNotInventSlots`
7. `MissingCandidateDigestMismatchAndReservationCollisionFailAtomically`
8. `PreplacementDoesNotSpawnObjectsMutateScenesWriteSavesOrLoadAssets`
9. `PreplacementReportsExclusionSurfaceForMap18_03`
10. `Map18HandoffKeepsMap18_03Locked`

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
- final Editor state: `ready`, compiling `false`, domain reload `false`, PlayMode `stopped`
- final production and test assembly compile errors: `0`
- initial focused test assembly compile diagnostics found/fixed: `1/1` (ambiguous NUnit comparer overload, fixed before test execution)
- final relevant Console errors: `0`
- final relevant Console warnings: `0`
- EditMode Tests: `MAP18_02 10/10 PASS`
- authoritative test result source: `Unity official pipeline response`
- PlayMode Tests: `NOT RUN`
- Scene/Prefab/Tilemap Changes: `NONE`

## Completion Gate

- MAP18_02 focused tests pass: `PASS`
- required trigger and three CoreResources logically preplaced exactly once: `PASS`
- world-unique/max-count rules and atomic rejection enforced: `PASS`
- MAP13 authoritative Reward persistence keys used; legacy short keys rejected: `PASS`
- existing MAP18_01 slots, stable IDs and reservation keys reused without invention: `PASS`
- plan and stable-ID-set determinism proven: `PASS`
- MAP18_03 exclusion/consumer surface created: `PASS`
- runtime spawn, reward, inventory, device, roll, save and Unity side effects absent: `PASS`
- optimization rewrite, broad refactor and regression runs absent: `PASS`
- MAP18_03 remains LOCKED / NOT STARTED: `PASS`

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. MAP18_01의 private test fixture와 중복되는 test-only helper는 후속 cleanup 후보로만 기록했으며 공용화하지 않았다. MAP18_03은 열거나 실행하지 않았다.
