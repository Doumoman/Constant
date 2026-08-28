```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT
  task_file: TASKS/MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT.md
  requires_current_task: NONE
  requires_completed_task: MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS
  requires_result:
    path: REPORTS/MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS_RESULT.md
    status: PASS
    sha256: ed7de7b3c40c287a88f309d1afc5c2e09c1987f31057bf4abb8bf52f24a10a29
  requires_installed_task:
    path: TASKS/MAP11_01_IMPLEMENT_CLUSTER_FOOTPRINT_AND_LOCAL_CANVAS.md
    sha256: 73871d0fda4e1dc7c57d2c3238ce02430b40f747662a2f224793915edc6cd8b0
  sets_current_task: MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT
```

# MAP11_02 — Implement Cluster Roles and Socket Contract

```text
TASK: MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 validated MAP09_04 role/port authority를 MAP11_01의 transformed Local Canvas에 투영하고, 다음 두 연결을 immutable contract로 검증한다.

```text
Sector external socket evidence
→ projected Entry/Exit port
→ projected Entry/Exit role anchor
→ referenced internal SpineVariant node
```

| 소유 | 소유하지 않음 |
|---|---|
| 6종 role anchor의 compiled tile projection | traversal edge tile compiler |
| transformed Entry/Exit port tile·side | Envelope set 생성 |
| sector socket compatibility evidence | sector placement/socket 선택 solver |
| role↔variant node referential connection | base/high/recovery route witness |
| immutable projection/result/digest | shell·MicroPattern·final Canvas |

`Reward`는 MAP09_04 authority대로 `0+`다. 나머지 `Entry/BuildUp/Core/Recovery/Exit`는 필수다.

## 1. No-Regression Policy

정상 실행은 category `MAP11_02`만 선택한다.

```text
MAP11_02 focused selection: required
Prior MAP09/MAP10/MAP11_01 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

다음 실제 trigger가 있을 때만 관련 최소 범위를 판단한다.

- compile/Console error가 기존 authority에서 발생
- MAP09_04 role/port 또는 MAP11_01 mapping behavior drift
- 기존 production/test/CSV/meta 예상 밖 변경
- asmdef/GUID/namespace/authority ownership 위반

Task-owned 구현·fixture 문제는 task-owned 파일만 고치고 `MAP11_02`만 재실행한다. 기존 authority 결함이면 이전 파일을 수정하지 말고 owner·원인·최소 범위를 보고한 뒤 `STATUS: BLOCKED`로 STOP한다.

## 2. Read-Only Authorities

Preflight에서 exact 확인한다.

1. MAP11_01 Result status/SHA와 installed/archive Task SHA
2. MAP11_02만 CURRENT, MAP11_03 LOCKED, inbox candidate 0
3. MAP09_04 `TerrainClusterContract`, role anchors, ports, SpineVariants/nodes, validator/digest
4. exact roles `Entry/BuildUp/Core/Recovery/Reward/Exit`
5. exact primary port kinds `Entry/Exit`, outward sides `L/R/U/D`, compatible RouteType integer set `0..4`
6. MAP11_01 Local Canvas identity/digest, transform, active mask, source↔compiled tile lookup
7. existing MAP01 route/sector external socket public definitions and stable identity
8. MAP09_02 RouteType/GeneralRouteAccess ownership; this Task has compatibility-only responsibility
9. Authoring `52`, MAP10 `24/453`, Generated CSV 0
10. compile/Console, meta/GUID, dirty/staged paths

다음이면 `BLOCKED`다.

- predecessor mismatch 또는 MAP11_01 미완료
- 기존 role/port/socket/RouteType authority 수정·복제 없이는 구현 불가
- MAP11_01 source identity/digest와 MAP09_04 contract가 일치하지 않음
- task allowlist가 사용자 변경과 겹침

## 3. Exact Write Boundary

신규 파일만 허용한다.

```text
Runtime:
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRoleProjection.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterSocketConnection.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRoleSocketCompiler.cs(.meta)

Focused test:
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterRoleSocketCompilerTests.cs(.meta)

Namespace:
StarNight.Map.WorldGeneration.TerrainClusters

Assembly:
Game.Map.Runtime / Game.Map.Tests.EditMode
```

동일 책임을 더 적은 신규 C# 파일로 구현할 수는 있다. 기존 MAP00~MAP11_01 production/test/CSV/meta 파일은 수정하지 않는다. 실제 inventory와 public surface를 Result에 기록한다.

## 4. Compile Request and Identity Binding

compiler input은 최소 다음 authority를 가진다.

```text
validated TerrainClusterContract + canonical digest
successful TerrainClusterLocalCanvas + canonical digest
sector external socket compatibility evidence for primary Entry and Exit
```

compile 전 identity binding:

- contract의 `TerrainClusterId`와 Local Canvas cluster ID가 exact 같다.
- Local Canvas source-footprint digest/coordinates가 contract footprint와 exact 같다.
- Local Canvas transform은 모든 role/node/port projection에 동일하게 적용한다.
- contract validator가 실패하거나 Local Canvas result가 실패한 입력은 거부한다.
- mismatch에서 partial roles/ports/links/socket connections/digest를 publish하지 않는다.

