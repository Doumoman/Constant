```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
  task_file: TASKS/MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS.md
  requires_current_task: NONE
  requires_completed_task: MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS
  requires_result:
    path: REPORTS/MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS_RESULT.md
    status: PASS
    sha256: 58098e69a185779404bc30163ccf31f1bf9fcc0582f938eb97d4061ac651937a
  requires_installed_task:
    path: TASKS/MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS.md
    sha256: f2a3e11a802da1faca5c5e0205ce5061596df68cb6d6327fc851a26a8e09c7c3
  sets_current_task: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
```

# MAP09_05 — Implement Activity and Event Contracts

```text
TASK: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

---

## 0. 목적과 범위

강한 플레이 사건인 `ActivityStructure`와 런별 marker 변형인 `EventOverlay`를 서로 다른 immutable 계약으로 구현한다.

```text
ActivityStructure = static shell 위의 Cue/Activation/Core/Reward/Recovery 사건
EventOverlay       = collision shell을 바꾸지 않는 NPC/Reward/State marker 변형
```

이번 Task는 계약과 validator만 구현한다. 실제 prefab, tile mutation, 배치 solver, 빈도/cap, cooldown, RNG, CSV, 실행 상태 머신은 만들지 않는다.

---

## 1. Preflight

변경 전 확인·보고:

1. MAP09_04 Result/설치 Task/Archive 상태·SHA exact 일치
2. MAP09_05만 CURRENT, inbox candidate 0
3. MAP09_01~04 live digest와 Result 일치
4. MAP09_02 layer ownership/access/pacing mode
5. MAP09_04 TraversalGraph·TerrainCluster·protected envelope API
6. approved `Activities`, `EventOverlays` Runtime/Test roots와 assembly
7. Authoring `50/50` manifest, meta/GUID, compile/Console, dirty worktree

predecessor mismatch, 기존 Traversal/Access authority 수정 필요, type collision, allowlist와 사용자 변경 중첩이면 `BLOCKED`다.

기존 MCP_INBOX/Archive 대량 dirty state는 읽기 전용으로 보존하고 stage하지 않는다.

---

## 2. ActivityStructure Contract

위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/Activities/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/Activities/
Namespace: StarNight.Map.WorldGeneration.Activities
```

최소 semantic types:

```text
ActivityStructureId
ActivitySlotId / ActivitySlotKind / ActivitySlot
ActivityCueKind / ActivityCue
MechanismNodeKind / MechanismRelationKind / MechanismGraph
ProgressionPhaseKind / ProgressionEdgeKind / ProgressionGraph
ActivityRemovalSafety
ActivityStructureContract
ActivityContractValidator / ValidationError / Result
```

### 2.1 ID와 static shell reference

```text
ActivityStructureId: ^ACT_[A-Z0-9_]+$
Slot ID:             ^SLOT_[A-Z0-9_]+$
Mechanism node:      ^MECH_[A-Z0-9_]+$
Progression node:    ^PROG_[A-Z0-9_]+$
```

- Activity는 existing `TerrainClusterId`와 compatible `SpineVariantId`를 참조한다.
- static collision shell, TraversalGraph, Entry/Exit, Envelope ownership은 TerrainCluster에 남는다.
- Activity 제거 상태에서도 referenced baseline SpineVariant의 Entry→Exit path와 AccessClass가 유지돼야 한다.
- Activity는 RouteType/AccessClass/PacingRole을 배정하지 않고 compatibility만 선언한다.

### 2.2 Slots와 cues

exact slot kinds:

```text
Cue
Trigger
Device
Hazard
Projectile
Reward
Recovery
Reset
Npc
```

exact cue kinds:

```text
Visual
Audio
Environment
Motion
```

- slot은 unique ID, explicit `LocalTileCoord`, kind를 가진다.
- 모든 slot은 referenced active footprint 안에 있어야 한다.
- Cue/Trigger/Recovery slot은 각각 최소 1개다.
- Cue는 activation 전에 감지 가능해야 하며 Cue slot을 참조한다.
- cue collection은 non-empty이고 동일 `(kind,slot)` duplicate를 거부한다.
- slot은 prefab/asset reference가 아니라 authoring marker다.

### 2.3 MechanismGraph

MechanismGraph는 장치 작동 관계만 표현한다.

exact node kinds:

