```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT
  task_file: TASKS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT.md
  requires_current_task: NONE
  requires_completed_task: MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS
  requires_result:
    path: REPORTS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS_RESULT.md
    status: PASS
    sha256: 18ce7c28e876d40e9c40c2e89e2dd984e315cb84c1e979374036375ab303452b
  requires_installed_task:
    path: TASKS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS.md
    sha256: d678721d2cfc42b809ccc36335e84657a7312d8dddacbe2233c0b7ba1a28b211
  sets_current_task: MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT
```

# MAP18_02 - Place Mandatory and Unique Content

```text
TASK: MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT
PHASE: MAP18 - Population / Content Placement / Runtime State Preparation
STATUS: CURRENT
NEXT: MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP18_01의 content slot index와 stable spawn ID 위에, 필수 진행 trigger와 세 CoreResource를 **logical preplacement plan**으로 선배치한다.

이번 Task의 책임은 다음 네 가지다.

```text
1. required trigger 1개와 MoonCore/CassiaSap/StarNuruk 3개를 world-unique content key로 정의한다.
2. MAP18_01 slot index의 mandatory/unique candidate 중 stable order로 적합한 slot을 선택한다.
3. 각 선택을 stable spawn ID + reservation key + max count rule이 결합된 immutable preplacement entry로 만든다.
4. MAP18_03 이후 shop/resource/hazard/enemy population이 이미 예약된 필수/unique slot을 침범하지 않도록 exclusion surface를 제공한다.
```

이번 Task에서 말하는 "place"는 Scene이나 Tilemap에 실제 오브젝트를 놓는 뜻이 아니다.  
결과물은 순수 데이터 plan이며, runtime spawn, reward grant, inventory mutation, device execution, visual/gameplay object creation은 아직 하지 않는다.

금지:

```text
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
actual enemy/resource/shop/hazard/item runtime spawn
actual reward grant
inventory mutation
device execution
weighted random roll
budget spending beyond declared reservation counts
disk save/load file write/read
actual user save slot management
CSV authoring edits
Generated CSV commits
production seed approval
MAP18_03 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
logical preplacement가 실제 runtime spawn이나 reward 지급과 어떻게 다른지
required trigger와 MoonCore/CassiaSap/StarNuruk를 어떤 slot에 예약했는지
world-unique/max count와 reservation key가 무엇을 보장하는지
MAP18_03에 넘기는 exclusion/consumer surface
중복 코드나 하드코딩 후보를 발견했는지
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

또한 Result에는 아래 섹션을 포함한다.

