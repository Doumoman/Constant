```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
  task_file: TASKS/MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS.md
  requires_current_task: NONE
  requires_completed_task: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
  requires_result:
    path: REPORTS/MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS_RESULT.md
    status: PASS
    sha256: 7089f72367eb6b0369a73c3322db8052ad689531ebc48dcd71785a1f3341413e
  requires_installed_task:
    path: TASKS/MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS.md
    sha256: ae54470791006b6e302f00f225ac92657c3e428d0d8f8088854770faca1bc2b5
  sets_current_task: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
```

# MAP09_06 — Implement SpecialRegion, Canvas, and Slice Contracts

```text
TASK: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

---

## 0. Task Responsibility

이번 Task는 세 artifact의 **데이터 책임과 불변식만** 추가한다.

| Artifact | 이번 Task가 소유하는 책임 | 소유하지 않는 기능 |
|---|---|---|
| `SpecialRegion` | 선예약 footprint, fixed shell/slot 구분, stable persistence key | 일반 Cluster 조립, 실제 시설/NPC/보스, SaveData 상태 |
| `SectorCanvas` | 검증 전후 48×32 resolved cell snapshot과 source provenance | tile 조립·cleanup·검증 실행 |
| `GeneratedSlice` | validated Canvas의 12×8×16 저장 projection과 provenance | 후보 선택, 구조 생성, Authoring 입력 |

생성 순서는 그대로다.

```text
SpecialRegion 선예약 → Cluster/Pattern/Activity/Event → 48×32 검증
→ 12×8 Generated Slice 16개
```

실제 reservation solver, Canvas composer, validator, slicer, CSV writer, streaming/save는 후속 Task다.

---

## 1. No-Regression Policy

사용자 지시에 따라 문제가 발견되지 않는 한 이전 Task/category 테스트를 실행하지 않는다.

정상 실행:

```text
MAP09_06 focused only
Prior MAP00~09_05 test selections: 0
Legacy 19347 selections: 0
```

회귀 허용 trigger:

- focused test 실패
- compile/Console error
- predecessor live digest mismatch
- 기존 MAP00~09_05 파일의 task-owned modification 발견
- Authoring manifest, asmdef hash, GUID baseline drift

trigger가 없으면 회귀 실행은 금지한다. trigger가 있으면 Result에 원인·영향 owner·선택한 최소 regression 범위를 먼저 기록하고 관련 범위만 실행한다. 원인을 국소화할 수 없을 때만 전체 회귀를 고려하며, 임의로 PASS 범위를 확대하지 않는다.

---

## 2. Preflight

변경 전 읽기 전용 확인:

1. MAP09_05 Result status/SHA와 실제 설치·Archive Task SHA가 metadata와 exact 일치
2. MAP09_06만 CURRENT, inbox candidate 0
3. MAP09_01~05 live catalog/fixture digest가 각 Result와 일치
4. SpecialRegion ownership, existing Site reservation ID, AccessClass authority API
5. `Sector=48×32`, `MicroChunk=12×8`, 기존 MAP07 MicroChunk와 MAP08 boundary projection API
6. approved `SpecialRegions`, `Baking` Runtime/Test root와 assembly
7. Authoring `50/50` manifest, asmdef hash, meta/GUID, compile/Console, dirty worktree

predecessor mismatch, type collision, 기존 계약 수정 필요, allowlist와 사용자 변경 중첩이면 `BLOCKED`다. 이때 자동 회귀하지 않는다.

---

## 3. SpecialRegion Contract

위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/
Namespace: StarNight.Map.WorldGeneration.SpecialRegions
```

최소 semantic types:

```text
SpecialRegionId
SpecialRegionKind
SpecialRegionSectorOffset
SpecialRegionFootprint
SpecialRegionLayerKind
SpecialRegionSlotId / SpecialRegionSlotKind / SpecialRegionSlot
SpecialPersistenceKey / SpecialPersistenceScope
SpecialRegionContract
SpecialRegionValidator / ValidationError / Result
```

### 3.1 Identity와 footprint

```text
SpecialRegionId:      ^SR_[A-Z0-9_]+$
Slot ID:              ^SR_SLOT_[A-Z0-9_]+$
Persistence key:      ^SR_STATE_[A-Z0-9_]+$
```

exact kinds:

```text
Village
CoreResource
Forge
Boss
OptionalLandmark
```

- 기존 World/Site reservation stable ID를 반드시 참조한다.
- footprint는 normalized, unique, 4-neighbor connected Sector offset set이다.
- 지원 footprint는 `1×1`, `2×1`, `1×2`다.
- reservation footprint와 authored footprint가 exact 일치해야 한다.
- SpecialRegion footprint는 TerrainCluster보다 먼저 예약되며 immutable하다.
- entry/return port는 explicit sector/local tile/side와 existing AccessClass를 기록한다.
- 일반 `GeneralRouteAccess` authority를 소유하지 않으며 special entry compatibility만 갖는다.