```text
CueEmitter
Trigger
Device
Hazard
ProjectileEmitter
RewardEmitter
RecoveryController
ResetController
```

exact relations:

```text
Activates
Drives
Emits
Enables
Disables
Resets
```

- node/edge ID unique, reference 존재, self edge 금지
- node는 compatible slot을 참조한다.
- exact one Trigger-reachable mechanism component가 있어야 한다.
- Reward/Recovery/Reset node가 있으면 Trigger에서 도달 가능해야 한다.
- player Walk/Jump/... edge나 progression phase를 넣지 않는다.
- device 실행, projectile trajectory, timing, physics는 구현하지 않는다.

### 2.4 ProgressionGraph

exact phases:

```text
Cue
Activation
Core
Reward
Recovery
Reset
Exit
```

exact edge kinds:

```text
Advance
Failure
Reset
Exit
```

규칙:

- Cue/Activation/Core/Reward/Recovery/Exit phase는 각각 최소 1개다.
- start phase는 Cue, terminal phase는 Exit다.
- 최소 한 경로가 `Cue→Activation→Core→Reward→Recovery→Exit` 순서를 보존한다.
- Failure edge는 Recovery 또는 Reset으로만 간다.
- Reset edge는 Activation 또는 Core로만 돌아간다.
- Exit에서 outgoing edge 금지
- infinite cycle만 있고 Recovery/Exit로 나갈 수 없는 graph 거부
- Traversal movement나 mechanism device 관계를 포함하지 않는다.

### 2.5 Removal safety

`ActivityRemovalSafety`는 최소 다음을 명시한다.

```text
Referenced baseline SpineVariant
Entry/Exit traversal node
SafePocket tile set
Recovery tile set
PreserveStaticTraversal = true
PreserveAccessClass = true
PermanentSolidMutationAllowed = false
MandatoryExitDestructionAllowed = false
```

- SafePocket과 Recovery set은 non-empty, unique, active footprint 안이다.
- TerrainCluster protected envelope와 충돌하는 permanent write 선언은 금지한다.
- Activity 제거 전후 RouteType, AccessClass, TraversalGraph digest는 같아야 한다.
- 이 Task는 graph/digest reference를 검증하며 실제 prefab 제거·physics playthrough는 MAP12에서 수행한다.

---

## 3. EventOverlay Contract

