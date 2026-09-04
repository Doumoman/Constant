# MAP18_01 Build Slot Index and Stable Spawn IDs Result

TASK: MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP16의 projected marker/slot/provenance를 MAP18 content 배치 단계가 조회할 수 있는 immutable slot source record로 변환하고, sector·slice·source owner·category·pool 기준의 안정 정렬 index를 만드는 pure-data 계약을 추가했다. `GeneratedMarkerSlot`을 주소 입력으로 옮기는 adapter도 제공하므로 upstream의 owner, provenance와 source slot identity를 display name으로 축약하지 않는다.

slot index는 실제 content placement가 아니다. 이 단계는 resource/shop/hazard/enemy/pickup/device/activity/event/special이라는 9개 category와 versioned pool key, 실제 물리 slot을 중복 점유하지 않게 하는 reservation key만 게시한다. mandatory/unique 후보 query 역시 후속 배치를 위한 후보 목록일 뿐 reserve 상태를 변경하거나 콘텐츠를 선택하지 않는다.

stable spawn ID는 `POPULATION_STABLE_SPAWN_V1` namespace 아래 world seed, generator/data version, sector coordinate, slice와 local cell index, source owner/provenance/slot, category, pool namespace/version을 LF-normalized canonical line으로 묶어 SHA-256 lower-hex로 만든다. 같은 입력은 같은 ID를 만들고 seed/sector/slice/source/category/pool 변경 6종은 모두 다른 ID를 만들었다. 입력을 반복·역순·문화권 변경·재배열해도 index와 stable-ID-set digest가 변하지 않았다.

MAP18_02에는 stable order의 전체 slot entry, sector/slice/category/pool/source owner query, mandatory/unique preplacement 후보 query, collision-free reservation lookup과 stable spawn ID를 넘긴다. 실제 pool roll, budget spend, content placement, runtime spawn은 0이며 MAP18_02는 `LOCKED / NOT STARTED`로 유지했다.

