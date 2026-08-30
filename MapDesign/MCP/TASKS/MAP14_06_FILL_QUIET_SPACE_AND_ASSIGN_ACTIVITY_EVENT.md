```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT
  task_file: TASKS/MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT.md
  requires_current_task: NONE
  requires_completed_task: MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS
  requires_result:
    path: REPORTS/MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS_RESULT.md
    status: PASS
    sha256: bb330f365754d4f9e5bf491d3684fb09d2550a4b34d1eef90a5f69850aa50508
  requires_installed_task:
    path: TASKS/MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS.md
    sha256: e50fa3fb4e08b73f23aca0c6f533661eba761fc876318900819ee7d8c054fc09
  sets_current_task: MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT
```

# MAP14_06 - Fill Quiet Space and Assign Activity/Event

```text
TASK: MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_05가 게시한 sector별 `REFERENCE PATTERN CANVAS`를 소비해서 selected cluster 밖 또는 pattern이 소유하지 않은 나머지 sector free space를 `Quiet/Buffer` 후보로 채우고, MAP12의 Activity frequency/cap 및 Event marker-only assignment 규칙을 사용해 sector-local Activity/Event marker plan을 게시한다.

```text
SectorPlannerInput
SectorPacingAssignment
SectorFixedAnchorPlan
SectorClusterPlacementPlan
SectorSpineEnvelopePlan
SectorClusterRolePatternPlan
SectorPatternRenderPlan
MAP12 Activity compatibility/frequency/cap authority
MAP12 Event overlay assignment authority
→ SectorQuietFillPlanner
→ SectorActivityEventPlacementPlanner
→ immutable SectorQuietActivityEventPlan
→ MAP14_07 final ownership/conflict input
```

이번 Task는 Quiet fill과 Activity/Event marker 배치의 **계획 데이터**만 게시한다. final canvas ownership, layer conflict resolver, local retry/backtracking, MAP14 production RNG policy, Tilemap bake, Scene/Prefab/GameObject 반영, collider/physics/player traversal, reward/combat/crafting/NPC 실행은 하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, Quiet/Buffer/Activity/Event 실제 수치, MAP12 public planner 사용 증거, 보호 영역 침범 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| MAP14_05 reference pattern canvas 이후의 remaining free-space classification | final sector ownership canvas |
| Quiet/Buffer cell plan and reason evidence | layer conflict priority or double-owner resolver |
| sector-local Activity opportunity projection | local retry/backtracking |
| MAP12 Activity compatibility/frequency/cap handoff | MAP14 production RNG policy |
| sector-local Event marker opportunity projection | world-scale 169-sector solve |
| MAP12 Event marker-only assignment handoff | actual Activity/Event/NPC/reward spawn |
| no-write proof for ProtectedOpen, anchors, Special fixed shells and pattern cells | gameplay state machine, inventory, combat or crafting execution |
| immutable handoff to MAP14_07 | Tilemap/collider/Scene/Prefab/GameObject mutation |
| focused EditMode tests | PlayMode physics/player reachability |

Activity/Event placement here means marker-only immutable plan publication. It cannot claim final terrain ownership, live gameplay visibility, persistence state, or runtime object spawning.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_06`만 선택한다.

```text
MAP14_06 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01~05 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_06` category로 제한한다.

신규 task-owned failure는 신규 MAP14_06 allowlist 파일만 수정하고 `MAP14_06` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_05 Result: PASS
MAP14_05 Result SHA-256:
bb330f365754d4f9e5bf491d3684fb09d2550a4b34d1eef90a5f69850aa50508

MAP14_05 installed Task SHA-256:
e50fa3fb4e08b73f23aca0c6f533661eba761fc876318900819ee7d8c054fc09

MAP14_05 COMPLETE / MAP14_06 CURRENT / MAP14_07 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput and SectorPacingAssignment
MAP14_02: SectorFixedAnchorPlan and anchor identities
MAP14_03: SectorClusterPlacementPlan, chosen cluster/variant, footprint cells
MAP14_04: SectorSpineEnvelopePlan, ProtectedOpen, node/edge/route identities
MAP14_05: SectorClusterRolePatternPlan and SectorPatternRenderPlan
MAP12_01~02: ActivityShellCanvas and removal-safety proof where public
MAP12_03: ActivityCompatibility, ActivityCandidateIndex, ActivityFrequencyPlanner authority
MAP12_04: EventOverlayCandidateIndex and EventOverlayAssignmentPlanner authority
MAP12_05~07: physical Activity/Event catalog facts and exit-approved marker-only constraints where public
MAP13: SpecialRegion footprint/fixed shell/Village reference identity where public
MAP09: layer ownership, PacingRole, AccessClass, MicroPattern/MicroChunk constants
```

MAP14_06 must consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_06-side projection only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorQuietActivityEventPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorQuietFillPlanner.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorActivityEventPlacementPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorQuietActivityEventPlannerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_06
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
final canvas ownership/conflict resolver
MAP14 retry/RNG policy
```

