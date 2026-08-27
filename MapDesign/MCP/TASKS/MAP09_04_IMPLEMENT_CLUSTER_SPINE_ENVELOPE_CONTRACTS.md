```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS
  task_file: TASKS/MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS.md
  requires_current_task: NONE
  requires_completed_task: MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS
  requires_result:
    path: REPORTS/MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS_RESULT.md
    status: PASS
    sha256: b75ad30b3d322223d939437654bbd098629c1fe4b7c49e06ed170626eeb25174
  requires_installed_task:
    path: TASKS/MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS.md
    sha256: 1b137570f8ccb9c3970dfe6fc4400de1a2268f3a4e9ebcd4d9ed1a8870e2cd74
  sets_current_task: MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS
```

# MAP09_04 — Implement Cluster, Spine, and Envelope Contracts

```text
TASK: MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

---

## 0. 목적과 경계

`TerrainCluster`의 footprint·role anchor·entry/exit와 플레이어 이동용 Route Spine/Traversal Envelope를 하나의 immutable authoring contract로 정의한다.

이 Task는 **계약과 validator만 구현**한다.

```text
포함: footprint, role, port, SpineVariant, MovementKind, protected envelope
제외: tile 생성, graph compiler, pathfinding, physics simulation, renderer, CSV, RNG
```

PDF의 초록·주황 설명선은 좌표 데이터로 가져오지 않는다. 플레이어 이동은 `TraversalGraph`로만 표현하며 장치 작동과 상태 진행을 섞지 않는다.

---

## 1. Preflight

변경 전 확인하고 Result에 기록한다.

1. MAP09_03 Result/설치 Task/Archive 상태·SHA exact 일치
2. MAP09_04만 CURRENT, inbox candidate 0
3. MAP09_01/02/03 live digest와 Result 일치
4. 기존 `RouteType` 0~4, `AccessClass`, `PacingRole`, `LocalTileCoord` API
5. existing 12×8 MicroChunk constants와 4×4 MicroPattern ownership
6. approved `TerrainClusters` Runtime/Test root·namespace·assembly
7. Authoring `50/50` manifest, meta/GUID, compile/Console, dirty worktree

다음이면 `BLOCKED`다.

- predecessor mismatch 또는 MAP09_03 미완료
- 기존 RouteType/Access authority 수정 없이는 구현 불가
- 기존 TerrainCluster production type이 다른 의미로 충돌
- allowlist와 사용자 변경이 겹침

기존 MCP_INBOX/Archive 대량 dirty state는 보존하고 stage하지 않는다.

---

## 2. TerrainCluster Footprint Contract

위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/
Namespace: StarNight.Map.WorldGeneration.TerrainClusters
```

최소 semantic types:

```text
TerrainClusterId
ClusterChunkCoord
ClusterFootprint
ClusterRoleKind / ClusterRoleAnchor
ClusterPortKind / ClusterPort
TraversalGraphKind
TraversalMovementKind
TraversalNode / TraversalEdge
TraversalEnvelope
SpineVariantId / SpineVariant
TerrainClusterTraversalContract
TerrainClusterContractValidator
ValidationError / Result
```

### 2.1 ID와 footprint

```text
TerrainClusterId: ^TC_[A-Z0-9_]+$
SpineVariantId:   ^SPINE_[A-Z0-9_]+$
Traversal node:   ^NODE_[A-Z0-9_]+$
Traversal edge:   ^EDGE_[A-Z0-9_]+$
MicroChunk cell: 12×8 exact
```

- footprint는 normalized local MicroChunk 좌표의 explicit active-cell set이다.
- active cell은 unique, nonnegative, canonical `(y,x)` order이며 4-neighbor connected다.
- standard TerrainCluster는 active chunk `2..5`다.
- `6`은 caller가 제공한 exact `TerrainClusterId` allowlist에 있을 때만 허용한다.
- `1`, `7+`, disconnected, hole을 연결로 위장한 diagonal-only footprint는 거부한다.
- footprint는 12×8 저장 청크를 **참조**하지만 MicroChunk 내용을 저작하거나 독립 추첨하지 않는다.

### 2.2 Role anchors

exact roles:

```text
Entry
BuildUp
Core
Recovery
Reward
Exit
```

- `Entry`, `BuildUp`, `Core`, `Recovery`, `Exit`는 각각 최소 1개다.
- `Reward`는 0개 이상이며 필수 경로 조건이 아니다.
- 모든 anchor는 unique stable ID와 explicit `LocalTileCoord`를 가진다.
- anchor tile은 active footprint가 소유한 12×8 영역 안에 있어야 한다.
- Entry와 Exit anchor는 서로 달라야 한다.
- 역할은 graph node와 연결되지만 mechanism trigger나 progression state가 아니다.

