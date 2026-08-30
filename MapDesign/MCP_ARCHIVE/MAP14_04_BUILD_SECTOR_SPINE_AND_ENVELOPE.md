```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
  task_file: TASKS/MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE.md
  requires_current_task: NONE
  requires_completed_task: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
  requires_result:
    path: REPORTS/MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES_RESULT.md
    status: PASS
    sha256: db59ad87178df42b5b64d51ccc74b850f9e456209fec8a67b1f19218ee22bad6
  requires_installed_task:
    path: TASKS/MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES.md
    sha256: 6721bd97e9682ce220073e9929ef01285322815cb5d14f4ce1f3a2e36ba832cf
  sets_current_task: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
```

# MAP14_04 - Build Sector Spine and Envelope

```text
TASK: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01 input, MAP14_02 fixed anchors, MAP14_03 cluster placement plan을 소비해서 한 sector 안의 route spine graph와 traversal envelope/protected set을 만든다.

```text
SectorPlannerInput
SectorPacingAssignment
SectorFixedAnchorPlan
SectorClusterPlacementPlan
→ SectorSpineGraphBuilder
→ SectorTraversalEnvelopeBuilder
→ immutable SectorSpineEnvelopePlan
→ MAP14_05 pattern-zone/render input
```

이번 Task는 “이동 골격과 보호 영역”까지만 소유한다. 실제 MicroPattern 선택/렌더링, terrain cleanup, Activity/Event 배치, final canvas ownership, retry/RNG, PlayMode physics, Scene/Prefab/Tilemap 반영은 하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, spine/envelope 실제 수치, 경로가 무엇을 보장하고 무엇을 아직 보장하지 않는지, 미구현 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| sector-local spine graph value model | MicroPattern selection/render |
| external route socket, Special entry/return, cluster port node publication | terrain cleanup |
| mandatory/optional/recovery/static route edge publication | Activity/Event placement |
| traversal envelope and ProtectedOpen cell set | final canvas ownership/conflict resolver |
| clearance/landing/recovery cell evidence | retry/RNG/backtracking |
| fixed anchor and cluster placement identity preservation | PlayMode physics/player collider |
| deterministic digest and atomic errors | Scene/Prefab/Tilemap/GameObject |
| MAP14_05 handoff contract | production 169-sector world assembly |

Spine/envelope plan은 static design proof다. It proves that public anchors and cluster placements can be connected by a deterministic sector-local route skeleton. It does not prove live physics reachability, final tile material, collider contact, jump tuning, gameplay rewards, or actual player execution.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_04`만 선택한다.

```text
MAP14_04 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01/MAP14_02/MAP14_03 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_04` category로 제한한다.

신규 task-owned failure는 신규 MAP14_04 allowlist 파일만 수정하고 `MAP14_04` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_03 Result: PASS
MAP14_03 Result SHA-256:
db59ad87178df42b5b64d51ccc74b850f9e456209fec8a67b1f19218ee22bad6

MAP14_03 installed Task SHA-256:
6721bd97e9682ce220073e9929ef01285322815cb5d14f4ce1f3a2e36ba832cf

MAP14_03 COMPLETE / MAP14_04 CURRENT / MAP14_05 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput and SectorPacingAssignment
MAP14_02: SectorFixedAnchorPlan and anchor identities
MAP14_03: SectorClusterPlacementPlan, chosen cluster/variant, footprint cells
MAP11: cluster route/socket/variant facts where publicly exposed
MAP09: AccessClass, PacingRole, layer ownership separation
```

MAP14_04 should consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_04-side adapter only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineEnvelopePlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineGraphBuilder.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorTraversalEnvelopeBuilder.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorSpineEnvelopeTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_04
```

수정·생성 금지:

```text
existing C# / test / CSV / meta
Editor production C# / Editor test C#
Authoring or Generated CSV/meta
schema registry/test
asmdef / asmref
Scene / Prefab / ScriptableObject / Tilemap / Material / Texture
Settings / Packages
PlayMode test/helper
debug export, preview window, generated report asset
```

`SectorPlanning` folders and metas were created by MAP09_00. If missing, report `BLOCKED`; do not create folder metas in this Task.

## 5. Runtime API Surface

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_04 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorSpineNodeKind
SectorSpineEdgeKind
SectorTraversalEnvelopeCellKind
SectorSpineEndpointRole
SectorSpineNode
SectorSpineEdge
SectorTraversalEnvelopeCell
SectorSpineEnvelopePlan
SectorSpineEnvelopeBuildRequest
SectorSpineEnvelopeBuildResult
SectorSpineEnvelopeErrorCode
SectorSpineEnvelopeError
SectorSpineGraphBuilder.Build
SectorTraversalEnvelopeBuilder.Build
SectorSpineEnvelopeCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial spine/envelope plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum node kinds:

