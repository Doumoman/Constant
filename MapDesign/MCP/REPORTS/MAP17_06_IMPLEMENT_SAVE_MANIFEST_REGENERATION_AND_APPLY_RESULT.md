# MAP17_06 Implement Save Manifest Regeneration And Apply Result

TASK: MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP17_05의 in-memory sector modification storage 위에 저장 manifest와 regeneration apply 계약을 추가했다. Manifest header는 world seed, generator/data/schema version, geometry/placement/bake/cache/window-handle/storage digest를 게시하고, 169개 world sector 가운데 실제로 수정된 1개 sector와 5개 modification record만 직렬화한다. 수정되지 않은 168개 sector의 full tile data나 population/content spawn ID는 저장하지 않으며 seed와 검증된 base digest로 재생성한다.

Canonical serializer/parser는 field 순서가 고정된 LF-normalized UTF-8 no-BOM payload를 메모리에서 만들고 읽는다. Unknown field, duplicate sector, duplicate record, unsupported version, seed/version/hash mismatch와 missing target은 owner/key/expected/actual/reason을 포함한 deterministic failure로 원자적으로 거부한다. 파일 경로, timestamp, frame count, Unity object ID는 payload에 들어가지 않으며 실제 disk save/load는 수행하지 않는다.

Regeneration apply는 재생성된 base snapshot을 변경하지 않는 pure-data 계획이다. MAP17_05의 DestroyTile, ReplaceTile, CollectPickup, ChangeDeviceState, ConsumeSlot을 각각 한 번씩 재생하고 output dirty revision 5와 modification set digest를 manifest entry와 일치시킨다. Unity Tilemap, Collider, Rigidbody, Physics2D, GameObject, Scene/Prefab 또는 asset load 작업은 하지 않는다. 이 계약과 focused evidence만 MAP17_07에 넘기며 MAP17_07은 LOCKED / NOT STARTED로 유지했다.

