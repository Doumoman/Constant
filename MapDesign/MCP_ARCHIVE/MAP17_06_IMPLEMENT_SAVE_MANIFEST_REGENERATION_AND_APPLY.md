```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY
  task_file: TASKS/MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY.md
  requires_current_task: NONE
  requires_completed_task: MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE
  requires_result:
    path: REPORTS/MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE_RESULT.md
    status: PASS
    sha256: 31e563a7995bb4ef560e9df078efd653ab01c340cac033793b281ee2e1b8884c
  requires_installed_task:
    path: TASKS/MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE.md
    sha256: d3d2917fce5af82298c65db09f1047a46cdc9bd9d8945750930ef441dcd57877
  sets_current_task: MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY
```

# MAP17_06 - Implement Save Manifest Regeneration and Apply

```text
TASK: MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY
PHASE: MAP17 - Tilemap Bake / Streaming / Save Preparation
STATUS: CURRENT
NEXT: MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP17_05의 in-memory sector modification storage를 기반으로, seed 재생성 후 변경분을 다시 적용할 수 있는 **canonical save manifest payload**와 **regeneration apply plan**을 구현한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. world seed / generator version / data version / base digest / modified sector만 포함하는 manifest payload를 만든다.
2. unmodified sector 168개는 manifest entry로 저장하지 않고 seed regeneration 대상임을 증명한다.
3. manifest payload를 canonical text로 serialize/parse round-trip하고 digest를 고정한다.
4. regenerated base sector에 MAP17_05 modification record를 pure-data로 재적용하는 apply plan을 만든다.
```

이번 Task는 **디스크 save/load 파일을 쓰는 단계가 아니다.**  
Manifest는 파일이 아니라 메모리 안의 canonical payload/string model이다. 실제 파일 경로, 슬롯 저장, 사용자 save data, 플랫폼 저장소, encryption/compression은 아직 구현하지 않는다.

금지:

```text
System.IO file write/read
disk save/load file creation
Unity Tilemap / Collider / Rigidbody / Physics2D 생성 또는 변경
GameObject / Prefab instantiate, enable, disable, destroy
Scene / Prefab / Tilemap mutation
Camera / streaming loader integration
Addressables / Resources / AssetDatabase load
population stable spawn ID generation
production seed approval
```

MAP17_07은 이 Task가 만든 manifest round-trip, regeneration apply, hash mismatch evidence를 사용해 bake/stream/save 성능과 spike를 측정한다.

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
save manifest payload가 실제 save file write와 어떻게 다른지
seed regeneration + modified sector apply가 무엇을 보장하는지
unmodified sector를 저장하지 않는다는 점
population stable spawn ID를 만들지 않았다는 점
MAP17_07에 넘기는 산출물
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP17_05 Result exists
MAP17_05 Result STATUS: PASS
MAP17_05 Result SHA-256:
31e563a7995bb4ef560e9df078efd653ab01c340cac033793b281ee2e1b8884c

MAP17_05 installed task SHA-256:
d3d2917fce5af82298c65db09f1047a46cdc9bd9d8945750930ef441dcd57877

MAP17_05 modification set digest:
a07d0f4387924f080ac34a62161a5de673e34f00e0d200ba48070efe0de6f180

MAP17_05 storage snapshot digest:
7b4e507333f24ab61698422e17870ab86325d3aff5a129d8d4837d3fb9c3305f

MAP17_05 apply plan digest:
62a608b6cae1ce398ff5c31e56f6eeb0af46e6630e61534d62229ce553cd5300