```text
ExternalSocket
BoundaryBridge
ClusterEntry
ClusterExit
SpecialEntry
SpecialReturn
RecoveryJoin
OptionalBranch
```

Minimum edge kinds:

```text
MandatoryLow
MandatorySpecialConnector
BoundaryConnector
ClusterConnector
OptionalHigh
Recovery
Return
```

Minimum envelope cell kinds:

```text
Centerline
Floor
Clearance
Landing
Recovery
ProtectedOpen
ProtectedAnchorBridge
```

Minimum error groups:

```text
MissingInput | MissingAnchorPlan | MissingClusterPlacementPlan | SectorMismatch
MissingEndpoint | DuplicateNode | DuplicateEdge | NodeOutOfBounds
EdgeOutOfBounds | EdgeCrossesBlockingAnchor | EdgeCrossesUnplacedCluster
MissingMandatoryRoute | MissingRecoveryRoute | MissingSpecialConnector
EnvelopeOutOfBounds | EnvelopeOverlapsBlockingAnchor | EnvelopeMissingClearance
EnvelopeMissingLanding | ProtectedSetMismatch | RouteAccessMutationClaim
AnchorMutationClaim | ClusterMutationClaim | PatternMutationClaim
ActivityMutationClaim | SolverMutationClaim | RngMutationClaim | TileMutationClaim
NonCanonicalPublication
```

## 6. Spine Graph Contract

`SectorSpineGraphBuilder.Build` receives a valid `SectorPlannerInput`, matching `SectorPacingAssignment`, matching `SectorFixedAnchorPlan`, and matching `SectorClusterPlacementPlan`.

For each placed sector, publish typed nodes from existing public evidence:

```text
external route socket anchors -> ExternalSocket nodes
boundary fixed/warning anchors -> BoundaryBridge evidence nodes only when route-relevant
placed cluster footprint/variant -> ClusterEntry and ClusterExit nodes
Core/Forge/Boss entry-return anchors -> SpecialEntry and SpecialReturn nodes
recovery evidence -> RecoveryJoin nodes
optional/high evidence -> OptionalBranch nodes
```

Rules:

- every node coordinate is sector-local tile coordinate inside `48×32`.
- node source identity includes the source plan/digest and exact anchor/cluster/region ID.
- every edge has ordered from/to node IDs, edge kind, route class, movement/evidence label, clearance requirements and source identity.
- mandatory route must connect required external entry/exit and all required Special/cluster mandatory endpoints.
- optional/high/recovery edges must rejoin mandatory low route through `RecoveryJoin` or `Return`.
- no synthetic teleport, unowned carve, pattern write, final tile material or gameplay object is created.
- if a mandatory endpoint cannot be connected using reference evidence, fail atomically.

If MAP11 live traversal edges are not public enough for this layer, use deterministic `REFERENCE SPINE GRAPH` fixture routes derived from MAP14_03 placement rectangles. Result must label these as reference routes, not production tile reachability.

## 7. Traversal Envelope Contract

`SectorTraversalEnvelopeBuilder.Build` receives only a successful spine graph and the fixed anchor/cluster plans. It publishes envelope cells and ProtectedOpen evidence.

Rules:

- centerline cells are exact ordered route cells from spine edges.
- floor/clearance/landing/recovery cells are derived deterministically from edge kind and movement evidence.
- all envelope cells are inside `48×32`.
- ProtectedOpen includes centerline, clearance, landing, recovery and anchor bridge cells required by the static route skeleton.
- envelope may overlap route socket, boundary bridge, Special entry-return or apron/buffer anchors only when the overlap is explicitly compatible.
- envelope must not overlap blocking SpecialFootprint, SiteReservation or incompatible fixed anchors.
- envelope must not enter unplaced cluster footprint cells or outside the chosen cluster placement.
- output contains no tile material, no collider, no final ownership and no MicroPattern operation.

Success report should publish:

```text
sector count
node count by kind
edge count by kind
envelope cell count by kind
ProtectedOpen cell count
anchor-compatible overlap count
blocking-anchor overlap count 0
cluster connector count
Special connector count
mandatory route count
optional/high/recovery route count
spine graph digest
envelope digest
spine-envelope plan digest
MAP14_05 handoff readiness flag
```

Failure should publish:

```text
plan null
digest empty
stable sorted errors only
mutation counters 0
```