compiler는 SpineVariant를 선택하지 않는다. source contract가 가진 모든 variant를 투영·검증한다.

## 5. Role Anchor Projection

exact role kinds:

```text
Entry
BuildUp
Core
Recovery
Reward
Exit
```

각 authored role anchor를 다음 immutable projected record로 만든다.

```text
stable anchor ID
ClusterRoleKind
source LocalTileCoord
compiled LocalTileCoord
compiled owning ClusterChunkCoord
referenced traversal node ID
MAP11_01 transform/source mapping evidence
```

규칙:

- `Entry/BuildUp/Core/Recovery/Exit` 각각 최소 1개, `Reward` 0개 이상을 보존한다.
- source anchor ID/kind/node reference를 변경하거나 새 role을 추론하지 않는다.
- compiled coordinate는 MAP11_01 `TryGetCompiledTile` 결과와 exact 같아야 한다.
- compiled tile과 owning chunk는 모두 `Active`여야 한다.
- inactive/out-of-bounds/unmapped tile은 atomic failure다.
- projected anchors는 stable anchor ID ordinal 순으로 canonical publish한다.
- 같은 source coordinate에 여러 역할이 존재해도 MAP09_04가 허용한 의미를 임의로 합치지 않는다.

## 6. Port Projection and Side Transform

MAP09_04의 exact one primary Entry port와 exact one primary Exit port를 투영한다.

각 projected port는 최소 다음을 가진다.

```text
ClusterPortKind Entry | Exit
source and compiled LocalTileCoord
source and compiled outward side
linked projected role anchor ID/kind
compatible existing RouteType integers
source port provenance
```

side transform:

| Transform | L | R | U | D |
|---|---|---|---|---|
| `R0` | L | R | U | D |
| `MirrorX` | R | L | U | D |
| `MirrorY` | L | R | D | U |
| `R180` | R | L | D | U |

규칙:

- projected port tile은 linked role anchor tile과 exact 같다.
- Entry port는 Entry role, Exit port는 Exit role만 참조한다.
- projected tile은 Active다.
- compiled outward side의 인접 tile은 Local Canvas 밖이거나 explicit Inactive여야 한다.
- active tile을 향하는 port, undefined side, duplicate/missing primary port를 거부한다.
- compatible RouteType set은 source와 exact 같고 canonical integer order다.
- RouteType, AccessClass, side codec, general route authority를 새로 만들지 않는다.

## 7. Internal Spine Connection Projection

이번 Task는 edge geometry를 컴파일하지 않고 **role anchor와 authored node의 연결만** 투영한다.

각 actual SpineVariant에 대해:

1. variant ID와 baseline flag를 보존한다.
2. role anchor가 참조하는 traversal node가 variant 안에 존재하는지 확인한다.
3. node source coordinate를 MAP11_01 lookup으로 compiled coordinate에 투영한다.
4. role anchor compiled coordinate와 linked node compiled coordinate가 exact 같은지 확인한다.
5. Entry/BuildUp/Core/Recovery/Exit role link가 존재해야 한다.
6. authored Reward anchor가 있으면 해당 link도 존재해야 한다.
7. primary Entry/Exit port가 연결된 role/node chain을 exact 확인한다.

published link 최소 필드:

```text
SpineVariantId
baseline flag
role anchor ID/kind
traversal node ID
source/compiled coordinate
EntryPort | ExitPort | InternalRole connection kind
```

MAP09_04 graph의 directed Entry→Exit reachability 결과를 보존·참조할 수 있지만, edge path, centerline, floor, clearance, jump arc, drop column, landing, recovery tile set을 생성하지 않는다.

## 8. Sector Socket Compatibility Connection

Sector socket은 기존 authority를 소비하는 **compatibility evidence**다. 새 socket ID/side/RouteType codec이나 배정 authority를 만들지 않는다.

각 primary port는 exact one external socket evidence와 연결한다.

필수 evidence:

```text
existing sector recipe/socket stable identity
existing socket side
owning existing RouteType integer
mandatory-allowed fact
bound ClusterPortKind Entry | Exit
```

검증:

- Entry/Exit 각각 binding exact one, socket identity unique
- socket side는 projected port outward side와 exact 같다.
- owning RouteType은 projected port compatible set 안에 있다.
- primary Entry/Exit는 mandatory-allowed socket만 연결한다.
- binding port kind/role/node chain이 서로 일치한다.
- input order를 바꿔도 같은 connection/digest다.

이 Task는 sector 좌표 배치, socket 후보 탐색·선택, band/edge-signature 해결, 이웃 sector 연결, world route mutation을 하지 않는다. 기존 external socket public type을 직접 소비할 수 없으면 exact public values를 감싸는 task-owned evidence adapter만 허용하며, authority를 재정의해서는 안 된다.

## 9. Publication, Errors, and Digest

최소 semantic surface:

```text
ProjectedClusterRoleAnchor
ProjectedClusterPort
ProjectedRoleSpineLink
ClusterSectorSocketEvidence / Connection
TerrainClusterRoleSocketCompileRequest
TerrainClusterRoleSocketContract
TerrainClusterRoleSocketCompileError / Result
TerrainClusterRoleSocketCompiler
```