`SectorPlanning` folders and metas were created by MAP09_00. If missing, report `BLOCKED`; do not create folder metas in this Task.

## 5. Runtime API Surface

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_06 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorQuietFillCellKind
SectorQuietFillSourceKind
SectorActivityEventMarkerKind
SectorActivityEventPlacementState
SectorQuietFillCell
SectorActivityOpportunityProjection
SectorEventMarkerOpportunityProjection
SectorActivityPlacementDecision
SectorEventMarkerPlacementDecision
SectorQuietActivityEventPlan
SectorQuietActivityEventBuildRequest
SectorQuietActivityEventBuildResult
SectorQuietActivityEventErrorCode
SectorQuietActivityEventError
SectorQuietFillPlanner.Fill
SectorActivityEventPlacementPlanner.Place
SectorQuietActivityEventCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum Quiet fill cell kinds:

```text
QuietBuffer
QuietAir
QuietSolid
RouteMargin
BoundaryMargin
SpecialMargin
ActivityCandidate
EventCandidate
ProtectedNoWrite
ReservedNoWrite
AlreadyPatternRendered
```

Minimum source kinds:

```text
ReferencePatternCanvas
ProtectedOpen
RouteEnvelope
BoundaryAnchor
SpecialAnchor
SpecialFixedShell
VillageReference
ClusterFootprint
ClusterPatternZone
ActivityCompatibility
EventMarkerOpportunity
ManualReferenceFixture
```

Minimum marker kinds:

```text
ActivityCue
ActivityCore
ActivityReward
ActivityRecovery
EventTerrain
EventActivity
EventSpecial
EventEmpty
```

Minimum error groups:

```text
MissingInput | MissingPatternRenderPlan | MissingSpineEnvelopePlan
MissingActivityAuthority | MissingEventAuthority | SectorMismatch
QuietCellOutOfBounds | DuplicateQuietCell | QuietCellTouchesProtectedOpen
QuietCellTouchesFinalOwner | PatternCanvasMutationClaim
ActivityOpportunityOutOfBounds | ActivityOpportunityOverlapsProtected
ActivityFrequencyRejected | ActivityStrongCapViolation
ActivityRemovalSafetyMissing | ActivityMarkerMutationClaim
EventOpportunityOutOfBounds | EventOpportunityOverlapsProtected
EventAssignmentRejected | EventCooldownViolation | MissingEmptyEvent
EventMarkerMutationClaim | SpecialPersistenceMutationClaim
AnchorMutationClaim | ClusterMutationClaim | SpineEnvelopeMutationClaim
OwnershipMutationClaim | SolverMutationClaim | RngMutationClaim
TileMutationClaim | NonCanonicalPublication
```

## 6. Quiet/Buffer Fill Contract

`SectorQuietFillPlanner.Fill` receives valid MAP14_01~05 plans for the same sector set.

For each sector:

- classify every sector-local tile or cell that is not already claimed by selected cluster role cells, MAP14_05 pattern zones, MAP14_04 ProtectedOpen, MAP14_02 fixed anchors, or MAP13 Special fixed shells.
- publish Quiet/Buffer cells as planning evidence only; do not write a final canvas owner.
- keep `AlreadyPatternRendered` evidence for MAP14_05 cells that should not be overwritten.
- keep `ProtectedNoWrite` or `ReservedNoWrite` evidence for route, anchor, Special fixed shell, Village reference and boundary bridge cells.
- maintain exact `48x32` sector-local bounds.
- do not carve, bridge, close gaps, add fallback corridors, or change route/access/socket identities.
- do not mutate MAP14_05 render cells. The before/after `SectorPatternRenderPlan` digest must be identical.

The fill result should distinguish:

