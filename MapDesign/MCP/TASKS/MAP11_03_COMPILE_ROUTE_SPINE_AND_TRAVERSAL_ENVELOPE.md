```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
  task_file: TASKS/MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE.md
  requires_current_task: NONE
  requires_completed_task: MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT
  requires_result:
    path: REPORTS/MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT_RESULT.md
    status: PASS
    sha256: 824c0b93c791539507a92390a0b1a26ec2f41748de2373b1f6499fbc272d6ded
  requires_installed_task:
    path: TASKS/MAP11_02_IMPLEMENT_CLUSTER_ROLES_AND_SOCKET_CONTRACT.md
    sha256: cafac59a3ad2dff40ce51c6dba249da02505b847ea1e9a9730ce3aaf1bcf89d3
  sets_current_task: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
```

# MAP11_03 — Compile Route Spine and Traversal Envelope

```text
TASK: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Responsibility

이번 Task는 MAP09_04에 authored/validated된 Traversal edge와 Envelope tile sets를 MAP11_01의 transformed Local Canvas에 투영하고, MAP11_02의 role/socket/node 연결과 결합해 immutable compiled Route Spine을 만든다.

```text
authored SpineVariant nodes/edges/envelopes
→ transformed compiled nodes and movement edges
→ seven named envelope sets
→ RouteSpine / TraversalEnvelope protected provenance
→ immutable variant compilation
```

| 소유 | 소유하지 않음 |
|---|---|
| 모든 SpineVariant node/edge 좌표 투영 | 새 route/edge authoring |
| 7종 authored envelope set 투영 | jump arc·clearance 물리 추론 |
| RouteSpine/Envelope 보호 타일 provenance | base/high/recovery witness |
| transformed graph 무결성·reachability 보존 | shell·pattern 렌더링 |
| immutable compiled variants/result/digest | RNG/variant 선택 |

이번 Task는 authored set을 **compile**한다. endpoints에서 임의 선을 긋거나 arc/floor/clearance를 새로 생성하지 않는다.

## 1. No-Regression Policy

정상 실행은 category `MAP11_03`만 선택한다.

```text
MAP11_03 focused selection: required
Prior MAP09/MAP10/MAP11_01~02 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

실제 trigger는 다음으로 제한한다.

- compile/Console error가 기존 authority에서 발생
- MAP09_04 Envelope 또는 MAP11_01/02 compiled behavior drift
- 기존 production/test/CSV/meta 예상 밖 변경
- asmdef/GUID/namespace/authority 위반

Task-owned 코드·fixture 문제는 task-owned 파일만 고치고 `MAP11_03`만 재실행한다. 기존 authority 결함이면 이전 파일을 수정하지 말고 owner·원인·최소 범위를 보고한 뒤 `STATUS: BLOCKED`로 STOP한다.

## 2. Read-Only Authorities

Preflight에서 exact 확인한다.

1. MAP11_02 Result status/SHA와 installed/archive Task SHA
2. MAP11_03만 CURRENT, MAP11_04 LOCKED, inbox candidate 0
3. MAP09_04 exact movement kinds `Walk/Jump/Drop/Climb/Slide/Bounce`
4. MAP09_04 edge fields: node refs, start/end, clearance dimensions, landing, recovery, mandatory, TraversalEnvelope
5. exact envelope sets `Centerline/Floor/Clearance/JumpArc/DropColumn/Landing/Recovery`
6. MAP11_01 Local Canvas identity/digest/transform/active mask/source↔compiled lookup
7. MAP11_02 projected roles/ports/variant-node links and canonical digest
8. MAP10 protected source kinds `RouteSpine` and `TraversalEnvelope` are read-only downstream semantics
9. Authoring `52`, MAP10 `24/453`, Generated CSV 0
10. compile/Console, meta/GUID, dirty/staged paths

다음이면 `BLOCKED`다.

- predecessor mismatch 또는 MAP11_02 미완료
- source contract, Local Canvas, role/socket contract identity/digest가 서로 불일치
- 기존 movement/envelope/protected-source authority 수정·복제 없이는 구현 불가
- task allowlist가 사용자 변경과 겹침

## 3. Exact Write Boundary

신규 파일만 허용한다.