기존 naming과 충돌하면 의미를 보존하는 최소 이름 조정이 가능하다.

publication rules:

- 모든 collection defensive copy/read-only
- errors accumulated, deduplicated, stable-sorted
- 실패 시 partial contract/roles/ports/links/connections/digest `0`
- digest는 ruleset, source contract/local Canvas digests, transform, every projected role/port/link, socket identity/side/RouteType/mandatory fact를 포함
- display text, notes, timestamp, locale, object hash, input/reflection/file order는 제외
- input reversal과 culture change는 같은 artifact/digest

최소 error distinctions:

```text
MissingInput
InvalidSourceContract
LocalCanvasIdentityMismatch
LocalCanvasDigestMismatch
MissingRequiredRole
RoleProjectionMissing
RoleOutsideActiveMask
MissingOrDuplicatePrimaryPort
PortRoleMismatch
InvalidTransformedPortSide
PortNotOutward
MissingVariantRoleNode
RoleNodeCoordinateMismatch
MissingSocketBinding
DuplicateSocketBinding
SocketSideMismatch
RouteTypeIncompatible
MandatorySocketRejected
EntryExitConnectionMismatch
NonCanonicalPublication
```

## 10. Exact Non-Ownership

금지:

- existing MAP09_04/MAP11_01 production/test 수정
- RouteType/AccessClass/socket/side/graph authority duplicate
- SpineVariant 선택, RNG, weights
- traversal edge coordinate compiler
- Centerline/Floor/Clearance/JumpArc/DropColumn/Landing/Recovery set 생성
- physics, jump simulation, base/high/recovery route witness
- Solid/Air shell, density, cleanup
- MAP10 transform/renderer/selector 호출
- starter TerrainCluster/CSV/Authoring/Generated 제작
- sector placement/planner/world assembly
- final SectorCanvas, Slice, Tilemap/Scene/Prefab/SO
- EditorWindow/PlayMode/WorldGenerationRoot wiring
- asmdef/asmref/Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated path 수정/stage/commit, Git push

신규 Runtime 금지 symbol:

```text
UnityEditor
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
System.Random
UnityEngine.Random
```

## 11. Focused Verification

category `MAP11_02`만 실행하고 최소 다음을 검증한다.

1. six role kinds, five required, Reward 0+ projection
2. all role source→compiled coordinates under four transforms
3. active tile/chunk ownership and inactive/out-of-bounds rejection
4. exact primary Entry/Exit port projection
5. four-transform outward side matrix
6. port-role tile identity and active-to-outside/inactive direction
7. compatible RouteType preservation
8. every SpineVariant required role→node link
9. role/node compiled coordinate equality
10. Entry port→Entry role→node and Exit chain integrity
11. Entry/Exit exact external socket binding
12. socket side, RouteType, mandatory-allowed compatibility
13. missing/duplicate/mismatched binding atomic failures
14. collection immutability, canonical order, deterministic digest
15. reversed input/culture stability and semantic-change digest difference
16. no edge/envelope/shell/pattern/sector placement side effects

Task-owned 실패는 task-owned 파일만 고치고 `MAP11_02`만 재실행한다.

## 12. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_02 focused: all discovered executed and PASS; skip/inconclusive 0
MAP11_01 Result SHA: ed7de7b3... exact
MAP11_01 existing production/test/meta modifications: 0
MicroPattern definitions / physical rows: 24 / 453 unchanged
Catalog CSV SHA: f9d9e9cc... unchanged
Cells CSV SHA: e702ae5d... unchanged
Full 52-file Authoring manifest: 4415ae4a... unchanged
Generated CSV: 0
existing MAP00~MAP11_01 production/test/CSV/meta modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new C#/meta valid; duplicate GUID 0
unapplied candidate / diff-check / unrelated staged paths: 0 / 0 / 0
```

## 13. Required Result

```text
MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT_RESULT.md
```

상단:

```text
TASK: MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT
STATUS: PASS | BLOCKED
MAP11_02: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | role/port를 Local Canvas에 투영하고 socket↔role↔node connection 검증 |
| Added functions | projected role/port/link, socket evidence/connection, compiler/result/digest |
| Inputs consumed | MAP09_04 contract, MAP11_01 Local Canvas mapping, existing socket/RouteType facts |
| Outputs produced | immutable role/socket contract 또는 atomic errors |
| Explicit non-ownership | edge/envelope/route/shell/pattern/sector placement 미구현 |
| Downstream consumers | MAP11_03 edge/envelope compiler와 후속 sector planner |

이후 predecessor/Status, file/public surface, role projection, port side matrix, internal Spine links, sector socket compatibility, immutability/digest/error, focused/no-regression, static/change scope, commit handoff를 실제 증거로 기록한다.

```text
MAP11_02 focused: discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

PASS일 때만 Finalize하고 task-owned production/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_02: implement cluster roles and socket contract
Push: NOT PERFORMED
```

Result가 PASS여도 MAP11_03을 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
