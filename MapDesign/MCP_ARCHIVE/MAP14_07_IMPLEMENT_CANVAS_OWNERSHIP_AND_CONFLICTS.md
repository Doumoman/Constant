```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS
  task_file: TASKS/MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS.md
  requires_current_task: NONE
  requires_completed_task: MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT
  requires_result:
    path: REPORTS/MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT_RESULT.md
    status: PASS
    sha256: 4f5ee342ee1b3ed2d583b53bd229774b7a70d0bbad4df326945af7d99d44fb31
  requires_installed_task:
    path: TASKS/MAP14_06_FILL_QUIET_SPACE_AND_ASSIGN_ACTIVITY_EVENT.md
    sha256: 7856c7f3db536989b20ffa996333b38960782dd0b5bb77713122040fe3e45e05
  sets_current_task: MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS
```

# MAP14_07 - Implement Canvas Ownership and Conflicts

```text
TASK: MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01~06이 게시한 anchor, spine, cluster, pattern, Quiet, Activity/Event marker evidence를 한 장의 immutable sector-local ownership canvas로 합친다.

```text
SectorPlannerInput
SectorPacingAssignment
SectorFixedAnchorPlan
SectorClusterPlacementPlan
SectorSpineEnvelopePlan
SectorClusterRolePatternPlan
SectorPatternRenderPlan
SectorQuietActivityEventPlan
→ SectorCanvasOwnershipClaimBuilder
→ SectorCanvasOwnershipResolver
→ immutable SectorCanvasOwnershipPlan
→ MAP14_08 retry/RNG policy input
```

이번 Task는 **final reference ownership canvas**를 만든다. 여기서 final은 MAP14 내부의 in-memory ownership 판정이라는 뜻이며 Tilemap bake, Scene/Prefab/GameObject 생성, collider/physics/player traversal, local retry/RNG policy, 169-sector world assembly, Activity/Event runtime spawn은 하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, ownership claim/winner/suppressed/conflict 실제 수치, 우선순위 증거, double-owner 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| Special/Boundary/Spine/Cluster/Pattern/Quiet/Activity/Event ownership claim 모델 | local retry/backtracking |
| sector-local `48x32` claim aggregation | MAP14 production RNG policy |
| deterministic priority winner selection | pattern/cluster reselection |
| suppressed claim evidence | fallback corridor carve or gap repair |
| double-owner and forbidden-overlap detection | 169-sector world assembly |
| final reference canvas digest and handoff | Tilemap bake / MicroChunk slice / streaming |
| marker-overlay ownership plane validation | collider/physics/player traversal |
| focused EditMode tests | Scene/Prefab/GameObject mutation or gameplay spawn |

Do not silently overwrite lower-priority claims. Every overlapping claim must become either an allowed suppressed claim with explicit priority evidence or an atomic conflict error.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_07`만 선택한다.

```text
MAP14_07 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01~06 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_07` category로 제한한다.

신규 task-owned failure는 신규 MAP14_07 allowlist 파일만 수정하고 `MAP14_07` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_06 Result: PASS
MAP14_06 Result SHA-256:
4f5ee342ee1b3ed2d583b53bd229774b7a70d0bbad4df326945af7d99d44fb31

MAP14_06 installed Task SHA-256:
7856c7f3db536989b20ffa996333b38960782dd0b5bb77713122040fe3e45e05

MAP14_06 COMPLETE / MAP14_07 CURRENT / MAP14_08 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput and SectorPacingAssignment
MAP14_02: SectorFixedAnchorPlan and anchor identities
MAP14_03: SectorClusterPlacementPlan, chosen cluster/variant, footprint cells
MAP14_04: SectorSpineEnvelopePlan, ProtectedOpen, route envelope, node/edge identities
MAP14_05: SectorClusterRolePatternPlan and SectorPatternRenderPlan
MAP14_06: SectorQuietActivityEventPlan and Quiet/Activity/Event marker decisions
MAP13: SpecialRegion fixed shell, Village reference, Core/Forge/Boss identity where public
MAP12: Activity/Event marker-only identity and no-mutation evidence where public
MAP10/MAP11: MicroPattern/TerrainCluster identity where public
MAP09: layer ownership, PacingRole, AccessClass, MicroPattern/MicroChunk constants
```

MAP14_07 must consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_07-side projection only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipClaimBuilder.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipResolver.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorCanvasOwnershipResolverTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_07
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
MAP14 retry/RNG policy
Tilemap bake or MicroChunk slice exporter
```