검증은 EditMode category `MAP17_06` 한 번만 선택했고 정확히 10개가 발견·실행되어 10/10 PASS했다. 회귀 trigger가 없었으므로 이전 Task, PlayMode, legacy 19347, unfiltered 또는 full regression은 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSaveManifest.cs` | Version/header, modified-sector entry, immutable manifest/payload/result, deterministic validation failure와 manifest/payload digest 모델을 정의한다. | Disk save slot, Unity object identity, unmodified full tile data와 population/content spawn ID를 소유하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSaveManifestSerializer.cs` | 고정 field-order canonical LF text를 serialize/parse하고 payload/manifest hash, duplicate 및 unknown field를 검증한다. | `System.IO`, file path, timestamp, frame count 또는 asset load를 사용하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorRegenerationApply.cs` | Pure-data regeneration request, per-record apply command/plan/result와 stable apply digest를 정의한다. | 재생성 base를 in-place 변경하거나 Tilemap/Collider/Physics/GameObject를 조작하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSaveManifestService.cs` | MAP17_05 storage에서 modified-only manifest를 만들고 seed/version/base digests를 검증한 뒤 5종 modification apply plan을 생성한다. | Full generator 실행, durable persistence, runtime spawn과 MAP17_07 성능 측정을 수행하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSaveManifestRegenerationTests.cs` | 정확한 MAP17_06 focused test 10개로 manifest scope, round-trip, regeneration, atomic failure, digest determinism과 side-effect 부재를 검증한다. | PlayMode, prior category, legacy 및 full regression을 선택하지 않는다. |

## Preconditions and Installation

- MAP17_05 Result exists: YES
- MAP17_05 Result independent PASS line count: 1
- MAP17_05 Result SHA-256 required/actual: `31e563a7995bb4ef560e9df078efd653ab01c340cac033793b281ee2e1b8884c` / `31e563a7995bb4ef560e9df078efd653ab01c340cac033793b281ee2e1b8884c`
- MAP17_05 installed Task SHA-256 required/actual: `d3d2917fce5af82298c65db09f1047a46cdc9bd9d8945750930ef441dcd57877` / `d3d2917fce5af82298c65db09f1047a46cdc9bd9d8945750930ef441dcd57877`
- MAP17_06 inbox/install/archive SHA-256: `52e97516e909f8c5580d6832b67eb1fdc206d85376b9b4f2cef12b65aae1619b`
- inbox candidates validated: 1
- legacy inbox candidates: 0
- installed/archive byte equality: YES
- MAP17_05 modification set digest reused: `a07d0f4387924f080ac34a62161a5de673e34f00e0d200ba48070efe0de6f180`
- MAP17_05 storage snapshot digest reused: `7b4e507333f24ab61698422e17870ab86325d3aff5a129d8d4837d3fb9c3305f`
- MAP17_05 apply plan digest reused: `62a608b6cae1ce398ff5c31e56f6eeb0af46e6630e61534d62229ce553cd5300`
- source modified sectors observed: 1/1
- source modification records observed: 5/5
- source dirty revision observed: 5
- source sector local index range covered: 0..1535
- source population/content spawn ids observed: 0

## Manifest and Serialization Evidence

- manifest schema version published: `MAP17_06_SAVE_MANIFEST_V1`
- manifest header fields published: 10
- manifest modified sector entries: 1/1
- manifest unmodified sectors omitted: 168/168
- manifest modification records serialized: 5/5
- manifest full tile data entries serialized: 0
- manifest Unity object ids serialized: 0
- manifest file paths/timestamps/frame counts serialized: 0/0/0
- manifest population/content spawn ids serialized: 0
- canonical payload generated in memory: YES
- canonical payload parsed in memory: YES
- serializer/parser round-trip equality: YES
- canonical payload encoding: LF-normalized UTF-8 no BOM
- canonical payload bytes: 20518
- unknown field policy covered: REJECTED, 1/1
- duplicate sector entry failure probes: 1/1
- duplicate record id failure probes: 1/1
- unsupported version failure probes: 1/1

## Regeneration and Atomic Failure Evidence

- regeneration request seed/version/digest validation probes: 9/9
- geometry/placement/bake/cache/window/storage digest probes: 6/6
- unmodified sector regeneration-by-seed probes: 168/168
- full generator executions: 0
- modified sector apply plan count: 1
- apply command count: 5
- DestroyTile/ReplaceTile/CollectPickup/ChangeDeviceState/ConsumeSlot reapplied: 1/1/1/1/1
- input regenerated base in-place mutations: 0
- output dirty revision equals manifest dirty revision: YES
- output modification set digest equals manifest entry digest: YES
- hash/version/seed mismatch failure probes: 5/5
- missing target/stale manifest failure probes: 7/7
- atomic failure partial apply mutations: 0

## Determinism and Digest Evidence

- manifest digest lower-hex SHA-256: YES
- manifest digest: `18bb9bd0ada73c2c84b9b400675d792a0e9c206f4ee5bd5eec897468154cd27a`
- canonical payload digest lower-hex SHA-256: YES
- canonical payload digest: `af88b4751877d4a03b0854eefea089bab70c542717d676d8eb52655b67ebac04`
- regeneration apply digest lower-hex SHA-256: YES
- regeneration apply digest: `13a1d61f92382f05460e7bc5c39f75b39c8e24850918bd7b94e8ace330504568`
- repeat/reverse/culture/sector-order/record-order digest mismatches: 0/0/0/0/0
- manifest/payload/apply mutation sensitivity probes passed: 3/3

## Forbidden Side-Effect Evidence

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
- Addressables/Resources/AssetDatabase loads: 0/0/0
- Authoring CSV edits: 0
- Generated CSV/assets committed: 0/0
- runtime objects spawned: 0
- production seed approvals: 0
- `System.IO` imports in production sources: 0
- UnityEngine/UnityEditor imports in production sources: 0/0
- MAP17_07 started: NO

## Focused Validation

```text
mode: EditMode
category_names: [MAP17_06]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 11.16
```

Exact focused test names:

1. `SaveManifestPublishesSeedVersionHashesAndOnlyModifiedSectors`
2. `ManifestSerializerRoundTripsCanonicalPayloadWithoutDiskIO`
3. `UnmodifiedSectorsRegenerateFromSeedWithoutManifestEntries`
4. `RegenerationRequestValidatesBaseGeometryBakeCacheWindowAndStorageDigests`
5. `RegenerationApplyPlanReplaysDestroyReplaceCollectDeviceAndSlotChangesAsPureData`
6. `HashVersionSeedAndStaleManifestMismatchesFailAtomically`
7. `DuplicateUnknownUnmodifiedSectorAndRecordPayloadFailuresAreDeterministic`
8. `ManifestDigestsAreStableAcrossRepeatReverseCultureSectorAndRecordOrder`
9. `SaveManifestDoesNotWriteFilesLoadAssetsMutateScenesOrSpawnObjects`
10. `Map17HandoffKeepsMap17_07Locked`

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
- successful explicit recompiles during implementation: 2
- Compile Errors: 0
- Relevant Console Errors: 0
- Relevant Warnings: 0
- EditMode Tests: MAP17_06 10/10 PASS
- Test result XML: `C:/Users/user/AppData/LocalLow/DefaultCompany/별을 물어오는 밤/TestResults.xml`
- Test result XML timestamp: `2026-09-04T21:38:34.1586836+09:00`
- PlayMode Tests: NOT RUN
- Scene/Prefab Changes: NONE
- Tilemap Changes: NONE

## Completion Gate

- save manifest payload and regeneration apply contract created: PASS
- manifest stores seed/version/hash and modified sectors only: PASS
- unmodified sectors omitted and regeneration-by-seed proven: PASS
- deterministic serialize/parse round-trip and atomic mismatch rejection: PASS
- no disk save/load file write/read: PASS
- no population/content stable spawn IDs: PASS
- no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work: PASS
- no Scene/Prefab/Tilemap mutation: PASS
- focused-only policy preserved: PASS
- MAP17_07 remains LOCKED / NOT STARTED: PASS

## Out-of-Scope Findings

작업 시작 전부터 존재한 `Constant.slnx`, TerrainClusters 관련 meta 3개, `MAP17_01_REPAIR_INSTALLED_TASK_BODY_SHA_PRECONDITION.md`, `PRE_MAP17_STRUCTURE_OBSERVATION_AUDIT_RESULT.md` 변경은 수정하거나 stage하지 않았다. Master task list는 수정하지 않았다. MCP protocol에 따른 installed Task, archive, Result와 implementation status 외의 문서는 변경하지 않았다.