위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/EventOverlays/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/EventOverlays/
Namespace: StarNight.Map.WorldGeneration.EventOverlays
```

최소 semantic types:

```text
EventOverlayId
EventMarkerId
EventOverlayKind
EventMarkerOperation
EventMarkerAssignment
EventOverlayContract
EventOverlayValidator / ValidationError / Result
```

ID:

```text
EventOverlayId: ^EVT_[A-Z0-9_]+$
EventMarkerId:  ^MARKER_[A-Z0-9_]+$
```

exact overlay kinds:

```text
Npc
Reward
State
Cosmetic
Empty
```

exact marker operations:

```text
EnableMarker
DisableMarker
SpawnNpc
SpawnReward
SetState
```

규칙:

- assignment는 existing TerrainCluster/Activity marker ID와 explicit operation/payload를 참조한다.
- `Empty` variant만 assignment `0`을 허용한다.
- non-empty overlay는 assignment `1+`, unique target marker를 가진다.
- operation과 kind가 호환돼야 하며 payload ID는 stable token이다.
- collision/solid/background/RouteType/AccessClass/Pacing/TraversalGraph/Envelope mutation 필드는 존재할 수 없다.
- MechanismGraph와 ProgressionGraph를 소유하지 않는다.
- Event 제거 전후 static shell, mandatory path, access, Activity removal safety가 동일해야 한다.
- 빈도 `3~8%`, cooldown, cap, candidate 선택, 별도 RNG stream은 MAP12에서 구현한다.

---

## 4. Graph Separation, Immutability, Digest

exact ownership:

```text
TraversalGraph  → TerrainCluster (MAP09_04)
MechanismGraph  → ActivityStructure
ProgressionGraph → ActivityStructure
EventOverlay    → marker assignments only
```

다른 graph kind의 node/edge를 섞거나 같은 ID를 cross-graph reference로 오인하면 validation failure다.

- 모든 collection defensive copy/read-only
- errors accumulated/stable-sorted/deduplicated
- invalid input partial publish/digest/RNG/file/Unity lifecycle 사용 0
- Activity digest: shell refs, compatibility, slots, cues, 두 graph, removal safety
- Event digest: kind, referenced shell/activity, canonical marker assignments
- display text, timestamp, locale, input/reflection/file order 제외

최소 error groups:

```text
InvalidId | InvalidShellReference | InvalidSlot | MissingCueOrTrigger
InvalidCue | InvalidGraphKind | DuplicateNodeOrEdge | MissingReference
InvalidMechanismRelation | UnreachableMechanismNode
MissingProgressionPhase | InvalidProgressionOrder | InvalidFailureOrReset
NoRecoveryOrExit | InvalidRemovalSafety | ProtectedMutation
InvalidOverlayKind | InvalidMarker | InvalidMarkerOperation
NonMarkerMutation | NonEmptyWithoutAssignment | EmptyWithAssignment
```

---

## 5. 변경 경계

허용:

- `Activities/`, `EventOverlays/` 신규 Runtime C#/meta
- 대응 두 EditMode test root 신규 C#/meta
- Result, 설치/Archive Task, Finalize Status

금지:

- MAP00~09_04 existing production/test 수정
- TerrainClusters/Pipeline/MicroPatterns 등 다른 V2 root 수정
- actual state machine/prefab/physics/projectile/tile mutation 실행
- CSV/Authoring/Generated/Scene/Prefab/SO/Editor/asmdef/Settings/Packages 변경
- placement/frequency/cap/cooldown/RNG 구현
- SpecialRegion content 선행 구현
- RouteType/Access/Pacing/Traversal authority 재정의
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

## 6. 필수 검증

Focused `MAP09_05`:

1. valid Activity shell/slot/cue contract
2. Trigger-reachable MechanismGraph와 kind/relation 분리
3. required Progression phases와 ordered success path
4. Failure/Reset/Exit 및 infinite-loop negative cases
5. removal-safe flags, safe pocket, recovery, protected-write rejection
6. Activity 제거 전후 traversal/access identity
7. valid marker-only EventOverlay와 explicit Empty
8. kind/operation/payload matrix
9. Event의 collision/route/access/graph mutation rejection
10. graph ownership 분리
11. immutable collections와 deterministic digest
12. no RNG/file/Unity lifecycle/forbidden symbol

최종 회귀:

```text
MAP09_05 focused: >0, all PASS
MAP09_04: 71/71 PASS
MAP09_03: 62/62 PASS
MAP09_02: 38/38 PASS
MAP09_01: 26/26 PASS

MAP08: 9220/9220 PASS
MAP07: 5422/5422 PASS
MAP06: 2746/2746 PASS
MAP05: 1959/1959 PASS
Distinct: 19347/19347 PASS
```

각 selection의 discovered/executed/pass/fail/skip을 분리 보고한다. timeout, zero-selection, prior replay는 PASS가 아니다.

Static/Unity:

```text
compile/Console/relevant warning: 0/0/0
Authoring CSV/meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring/Generated/Scene/Prefab/Settings/Packages/asmdef task changes: 0
existing MAP00~09_04 modifications: 0
other V2 root changes: 0
duplicate GUID/unapplied candidate/diff-check errors: 0/0/0
unrelated staged/included: 0
```

---

## 7. Result, Commit, Stop

Result:

```text
MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS_RESULT.md
```

상단:

```text
TASK: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
STATUS: PASS | FAIL | BLOCKED
MAP09_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_06_IMPLEMENT_SPECIAL_CANVAS_AND_SLICE_CONTRACTS: LOCKED / DO NOT START
```

필수 보고:

1. predecessor/Status/dirty preflight
2. 두 root의 신규 파일 inventory
3. Activity shell/cue/Mechanism/Progression/removal 계약과 digest
4. Event marker-only/empty 계약과 digest
5. focused, MAP09, required `19347` 회귀
6. Unity/static/change-scope/out-of-scope
7. atomic commit subject, `SELF`, CLI 실제 hash handoff

PASS/finalize 뒤 task-owned 파일만 commit한다.

```text
Subject: MAP09_05: implement activity and event contracts
Push: NOT PERFORMED
```

설치/Archive Task, 신규 Runtime/Test/meta, Result, Finalize Status만 포함한다. 실패 시 같은 MAP09_05 repair만 보고하고 MAP09_06을 자동 시작하지 않는다.