기존 `BakingCanonicalDigest`, `GeneratedSectorCoordinate`, `GeneratedSectorLocalCellIndex`, `GeneratedMarkerSlotOwner`와 MAP16 marker slot public contract를 재사용해 좌표·SHA 구현 중복은 추가하지 않았다. namespace/policy/version과 테스트 기준값은 이름 있는 상수로 정의했으며 새 duplicate helper나 정리 대상 hardcoding 후보는 발견하지 않았다. 검증은 EditMode category `MAP18_01`만 한 번 실행했고 회귀 trigger가 없어서 이전 category, legacy 19347, PlayMode, unfiltered 및 full regression은 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedContentSlotIndex.cs` | content category, versioned pool key, canonical slot address, index entry, immutable stable-order index와 sector/slice/category/pool/source/reservation query를 정의한다. | 실제 content 선택·reserve/commit·배치·pool roll·budget spend 또는 Unity object 작업을 수행하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedStableSpawnId.cs` | `POPULATION_STABLE_SPAWN_V1` namespace와 canonical address 기반 lower-hex SHA-256 value/factory를 정의한다. | Guid, random, time, frame, Unity object ID 또는 file path를 identity로 사용하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedContentSlotIndexBuilder.cs` | MAP16 marker/slot/provenance adapter, source request, validation failure/result, atomic index builder와 deterministic digest를 정의한다. | 잘못된 source를 임의 생성·보정하지 않고 실제 spawn/placement 또는 retry를 수행하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedContentSlotIndexTests.cs` | 정확한 MAP18_01 focused test 10개로 index/query, stable ID 변화 민감성, collision atomicity, digest 안정성, 비-spawn 경계와 MAP18_02 lock을 검증한다. | 이전 Task, PlayMode, legacy, unfiltered 또는 full regression을 선택하지 않는다. |
| `MapDesign/MCP/REPORTS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS_RESULT.md` | 실제 수량·digest·부작용·Unity 검증과 handoff 상태를 기록한다. | MAP18_02를 열거나 실행하지 않는다. |
| `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | PASS 확정 후 MAP18_01을 COMPLETE로 finalize하고 Current Task를 NONE으로 닫는다. | MAP18_02 row를 변경하지 않는다. |

## Slot Index and Stable ID Summary

- MAP17_08 audit report digest reused: `8b4849bf11ac6807a9e8a9d699a166eaa61e5c600454e410bae1ad47480545a0`
- MAP17 phase exit verdict reused: `PASS`
- MAP18_01 handoff approved by audit reused: `YES`
- MAP17 warnings carried forward: `2`
- MAP17 warnings block MAP18_01: `NO`
- slot source records observed: `12`
- slot index entries created: `12`
- unique slot addresses: `12`
- duplicate slot addresses rejected: `1/1`
- unique reservation keys: `12`
- reservation key collision probes: `1/1`
- categories published: `9`
- pool keys published: `3`
- source owner kinds published: `7`
- sector query probes: `1`
- sector+slice query probes: `1`
- category query probes: `1`
- pool query probes: `1`
- source owner query probes: `1`
- mandatory/unique candidate query probes: `1`
- mandatory/unique candidates observed: `5`
- reservation lookup probes: `1`
- stable spawn id namespace: `POPULATION_STABLE_SPAWN_V1`
- stable spawn ids lower-hex SHA-256: `YES`
- stable spawn ids created: `12`
- stable spawn id duplicate collisions: `0`
- same input stable id equality: `YES`
- seed/sector/slice/source/category/pool mutation distinction probes: `6/6`
- modification id namespace collision probes: `1/1`
- Guid.NewGuid/random/time/frame/object/file-path identity usage: `0/0/0/0/0/0`
- slot index digest lower-hex SHA-256: `YES`
- slot index digest: `889c25815c9d0bffe6c6ea785b66c55e79f0e8e93631771f0ec30a0b39c2b6bd`
- stable id set digest lower-hex SHA-256: `YES`
- stable id set digest: `bfc341e0c62a62d8846580b9455874df9e30573bd4c5f6cc450d719c89464b8a`
- repeat/reverse/culture/input-order digest mismatches: `0/0/0/0`
- mutation sensitivity probes passed: `6/6`
- stale expected-digest rejection probes: `1/1`
- atomic failure partial entries/mutations/retries: `0/0/0`

## Non-Spawn Boundary and Risk Notes

- actual content placements performed: `0`
- weighted pool rolls performed: `0`
- budget spends performed: `0`
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
- shared fixture consolidation: `0`
- new duplicate helper candidates: `0`
- unnamed hardcoding candidates: `0`
- MAP17_08 performance WARN owner: `LATER_APPROVED_OPTIMIZATION_TASK`
- MAP17_08 fixture cleanup owner: `LATER_APPROVED_CLEANUP_TASK`
- MAP18_02 started: `NO`

## Validation and Atomic Failure Evidence

- missing upstream source probes rejected: `1/1`
- out-of-bounds sector probes rejected: `1/1`
- out-of-bounds slice probes rejected: `1/1`
- out-of-bounds sector-local cell probes rejected: `1/1`
- out-of-bounds slice-local cell probes rejected: `1/1`
- invalid category probes rejected: `1/1`
- invalid pool key probes rejected: `1/1`
- missing provenance probes rejected: `1/1`
- duplicate address probes rejected: `1/1`
- reservation collision probes rejected: `1/1`
- expected digest mismatch probes rejected: `1/1`
- failure owner/reason/offending key/expected/actual: `PUBLISHED`
- partial index entries after any rejection: `0`
- partial mutations after any rejection: `0`
- retry loops after any rejection: `0`

## Preconditions and Installation

- MAP17_08 Result exists: `YES`
- MAP17_08 Result independent PASS line count: `1`
- MAP17_08 Result SHA-256 required/actual: `aca1f360dc9ffe4c5f96479ae7d2d69526cd9e8d6d6fed442c1c2fb58c998fb1` / `aca1f360dc9ffe4c5f96479ae7d2d69526cd9e8d6d6fed442c1c2fb58c998fb1`
- MAP17_08 installed Task SHA-256 required/actual: `b68f5808d4a7c3cea90a18a69eecc3eda86357dc2ca669890c77c8aaecc22be0` / `b68f5808d4a7c3cea90a18a69eecc3eda86357dc2ca669890c77c8aaecc22be0`
- MAP18_01 inbox/install/archive SHA-256: `d678721d2cfc42b809ccc36335e84657a7312d8dddacbe2233c0b7ba1a28b211`
- inbox candidates validated: `1`
- legacy inbox candidates: `0`
- installed/archive byte equality: `YES`
- Current Task before apply: `NONE`
- MAP17_08 before apply: `COMPLETE`
- MAP18_01 before apply: `LOCKED`
- MAP18_02 before/after task execution: `LOCKED / LOCKED`
- unrelated staged files before apply: `0`
- Master task list edited: `NO`
- protocol-required archive is the only Phase A path outside the Task body allowlist: `YES`

## Focused Validation

```text
mode: EditMode
category_names: [MAP18_01]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 1.04
```

Exact focused test names:

1. `ContentSlotIndexBuildsStableSectorSliceSourceCategoryAndPoolEntries`
2. `StableSpawnIdsAreDeterministicAndSeparatedFromModificationIds`
3. `StableSpawnIdsChangeWhenSeedSectorSliceSourceCategoryOrPoolChanges`
4. `SlotIndexQueriesBySectorSliceCategoryPoolAndSourceInStableOrder`
5. `ReservationKeysRejectDuplicateAddressAndCollisionAtomically`
6. `SlotIndexRejectsOutOfBoundsSliceCellSectorAndInvalidCategory`
7. `SlotIndexDigestIsStableAcrossRepeatReverseCultureAndInputOrder`
8. `SlotIndexDoesNotRollPoolsPlaceContentSpawnObjectsOrMutateScenes`
9. `SlotIndexReportsMap17WarningsAsNonBlockingHandoffRisks`
10. `Map18HandoffKeepsMap18_02Locked`

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
- successful explicit recompiles: `2`
- Compile Errors: `0`
- Relevant Console Errors: `0`
- Relevant Warnings: `0`
- EditMode Tests: `MAP18_01 10/10 PASS`
- Test result XML: `C:/Users/user/AppData/LocalLow/DefaultCompany/별을 물어오는 밤/TestResults.xml`
- Test result XML timestamp: `2026-09-05T00:08:05.4758761+09:00`
- PlayMode Tests: `NOT RUN`
- Scene/Prefab/Tilemap Changes: `NONE`

## Completion Gate

- content slot index and stable spawn ID contracts created: `PASS`
- stable IDs deterministic and modification namespace-separated: `PASS`
- sector/slice/category/pool/source queries stable: `PASS`
- duplicate address/reservation collision atomic rejection: `PASS`
- MAP17 WARN risks carried without regression trigger: `PASS`
- placement/roll/budget/spawn and disk/Unity side effects absent: `PASS`
- optimization, broad refactor and fixture consolidation absent: `PASS`
- focused-only validation preserved: `PASS`
- MAP18_02 remains LOCKED / NOT STARTED: `PASS`

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. MAP17_08의 성능 WARN과 fixture 정리 후보는 후속 owner에 그대로 남겼으며 최적화·cleanup을 수행하지 않았다. MAP18_02는 시작하거나 unlock하지 않았다.