```text
Runtime:
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRouteSpine.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterTraversalEnvelope.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterTraversalCompiler.cs(.meta)

Focused test:
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterTraversalCompilerTests.cs(.meta)

Namespace:
StarNight.Map.WorldGeneration.TerrainClusters

Assembly:
Game.Map.Runtime / Game.Map.Tests.EditMode
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 기존 MAP00~MAP11_02 production/test/CSV/meta 파일은 수정하지 않는다. 실제 inventory와 public surface를 Result에 기록한다.

## 4. Compile Request and Authority Binding

input:

```text
validated TerrainClusterContract + digest
successful TerrainClusterLocalCanvas + digest
successful TerrainClusterRoleSocketContract + digest
```

compile 전 exact binding:

- 세 artifact의 `TerrainClusterId`가 같다.
- source footprint identity/digest와 Local Canvas transform이 일치한다.
- MAP11_02가 참조한 SpineVariant/node/role/port identity가 source contract와 같다.
- input 중 하나라도 invalid/unpublished/mismatched면 partial output/digest 없이 실패한다.
- source의 모든 SpineVariant를 compile하며 variant 선택이나 가중치 계산은 하지 않는다.

## 5. Compiled Route Spine Nodes

각 source `TraversalNode`를 MAP11_01 mapping으로 투영한다.

published node 최소 필드:

```text
SpineVariantId
node ID
source LocalTileCoord
compiled LocalTileCoord
compiled owning ClusterChunkCoord
mandatory fact
linked projected role anchor IDs/kinds
source provenance
```

규칙:

- node ID와 variant membership을 그대로 보존한다.
- compiled coordinate와 owning chunk는 Active여야 한다.
- MAP11_02 role-node link가 있으면 node coordinate와 exact 같아야 한다.
- duplicate/missing/out-of-bounds/inactive node는 atomic failure다.
- node collection은 variant ID, node ID ordinal로 canonical publish한다.

## 6. Compiled Traversal Movement Edges

각 source edge를 같은 transform으로 투영한다.

published edge 최소 필드:

```text
SpineVariantId / edge ID
From/To node ID
TraversalMovementKind
source and compiled Start/End
minimum clearance width/height
source and compiled Landing/Recovery
Mandatory flag
compiled TraversalEnvelope
source edge provenance
```

규칙:

- exact movement kind를 보존하며 undefined 값을 거부한다.
- From/To node는 compiled variant 안에 존재한다.
- compiled Start/End는 compiled From/To node coordinate와 exact 같다.
- clearance dimensions `>=1`을 보존한다.
- landing/recovery는 MAP11_01 lookup을 통해 Active tile로 투영한다.
- self edge, missing node, coordinate mismatch, inactive tile을 거부한다.
- mandatory edge/node의 source directed reachability와 Entry→Exit path를 compiled graph에서도 보존한다.
- MirrorY/R180에서도 authored MovementKind를 임의로 다른 enum으로 바꾸지 않는다. 실제 물리 가능성은 MAP11_04 witness 책임이다.

## 7. Seven Compiled Envelope Sets

각 edge의 exact sets:

```text
Centerline
Floor
Clearance
JumpArc
DropColumn
Landing
Recovery
```

각 source tile을 MAP11_01 lookup으로 compiled tile에 투영하고 set kind와 source provenance를 보존한다.

공통 invariant:

- 각 set은 unique, canonical `(y,x)` order다.
- 모든 coordinate는 Local Canvas Active tile이다.
- Centerline은 non-empty이고 compiled Start/End를 포함한다.
- Clearance는 non-empty다.
- Landing set은 compiled landing tile을 포함한다.
- Recovery set은 compiled recovery tile을 포함한다.
- Floor와 Clearance는 compiled coordinate 기준으로 disjoint다.

movement matrix:

| Movement | Required | Must be empty |
|---|---|---|
| `Walk` | Floor, Clearance, Landing, Recovery | JumpArc, DropColumn |
| `Jump` | Clearance, JumpArc, Landing, Recovery | DropColumn |
| `Drop` | Clearance, DropColumn, Landing, Recovery | JumpArc |
| `Climb` | Clearance, Landing, Recovery | JumpArc, DropColumn |
| `Slide` | Floor, Clearance, Landing, Recovery | JumpArc, DropColumn |
| `Bounce` | Clearance, JumpArc, Landing, Recovery | DropColumn |

MAP09_04 validator가 가진 더 엄격한 exact matrix가 있으면 그 authority를 그대로 소비하며 완화하지 않는다. transformed set cardinality는 source와 exact 같아야 한다.

## 8. Protected Tile Publication

후속 MAP11_05가 MAP10 protected-mask planner에 전달할 수 있도록 protection을 provenance와 함께 게시한다. 이번 Task는 MAP10 planner/renderer를 호출하지 않는다.

exact protection source projection:

```text
RouteSpine:
  all compiled nodes
  every edge Start/End
  every Centerline tile

TraversalEnvelope:
  Floor
  Clearance
  JumpArc
  DropColumn
  Landing
  Recovery