### 2.3 Entry/Exit ports

- exact one primary Entry port와 exact one primary Exit port를 둔다.
- port는 role anchor, local tile, 외향 side `L/R/U/D`, compatible existing integer RouteType set을 기록한다.
- port tile은 active footprint의 외곽이어야 하고 side는 footprint 바깥을 향해야 한다.
- RouteType과 `GeneralRouteAccess` authority는 기존 MAP09_02 계약에 남는다.
- Cluster는 RouteType/AccessClass를 배정하지 않고 compatibility만 선언한다.
- duplicate RouteType enum/codec/socket authority를 만들지 않는다.

---

## 3. Graph Separation and SpineVariant

exact graph kinds:

```text
Traversal
Mechanism
Progression
```

- 이번 Task는 `Traversal` graph만 구현한다.
- `Mechanism`은 장치·투사체·기어 작동 관계, `Progression`은 Trigger→Reward→Reset→Exit 상태 관계로 예약한다.
- Mechanism/Progression node 또는 edge를 TraversalGraph에 넣으면 validation failure다.
- 실제 Mechanism/Progression contract는 MAP09_05에서 연다.

`TerrainClusterTraversalContract`는 SpineVariant를 `1+`개 가질 수 있다.

- variant ID unique
- exact one baseline variant
- 같은 footprint에 여러 variant 허용
- variant 선택/RNG/가중치는 후속 Task
- 모든 variant가 primary Entry→Exit mandatory traversal path를 가져야 함
- 재질·장식만 다른 것은 별도 SpineVariant로 계산하지 않음

### 3.1 Traversal graph

exact movement kinds:

```text
Walk
Jump
Drop
Climb
Slide
Bounce
```

각 edge는 최소 다음 immutable 데이터를 가진다.

```text
Edge ID
From/To node ID
MovementKind
Start/End LocalTileCoord
Minimum clearance width/height
Landing LocalTileCoord
Recovery LocalTileCoord
Mandatory flag
TraversalEnvelope
```

- node/edge ID는 unique하고 모든 reference가 존재해야 한다.
- self edge, undefined movement, out-of-footprint tile을 거부한다.
- edge Start/End는 From/To node tile과 exact 일치한다.
- clearance width/height는 각각 `>=1`이다.
- landing/recovery는 explicit하며 active footprint 안에 있어야 한다.
- `Jump`, `Drop`, `Bounce`는 landing과 recovery가 필수다.
- `Slide`는 recovery가 필수다.
- Walk/Climb도 명시적 end landing과 safe recovery anchor를 기록한다.

graph validator는 tile physics를 추측하지 않고 directed graph 기준으로 다음만 증명한다.

```text
primary Entry → primary Exit mandatory path 존재
Entry에서 BuildUp/Core/Recovery/Exit 역할 도달 가능
Reward는 선택 경로일 수 있음
모든 mandatory edge/node가 Entry에서 도달 가능
orphan mandatory component 0
```

---

## 4. Traversal Envelope Contract

각 edge envelope는 다음 protected tile set을 immutable하게 구분한다.

```text
Centerline
Floor
Clearance
JumpArc
DropColumn
Landing
Recovery
```

공통 규칙:

- 각 set은 unique `LocalTileCoord`, canonical order, active footprint bounds 안이다.
- Centerline은 non-empty이고 Start/End를 포함한다.
- Clearance는 non-empty이며 centerline의 이동 공간을 보호한다.
- Landing set은 edge의 explicit landing tile을 포함한다.
- Recovery set은 explicit recovery tile을 포함한다.
- Floor는 고체 지지 후보이며 Clearance와 같은 tile을 동시에 소유할 수 없다.

movement별 규칙:

| Movement | 추가 필수/금지 |
|---|---|
| `Walk` | Floor·Clearance 필수; JumpArc/DropColumn 비어 있음 |
| `Jump` | JumpArc·Landing·Recovery 필수; DropColumn 비어 있음 |
| `Drop` | DropColumn·Landing·Recovery 필수; JumpArc 비어 있음 |
| `Climb` | Clearance 필수; JumpArc/DropColumn 비어 있음 |
| `Slide` | Floor·Clearance·Recovery 필수; JumpArc/DropColumn 비어 있음 |
| `Bounce` | JumpArc·Landing·Recovery 필수; DropColumn 비어 있음 |

모든 envelope set의 합집합은 후속 MicroPattern보다 우선하는 protected source다. MAP09_03의 `ForceNoChange`/`RejectCandidate` 외에 protected write 허용 정책을 추가하지 않는다.

이번 Task는 envelope를 저장·검증할 뿐 movement segment에서 set을 자동 계산하거나 실제 충돌/점프 가능성을 판정하지 않는다. 컴파일과 physics witness는 MAP11이다.