Current Task before apply: NONE
MAP17_05: COMPLETE
MAP17_06: LOCKED before apply
MAP17_07: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP17_05/MAP17_04/MAP17_03/MAP17_02/MAP17_01 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
GeneratedTerrainGeometrySnapshot
BakingCanonicalDigest
GeneratedCellPlacementPlan
GeneratedTilemapBakePlan
GeneratedColliderCacheKey
GeneratedSectorRuntimeHandle
GeneratedSectorStreamingWindow
GeneratedSectorModificationRecord
GeneratedSectorModificationStorage
GeneratedSectorModificationStore
GeneratedSectorModificationApplyPlan
```

기준 수량:

```text
world sectors: 13x13 = 169
sector size: 48x32 cells
sector local index range: 0..1535
logical layer count: 7
MAP17_05 focused fixture modified sectors: 1
MAP17_05 focused fixture modification records: 5
MAP17_05 dirty revision: 5
unmodified sectors in reference world: 168
```

MAP17_05는 actual disk save/load와 manifest generation을 하지 않았다. 이번 Task는 manifest **payload** 생성과 parse/apply plan만 소유한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedSaveManifestVersion` | save manifest schema/generator/data version value object |
| `GeneratedSaveManifestHeader` | seed, generator/data version, geometry/bake/cache/window/storage base digest 요약 |
| `GeneratedModifiedSectorManifestEntry` | sector coordinate, dirty revision, base digest, modification set digest, record count |
| `GeneratedSaveManifestRecordPayload` | MAP17_05 modification record의 canonical manifest payload |
| `GeneratedWorldSaveManifest` | header와 modified sector entries만 담은 immutable manifest |
| `GeneratedSaveManifestPayload` | canonical text/string payload와 payload digest |
| `GeneratedSaveManifestSerializer` | manifest -> canonical text, canonical text -> manifest round-trip |
| `GeneratedSaveManifestValidationFailure` | missing/duplicate/unknown/unmodified/stale/hash mismatch/version mismatch reason |
| `GeneratedSaveManifestResult` | success/failure wrapper |
| `GeneratedSectorRegenerationRequest` | seed로 다시 만든 base sector와 manifest entry를 맞춰보는 pure-data request |
| `GeneratedSectorRegenerationApplyPlan` | regenerated base에 modification records를 순서대로 적용하는 pure-data plan |
| `GeneratedSectorRegenerationApplyResult` | success/failure wrapper |
| `GeneratedSaveManifestDigest` | manifest, payload, regeneration apply digest |
| `GeneratedSaveManifestService` | build/validate/serialize/parse/regen-apply를 수행하는 pure-data service |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSaveManifest.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSaveManifestSerializer.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorRegenerationApply.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSaveManifestService.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSaveManifestRegenerationTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Save manifest 규칙

### 5.1 Manifest payload scope

Manifest는 seed로 재생성 가능한 base world를 통째로 저장하지 않는다.

필수 포함:

```text
manifest schema version
world seed or explicit reference seed
generator version
data version
geometry snapshot digest or ordered geometry lines digest
base placement digest
base logical bake digest
base collider cache digest
base window/handle digest
modified sector count
modified sector entries
manifest payload digest
```

필수 제외:

```text
unmodified sector entries
full 169-sector tile data
full 10752 layer records
Tilemap cells serialized as save data
Unity instance ids
GameObject names
file paths
timestamps
frame counts
random GUIDs
population/content spawn ids
```

Reference fixture 기준:

```text
modified sectors serialized: 1/1
unmodified sectors omitted: 168/168
modification records serialized: 5/5
```

### 5.2 Canonical serializer/parser

Serializer는 deterministic canonical text를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM if bytes are requested
lower-hex SHA-256
stable field order
stable sector order
stable record order
round-trip manifest equality
unknown field rejection or explicit ignored-extension policy
duplicate sector entry rejection
duplicate record id rejection
```

`System.IO`로 파일을 쓰거나 읽지 않는다. Unit test는 string/byte array in-memory payload만 사용한다.

### 5.3 Regeneration validation

Seed regeneration 자체의 full generator 실행은 이번 Task의 소유가 아니다. 대신 regenerated base를 나타내는 pure-data request를 검증한다.

필수 검증:

```text
seed matches
generator version matches
data version matches
geometry digest matches
base placement digest matches
base logical bake digest matches
base collider cache digest matches
base window/handle digest matches
modified sector coordinate exists in 13x13 world
manifest record target exists in regenerated base sector
```

Mismatch는 atomic failure다. 자동으로 manifest를 고치거나 base digest를 갱신하지 않는다.

### 5.4 Regeneration apply

`GeneratedSectorRegenerationApplyPlan`은 regenerated base sector에 MAP17_05 modification record를 pure-data로 다시 적용한다.

필수:

```text
DestroyTile reapplied
ReplaceTile reapplied
CollectPickup reapplied
ChangeDeviceState reapplied
ConsumeSlot reapplied
apply order deterministic
output modified sector dirty revision equals manifest dirty revision
output modification set digest equals manifest entry digest
output apply plan digest stable
input regenerated base not mutated in place
```

실제 Tilemap, Collider, GameObject, item grant, device execute, spawn state는 변경하지 않는다.

### 5.5 Hash/version mismatch policy

다음은 atomic failure다.

```text
manifest schema version unsupported
generator version mismatch
data version mismatch
seed mismatch
geometry digest mismatch
placement/bake/cache/window/storage digest mismatch
modified sector entry references unmodified sector with no records
record target missing in regenerated base
duplicate modified sector entry
duplicate modification stable id with different payload
```

Failure는 reason, owner, offending key, expected/actual digest를 deterministic하게 보고한다.

### 5.6 Digest

`BakingCanonicalDigest`를 사용해서 manifest, payload, regeneration apply digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable sector entry order
stable modification record order
mutation sensitivity
```

