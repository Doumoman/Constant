```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
  task_file: TASKS/MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES.md
  requires_current_task: NONE
  requires_completed_task: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
  requires_result:
    path: REPORTS/MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS_RESULT.md
    status: PASS
    sha256: 3220f4d137e0158deed95fcc3e09a6ec9a82fdf9ae1ba183348d40e892099855
  requires_installed_task:
    path: TASKS/MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS.md
    sha256: 8ea544e3569bea032a5570cb4dbe0ba14f0dd73575e2ca78b946c2ae845ccd0b
  sets_current_task: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
```

# MAP14_03 — Build and Place Cluster Candidates

```text
TASK: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
PHASE: MAP14 — Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01의 `SectorPlannerInput`/`PacingRole`과 MAP14_02의 `SectorFixedAnchorPlan`을 소비해서, 각 sector에 들어갈 TerrainCluster 후보를 만들고 **추상 cluster placement plan**까지 게시한다.

```text
SectorPlannerInput
SectorPacingAssignment
SectorFixedAnchorPlan
MAP11 TerrainCluster catalog / Quiet pool public authority
→ SectorClusterCandidateBuilder
→ stable candidate set
→ SectorClusterPlacementPlanner
→ immutable cluster placement plan
→ MAP14_04 spine/envelope input
```

이번 Task는 `48×32` sector 안의 4×4 MicroChunk grid에서 TerrainCluster footprint를 어디에 예약할지까지만 정한다. 실제 tile write, route spine edge, traversal envelope, MicroPattern render, cleanup, Activity/Event placement, canvas ownership, retry/RNG, PlayMode reachability는 구현하지 않는다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, 후보/배치 실제 수치, 선택 이유, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| TerrainCluster candidate value model | route spine / traversal envelope |
| biome/pacing/socket/free-footprint/density candidate filtering | MicroPattern render / terrain cleanup |
| deterministic candidate scoring and ordering | Activity/Event placement |
| 4×4 MicroChunk-grid abstract cluster placement | final canvas ownership/conflict resolver |
| fixed anchor avoidance and fit proof | retry/RNG/backtracking |
| constraint-large-first placement order | actual tile path / PlayMode physics |
| placement digest and failure evidence | Scene/Prefab/Tilemap/gameplay object |
| MAP14_04 handoff contract | production world assembly / MAP15 rollback |

Cluster placement plan은 “이 cluster footprint가 이 sector-local MicroChunk 영역을 예약한다”는 추상 계획이다. 아직 solid/air tile, collider, object spawn, final ownership, exact player traversal path를 만들지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_03`만 선택한다.

```text
MAP14_03 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01/MAP14_02 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_03` category로 제한한다.

신규 task-owned failure는 신규 MAP14_03 allowlist 파일만 수정하고 `MAP14_03` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_02 Result: PASS
MAP14_02 Result SHA-256:
3220f4d137e0158deed95fcc3e09a6ec9a82fdf9ae1ba183348d40e892099855

MAP14_02 installed Task SHA-256:
8ea544e3569bea032a5570cb4dbe0ba14f0dd73575e2ca78b946c2ae845ccd0b

