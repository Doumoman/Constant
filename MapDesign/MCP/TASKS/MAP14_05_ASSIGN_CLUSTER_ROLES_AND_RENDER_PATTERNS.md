```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS
  task_file: TASKS/MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS.md
  requires_current_task: NONE
  requires_completed_task: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
  requires_result:
    path: REPORTS/MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE_RESULT.md
    status: PASS
    sha256: 3c5db3172a43866148d769ddf7b4da5c26554c6cf659f0389e017aafa8a52537
  requires_installed_task:
    path: TASKS/MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE.md
    sha256: 937faa91439188f170921e2492020f24c666d7784c2446cc2df2c981250cfd4e
  sets_current_task: MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS
```

# MAP14_05 - Assign Cluster Roles and Render Patterns

```text
TASK: MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01~04가 만든 planner input, anchors, cluster placements, spine/envelope를 소비해서 placed TerrainCluster 안의 role cells와 4x4 MicroPattern zones를 고정하고 MAP10 renderer로 in-memory pattern canvas를 게시한다.

```text
SectorPlannerInput
SectorPacingAssignment
SectorFixedAnchorPlan
SectorClusterPlacementPlan
SectorSpineEnvelopePlan
MAP10 MicroPattern application + ordered renderer authority
→ SectorClusterRoleZoneBuilder
→ SectorPatternRenderPlanner
→ immutable SectorPatternRenderPlan
→ MAP14_06 quiet fill and Activity/Event input
```

이번 Task는 처음으로 MicroPattern render를 적용한다. 단, output은 final Tilemap이나 final ownership canvas가 아니라 **REFERENCE PATTERN CANVAS**다. Activity/Event, Quiet fill, final canvas ownership/conflict resolver, retry/RNG, PlayMode physics, Scene/Prefab/Tilemap 반영은 하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, role cell/pattern zone/render 실제 수치, MAP10 renderer 사용 증거, 보호 영역 침범 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| cluster footprint 안 role cell assignment | Quiet/free-space fill |
| 12x8 MicroChunk -> 4x4 MicroPattern zone partition | Activity/Event placement |
| MicroPattern deterministic selection evidence | final canvas ownership/conflict resolver |
| MAP10 application planner and ordered renderer invocation | local retry/RNG/backtracking |
| ProtectedOpen/anchor no-write proof | tilemap bake / streaming slice |
| in-memory pattern render delta and digest | PlayMode physics/player collider |
| pattern-rendered reference canvas handoff | Scene/Prefab/Tilemap/GameObject mutation |
| focused EditMode tests | production 169-sector world assembly |

Pattern render here means immutable data publication only. It may produce layer/value deltas in memory, but it cannot claim final terrain ownership, collider validity, or live gameplay reachability.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_05`만 선택한다.

```text
MAP14_05 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01~04 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_05` category로 제한한다.

신규 task-owned failure는 신규 MAP14_05 allowlist 파일만 수정하고 `MAP14_05` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_04 Result: PASS
MAP14_04 Result SHA-256:
3c5db3172a43866148d769ddf7b4da5c26554c6cf659f0389e017aafa8a52537

MAP14_04 installed Task SHA-256:
937faa91439188f170921e2492020f24c666d7784c2446cc2df2c981250cfd4e