```text
quiet fill cells
buffer fill cells
reserved/no-write cells
already pattern-rendered cells
protected/no-write cells
unclassified remainder cells
```

`unclassified remainder cells` may be positive only when Result explains the explicit reason and proves they are outside MAP14_06 ownership. A normal fixture should make the count 0.

If fill cannot be published without overlap, out-of-bounds, protected, owner or mutation violations, fail atomically.

## 7. Activity Placement Contract

`SectorActivityEventPlacementPlanner.Place` consumes the successful Quiet fill plan plus MAP12 public Activity compatibility/frequency/cap authority.

Activity behavior:

- build sector-local Activity opportunities only from eligible Quiet/Buffer or ActivityCandidate areas.
- reject opportunities that overlap ProtectedOpen, fixed anchors, Special fixed shell, already pattern-rendered protected cells, route envelope no-write cells, or reserved boundary/Special entry cells.
- consume MAP12 public compatibility/frequency/cap rules rather than reimplementing private CSV parsing.
- preserve MAP12 shell/removal-safety proof: selected Activity markers must remain removable and static route identity must not change.
- preserve Strong cap evidence if MAP12_03 public planner is used.
- publish marker decisions only; do not spawn gameplay objects, do not mutate Activity catalog, and do not write final ownership.

RNG policy:

- Do not create a new MAP14 RNG stream.
- Prefer deterministic reference selection from stable opportunity order where this Task can prove compatibility without random draw.
- If a MAP12 public planner requires deterministic RNG, use only its approved stream through public API and report stream name/draw count. Do not add retry/backtracking or new production RNG policy.
- If this cannot be done without opening MAP14_08-owned policy, report `BLOCKED`.

Required Activity evidence:

```text
activity opportunity count
activity compatible candidate count
activity selected count
activity rejected count by reason
Strong selected count
Strong cap before/after evidence where public
removal-safety identity preserved YES
activity marker mutation count 0
activity runtime spawn count 0
```

## 8. Event Marker Assignment Contract

Event placement consumes the same successful Quiet/Activity plan plus MAP12 public Event overlay assignment authority.

Event behavior:

- project TerrainCluster/Quiet/Activity/Special marker opportunities without modifying their owners.
- every opportunity must have exactly one compatible Empty candidate and zero or more non-empty compatible Event candidates according to MAP12_04 rules.
- selected non-empty Event markers must respect MAP12 cooldown evidence where public.
- Empty remainder is explicit and counted, not omitted.
- Event markers are marker-only. They cannot change Canvas, Static Shell, route, access, pacing, envelope, protection, Special persistence, Scene/Prefab, or Tilemap.
- SpecialRegion opportunities may use MAP13 fixed-shell and approach evidence but cannot transfer persistence ownership to Event.

Required Event evidence:

```text
event marker opportunity count
non-empty compatible candidate count
empty compatible candidate count
event assigned non-empty count
event assigned Empty count
cooldown exclusion count
cooldown violation count 0
event marker mutation count 0
event runtime spawn count 0
```

## 9. Identity and No-Mutation Proof

Build must prove before/after equality for:

```text
SectorPlannerInput digest
PacingAssignment digest
FixedAnchorPlan digest
ClusterPlacementPlan digest
SpineEnvelopePlan digest
SectorClusterRolePatternPlan digest
SectorPatternRenderPlan digest
MAP12 Activity catalog/profile/plan digests consumed where public
MAP12 Event catalog/profile/plan digests consumed where public
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells
ProtectedOpen coordinates and envelope digest
MAP10 pattern render cell identities
```

The following counters must remain 0:

```text
final canvas ownership write
layer conflict resolution
solver invocation
MAP14 RNG draw
retry/backtracking
tilemap write
Scene/Prefab/Tilemap/GameObject mutation
Activity runtime spawn
Event runtime spawn
Special persistence mutation
reward/combat/crafting/inventory/NPC execution
```

If MAP12 public planners are called and they report approved deterministic RNG draws, those draws must be reported separately and must not be counted as MAP14 RNG policy.

## 10. Focused Fixture Matrix