MAP14_02 COMPLETE / MAP14_03 CURRENT / MAP14_04 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput, SectorPacingAssignment, public fixture/input values
MAP14_02: SectorFixedAnchorPlan, anchor rects/kinds/source identities/priority
MAP11: TerrainCluster catalog, biome/pacing/route/socket/footprint/variant facts
MAP11_06: Quiet/Buffer cluster pool where public
MAP09: PacingRole, AccessClass, layer ownership separation
MAP10: MicroPattern authority summary only; no render call
```

MAP14_03 should consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_03-side adapter only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidatePlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidateBuilder.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterPlacementPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterCandidatePlacementTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_03
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

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_03 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorClusterCandidate
SectorClusterCandidateSet
SectorClusterCandidateReason
SectorClusterFootprintCell
SectorClusterFootprintPlacement
SectorClusterPlacement
SectorClusterPlacementPlan
SectorClusterCandidateBuildRequest
SectorClusterCandidateBuildResult
SectorClusterPlacementRequest
SectorClusterPlacementBuildResult
SectorClusterCandidateErrorCode
SectorClusterCandidateError
SectorClusterCandidateBuilder.Build
SectorClusterPlacementPlanner.Place
SectorClusterCandidateCanonicalDigest
SectorClusterPlacementCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial candidate set or placement plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum candidate reasons:

```text
BiomeCompatible
PacingPrimaryMatch
PacingCandidateMatch
RouteSocketCompatible
AccessCompatible
FootprintFitsFreeGrid
AvoidsFixedAnchor
DensityWithinPolicy
QuietPoolCompatible
SpecialAdjacencyCompatible
ConstraintLargeFirst
```

Minimum error groups:

```text
MissingInput | MissingAnchorPlan | MissingAssignment | SectorMismatch
MissingClusterCatalog | NoCandidateForSector | DuplicateCandidate
InvalidFootprint | FootprintOutOfBounds | AnchorOverlap
SocketMismatch | AccessMismatch | PacingMismatch | BiomeMismatch
DensityOutOfPolicy | PlacementOverlap | PlacementOrderViolation
SolverMutationClaim | RngMutationClaim | TileMutationClaim | NonCanonicalPublication
```

## 6. Candidate Contract

`SectorClusterCandidateBuilder.Build` receives a valid `SectorPlannerInput`, matching `SectorPacingAssignment` list, matching `SectorFixedAnchorPlan`, and a public TerrainCluster source projection.

For each sector, publish ordered candidates that prove:

```text
sector coordinate and index match input
candidate cluster ID and variant ID are stable
biome compatibility is explicit
PacingRole compatibility is explicit
RouteType/socket compatibility is explicit where available
AccessClass compatibility is explicit where available
footprint cells are unique, connected, and inside 4x4 MicroChunk grid
each footprint placement maps to 12x8 tile rects inside 48x32
hard fixed anchors are avoided
density policy is evidence-only and does not tune gameplay thresholds
candidate score is deterministic and uses no RNG
```

Candidate scoring must be stable:

```text
hard compatibility gates first
then primary PacingRole match
then candidate PacingRole match
then route/socket/access match
then larger valid footprint when constraints are tighter
then lower anchor proximity penalty
then cluster catalog order
then variant order
then sector coordinate order
```

MAP14_03 may use deterministic `REFERENCE CLUSTER CANDIDATE` projections for focused fixtures if live 169-sector input is not public. These fixtures must not claim production world seeds.

## 7. Placement Contract

`SectorClusterPlacementPlanner.Place` receives only a successful candidate set and matching anchor plan. It publishes an abstract placement plan.

Placement rules:

- place candidates only on the 4×4 MicroChunk grid.
- each placed footprint cell maps to a `12×8` tile rect.
- all placed footprint cells must be inside the sector.
- placed cells cannot overlap each other.
- placed cells cannot overlap incompatible fixed anchor rects from MAP14_02.
- side route anchors may be used as scoring evidence but cannot be overwritten.
- SpecialRegion footprint/site/entry/buffer anchors have priority over clusters.
- Village reference marker does not become a progression blocker.
- Merchant/Maru deferred optional facts create no cluster placement.
- Activity/Event availability creates no placement in this Task.
- no route spine, path edge, carve, tile write, RNG, retry or auto-fix is allowed.

Constraint-large-first means:

```text
sectors with mandatory SpecialRegion anchors before plain sectors
then sectors with external route/boundary anchors
then larger cluster footprints before smaller footprints
then primary pacing match before secondary match
then stable cluster/variant order
```

If no candidate fits, or if all candidates collide with anchors, fail atomically. Do not shrink, rotate, shift outside the candidate's approved transform, carve through anchors, or silently fall back to dummy filler.

Success report should publish:

```text
sector count
candidate count total and per sector
accepted placement count
rejected candidate count by reason
placed footprint cell count
free footprint cell count
anchor-overlap count 0
placement-overlap count 0
cluster IDs and variant IDs in chosen order
candidate set digest
placement plan digest
MAP14_04 handoff readiness flag
```

Failure should publish:

```text
candidate set or placement plan null as appropriate
digest empty
stable sorted errors only
mutation counters 0
```

## 8. Focused Fixture Matrix

Reuse the MAP14_01/MAP14_02 fixture idea through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected cluster candidate / placement responsibility |
|---|---|
| `PlainTraversalBoundarySector` | traversal-compatible candidates avoid route/boundary anchors |
| `QuietBufferSector` | Quiet pool candidate can place without mandatory blockers |
| `VillageReferenceSector` | Safe/Landmark-compatible candidate respects Village reference marker |
| `CoreResourceSector` | Resource-compatible candidate avoids Core footprint/site/buffer anchors |
| `ForgeLandmarkSector` | Landmark/Machinery-compatible candidate avoids Forge anchors |
| `BossGateSector` | Boss-compatible candidate avoids Boss anchors |
| `ActivityCompatibleSector` | Activity candidate evidence does not create Activity placement |
| `DeferredOptionalSector` | optional deferred facts produce no placed ownership claim |
| `NeighborInfluencedSector` | neighbor facts influence candidate reasons only |
| `InvalidInputCases` | no-candidate/collision/duplicate/mutation failures publish zero plan |

Fixtures are `REFERENCE CLUSTER CANDIDATE` and `REFERENCE CLUSTER PLACEMENT` examples, not production world seeds.

## 9. Required Tests

`SectorClusterCandidatePlacementTests` must include 9~12 focused tests in category `MAP14_03`.

Minimum assertions:

1. `BuildPublishesStableClusterCandidatesFromPlannerInputAndAnchors`
   - exact fixture sectors, candidate total/per-sector, immutable candidate set, lower-hex digest.
2. `CandidatesRespectBiomePacingRouteSocketAccessAndFootprintCompatibility`
   - candidate reasons include required biome/pacing/socket/access/fit evidence.
3. `CandidatesAvoidFixedAnchorsWithoutMutatingAnchors`
   - anchor identities before/after match; no hard-anchor overlap.
4. `PlacePublishesConstraintLargeFirstClusterPlacementPlan`
   - accepted placements are deterministic, larger/more constrained sectors first, digest stable.
5. `PlacedFootprintsStayInsideFourByFourGridAndDoNotOverlap`
   - 4×4 MicroChunk cells and derived 12×8 rects are inside 48×32 and non-overlapping.
6. `SpecialVillageOptionalAndActivityBoundariesRemainNonOwningWhereRequired`
   - Special anchors respected; Village non-blocking; Merchant/Maru and Activity/Event produce no ownership placement.
7. `NoCandidateCollisionAndMutationClaimsFailAtomically`
   - no plan/digest on no-candidate, all-collide, duplicate, solver/RNG/tile mutation inputs.
8. `CandidateAndPlacementPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
   - repeat/reverse/`tr-TR` stable candidate and placement digests.