```text
## Mandatory Unique Placement Summary
## Runtime Spawn Boundary and Risk Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_01 Result exists
MAP18_01 Result STATUS: PASS
MAP18_01 Result SHA-256:
18ce7c28e876d40e9c40c2e89e2dd984e315cb84c1e979374036375ab303452b

MAP18_01 installed task SHA-256:
d678721d2cfc42b809ccc36335e84657a7312d8dddacbe2233c0b7ba1a28b211

MAP18_01 slot index digest:
889c25815c9d0bffe6c6ea785b66c55e79f0e8e93631771f0ec30a0b39c2b6bd

MAP18_01 stable id set digest:
bfc341e0c62a62d8846580b9455874df9e30573bd4c5f6cc450d719c89464b8a

MAP18_01 slot source records:
12

MAP18_01 mandatory/unique candidate count:
5

MAP17_08 WARN risks:
carried forward, do not block MAP18_02

Current Task before apply: NONE
MAP18_01: COMPLETE
MAP18_02: LOCKED before apply
MAP18_03: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 MAP13/MAP18 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
CoreResourceRegionStarterCatalog
CoreResourceRegionDefinition
CoreResourceRegionPlan
SpecialPersistenceKey
GeneratedContentSlotCategory
GeneratedContentPoolKey
GeneratedContentSlotAddress
GeneratedContentSlotIndexEntry
GeneratedContentSlotIndex
GeneratedStableSpawnId
GeneratedStableSpawnIdFactory
GeneratedContentSlotIndexBuilder
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP13/MAP18_01 기존 파일을 대규모 변경하지 않는다.

기준 수량:

```text
required trigger preplacements: 1
core resource preplacements: 3
mandatory unique preplacements total: 4
core resources: MoonCore, CassiaSap, StarNuruk
MAP18_01 slot source records: 12
MAP18_01 mandatory/unique candidates: 5
minimum required candidates: 4
world unique max count per required key: 1
```

MAP18_01의 candidate가 4개 미만이거나 세 CoreResource key를 authoritative catalog에서 확인할 수 없으면, 임의 slot이나 임의 key를 만들지 말고 `BLOCKED`로 멈춘다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedMandatoryContentKey` | required trigger와 세 CoreResource의 unique content key를 정의한다. |
| `GeneratedMandatoryUniqueRule` | required/exactly-one/world-unique/max-count/exclusion policy를 정의한다. |
| `GeneratedMandatoryUniquePlacementEntry` | content key, selected slot entry, stable spawn ID, reservation key, source proof를 담는다. |
| `GeneratedMandatoryUniquePlacementPlan` | 네 필수 placement entry와 remaining candidate/exclusion lookup/digest를 안정 정렬로 묶는다. |
| `GeneratedMandatoryUniquePlacementFailure` | missing candidate, duplicate unique key, max count 초과, reservation collision, source mismatch를 deterministic하게 보고한다. |
| `GeneratedMandatoryUniqueContentPreplacer` | MAP18_01 slot index와 MAP13 CoreResource catalog를 검증한 뒤 pure-data placement plan을 만든다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedMandatoryUniquePlacement.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Population/GeneratedMandatoryUniqueContentPreplacer.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/GeneratedMandatoryUniquePlacementTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Placement 규칙

### 5.1 Required content keys

다음 logical content key를 게시한다.

```text
RequiredProgressTrigger
MoonCore
CassiaSap
StarNuruk
```

필수:

```text
all four keys are required
each key has max world count 1
each key has exactly one selected slot
each key has one stable spawn ID
each key has one reservation key
CoreResource keys match MAP13_06 authoritative identities
short/legacy persistence keys are not accepted
```

CoreResource reward key는 MAP13_06 repair 이후 정본인 `SpecialPersistenceKey.ForSlot(regionId, Reward, slotId)` 계열 authoritative key와 연결한다. 예전 short key 문자열을 alias나 fallback으로 허용하지 않는다.

### 5.2 Candidate selection

Candidate selection은 stable order 기반 deterministic selection이다.

필수:

```text
consume MAP18_01 mandatory/unique candidate query
minimum candidates required: 4
stable sort by content-specific suitability, sector, slice, local index, source owner, source slot, pool key
no input list order dependency
no random roll
no retry loop
no implicit slot creation
no candidate mutation
```

Slot이 부족하거나 특정 required key를 만족하는 candidate가 없으면 partial plan을 만들지 않고 atomic failure를 반환한다.

### 5.3 Reservation and exclusion

Preplacement entry는 reservation key를 점유했다고 표시하는 pure-data plan이다. 실제 runtime reserve/commit 상태나 GameObject는 만들지 않는다.

필수:

```text
unique reservation keys: 4/4
duplicate reservation rejected
same physical slot double-use rejected unless explicitly allowed by rule; default is rejected
MAP18_03 exclusion lookup exposes reserved keys
world unique max count violations rejected
stable spawn ID duplicates rejected
remaining candidate count reported
```

### 5.4 Digest