```

각 protected coordinate는 다음 provenance를 유지한다.

```text
source kind RouteSpine | TraversalEnvelope
SpineVariantId
node ID and/or edge ID
envelope set kind when applicable
source and compiled coordinate
mandatory fact
```

- 같은 coordinate에 여러 provenance가 있으면 coordinate는 coalesce하되 모든 unique provenance를 보존한다.
- RouteSpine과 TraversalEnvelope가 같은 coordinate를 보호하면 두 source kind를 모두 보존한다.
- protected set은 variant별 collection과 whole-compiled-artifact union을 모두 deterministic하게 제공한다.
- protected tile에 실제 write를 허용하는 policy를 만들지 않는다.

## 9. Publication, Errors, and Digest

최소 semantic surface:

```text
CompiledTraversalNode
CompiledTraversalEdge
CompiledTraversalEnvelope
ClusterTraversalProtectionSourceKind
ClusterTraversalProtectedTile / Provenance
CompiledClusterSpineVariant
TerrainClusterTraversalCompileRequest
TerrainClusterTraversalCompilation
TerrainClusterTraversalCompileError / Result
TerrainClusterTraversalCompiler
```

기존 naming 충돌 시 의미를 보존하는 최소 이름 조정은 가능하다.

publication rules:

- all collections defensive copy/read-only
- errors accumulated, deduplicated, stable-sorted
- failure에서 partial variants/nodes/edges/envelopes/protection/digest `0`
- digest는 ruleset, three input digests, transform, every variant/node/edge field, all seven sets, all protection provenance를 포함
- display text, timestamp, locale, object hash, input/reflection/file order는 제외
- input reversal/culture change는 같은 artifact/digest

최소 error distinctions:

```text
MissingInput
ArtifactIdentityMismatch
ArtifactDigestMismatch
InvalidSourceContract
MissingVariant
DuplicateNodeOrEdge
NodeProjectionMissing
NodeOutsideActiveMask
MissingNodeReference
SelfEdge
EdgeAnchorMismatch
InvalidMovement
InvalidClearance
LandingProjectionMissing
RecoveryProjectionMissing
EnvelopeProjectionMissing
EnvelopeOutsideActiveMask
MovementEnvelopeMismatch
FloorClearanceConflict
MissingEntryExitPath
UnreachableMandatoryElement
ProtectionProvenanceMismatch
NonCanonicalPublication
```

## 10. Exact Non-Ownership

금지:

- existing MAP09_04/MAP11_01/MAP11_02 production/test 수정
- source edge/envelope/graph authority 변경 또는 duplicate
- endpoints에서 line/arc/floor/clearance 자동 생성
- collision/physics/jump feasibility simulation
- baseline/high/recovery route 분류 또는 2~5초 복귀 witness
- Solid/Air static shell, terrain density, cleanup
- MAP10 planner/renderer/selector 실행
- starter TerrainCluster/CSV/Authoring/Generated 제작
- sector placement/planner/world assembly
- Activity/Event/SpecialRegion 조립
- final SectorCanvas/Slice/Tilemap/Scene/Prefab/SO
- EditorWindow/PlayMode/WorldGenerationRoot wiring
- RNG/variant selection/weight
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

category `MAP11_03`만 실행하고 최소 다음을 검증한다.

1. all variants/nodes/edges compiled; variant selection 0
2. node projection under R0/MirrorX/MirrorY/R180
3. all six movement kinds preserved
4. From/To and Start/End exact binding
5. landing/recovery active projection
6. all seven named sets transform/cardinality/order
7. Centerline/clearance/landing/recovery common invariants
8. exact movement envelope matrix
9. Floor/Clearance conflict rejection
10. inactive/out-of-bounds set rejection
11. Entry→Exit and mandatory reachability preservation
12. MAP11_02 role/port/node connection consistency
13. RouteSpine protected node/start/end/centerline publication
14. TraversalEnvelope six-set protected publication
15. same-coordinate provenance coalescing without loss
16. atomic accumulated errors and partial output 0
17. immutable/canonical publication and deterministic digest
18. reversed input/culture stability; semantic change changes digest
19. no physics/witness/shell/pattern/sector side effects

Task-owned 실패는 task-owned 파일만 고치고 `MAP11_03`만 재실행한다.

## 12. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_03 focused: all discovered executed and PASS; skip/inconclusive 0
MAP11_02 Result SHA: 824c0b93... exact
existing MAP11_01~02 production/test/meta modifications: 0
MicroPattern definitions / physical rows: 24 / 453 unchanged
Catalog CSV SHA: f9d9e9cc... unchanged
Cells CSV SHA: e702ae5d... unchanged
Full 52-file Authoring manifest: 4415ae4a... unchanged
Generated CSV: 0
existing MAP00~MAP11_02 production/test/CSV/meta modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new C#/meta valid; duplicate GUID 0
unapplied candidate / diff-check / unrelated staged paths: 0 / 0 / 0
```

## 13. Required Result

```text
MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE_RESULT.md
```

상단:

```text
TASK: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
STATUS: PASS | BLOCKED
MAP11_03: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| Field | Required report |
|---|---|
| Task responsibility | authored traversal graph/envelope를 transformed compiled artifact로 투영 |
| Added functions | compiled nodes/edges/sets/protection/compiler/result/digest |
| Inputs consumed | MAP09_04 graph, MAP11_01 mapping, MAP11_02 role/socket contract |
| Outputs produced | immutable compiled variants와 protected provenance 또는 atomic errors |
| Explicit non-ownership | physics/witness/shell/pattern/starter/sector 미구현 |
| Downstream consumers | MAP11_04 route witness, MAP11_05 protected pattern zones |

이후 predecessor/Status, file/public surface, variant/node/edge projection, seven-set matrix, protection provenance, graph reachability, immutability/digest/error, focused/no-regression, static/change scope, commit handoff를 실제 증거로 기록한다.

```text
MAP11_03 focused: discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

PASS일 때만 Finalize하고 task-owned production/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_03: compile route spine and traversal envelope
Push: NOT PERFORMED
```

Result가 PASS여도 MAP11_04를 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