`SectorPlanning` folders and metas were created by MAP09_00. If missing, report `BLOCKED`; do not create folder metas in this Task.

## 5. Runtime API Surface

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_07 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorCanvasOwnerKind
SectorCanvasOwnershipPlane
SectorCanvasClaimState
SectorCanvasOwnershipPriority
SectorCanvasOwnershipClaim
SectorCanvasOwnedCell
SectorCanvasSuppressedClaim
SectorCanvasConflict
SectorCanvasOwnershipPlan
SectorCanvasOwnershipBuildRequest
SectorCanvasOwnershipBuildResult
SectorCanvasOwnershipErrorCode
SectorCanvasOwnershipError
SectorCanvasOwnershipClaimBuilder.BuildClaims
SectorCanvasOwnershipResolver.Resolve
SectorCanvasOwnershipCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial ownership plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum owner kinds:

```text
SpecialRegion
Boundary
Spine
TerrainCluster
MicroPattern
Quiet
ActivityMarker
EventMarker
ReservedNoWrite
ProtectedNoWrite
Empty
```

Minimum ownership planes:

```text
Terrain
Protection
Reservation
Marker
Evidence
```

Minimum claim states:

```text
Winner
SuppressedByPriority
AllowedCoPlaneEvidence
RejectedConflict
RejectedForbiddenOverlap
RejectedOutOfBounds
RejectedMutationClaim
```

Minimum error groups:

```text
MissingInput | MissingQuietActivityEventPlan | MissingPatternRenderPlan
MissingSpineEnvelopePlan | SectorMismatch | ClaimOutOfBounds
DuplicateClaimIdentity | MissingRequiredClaim | MissingPriorityRule
ForbiddenOverlap | DoubleOwnerConflict | MarkerPlaneConflict
ProtectionPlaneConflict | ReservationPlaneConflict | TerrainPlaneConflict
SuppressedClaimWithoutWinner | WinnerWithoutClaim | OwnedCellOutOfBounds
CanvasCoverageMismatch | NonCanonicalPublication
SpecialPersistenceMutationClaim | BoundaryMutationClaim | SpineEnvelopeMutationClaim
ClusterMutationClaim | PatternRenderMutationClaim | QuietMutationClaim
ActivityMarkerMutationClaim | EventMarkerMutationClaim
SolverMutationClaim | RngMutationClaim | TileMutationClaim | SceneMutationClaim
```

## 6. Ownership Priority Contract

`SectorCanvasOwnershipResolver.Resolve` receives a successful claim build result and applies deterministic priority rules. The default owner priority is:

```text
1. SpecialRegion
2. Boundary
3. Spine
4. TerrainCluster
5. MicroPattern
6. Quiet
7. ActivityMarker
8. EventMarker
9. ReservedNoWrite / ProtectedNoWrite / Empty evidence
```

Plane rules:

- `Terrain` plane has at most one winner per sector-local tile.
- `Protection` plane has at most one winner per sector-local tile; it may coexist with `Terrain` only as no-write evidence.
- `Reservation` plane has at most one winner per sector-local tile; it may coexist with `Terrain` only when the terrain owner is the same upstream identity or the reservation explicitly marks no-write.
- `Marker` plane may coexist with `Terrain`, `Protection` and `Reservation`, but only if the marker source is allowed by MAP12/MAP13 marker-only evidence.
- `Evidence` plane can carry diagnostic facts and must never become a tile owner.

Priority does not mean last-write-wins. If two claims target the same plane and tile:

- higher priority becomes winner only when the lower claim is allowed to be suppressed.
- suppression must publish winner ID, suppressed claim ID, priority comparison and reason.
- equal priority duplicate or ambiguous priority is `DoubleOwnerConflict`.
- forbidden overlaps are atomic conflicts even when one priority is higher.

Special fixed shell, boundary bridges, route spine/ProtectedOpen, cluster footprint, MicroPattern render, Quiet fill, Activity marker and Event marker must all remain traceable to their source task identity.

