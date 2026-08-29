```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
  task_file: TASKS/MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES.md
  requires_current_task: NONE
  requires_completed_task: MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES
  requires_result:
    path: REPORTS/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES_RESULT.md
    status: PASS
    sha256: fe46bf44bc8efc6b95ab10536fdd5a37fe13ba3bce6df8c2f8f67676a1dc7b0f
  requires_installed_task:
    path: TASKS/MAP13_01_IMPLEMENT_SPECIAL_FOOTPRINT_SITE_BRIDGE_AND_COORDINATES.md
    sha256: 7c487cfb1aa405c4300496f5d87b2adc27864694f59385417f24edc2896bb4bb
  sets_current_task: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
```

# MAP13_02 — Entry Buffer, Priority and Collision Rules

```text
TASK: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
PHASE: MAP13 — SpecialRegion / Village / Mandatory Landmarks
STATUS: CURRENT
NEXT: MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP13_01의 placed port/site bridge에 mandatory no-tool entry, 내부 apron, 양방향 Entry/Return 증거와 전후 Quiet chunk를 결합한다. 동시에 Special 배치와 후속 TerrainCluster/Activity가 겹칠 때 사용할 deterministic priority/collision verdict를 정의한다.

```text
SpecialRegionSiteBridge
+ explicit internal apron evidence
+ MAP11 exact 2-chunk Quiet Buffer candidate placements
→ Entry/Return + Before/After buffer binding
→ tile occupancy claims
→ Boss > Forge > Core > Village > Rare > Cluster > Activity verdict
```

이번 Task는 위치를 탐색하거나 타일을 쓰지 않는다. caller가 제시한 plan을 검증하여 immutable result만 게시한다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 모든 신규/수정 script의 전체 경로, class/method별 input→output, 새 기능, 파이프라인 위치, 미구현, Editor/게임 가시성을 실제 결과로 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| mandatory socket과 Entry/Return port pairing | MAP03 reservation solver 변경 |
| region 내부 apron 검증 | 실제 shell/road/facility authoring |
| MAP11 Quiet 후보의 Before/After 1 chunk씩 binding | Quiet TerrainCluster 생성·선택·RNG |
| 정적 bidirectional/no-tool witness | PlayerController/collider/physics reachability |
| exact owner priority와 collision decision | 타일 overwrite/delete/carve/replan 실행 |
| focused MAP13_02 test | MAP13_03 fixed shell/slot/persistence 구현 |

Village, 자원, Forge, Boss, Rare content나 Scene/Prefab/Tilemap/gameplay object는 추가하지 않는다.

## 2. Focused-Only Policy

정상 실행은 EditMode category `MAP13_02`만 선택한다.

```text
MAP13_02 EditMode: required
MAP03/MAP09/MAP10/MAP11/MAP12/MAP13_01 selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

current public API 호출은 과거 category 재실행이 아니다. upstream defect면 기존 파일을 고치지 말고 owner/invariant/reason/minimum verification을 기록해 `BLOCKED`로 STOP한다. Task-owned 신규 파일 문제만 신규 파일 안에서 수정하고 `MAP13_02`만 재실행한다.

## 3. Read-Only Preflight

```text
MAP13_01 Result: PASS
MAP13_01 Result SHA-256:
fe46bf44bc8efc6b95ab10536fdd5a37fe13ba3bce6df8c2f8f67676a1dc7b0f

MAP13_01 installed Task SHA-256:
7c487cfb1aa405c4300496f5d87b2adc27864694f59385417f24edc2896bb4bb

MAP13_01 COMPLETE / MAP13_02 CURRENT / MAP13_03 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP13_01 SpecialRegionSiteBridge and placed sector/slot/port bindings
MAP09 SpecialRegionContract, AccessClass, PacingRole, LocalTileCoord
MAP03 SiteEntryAnchor/SiteEntrySide and mandatory route identity
MAP11 TerrainClusterQuietBufferCandidate/Pool/Query
MAP11 two active chunks, baseline witness, Entry/Exit side and RouteType evidence
Sector 48×32 / MicroChunk 12×8 constants
```

기존 type을 재정의하거나 MAP11 candidate를 test에서 재계산하지 않는다. missing public access나 기존 파일 수정이 필요하면 `BLOCKED`다.

## 4. Exact Write Boundary