Reuse the MAP14_01~05 fixture chain through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected MAP14_06 responsibility |
|---|---|
| `PlainTraversalBoundarySector` | fills non-cluster space as Quiet/BoundaryMargin and keeps route/boundary protected writes 0 |
| `QuietBufferSector` | creates positive Quiet/Buffer fill and eligible Activity/Event opportunity evidence |
| `VillageReferenceSector` | preserves Village `ReferenceOnly` identity and blocks progression ownership |
| `CoreResourceSector` | permits Special approach marker opportunities without reward execution |
| `ForgeLandmarkSector` | permits static event opportunity evidence without crafting execution |
| `BossGateSector` | permits boss approach marker evidence without combat object creation |
| `ActivityCompatibleSector` | selects or rejects Activity markers by MAP12 compatibility/frequency/cap only |
| `DeferredOptionalSector` | optional/deferred facts may influence marker eligibility only; placed Special ownership 0 |
| `NeighborInfluencedSector` | neighbor evidence may affect Quiet/Event labels only, not sockets/world rollback |
| `InvalidInputCases` | protected overlap, missing MAP12 authority, duplicate quiet cell, mutation claims fail atomically |

Fixtures are `REFERENCE QUIET ACTIVITY EVENT` examples, not production world seeds.

## 11. Required Tests

`SectorQuietActivityEventPlannerTests` must include 9~12 focused tests in category `MAP14_06`.

Minimum assertions:

1. `FillPublishesQuietAndBufferForRemainingReferenceCanvasSpace`
   - valid MAP14_01~05 input publishes immutable Quiet fill plan, lower-hex digest, no partial error.
2. `QuietFillAvoidsProtectedOpenAnchorsSpecialShellsAndPatternCells`
   - ProtectedOpen, boundary anchors, Special fixed shells and already pattern-rendered no-write cells receive no fill ownership.
3. `QuietFillIsInBoundsUniqueAndDoesNotMutatePatternRender`
   - all fill cells are inside `48x32`, unique, and MAP14_05 pattern render digest remains unchanged.
4. `ActivityOpportunitiesUseMap12CompatibilityFrequencyAndCaps`
   - Activity opportunities and selected markers come from MAP12 public compatibility/frequency/cap authority.
5. `ActivityMarkersPreserveRemovalSafetyAndStaticRouteIdentity`
   - removal-safety proof and route/access identity are preserved; runtime spawn and marker mutation counters 0.
6. `EventMarkerAssignmentUsesMap12CooldownAndExplicitEmpty`
   - Event opportunity decisions include non-empty and explicit Empty assignments with cooldown violation 0.
7. `SpecialVillageBossForgeMarkersRemainNonOwning`
   - Special/Village/Boss/Forge evidence remains marker-only and persistence/progression/combat/crafting mutation 0.
8. `NoFinalOwnershipRetryTilePhysicsOrSceneMutation`
   - final ownership, resolver, retry, MAP14 RNG, Tilemap, Scene/Prefab/GameObject and PlayMode counters are 0.
9. `InvalidProtectedOverlapMissingAuthorityDuplicateAndMutationClaimsFailAtomically`
   - invalid requests publish null plan, empty digest, stable-sorted errors and zero mutation.
10. `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
    - repeat/reverse/`tr-TR` produce identical quiet/activity/event digests and stable marker decisions.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 12. Expected Result Report

Result must begin:

```text
TASK: MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT
STATUS: PASS | FAIL | BLOCKED
MAP14_06: COMPLETE ELIGIBLE only when PASS
MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 Quiet/Buffer fill + Activity/Event marker-only plan이며 final Tilemap/ownership/gameplay spawn이 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 sector/quiet/buffer/reserved/protected/pattern-rendered/unclassified 수치
- Activity opportunity/compatible/selected/rejected/Strong/removal-safety 수치
- Event opportunity/non-empty/Empty/cooldown 수치
- MAP12 Activity/Event public planner 또는 public projection을 사용했다는 증거
- ProtectedOpen, anchor, Special shell, MAP14_05 render identity가 변하지 않았다는 증거
- final ownership/retry/MAP14 RNG/Tilemap/Scene/Prefab/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_07

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_06]
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
Commit subject: MAP14_06: fill quiet space and assign activity event
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_07.

## 13. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT.md
MCP_ARCHIVE/MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT.md
MCP/REPORTS/MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorQuietActivityEventPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorQuietActivityEventPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorQuietFillPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorQuietFillPlanner.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorActivityEventPlacementPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorActivityEventPlacementPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorQuietActivityEventPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorQuietActivityEventPlannerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_07: do not start
STOP after Result and optional PASS finalize commit
```