## 7. Claim Builder Contract

`SectorCanvasOwnershipClaimBuilder.BuildClaims` consumes MAP14_01~06 public plans and creates canonical claims.

Required claim sources:

```text
SpecialRegion fixed shell / Village reference / Core / Forge / Boss approach
Boundary fixed slice / external sockets / boundary bridge / warning reserved cells
Spine centerline / route envelope / ProtectedOpen / recovery
TerrainCluster selected footprint and role cells
MicroPattern rendered cells and layer semantics
Quiet/Buffer fill cells
Activity marker decisions
Event marker decisions including explicit Empty evidence
ReservedNoWrite and ProtectedNoWrite evidence
```

Claims must include:

```text
claim ID
sector coordinate
sector-local tile coordinate
ownership plane
owner kind
owner priority
source task ID
source object ID
source digest
semantic value
required/optional flag
allow suppression flag
no-write flag
marker-only flag
```

The claim builder must prove:

- all claims are inside `48x32`.
- claim identity is unique.
- every required source in MAP14_01~06 contributes expected claims or explicit empty evidence.
- upstream plan digests remain before/after equal.
- no claim mutates source plans or source render cells.
- no Activity/Event marker becomes a terrain owner.

If claim creation cannot satisfy source coverage, identity or bounds, fail atomically.

## 8. Canvas Coverage and Conflict Contract

The resolved `SectorCanvasOwnershipPlan` must publish, per sector:

```text
owned cells by plane
winner claims
suppressed claims
allowed cross-plane coexistence count
empty/evidence-only coordinates
conflicts by type
coverage count
canonical digest
MAP14_08 handoff readiness flag
```

Coverage rule:

- every `48x32` coordinate in every reference sector must have either a Terrain winner or explicit no-terrain evidence.
- every ProtectedOpen coordinate must have Protection winner and no Terrain claim that violates no-write.
- every Special/Boundary reservation coordinate must have Reservation winner or matching owner evidence.
- every Activity/Event selected or Empty decision must be represented on the Marker or Evidence plane.
- no tile may have two winners in the same plane.
- no rejected conflict may remain in a PASS plan.

Allowed examples:

```text
TerrainCluster terrain + Spine protection evidence when route envelope owns no-write
MicroPattern terrain suppressed by SpecialRegion fixed shell
Quiet terrain suppressed by Boundary reservation
ActivityMarker marker over eligible Quiet/Terrain plane
EventMarker explicit Empty as Evidence plane
```

Forbidden examples:

```text
ActivityMarker as Terrain owner
EventMarker overwriting Special persistence
MicroPattern writing through ProtectedOpen no-write
Quiet becoming winner inside PatternRendered coordinate
Boundary and Special both winning Reservation plane
Spine and Boundary both winning Protection plane without explicit bridge rule
same-priority duplicate Terrain owner
```

## 9. Identity and No-Mutation Proof

Build/resolve must prove before/after equality for:

```text
SectorPlannerInput digest
PacingAssignment digest
FixedAnchorPlan digest
ClusterPlacementPlan digest
SpineEnvelopePlan digest
SectorClusterRolePatternPlan digest
SectorPatternRenderPlan digest
SectorQuietActivityEventPlan digest
MAP12 Activity/Event authority digests consumed where public
MAP13 SpecialRegion identity consumed where public
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells
ProtectedOpen coordinates and envelope digest
MAP10 pattern render cell identities
Quiet fill cell identities
Activity/Event marker decision identities
```

The following counters must remain 0:

```text
retry/backtracking
MAP14 RNG draw
solver invocation
pattern/cluster reselection
tilemap write
Scene/Prefab/Tilemap/GameObject mutation
Activity runtime spawn
Event runtime spawn
Special persistence mutation
reward/combat/crafting/inventory/NPC execution
```

Final reference canvas ownership writes are expected in memory and must be counted separately from Tilemap or Unity object mutation.

## 10. Focused Fixture Matrix