### 3.2 Fixed shell과 slots

exact layer kinds:

```text
FixedShell
ReplaceableSlot
```

exact slot kinds:

```text
Facility
Npc
Enemy
Event
Reward
Entry
Return
```

- FixedShell tile은 region 구조·collision·필수 entry/return을 나타내며 replaceable하지 않다.
- slot은 unique ID, sector offset, explicit `LocalTileCoord`, kind, persistence key를 가진다.
- 모든 slot은 footprint 안이고 FixedShell tile과 중복될 수 없다.
- Entry/Return slot은 각각 최소 1개이고 authored port와 일치한다.
- 필수 Reward/Resource slot은 persistence key 없이 publish할 수 없다.
- Facility/NPC/Enemy prefab, inventory, boss logic, 실제 tile content는 구현하지 않는다.

### 3.3 Persistence

exact scopes:

```text
Region
Slot
Reward
Encounter
```

- persistence key는 region ID와 scope/slot identity에 안정적으로 결합된다.
- 동일 region 안 key duplicate와 서로 다른 region의 key collision을 거부한다.
- 같은 `Seed + DataVersion + GeneratorVersion` 재생성에서도 key가 변하지 않는다.
- contract는 key와 초기 의미만 정의하고 runtime mutable state를 저장하지 않는다.
- Generated Slice는 key provenance를 운반할 수 있지만 state owner가 아니다.

---

## 4. SectorCanvas Contract

