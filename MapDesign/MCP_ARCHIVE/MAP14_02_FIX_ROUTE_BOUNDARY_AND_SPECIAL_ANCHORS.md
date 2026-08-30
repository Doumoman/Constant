```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
  task_file: TASKS/MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS.md
  requires_current_task: NONE
  requires_completed_task: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
  requires_result:
    path: REPORTS/MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE_RESULT.md
    status: PASS
    sha256: d8dc8e18c33dfb48b2e8de3a0b6f24b765a09a80f539a063f7299bb84168dcc2
  requires_installed_task:
    path: TASKS/MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE.md
    sha256: 6dca56885ccdbfc22f774a7da33bec1643bdd4f7b766ebd42a7c450f453fcf1a
  sets_current_task: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
```

# MAP14_02 — Fix Route, Boundary and Special Anchors

```text
TASK: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
PHASE: MAP14 — Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01이 만든 immutable `SectorPlannerInput`과 `PacingRole` assignment를 소비해서, Sector Planner가 가장 먼저 보호해야 하는 fixed anchor plan을 만든다.

이번 Task가 고정하는 것은 세 종류뿐이다.

```text
1. external route sockets: L/R/U/D side entry-exit anchors
2. MAP08 boundary evidence: side-aligned fixed slice / warning anchors
3. MAP13 SpecialRegion evidence: footprint / entry-return / apron-buffer anchors
```

```text
SectorPlannerInput + SectorPacingAssignment
→ SectorFixedAnchorPlanner
→ immutable SectorFixedAnchorPlan
→ MAP14_03 cluster candidate placement input
```

이번 Task는 cluster 후보를 만들거나 배치하지 않는다. route spine, traversal envelope, MicroPattern render, Activity/Event placement, final canvas ownership, retry/RNG, tile reachability도 구현하지 않는다. 충돌이 있으면 임의로 밀거나 통로를 뚫지 않고 atomic failure로 보고한다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, anchor 수치, 어떤 것이 실제로 고정됐고 아직 고정되지 않았는지, 미구현 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| fixed anchor value model | cluster candidate generation |
| external L/R/U/D route socket anchor publication | cluster placement / packing |
| MAP08 boundary side anchor and warning evidence | route spine / traversal envelope |
| MAP13 SpecialRegion footprint/buffer anchor publication | MicroPattern render / terrain cleanup |
| anchor priority, overlap rejection, stable digest | Activity/Event placement |
| deterministic no-RNG publication | final canvas ownership/conflict resolver |
| MAP14_03 handoff input | retry policy / failure backtracking |
| focused EditMode tests | actual tile path / PlayMode physics |

Anchor plan은 “여기는 후속 planner가 침범하면 안 되는 예약/보호 영역”을 의미한다. 아직 solid/air tile, final owner, collider, prefab, gameplay spawn을 만들지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_02`만 선택한다.

```text
MAP14_02 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_02` category로 제한한다.

신규 task-owned failure는 신규 MAP14_02 allowlist 파일만 수정하고 `MAP14_02` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_01 Result: PASS
MAP14_01 Result SHA-256:
d8dc8e18c33dfb48b2e8de3a0b6f24b765a09a80f539a063f7299bb84168dcc2

MAP14_01 installed Task SHA-256:
6dca56885ccdbfc22f774a7da33bec1643bdd4f7b766ebd42a7c450f453fcf1a