## 8. Identity and No-Mutation Proof

Build must prove before/after equality for:

```text
SectorPlannerInput digest
PacingAssignment digest
FixedAnchorPlan digest
ClusterPlacementPlan digest
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells
```

The following counters must remain 0:

```text
solver invocation
RNG draw
tile write
MicroPattern render
Activity/Event placement
final canvas ownership write
Scene/Prefab/Tilemap/GameObject mutation
```

## 9. Focused Fixture Matrix

Reuse the MAP14_01/02/03 fixture idea through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected spine/envelope responsibility |
|---|---|
| `PlainTraversalBoundarySector` | connect external route sockets and boundary bridge evidence |
| `QuietBufferSector` | simple low-pressure low route and minimal ProtectedOpen |
| `VillageReferenceSector` | connect around Village reference marker without progression blocker |
| `CoreResourceSector` | connect external/cluster endpoints to Core entry/reward/return evidence |
| `ForgeLandmarkSector` | connect Forge entry/process/return skeleton without inventory execution |
| `BossGateSector` | connect Boss gate/arena/return/recovery skeleton without combat execution |
| `ActivityCompatibleSector` | no Activity placement edge; route remains terrain-only |
| `DeferredOptionalSector` | optional Merchant/Maru deferred fact creates no placed Special connector |
| `NeighborInfluencedSector` | neighbor reasons affect edge labels only, not sockets or world rollback |
| `InvalidInputCases` | missing endpoint/blocking overlap/mutation failures publish zero plan |

Fixtures are `REFERENCE SPINE GRAPH` and `REFERENCE TRAVERSAL ENVELOPE` examples, not production world seeds.

## 10. Required Tests

`SectorSpineEnvelopeTests` must include 9~12 focused tests in category `MAP14_04`.

Minimum assertions:

1. `BuildPublishesCanonicalSpineEnvelopePlanFromClusterPlacements`
   - valid input/anchors/placements publish immutable plan, lower-hex digests, no partial error.
2. `SpineNodesRepresentExternalBoundaryClusterAndSpecialEndpoints`
   - node kinds/source identities match route sockets, anchors and chosen clusters.
3. `MandatoryLowRoutesConnectRequiredEndpointsInStableOrder`
   - required external, cluster and Special endpoints are connected in deterministic order.
4. `OptionalHighAndRecoveryRoutesRejoinMandatoryRoute`
   - optional/high/recovery branches rejoin RecoveryJoin or Return; no tool dependency.
5. `EnvelopePublishesProtectedOpenClearanceLandingAndRecoveryCells`
   - envelope cells inside `48×32`, kind counts stable, ProtectedOpen covers required route cells.
6. `EnvelopeAvoidsBlockingAnchorsAndPreservesCompatibleBridgeOverlaps`
   - compatible route/boundary/Special entry overlap allowed; blocking overlap fails.
7. `SpineEnvelopePreservesInputAnchorAndClusterIdentities`
   - input/assignment/anchor/cluster digests and route/access identities unchanged.
8. `InvalidMissingEndpointBlockingOverlapAndMutationClaimsFailAtomically`
   - missing endpoint, blocking anchor, edge out-of-bounds and mutation claims publish no plan/digest.
9. `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
   - repeat/reverse/`tr-TR` stable graph, envelope and plan digest.
10. `BuildDoesNotInvokePatternActivityRetryTileOrPhysicsSystems`
    - MicroPattern/Activity/Event/retry/RNG/tile/physics/Scene mutation counters all 0.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
STATUS: PASS | FAIL | BLOCKED
MAP14_04: COMPLETE ELIGIBLE only when PASS
MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 spine graph + traversal envelope/protected set이며 MicroPattern render가 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 sector fixture 수, node/edge/envelope/protected counts, mandatory/optional/recovery counts
- anchor-compatible overlap and blocking-overlap counts
- input/anchor/cluster identity가 변하지 않았다는 증거
- MAP13 SpecialRegion은 entry-return/static route evidence로만 소비됐다는 증거
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_05

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_04]
discovered: <N>
executed: <N>
passed: <N>
failed: 0
skipped: 0
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

If PASS:

```text
Commit subject: MAP14_04: build sector spine and envelope
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_05.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE.md
MCP_ARCHIVE/MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE.md
MCP/REPORTS/MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineEnvelopePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineEnvelopePlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineGraphBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineGraphBuilder.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorTraversalEnvelopeBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorTraversalEnvelopeBuilder.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorSpineEnvelopeTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorSpineEnvelopeTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_05: do not start
STOP after Result and optional PASS finalize commit
```