위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/Baking/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/
Namespace: StarNight.Map.WorldGeneration.Baking
```

최소 semantic types:

```text
SectorCanvasId
SectorCanvasCell
SectorCanvasLayerSnapshot
CanvasSourceKind / CanvasSourceRef
SectorCanvasProvenance
SectorCanvasValidationStamp
SectorCanvasContract
SectorCanvasContractValidator
```

### 4.1 Geometry와 cells

```text
Width/Height: 48/32 exact
Cell count: 1536 exact
Canonical index: y * 48 + x
Canonical order: 0..1535
```

- 모든 1×1 cell을 explicit하게 보관한다.
- duplicate/missing/out-of-range coordinate를 거부한다.
- cell은 resolved layer snapshot과 source provenance를 가지며 upstream artifact를 선택하지 않는다.

resolved layer fields:

```text
Solid
Background
Surface
Affordance
Material
Hazard
Marker
Owner
```

- payload는 stable ID 또는 explicit empty다.
- contradictory solid/air ownership, duplicate owner, protected source loss를 거부한다.
- source kind는 최소 `Boundary`, `SpecialRegion`, `TerrainCluster`, `MicroPattern`, `Activity`, `EventOverlay`, `Cleanup`을 구분한다.
- source reference는 stable ID/pass order를 보존한다.

### 4.2 Validation stamp

Canvas 상태:

```text
Unvalidated
Validated
```

Validated stamp는 최소 다음 digest를 가진다.

```text
Pass catalog
Layer catalog
Source artifact set
Resolved cells
Validation ruleset version
```

- `Unvalidated` Canvas는 Generated Slice 입력이 될 수 없다.
- 이번 Task는 stamp contract를 검증할 뿐 tile validation을 실행하거나 PASS stamp를 자체 발급하지 않는다.

---

## 5. Generated 12×8 Slice Contract

최소 semantic types:

```text
GeneratedSliceCoord
GeneratedSliceCell
GeneratedSliceProvenance
GeneratedMicroChunkSlice
GeneratedSliceSet
GeneratedSliceContractValidator
```

exact projection:

```text
Slice grid: 4×4 = 16
Each slice: 12×8 = 96 cells
Slice index: sliceY * 4 + sliceX
Canvas X: sliceX * 12 + localX
Canvas Y: sliceY * 8 + localY
```

- slice coordinates는 `0..3`, exact 16개, canonical index order다.
- 각 slice는 explicit 96 cells와 source Canvas ID/digest/stamp를 가진다.
- 16개 slice 합집합은 Canvas 1536 cells를 gap/overlap 없이 exact once 포함한다.
- cell resolved value와 provenance는 source Canvas와 byte/semantic equivalent여야 한다.
- 90도 회전, mirror, resampling, padding, slice-time mutation을 금지한다.
- SpecialRegion persistence key와 boundary provenance를 손실하지 않는다.
- 기존 MAP07 fixed MicroChunk authoring type을 수정·복제하지 않는다.
- Generated Slice는 저장/streaming/validation 출력이며 Authoring source로 역승격할 수 없다.

이번 Task는 slice model과 mapping validator만 구현한다. 실제 Canvas 절단은 MAP16, Tilemap bake/streaming/save는 MAP17이다.

---

## 6. Immutability, Digest, Errors

- 모든 collection defensive copy/read-only
- invalid input partial publish/digest/RNG/file/Unity lifecycle 사용 0
- errors accumulated/stable-sorted/deduplicated
- digest는 semantic IDs, footprint/slots/keys, all cells/layers/sources, stamp, slice mapping/provenance 포함
- display text, locale, timestamp, input/file/reflection order 제외

최소 error groups:

```text
InvalidId | MissingReservation | FootprintMismatch | InvalidFootprint
InvalidPort | InvalidFixedShell | InvalidSlot | SlotShellOverlap
MissingPersistenceKey | DuplicatePersistenceKey
InvalidCanvasDimensions | InvalidCanvasCell | MissingOrDuplicateCanvasCell
InvalidLayerSnapshot | InvalidSourceRef | ProtectedSourceLost
InvalidValidationStamp | UnvalidatedSliceSource
InvalidSliceCount | InvalidSliceCoord | InvalidSliceCellCount
SliceGapOrOverlap | SliceMappingMismatch | ProvenanceMismatch
ForbiddenSliceTransform | AuthoringGeneratedBoundaryViolation
```

---

## 7. 변경 경계

허용:

- `SpecialRegions/`, `Baking/` 신규 Runtime C#/meta
- 대응 두 EditMode test root 신규 C#/meta
- Result, 설치/Archive Task, Finalize Status

금지:

- MAP00~09_05 existing production/test 수정
- 다른 V2 root 수정
- solver/composer/tile validator/slicer/CSV writer/streaming/save 구현
- 실제 Special content, prefab, Scene, SO, Editor Window 생성
- Authoring/Generated CSV·meta 변경/생성
- asmdef/Settings/Packages 변경
- 기존 MicroChunk/RouteType/Access/Pacing authority 재정의
- 문제 trigger 없는 prior test/category 실행
- unrelated dirty path stage/commit

신규 Runtime scope 금지:

```text
UnityEditor
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
```

---

## 8. Focused Validation and Static Gate

Focused category `MAP09_06`만 실행한다.

1. Special footprint/reservation/port validation
2. FixedShell/slot 분리와 persistence key 안정성
3. 48×32/1536 explicit Canvas cells
4. layer/source/protected provenance validation
5. validation stamp와 unvalidated slice rejection
6. exact 4×4 slices, 각 12×8/96 cells
7. 1536-cell exact-once mapping
8. no transform/mutation/provenance loss
9. Authoring→Generated 역방향 금지
10. immutable collections와 deterministic digest
11. focused negative errors accumulated/sorted/deduped
12. no RNG/file/Unity lifecycle/forbidden symbol

Result에 반드시 기록:

```text
MAP09_06 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(reason)
PRIOR TASK TEST SELECTIONS: 0 (정상 경로)
LEGACY TEST SELECTIONS: 0 (정상 경로)
```

Static/Unity:

```text
compile/Console/relevant warning: 0/0/0
Authoring CSV/meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring/Generated task changes: 0/0
Scene/Prefab/Settings/Packages/asmdef task changes: 0
existing MAP00~09_05 modifications: 0
other V2 root changes: 0
duplicate GUID/unapplied candidate/diff-check errors: 0/0/0
unrelated staged/included: 0
```

---

## 9. Result Responsibility Report

Result:

```text
MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS_RESULT.md
```

상단:

```text
TASK: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
STATUS: PASS | FAIL | BLOCKED
MAP09_06: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES: LOCKED / DO NOT START
```

Result의 첫 구현 섹션은 반드시 `Responsibility and Added Functions`이며 다음 표를 실제 구현 기준으로 채운다.

| 필드 | 필수 보고 내용 |
|---|---|
| Task responsibility | 이번 Task가 책임지는 artifact와 경계 |
| Added functions | 새 타입·validator·digest가 실제 제공하는 기능 |
| Inputs consumed | 재사용한 기존 ID/contract/digest |
| Outputs produced | 후속 Task가 소비할 immutable artifact |
| Explicit non-ownership | 구현하지 않았고 다른 계층이 책임지는 기능 |
| Downstream consumers | MAP09_07, MAP13, MAP16, MAP17 중 실제 소비 대상 |

그 뒤 다음을 보고한다.

1. predecessor/Status/dirty preflight
2. 두 root의 신규 파일 inventory
3. Special/Canvas/Slice contract와 digest 증거
4. focused tests와 regression trigger/selection 수
5. Unity/static/change scope/out-of-scope
6. atomic commit subject, `SELF`, CLI 실제 hash handoff

PASS/finalize 뒤 task-owned 파일만 commit한다.

```text
Subject: MAP09_06: implement special canvas and slice contracts
Push: NOT PERFORMED
```

설치/Archive Task, 신규 Runtime/Test/meta, Result, Finalize Status만 포함한다. 실패 시 같은 MAP09_06 repair만 보고하고 MAP09_07을 자동 시작하지 않는다.