Reuse the MAP14_01~06 fixture chain through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected MAP14_07 responsibility |
|---|---|
| `PlainTraversalBoundarySector` | boundary/spine/cluster/pattern/Quiet claims resolve without same-plane double owners |
| `QuietBufferSector` | Quiet wins only where pattern/anchor/protection did not already own or reserve |
| `VillageReferenceSector` | Village remains reference/reservation evidence and does not become progression owner |
| `CoreResourceSector` | SpecialRegion fixed shell beats pattern/Quiet and keeps marker-only Event evidence |
| `ForgeLandmarkSector` | Forge reservation wins without crafting execution |
| `BossGateSector` | Boss approach/fixed shell wins without combat object creation |
| `ActivityCompatibleSector` | Activity marker stays Marker plane over allowed terrain owner |
| `DeferredOptionalSector` | deferred Merchant/Maru facts remain evidence/marker-only without Special ownership transfer |
| `NeighborInfluencedSector` | neighbor evidence does not create world rollback or socket mutation |
| `InvalidInputCases` | equal priority, forbidden overlap, missing claim, mutation claim, out-of-bounds fail atomically |

Fixtures are `REFERENCE OWNERSHIP CANVAS` examples, not production world seeds.

## 11. Required Tests

`SectorCanvasOwnershipResolverTests` must include 9~12 focused tests in category `MAP14_07`.

Minimum assertions:

1. `BuildClaimsPublishesAllSourceOwnersForReferenceSectorCanvas`
   - valid MAP14_01~06 input publishes immutable claim set, lower-hex digest, no partial error.
2. `ResolverAppliesSpecialBoundarySpineClusterPatternQuietMarkerPriority`
   - priority winners and suppressed claims match the declared order with explicit reason evidence.
3. `ResolvedCanvasHasNoSamePlaneDoubleOwners`
   - Terrain/Protection/Reservation/Marker planes each have at most one winner per tile.
4. `ProtectedOpenAnchorsSpecialShellsAndPatternNoWriteRulesHold`
   - ProtectedOpen, fixed anchors, Special shells and MAP14_05 pattern no-write evidence cannot be overwritten.
5. `ActivityAndEventMarkersRemainMarkerOnlyOrEvidenceOnly`
   - Activity/Event selected/Empty decisions never become Terrain owners and runtime spawn counters stay 0.
6. `CoveragePublishesTerrainWinnerOrExplicitNoTerrainEvidenceForEveryTile`
   - all 9 * 48 * 32 coordinates have exact coverage and canonical ownership/evidence.
7. `ConflictRulesRejectEqualPriorityForbiddenOverlapAndMissingWinner`
   - invalid conflicts fail atomically with null plan, empty digest and stable-sorted errors.
8. `UpstreamIdentityAndRenderQuietMarkerPlansAreNotMutated`
   - all upstream digests, route/access, cluster/pattern/Quiet/marker identities remain equal.
9. `NoRetryRngTilePhysicsSceneOrGameplayMutation`
   - retry, MAP14 RNG, solver, Tilemap, Scene/Prefab/GameObject, spawn and gameplay mutation counters are 0.
10. `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
    - repeat/reverse/`tr-TR` produce identical claim and ownership digests, winners and suppressions.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 12. Expected Result Report

Result must begin:

```text
TASK: MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS
STATUS: PASS | FAIL | BLOCKED
MAP14_07: COMPLETE ELIGIBLE only when PASS
MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 final reference ownership canvas이며 Tilemap/Scene/gameplay spawn이 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 sector/claim/winner/suppressed/coverage/conflict 수치
- owner kind별 claim/winner/suppressed count
- plane별 owned cell count
- Special/Boundary/Spine/Cluster/Pattern/Quiet/Activity/Event priority 증거
- same-plane double owner 0, forbidden overlap 0, unresolved conflict 0
- Activity/Event marker-only 및 explicit Empty evidence 증거
- ProtectedOpen, anchor, Special shell, MAP14_05 render, MAP14_06 Quiet/marker identity가 변하지 않았다는 증거
- retry/MAP14 RNG/Tilemap/Scene/Prefab/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_08

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_07]
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
Commit subject: MAP14_07: implement canvas ownership and conflicts
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_08.

## 13. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS.md
MCP_ARCHIVE/MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS.md
MCP/REPORTS/MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipClaimBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipClaimBuilder.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipResolver.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorCanvasOwnershipResolver.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorCanvasOwnershipResolverTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorCanvasOwnershipResolverTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_08: do not start
STOP after Result and optional PASS finalize commit
```