---

## 5. Immutability, Digest, Errors

- 모든 collection defensive copy/read-only
- caller mutation, input order, culture, reflection/file order와 무관
- invalid input은 partial publish/RNG/file/Unity lifecycle 사용 0
- contract digest는 footprint, roles, ports, variants, nodes, edges, movement, clearance, landing/recovery, 모든 envelope set을 canonical SHA-256에 포함
- display text, PDF line, timestamp, object hash는 제외

validator는 최소 다음을 구분한다.

```text
InvalidId | InvalidFootprintCount | SixChunkNotAllowlisted
DuplicateOrDisconnectedFootprint | InvalidRoleAnchor | MissingRequiredRole
InvalidPort | InvalidPortDirection | DuplicatePrimaryPort
InvalidGraphKind | DuplicateVariant | InvalidBaselineVariant
DuplicateNodeOrEdge | MissingNodeReference | SelfEdge
InvalidMovement | EdgeAnchorMismatch | InvalidClearance
InvalidLanding | InvalidRecovery | MissingEntryExitPath
UnreachableMandatoryElement | InvalidEnvelopeSet
FloorClearanceConflict | MovementEnvelopeMismatch
```

errors는 accumulated, stable-sorted, deduplicated한다.

---

## 6. 변경 경계

허용:

- `TerrainClusters/` 신규 Runtime C#/meta
- 대응 EditMode test C#/meta
- Result, 설치/Archive Task, Finalize Status

금지:

- MAP00~09_03 existing production/test 수정
- 다른 V2 Runtime root 수정
- CSV/Authoring/Generated/Scene/Prefab/SO/Editor Window 변경
- asmdef/asmref, ProjectSettings/Packages 변경
- tile renderer, graph compiler, pathfinder, physics probe, RNG, solver 구현
- MechanismGraph/ProgressionGraph production model 선행 구현
- Activity/Event/Special content 구현
- RouteType/Access authority 재정의
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

## 7. 필수 검증

Focused category `MAP09_04`:

1. standard 2..5 footprint와 allowlisted 6
2. disconnected/diagonal/invalid count 거부
3. required roles와 active-footprint projection
4. primary Entry/Exit port·outward side·RouteType compatibility
5. graph kind 분리와 Traversal-only enforcement
6. multiple SpineVariant와 exact one baseline
7. exact 6 MovementKind
8. node/edge/ref/anchor/clearance 검증
9. Entry→Exit 및 mandatory reachability
10. 7 envelope set과 movement matrix
11. Floor/Clearance 충돌 및 out-of-bounds 거부
12. landing/recovery 보호
13. immutable collections와 deterministic digest
14. no RNG/file/Unity lifecycle/forbidden symbol

최종 회귀:

```text
MAP09_04 focused: >0, all PASS
MAP09_03 exact: 62/62 PASS
MAP09_02 exact: 38/38 PASS
MAP09_01 exact: 26/26 PASS

MAP08: 9220/9220 PASS
MAP07: 5422/5422 PASS
MAP06: 2746/2746 PASS
MAP05: 1959/1959 PASS
Distinct: 19347/19347 PASS
```

각 selection의 discovered/executed/pass/fail/skip을 분리한다. timeout, zero-selection, 이전 결과 replay는 PASS가 아니다.

Static/Unity:

```text
compile/Console/relevant warning: 0/0/0
Authoring CSV/meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring/Generated/Scene/Prefab/Settings/Packages/asmdef task changes: 0
existing MAP00~09_03 modifications: 0
other V2 root changes: 0
duplicate GUID/unapplied candidate/diff-check errors: 0/0/0
unrelated staged/included: 0
```

---

## 8. Result, Commit, Stop

Result:

```text
MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS_RESULT.md
```

상단:

```text
TASK: MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS
STATUS: PASS | FAIL | BLOCKED
MAP09_04: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_05_IMPLEMENT_ACTIVITY_AND_EVENT_CONTRACTS: LOCKED / DO NOT START
```

필수 보고:

1. predecessor/Status/dirty preflight
2. 신규 파일 inventory
3. footprint/role/port/SpineVariant/graph/envelope 계약과 digest
4. focused, MAP09, required `19347` 회귀
5. Unity/static/change-scope gate
6. out-of-scope 상태
7. atomic commit subject와 `SELF` 표기, CLI 실제 commit hash handoff

PASS/finalize 뒤 task-owned 파일만 commit한다.

```text
Subject: MAP09_04: implement cluster spine envelope contracts
Push: NOT PERFORMED
```

설치/Archive Task, 신규 Runtime/Test/meta, Result, Finalize Status만 포함한다. 실패 시 같은 MAP09_04 repair만 보고한다. MAP09_05를 자동 시작하지 않는다.