9. `BuildAndPlaceDoNotInvokeSpinePatternActivityRetryOrTileSystems`
   - route spine, envelope, MicroPattern renderer, Activity/Event placement, retry, RNG, tile write counters all 0.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 10. Expected Result Report

Result must begin:

```text
TASK: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
STATUS: PASS | FAIL | BLOCKED
MAP14_03: COMPLETE ELIGIBLE only when PASS
MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 cluster candidate + abstract placement plan이며 spine/pattern/rendering이 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 sector fixture 수, candidate count, accepted placement count, rejected reason count, footprint/free/overlap 수치
- chosen cluster IDs/variant IDs and why they were selected
- fixed anchor identity가 변하지 않았다는 증거
- PacingRole이 access/route/socket을 바꾸지 않았다는 증거
- MAP13 SpecialRegion은 anchor obstacle/reference/deferred fact로만 소비됐다는 증거
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_04

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_03]
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
Commit subject: MAP14_03: build and place cluster candidates
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_04.

## 11. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES.md
MCP_ARCHIVE/MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES.md
MCP/REPORTS/MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidatePlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidatePlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidateBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidateBuilder.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterPlacementPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterPlacementPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterCandidatePlacementTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterCandidatePlacementTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_04: do not start
STOP after Result and optional PASS finalize commit
```