정상 범위는 Runtime 2개, focused test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionEntryBuffer.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPlacementCollision.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionEntryBufferCollisionTests.cs(.meta)
```

```text
Runtime: Game.Map.Runtime / StarNight.Map.WorldGeneration.SpecialRegions
Tests: Game.Map.Tests.EditMode / StarNight.Map.Tests.EditMode.WorldGeneration.SpecialRegions
Category: MAP13_02
```

수정 금지:

```text
existing C# / test / CSV / meta
asmdef / asmref
Authoring / Generated
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
```

추가 helper 파일, Editor window, fixture asset, serializer, CSV는 금지한다.

## 5. Entry, Apron and Quiet Buffer Contract

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다.

```text
SpecialRegionEntryApron
SpecialRegionQuietChunkRole: Before / After
SpecialRegionQuietChunkBinding
SpecialRegionBidirectionalWitness
SpecialRegionEntryBufferPlan
SpecialRegionEntryBufferCompileRequest
SpecialRegionEntryBufferCompiler.Compile
SpecialRegionEntryBufferErrorCode / Error / Result
```

### 5.1 Mandatory socket and ports

- input bridge는 MAP13_01 validation/digest가 유효해야 한다.
- exact one Entry port와 exact one Return port를 caller가 선택한다.
- 두 port는 valid MAP03 anchor/socket identity를 보존하고 같은 reservation에 속한다.
- access는 exact `MandatoryNoTool`이어야 한다. `ProgressionGate`나 Optional access는 general mandatory socket을 대신할 수 없다.
- Entry와 Return의 RouteType set은 source anchor 및 Quiet candidate와 non-empty intersection을 가져야 한다.
- port의 placed side, sector, tile exterior edge와 anchor exterior world sector는 MAP13_01 bridge와 exact 같다.
- compiler가 새 socket, route, access 또는 port를 만들지 않는다.

### 5.2 Internal apron

각 Entry/Return port는 caller-supplied rectangular apron 하나를 가진다.

- apron은 같은 placed region sector 안의 explicit `LocalTileCoord` rectangle이다.
- width/height는 각각 최소 `4`이며 exact cell count는 `width × height`다.
- 모든 cell은 unique, in `48×32`, region footprint 안이다.
- port tile과 그 바로 안쪽 cardinal neighbor를 포함한다.
- fixed-shell cell과 겹치지 않는다.
- Entry/Return 이외 Facility/Npc/Enemy/Event/Reward slot과 겹치지 않는다.
- 두 apron은 같거나 겹칠 수 있지만 union은 4-neighbor connected여야 한다.
- apron은 Air/collision 변경을 수행하지 않고 reserved-clear evidence만 게시한다.

### 5.3 Before/After Quiet chunks

`전후 Quiet 1청크`는 region과 직접 맞닿는 contact chunk가 앞/뒤 exact 하나씩이라는 뜻이다. MAP11 candidate 자체는 exact 2-active-chunk footprint와 baseline을 분리하지 않고 보존한다.

```text
Before candidate placement (2 chunks) → Entry와 맞닿는 contact chunk exact 1
After candidate placement  (2 chunks) → Return과 맞닿는 contact chunk exact 1
```

- Before/After는 distinct placement identity다. 같은 candidate definition ID 재사용은 가능하지만 같은 placed cells 재사용은 불가하다.
- 각 placement는 source candidate의 두 chunk adjacency, Entry→Exit baseline과 전체 cell evidence를 그대로 보존한다. 두 chunk를 떨어뜨리거나 region 양쪽으로 분할하지 않는다.
- 두 placement 모두 Quiet pacing, `MandatoryNoTool`, reward/marker/hazard `0/0/0`, protected write/change `0/0`인 기존 candidate evidence를 보존한다.
- Before contact chunk exact 하나는 Entry port의 exterior-adjacent tile을 포함한다.
- After contact chunk exact 하나는 Return port의 exterior-adjacent tile을 포함한다.
- 모든 chunk의 world sector/local chunk coordinate와 12×8 cell bounds가 exact하다.
- candidate Entry/Exit side 및 compatible RouteType이 selected port/anchor와 맞아야 한다.
- bridge footprint, apron, Before placement와 After placement 사이 tile overlap은 `0`이다.
- compiler는 pool query/selection/RNG, candidate transform, terrain render를 하지 않는다.

### 5.4 Bidirectional witness

Success는 최소 다음 static witness를 게시한다.

```text
Before Quiet → Entry socket → Entry apron → region interior
region interior → Return apron → Return socket → After Quiet
```

- forward와 reverse가 같은 typed port/apron/buffer evidence에서 성립한다.
- synthetic edge, teleport, carve, tool requirement와 one-way-only flag는 `0`이다.
- witness는 coordinate/adjacency/AccessClass/RouteType 계약이며 실제 jump/collider/physics 도달성을 주장하지 않는다.

## 6. Placement Priority and Collision Verdict

같은 Runtime 파일에 다음 semantic surface를 제공한다.

```text
SpecialRegionPlacementOwnerKind
SpecialRegionOccupancyClaim
SpecialRegionCollisionKind / Decision
SpecialRegionPlacementCollisionPlan
SpecialRegionPlacementCollisionCompiler.Compile
SpecialRegionPlacementCollisionErrorCode / Error / Result
```

Exact high-to-low priority:

| Priority | Owner |
|---:|---|
| 700 | Boss |
| 600 | Forge |
| 500 | CoreResource |
| 400 | Village |
| 300 | RareRegion |
| 200 | TerrainCluster |
| 100 | ActivityStructure |

Rules:

- claim은 stable owner ID/kind와 canonical world sector + local tile set을 가진다.
- mandatory route/boundary, MAP03 reservation footprint, Entry/Return port와 apron은 `HardProtected` evidence다. 어떤 owner도 이를 overwrite할 수 없다.
- no overlap은 모두 accepted다.
- 다른 priority overlap은 higher accepted/lower rejected decision을 게시한다.
- same priority의 다른 owner overlap은 `AmbiguousSamePriority` atomic failure다.
- already committed lower owner와 later higher owner 충돌은 기존 것을 삭제하지 않고 `RequiresReplan` verdict를 낸다.
- SpecialRegion footprint 내부에 TerrainCluster/Activity claim은 거부한다.
- Activity는 TerrainCluster 또는 어떤 Special/Rare/HardProtected owner도 이길 수 없다.
- priority는 후보 평가 순서만 정의하며 global GenerationLayer order를 재정의하지 않는다.
- compiler는 tile payload/owner를 쓰거나 제거하지 않고 decision/digest만 게시한다.

Collections은 defensive-copy/read-only/canonical order다. any invalid input은 plan/digest `0`; errors/decisions는 stable sort/dedupe한다. reverse input/repeat/`tr-TR`에서 동일하며 RNG/time/filesystem/Unity lifecycle/static mutable cache는 `0`이다.

Minimum error groups:

```text
MissingInput | BridgeDigestMismatch | InvalidPortPair | InvalidMandatoryAccess
InvalidApron | ApronBlocked | InvalidQuietCandidate | QuietChunkMismatch
BufferOverlap | MissingBidirectionalWitness | InvalidOwner | InvalidClaim
HardProtectedCollision | AmbiguousSamePriority | NonCanonicalPublication
```

## 7. Focused Tests

in-memory public fixtures로 최소 다음을 검증한다.

1. MAP13_01 valid bridge + Entry/Return exact port/anchor binding
2. minimum 4×4과 larger apron, inward neighbor, connected union
3. Before/After 각 2-chunk candidate placement의 footprint/baseline 보존과 contact chunk exact 하나씩
4. Before→Entry→apron 및 apron→Return→After static bidirectional witness
5. Boss>Forge>Core>Village>Rare>Cluster>Activity 전체 pairwise priority matrix
6. HardProtected, same-priority, already-committed lower conflict verdict
7. Special footprint/port/apron/Quiet/Cluster/Activity overlap rules
8. reverse/repeat/culture/immutability/digest와 RNG/world mutation 0
9. invalid access/apron/candidate/chunk/route/claim negative fixture의 atomic failure

MAP11 compiler/route solver, world placement search 또는 collision geometry를 test 안에 복제하지 않는다.

## 8. Verification and Required Result

Unity refresh/compile 후 `MAP13_02` EditMode만 실행한다.

```text
discovered = executed = passed
failed / skipped / inconclusive = 0 / 0 / 0
compile / relevant Console error = 0 / 0
prior category / legacy / PlayMode / unfiltered selections = 0 / 0 / 0 / 0
```

Static gate:

```text
new Runtime C#/meta: 2/2
new focused test C#/meta: 1/1
existing C#/test/CSV/meta modifications: 0
Authoring/Generated/Scene/Prefab/Tilemap/Settings/Packages changes: 0
duplicate GUID: 0
unapplied candidate/diff-check/unrelated staged: 0/0/0
Git push: NOT PERFORMED
```

Result 경로:

```text
MapDesign/MCP/REPORTS/MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES_RESULT.md
```

상단 verdict:

```text
TASK: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
STATUS: PASS | BLOCKED
MAP13_02: COMPLETE ELIGIBLE | NOT COMPLETE
MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE: LOCKED / DO NOT START
```

`## User-Facing Implementation Report`에서 다음을 실제 수치와 함께 보고한다.