MAP14_01 COMPLETE / MAP14_02 CURRENT / MAP14_03 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput, SectorPlannerInputBuilder, SectorPacingRolePlanner
MAP09: PacingRole, AccessClass, layer authority separation
MAP08: boundary pair/candidate/warning identity, if exposed through MAP14_01 snapshot
MAP13: SpecialRegion reference/reserved/deferred facts, if exposed through MAP14_01 snapshot
```

MAP14_02 should consume MAP14_01 public values. Do not reparse physical CSV and do not inspect private fields. If MAP14_01 lacks a required public value, add a small MAP14_02-side adapter only when it can read public values without changing MAP14_01 source. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 2개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlan.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorFixedAnchorPlannerTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_02
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

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_02 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorFixedAnchorKind
SectorFixedAnchorSource
SectorFixedAnchorPriority
SectorFixedAnchorRect
SectorFixedAnchor
SectorFixedAnchorPlan
SectorFixedAnchorBuildRequest
SectorFixedAnchorBuildResult
SectorFixedAnchorErrorCode
SectorFixedAnchorError
SectorFixedAnchorPlanner.Build
SectorFixedAnchorCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum anchor kinds:

```text
ExternalRouteSocket
BoundaryFixedSlice
BoundaryWarning
SpecialFootprint
SpecialEntryReturn
SpecialApronBuffer
SiteReservation
ReferenceOnlyMarker
```

Minimum source kinds:

```text
RouteSnapshot
BoundarySnapshot
SiteSnapshot
SpecialRegionSnapshot
OptionalRegionSnapshot
PacingAssignment
ReferenceFixture
```

Minimum error groups:

```text
MissingInput | MissingPacingAssignment | SectorMismatch
AnchorOutOfBounds | InvalidSideAnchor | InvalidBoundaryAnchor
InvalidSpecialAnchor | DeferredPlacedClaim | ReferenceLiveClaim
DuplicateAnchorId | IncompatibleOverlap | PriorityViolation
RouteAccessMutationClaim | BoundaryMutationClaim | SiteMutationClaim
SpecialMutationClaim | SolverMutationClaim | NonCanonicalPublication
```

## 6. Anchor Semantics

### 6.1 External route sockets

For each sector route snapshot, publish side-aligned `ExternalRouteSocket` anchors for every required external side.

Rules:

- sides are exactly L/R/U/D or the existing MAP14_01 side enum.
- anchors must be inside `48×32`.
- anchor rect must touch the matching side and only that side.
- source identity must include route type, access class and side.
- PacingRole assignment may be linked as evidence, but cannot add/remove/rename external sockets.
- no route spine edge, pathfinding edge, carve, teleport or tile write is created.

### 6.2 MAP08 boundary anchors

For every boundary snapshot with pair/candidate/warning evidence, publish `BoundaryFixedSlice` or `BoundaryWarning` anchors.

Rules:

- anchor rect must be side-aligned and inside `48×32`.
- source identity must include side, boundary pair ID, candidate ID and warning count.
- warnings are evidence, not failure, unless they claim mutation or out-of-bounds geometry.
- boundary anchors do not solve neighbor consistency; MAP15 owns world-level neighbor rollback.
- boundary anchors do not generate MicroPattern cells or final 12×8 slices.

If MAP14_01 snapshot exposes only summary boundary identity and no exact fixed cells, create a deterministic `REFERENCE ANCHOR PLAN` side strip for focused fixtures and report that production fixed-cell import remains unimplemented.

### 6.3 MAP13 SpecialRegion anchors

For placed/reference mandatory SpecialRegion facts, publish anchors for:

```text
SpecialFootprint
SpecialEntryReturn
SpecialApronBuffer
SiteReservation
```

Rules:

- CoreResource, Forge and Boss reserved mandatory facts may publish fixed anchors.
- Village reference shell may publish `ReferenceOnlyMarker` or reference anchors, but must not become a mandatory progression blocker.
- Merchant/Maru `DEFERRED TO MAP14` must publish no placed ownership, footprint, reservation, bridge, buffer or fixed-slot anchor.
- source identity must preserve region ID, binding, footprint dimensions or explicit reference status.
- SpecialRegion anchors do not instantiate gameplay objects, rewards, NPCs, Boss, shop, crafting, save state or Tilemap.

### 6.4 Priority and overlap

Publish a deterministic priority per anchor. Suggested priority order:

```text
SpecialFootprint / SiteReservation
SpecialEntryReturn / SpecialApronBuffer
ExternalRouteSocket
BoundaryFixedSlice
BoundaryWarning
ReferenceOnlyMarker
```

If two anchors overlap incompatibly, fail atomically with `IncompatibleOverlap`. Do not shift, shrink, carve or delete anchors to make the plan pass. Compatible overlap is allowed only when the same source identity intentionally emits multiple labels over the same rect, and that compatibility must be explicit in the plan evidence.

## 7. Build Request and Publication

`SectorFixedAnchorPlanner.Build` should accept a valid `SectorPlannerInput` and the matching `SectorPacingAssignment` list.

Validation must prove:

```text
input is published and digest-valid
assignment count and sector coordinates match input sectors
anchor rects are inside 48×32
side anchors touch expected side
route/access/socket identities are unchanged
boundary pair/candidate identities are unchanged
site/reservation identities are unchanged
SpecialRegion binding is unchanged
deferred optional placed claims remain 0
solver/RNG/tile/canvas/asset mutation counters are 0
```

Success report should publish:

```text
sector count
anchor count by kind/source/priority
per-sector anchor count
collision count 0
compatible overlap count
route/boundary/site/special identity digest before/after
canonical anchor plan digest
MAP14_03 handoff readiness flag
```

Failure should publish:

```text
plan null
digest empty
stable sorted errors only
mutation counters 0
```

## 8. Focused Fixture Matrix

Reuse the MAP14_01 fixture idea through public APIs where practical. Do not copy private implementation.

Minimum fixture coverage:

| Fixture | Expected anchor responsibility |
|---|---|
| `PlainTraversalBoundarySector` | route side anchors + MAP08 boundary warning/fixed-slice evidence |
| `QuietBufferSector` | zero or minimal route anchors; no Special/Boundary false claim |
| `VillageReferenceSector` | reference-only marker; no global progression blocker |
| `CoreResourceSector` | mandatory resource footprint/site/entry-return/buffer anchors |
| `ForgeLandmarkSector` | Forge footprint/site/process entry-return/buffer anchors |
| `BossGateSector` | Boss footprint/site/gate/arena/recovery-related fixed anchors |
| `ActivityCompatibleSector` | no Activity placement/marker/spawn anchor |
| `DeferredOptionalSector` | no placed anchors for Merchant/Maru |
| `NeighborInfluencedSector` | neighbor reasons do not create anchors |
| `InvalidInputCases` | mismatch/out-of-bounds/overlap/deferred-placed/mutation claim failures |

Fixtures are `REFERENCE ANCHOR PLAN` examples, not production world seeds.

## 9. Required Tests

`SectorFixedAnchorPlannerTests` must include 8~12 focused tests in category `MAP14_02`.

Minimum assertions:

1. `BuildPublishesCanonicalFixedAnchorPlanFromPlannerInput`
   - valid fixture input + assignments publish immutable plan, 48×32 bounds, lower-hex digest, no partial error.
2. `ExternalRouteSocketsAreSideAlignedAndDoNotMutateRouteAccess`
   - L/R/U/D anchors preserve route type, access class, socket identity.
3. `BoundaryAnchorsPreservePairCandidateWarningEvidence`
   - boundary pair/candidate/warning source identity preserved; warning is evidence not mutation.
4. `SpecialAnchorsReserveFootprintEntryReturnBufferBeforeClusters`
   - Core/Forge/Boss anchors exist before cluster placement and remain reference/reserved only.
5. `VillageReferenceAndDeferredOptionalDoNotBecomePlacedProgressionBlockers`
   - Village mandatory dependency 0; Merchant/Maru placed anchors 0.
6. `ActivityEventAndNeighborFactsDoNotCreateAnchors`
   - Activity candidate and neighbor reasons do not create placement/marker/spawn/path anchors.
7. `IncompatibleOverlapAndOutOfBoundsFailAtomically`
   - plan/digest publication 0; stable errors; no auto-fix.
8. `AssignmentMismatchAndMutationClaimsFailAtomically`
   - missing/mismatched assignments and route/boundary/site/special/solver mutation claims fail with no plan.
9. `AnchorPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
   - repeat/reverse/`tr-TR` stable plan digest and anchor ordering.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 10. Expected Result Report

Result must begin:

```text
TASK: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
STATUS: PASS | FAIL | BLOCKED
MAP14_02: COMPLETE ELIGIBLE only when PASS
MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 anchor plan이며 Solver/cluster placement가 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 sector fixture 수, anchor count by kind/source/priority, per-sector count, overlap/error 수치
- route/access/socket/boundary/site/special identity가 변하지 않았다는 증거
- MAP13 SpecialRegion은 reserved/reference/deferred contract로만 소비됐다는 증거
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_03

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_02]
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
Commit subject: MAP14_02: fix route boundary and special anchors
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_03.

## 11. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS.md
MCP_ARCHIVE/MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS.md
MCP/REPORTS/MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlan.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlan.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorFixedAnchorPlannerTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorFixedAnchorPlannerTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_03: do not start
STOP after Result and optional PASS finalize commit
```