MAP14_04 COMPLETE / MAP14_05 CURRENT / MAP14_06 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput and SectorPacingAssignment
MAP14_02: SectorFixedAnchorPlan and anchor identities
MAP14_03: SectorClusterPlacementPlan, chosen cluster/variant, footprint cells
MAP14_04: SectorSpineEnvelopePlan, ProtectedOpen, node/edge/route identities
MAP10_01~03: MicroPattern definition/cell, application planner, protected mask, ordered renderer
MAP10_04~05: MicroPattern biome candidate/profile and repetition signature where public
MAP11: TerrainCluster role/socket/pattern-zone intent where public
MAP09: layer ownership, PacingRole, AccessClass, MicroPattern/MicroChunk constants
```

MAP14_05 should consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_05-side adapter only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterRolePatternPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterRoleZoneBuilder.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPatternRenderPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterRolePatternRenderTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_05
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

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_05 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorClusterRoleCellKind
SectorPatternZoneKind
SectorPatternRenderLayer
SectorClusterRoleCell
SectorPatternZone
SectorPatternSelection
SectorPatternRenderCell
SectorClusterRolePatternPlan
SectorClusterRoleZoneBuildRequest
SectorClusterRoleZoneBuildResult
SectorPatternRenderRequest
SectorPatternRenderBuildResult
SectorPatternRenderErrorCode
SectorPatternRenderError
SectorClusterRoleZoneBuilder.Build
SectorPatternRenderPlanner.Render
SectorPatternRenderCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial role-zone plan or render plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum role cell kinds:

```text
ClusterEntry
ClusterExit
ClusterCore
RouteShoulder
BoundaryApproach
SpecialApproach
RecoverySupport
QuietBuffer
PatternFill
ProtectedOpen
```

Minimum pattern zone kinds:

```text
ClusterBody
ClusterEdge
RouteShoulder
BoundaryBlend
SpecialApproach
Recovery
QuietBuffer
Detail
ProtectedNoWrite
```

Minimum error groups:

```text
MissingInput | MissingSpineEnvelopePlan | MissingClusterPlacementPlan
SectorMismatch | RoleCellOutOfBounds | DuplicateRoleCell
PatternZoneOutOfBounds | PatternZoneOverlap | PatternZoneOutsideCluster
PatternZoneTouchesUnplacedFootprint | MissingPatternCandidate
ProtectedWriteAttempt | RendererConflict | RenderTargetMismatch
MicroPatternApplicationRejected | MicroPatternRendererRejected
RouteAccessMutationClaim | AnchorMutationClaim | ClusterMutationClaim
SpineEnvelopeMutationClaim | ActivityMutationClaim | OwnershipMutationClaim
SolverMutationClaim | RngMutationClaim | TileMutationClaim | NonCanonicalPublication
```

## 6. Role Cell and Pattern Zone Contract

`SectorClusterRoleZoneBuilder.Build` receives a valid `SectorPlannerInput`, matching assignments, fixed anchor plan, cluster placement plan, and spine-envelope plan.

For each selected cluster placement:

- publish role cells only inside the placed cluster footprint.
- a role cell maps to one sector-local 12x8 MicroChunk cell and carries its cluster ID, variant ID, role kind, source edge/node/anchor identity, and protection flag.
- role cells must cover every placed cluster footprint cell exactly once.
- cells crossed by route centerline/envelope are marked `ProtectedOpen` or `RouteShoulder` as appropriate.
- Special entry/return approach cells are marked `SpecialApproach`.
- boundary approach cells are marked `BoundaryApproach`.
- recovery-related cells are marked `RecoverySupport`.
- all other cluster body cells become `ClusterCore`, `PatternFill`, `QuietBuffer`, or `ClusterEdge` according to cluster pacing and position.

Pattern zones:

- each 12x8 MicroChunk cell is partitioned into six aligned 4x4 MicroPattern slots.
- each zone has exact tile rect inside `48x32`, exact owning role cell, and zone kind.
- zones never extend outside the placed cluster footprint.
- zones outside selected cluster placements are not created; MAP14_06 owns free-space/Quiet fill.
- zones may intersect ProtectedOpen only as `ProtectedNoWrite` or with MAP10 protected mask evidence.
- ProtectedOpen, Special entry/return, boundary bridge and route socket cells must receive no final pattern writes.
- pattern zones cannot create route/access/socket/Special ownership.

If role cells or zones cannot be published without overlap/out-of-bounds/protected violations, fail atomically.

## 7. MicroPattern Selection and Render Contract

`SectorPatternRenderPlanner.Render` receives a successful role-zone plan and the MAP10 public MicroPattern application/render authority.

Selection rules:

- select patterns deterministically from public MAP10 pattern authority.
- prefer biome-compatible patterns, then zone-kind compatibility, then role/pacing compatibility, then repetition-signature diversity, then stable pattern ID order.
- no RNG draw is allowed in MAP14_05.
- no retry/backtracking is allowed in MAP14_05.
- failure to find a compatible pattern is atomic `MissingPatternCandidate`.
- if MAP10 candidate APIs do not expose all needed selectors, use MAP14_05 reference projection adapters and report them as `REFERENCE PATTERN SELECTION`, not production RNG.

Render rules:

- use MAP10 `MicroPatternApplicationPlanner` or equivalent public application API to build renderer-ready application plans.
- pass ProtectedOpen, spine/envelope, boundary and Special entry evidence into the MAP10 protected mask.
- use MAP10 `MicroPatternOrderedRenderer` or equivalent public ordered renderer API to apply application plans to an in-memory render target.
- every rendered cell must be inside `48x32` and inside a pattern zone.
- protected writes must be `0`; masked/protected no-write evidence must be counted.
- conflicts reject atomically; no first/last/write-order fallback.
- no tilemap, Scene, prefab, asset, final sector canvas owner, Generated CSV or file export is written.

Success report should publish:

```text
sector count
cluster placement count
role cell count by kind
pattern zone count by kind
selected pattern count
MAP10 application plan count
MAP10 renderer invocation count
render target cell count
rendered changed cell count
idempotent/no-change cell count
protected mask hit count
protected write count 0
renderer conflict count 0
pattern zone overlap count 0
out-of-cluster zone count 0
role-zone plan digest
pattern render plan digest
MAP14_06 handoff readiness flag
```

Failure should publish:

```text
role-zone plan or render plan null as appropriate
digest empty
stable sorted errors only
mutation counters 0
```

## 8. Identity and No-Mutation Proof

Build/render must prove before/after equality for:

```text
SectorPlannerInput digest
PacingAssignment digest
FixedAnchorPlan digest
ClusterPlacementPlan digest
SpineEnvelopePlan digest
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells
ProtectedOpen coordinates and envelope digest
MAP10 source pattern definitions/digests
```

The following counters must remain 0:

```text
solver invocation
RNG draw
retry/backtracking
tilemap write
final canvas ownership write
Activity/Event placement
Scene/Prefab/Tilemap/GameObject mutation
```

MAP10 renderer invocation is expected to be positive. That is not a tilemap or final ownership mutation; it is an immutable in-memory renderer delta.

## 9. Focused Fixture Matrix

Reuse the MAP14_01~04 fixture chain through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected role/pattern responsibility |
|---|---|
| `PlainTraversalBoundarySector` | route shoulder and boundary blend zones with ProtectedOpen no-write evidence |
| `QuietBufferSector` | quiet/edge/body zones only inside selected cluster footprint |
| `VillageReferenceSector` | Village reference marker respected without progression ownership |
| `CoreResourceSector` | SpecialApproach zones around Core entry/return, reward execution 0 |
| `ForgeLandmarkSector` | SpecialApproach/Machinery-like pattern evidence without crafting execution |
| `BossGateSector` | Boss approach/body zones without combat object creation |
| `ActivityCompatibleSector` | Activity-compatible role evidence but no Activity/Event placement |
| `DeferredOptionalSector` | optional deferred fact may influence Discovery/Detail pattern choice only; placed Special ownership 0 |
| `NeighborInfluencedSector` | neighbor evidence influences zone labels only, not sockets/world rollback |
| `InvalidInputCases` | missing pattern/protected write/conflict/mutation failures publish zero plan |

Fixtures are `REFERENCE ROLE ZONE` and `REFERENCE PATTERN CANVAS` examples, not production world seeds.

## 10. Required Tests

`SectorClusterRolePatternRenderTests` must include 9~12 focused tests in category `MAP14_05`.

Minimum assertions:

1. `BuildPublishesClusterRoleCellsAndPatternZonesFromSpineEnvelope`
   - valid input/anchor/placement/spine publishes immutable role-zone plan, lower-hex digest, no partial error.
2. `RoleCellsCoverPlacedClusterFootprintsExactlyOnce`
   - every selected cluster footprint cell has exactly one role cell; no role cell outside placements.
3. `PatternZonesPartitionRoleCellsIntoAlignedFourByFourSlots`
   - six 4x4 zones per 12x8 role cell, inside `48x32`, inside cluster footprint, no overlap.
4. `ProtectedOpenBoundaryAndSpecialEntryCellsReceiveNoPatternWrites`
   - MAP14_04 ProtectedOpen and MAP14_02 anchors pass through MAP10 protected mask; protected writes 0.
5. `RenderUsesMap10ApplicationPlannerAndOrderedRenderer`
   - application plan count and renderer invocation count are positive and match selected zones.
6. `RenderedPatternCanvasIsInMemoryAndDoesNotFinalizeOwnership`
   - render delta/digest exists; tilemap/final canvas/asset/Scene mutation counters 0.
7. `PatternSelectionIsDeterministicWithoutRngOrRetry`
   - selected patterns stable by biome/zone/role/signature/order; RNG/retry counters 0.
8. `SpecialVillageOptionalAndActivityBoundariesRemainNonOwning`
   - Special approach rendered as static evidence only; Village non-blocking; Merchant/Maru and Activity/Event ownership 0.
9. `InvalidMissingPatternProtectedConflictAndMutationClaimsFailAtomically`
   - no role/render plan/digest on missing pattern, protected write attempt, renderer conflict, mutation claims.
10. `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
    - repeat/reverse/`tr-TR` stable role-zone and render digests.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 11. Expected Result Report

Result must begin:

```text
TASK: MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS
STATUS: PASS | FAIL | BLOCKED
MAP14_05: COMPLETE ELIGIBLE only when PASS
MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 role cell + MicroPattern render이며 Activity/Event/final Tilemap이 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 sector/cluster/role-cell/pattern-zone/selected-pattern/application-plan/renderer-invocation/rendered-cell 수치
- changed/no-change/protected-mask/protected-write/conflict 수치
- MAP10 renderer를 실제로 사용했다는 증거
- ProtectedOpen, anchor, cluster, route/access identity가 변하지 않았다는 증거
- MAP13 SpecialRegion은 static approach/ProtectedOpen evidence로만 소비됐다는 증거
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_06

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_05]
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
Commit subject: MAP14_05: assign cluster roles and render patterns
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_06.

## 12. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS.md
MCP_ARCHIVE/MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS.md
MCP/REPORTS/MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterRolePatternPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterRolePatternPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterRoleZoneBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterRoleZoneBuilder.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPatternRenderPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPatternRenderPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterRolePatternRenderTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterRolePatternRenderTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_06: do not start
STOP after Result and optional PASS finalize commit
```