- 신규/수정 script 전체 경로
- script/class/method별 책임과 input→output
- apron 크기/cell/overlap, port/anchor, Before/After chunk와 witness 결과
- 7단계 priority 및 collision matrix 결과
- 새로 가능해진 것과 파이프라인 위치
- 아직 미구현한 MAP13_03+ 기능
- Editor/게임 가시성

`## Responsibility and Added Functions`에는 아래를 표로 보고한다.

| Field | Required evidence |
|---|---|
| Task responsibility | entry/apron/Quiet binding + priority/collision verdict |
| Added scripts | Runtime 2 + test 1 exact paths |
| Added functions | public type/method별 sole responsibility |
| Inputs consumed | MAP13_01 bridge + MAP11 Quiet candidate + explicit claims |
| Outputs produced | immutable entry-buffer plan, witness, collision decisions/digest |
| Explicit non-ownership | placement search/RNG, terrain writes, content, gameplay, MAP13_03 |
| Downstream consumer | 별도 검수 후 MAP13_03만 unlock 가능 |

그 뒤 focused test, static scope, regression selections, task-owned files와 commit handoff를 기록한다.

정상 문구:

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

PASS일 때만 Status Finalize 후 task-owned 파일만 atomic commit한다.

```text
Subject: MAP13_02: implement entry buffers and collision priority
Push: NOT PERFORMED
```

Result가 PASS여도 MAP13_03을 자동 시작하지 않고 별도 검수까지 LOCKED로 유지한다.