`BakingCanonicalDigest`를 사용해 plan digest와 placement ID set digest를 만든다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable candidate order
mutation sensitivity
```

Digest material에는 machine path, timestamp, frame count, Unity object instance ID를 넣지 않는다.

### 5.5 Failure policy

다음은 atomic failure다.

```text
missing MAP18_01 slot index digest
slot index digest mismatch
stable id set digest mismatch
mandatory/unique candidate count below 4
missing RequiredProgressTrigger candidate
missing MoonCore candidate
missing CassiaSap candidate
missing StarNuruk candidate
CoreResource authoritative identity mismatch
legacy short persistence key accepted
duplicate unique content key
max world count exceeded
reservation key collision
stable spawn ID collision
attempted pool roll or runtime spawn
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
shop inventory generation
non-mandatory resource node population
hazard/enemy placement
activity/event runtime state instantiation
special region state export
weighted random roll
gameplay reward grant
inventory mutation
device execution
actual runtime spawn
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
Scene mutation
Prefab mutation
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
System.IO file write/read for save data
disk save/load file creation
actual user save slot management
platform save storage
CSV authoring edits
Generated CSV commits
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
production seed approval
MAP18_03 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP18_02`만 선택한다.

```text
MAP18_02 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18_01 selections: 0
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
MandatoryUniquePreplacementCreatesRequiredTriggerAndThreeCoreResources
PreplacementUsesSlotIndexStableIdsAndReservationKeysWithoutPoolRolls
CoreResourceKeysMatchMap13AuthoritativeRewardDefinitions
WorldUniqueAndMaxCountRulesRejectDuplicatesAtomically
PreplacementIsStableAcrossRepeatReverseCultureAndCandidateOrder
SelectionUsesStableSlotOrderAndDoesNotInventSlots
MissingCandidateDigestMismatchAndReservationCollisionFailAtomically
PreplacementDoesNotSpawnObjectsMutateScenesWriteSavesOrLoadAssets
PreplacementReportsExclusionSurfaceForMap18_03
Map18HandoffKeepsMap18_03Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP18_02]
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
MAP18_01 slot index digest reused:
MAP18_01 stable id set digest reused:
MAP18_01 slot source records reused:
MAP18_01 mandatory/unique candidate count reused:

required content keys published:
required trigger placements:
core resource placements:
MoonCore/CassiaSap/StarNuruk placements:
mandatory unique placement entries:
unique content keys:
unique stable spawn IDs:
unique reservation keys:
world unique max count rules:
remaining unreserved candidate count:
MAP18_03 exclusion entries:

CoreResource authoritative identity checks:
legacy short persistence keys accepted: 0
candidate selection uses stable order: YES
input order dependency detected: NO
random roll count:
retry loop count:
implicit slot creation count:
candidate mutation count:

placement plan digest lower-hex SHA-256: YES
placement plan digest:
placement stable id set digest lower-hex SHA-256: YES
placement stable id set digest:
repeat/reverse/culture/candidate-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing candidate failure probes:
digest mismatch failure probes:
duplicate unique key failure probes:
max count exceeded failure probes:
reservation collision failure probes:
stable spawn ID collision failure probes:
atomic failure partial placement entries:

logical preplacement entries created:
runtime content placements performed: 0
weighted pool rolls performed: 0
budget spends performed: 0
reward grants performed: 0
inventory mutations performed: 0
device executions performed: 0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
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
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Authoring CSV edits: 0
Generated CSV/assets committed: 0/0
production seed approvals: 0
MAP18_03 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Population/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Population/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_01_BUILD_SLOT_INDEX_AND_STABLE_SPAWN_IDS_RESULT.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP18_02 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes mandatory unique placement summary
Result includes runtime spawn boundary and risk notes
required trigger and three CoreResources are logically preplaced
world unique and max count rules are enforced
reservation/exclusion surface for MAP18_03 is created
CoreResource identities use MAP13 authoritative keys, not legacy short keys
selection is deterministic and does not invent slots
stable IDs and reservation keys are unique
no runtime spawn, reward grant, inventory mutation, device execution, or pool roll
no actual disk save/load file write/read
no Unity Tilemap/Collider/Rigidbody/Physics2D/GameObject/Camera/asset-load work
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
no Scene/Prefab/Tilemap mutation
MAP18_03 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP18_02_PLACE_MANDATORY_AND_UNIQUE_CONTENT: COMPLETE
MAP18_03_POPULATE_SHOPS_RESOURCES_AND_MAP_ELEMENTS: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP18_02: place mandatory and unique content
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