Digest canonical line은 domain field order를 명시한다. display name이나 file system order를 dependency key로 쓰지 않는다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
System.IO file write/read
disk save/load file creation
actual user save slot management
platform save storage
encryption/compression
full generator rerun as production seed approval
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
Scene mutation
Prefab mutation
GameObject instantiate / enable / disable / destroy
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
Authoring CSV edits
Generated CSV commits
population/content stable spawn ID generation
production seed approval
MAP17_07 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP17_06`만 선택한다.

```text
MAP17_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17_01/MAP17_02/MAP17_03/MAP17_04/MAP17_05 selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Compile check와 relevant Console check는 허용한다.

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 회귀를 돌리지 않는다. Result에 다음을 기록하고 멈춘다.

```text
REGRESSION TRIGGER DETECTED: YES
trigger owner:
broken invariant:
why focused proof is insufficient:
requested wider verification:
```

문제가 없다면 Result에 반드시 기록한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 8. 필수 Focused Tests

다음 test name을 그대로 포함한다. 프로젝트 test framework에 맞춰 클래스/파일은 조정할 수 있다.

```text
SaveManifestPublishesSeedVersionHashesAndOnlyModifiedSectors
ManifestSerializerRoundTripsCanonicalPayloadWithoutDiskIO
UnmodifiedSectorsRegenerateFromSeedWithoutManifestEntries
RegenerationRequestValidatesBaseGeometryBakeCacheWindowAndStorageDigests
RegenerationApplyPlanReplaysDestroyReplaceCollectDeviceAndSlotChangesAsPureData
HashVersionSeedAndStaleManifestMismatchesFailAtomically
DuplicateUnknownUnmodifiedSectorAndRecordPayloadFailuresAreDeterministic
ManifestDigestsAreStableAcrossRepeatReverseCultureSectorAndRecordOrder
SaveManifestDoesNotWriteFilesLoadAssetsMutateScenesOrSpawnObjects
Map17HandoffKeepsMap17_07Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP17_06]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
```

If the project already has equivalent focused tests and the exact count differs, explain why in Result. Silent substitution is not allowed.

## 9. Result 필수 증거

Result에는 아래 값을 실제 수치로 기록한다.

```text
MAP17_05 modification set digest reused:
MAP17_05 storage snapshot digest reused:
MAP17_05 apply plan digest reused:
source modified sectors observed: 1/1
source modification records observed: 5/5
source dirty revision observed: 5
source sector local index range covered: 0..1535
source population/content spawn ids observed: 0

manifest schema version published:
manifest header fields published:
manifest modified sector entries: 1/1
manifest unmodified sectors omitted: 168/168
manifest modification records serialized: 5/5
manifest full tile data entries serialized: 0
manifest Unity object ids serialized: 0
manifest file paths/timestamps/frame counts serialized: 0/0/0
manifest population/content spawn ids serialized: 0

canonical payload generated in memory: YES
canonical payload parsed in memory: YES
serializer/parser round-trip equality: YES
unknown field policy covered:
duplicate sector entry failure probes:
duplicate record id failure probes:
unsupported version failure probes:

regeneration request seed/version/digest validation probes:
unmodified sector regeneration-by-seed probes:
modified sector apply plan count:
DestroyTile/ReplaceTile/CollectPickup/ChangeDeviceState/ConsumeSlot reapplied: 1/1/1/1/1
input regenerated base in-place mutations: 0
output dirty revision equals manifest dirty revision: YES
output modification set digest equals manifest entry digest: YES
hash/version/seed mismatch failure probes:
missing target/stale manifest failure probes:
atomic failure partial apply mutations: 0

manifest digest lower-hex SHA-256: YES
manifest digest:
canonical payload digest lower-hex SHA-256: YES
canonical payload digest:
regeneration apply digest lower-hex SHA-256: YES
regeneration apply digest:
repeat/reverse/culture/sector-order/record-order digest mismatches: 0/0/0/0/0
mutation sensitivity probes passed:

System.IO file write/read calls: 0/0
disk save/load files created: 0/0
actual user save slot writes: 0
platform save storage writes: 0
Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
runtime objects spawned: 0
production seed approvals: 0
MAP17_07 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Baking/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP17_05_IMPLEMENT_SECTOR_MODIFICATION_STORAGE_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If an existing save manager, inventory, device, or runtime spawn component lives outside the allowed roots, read it if necessary but do not edit it. If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP17_06 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
save manifest payload and regeneration apply contract created
manifest stores seed/version/hash + modified sectors only
unmodified sectors omitted and regen-by-seed proven
no disk save/load file write/read
no population/content stable spawn ids
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP17_07 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP17_06_IMPLEMENT_SAVE_MANIFEST_REGENERATION_AND_APPLY: COMPLETE
MAP17_07_CREATE_BAKE_STREAM_SAVE_PERFORMANCE_TESTS: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP17_06: implement save manifest regeneration
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.

