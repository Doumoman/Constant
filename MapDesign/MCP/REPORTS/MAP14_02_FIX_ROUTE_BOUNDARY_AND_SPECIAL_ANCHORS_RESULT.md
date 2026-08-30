TASK: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
STATUS: PASS
MAP14_02: COMPLETE ELIGIBLE only when PASS
MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP14_02는 solver나 cluster placement가 아니라, 후속 planner가 침범하면 안 되는 `REFERENCE ANCHOR PLAN`을 만드는 Task다. MAP14_01의 immutable `SectorPlannerInput`과 matching `SectorPacingAssignment`를 입력으로 받아 external route socket, MAP08 boundary summary, MAP13 SpecialRegion/site facts를 검증된 rect/identity anchor로 고정한다. tile, path edge, final owner, collider, GameObject를 만들지 않는다.

추가된 production script는 두 개다.

- `SectorFixedAnchorPlan.cs`는 rect/projection/anchor/request/result/error/plan/count/digest의 immutable value surface를 소유한다.
- `SectorFixedAnchorPlanner.cs`는 MAP14_01 input·assignment와 명시적 public projection을 대조하고, side alignment·coverage·priority·overlap·reference/deferred·mutation 계약을 검증한 뒤 성공 시에만 plan을 게시한다.

추가된 test script는 `SectorFixedAnchorPlannerTests.cs` 하나다. 정확히 10개 `MAP14_02` EditMode test와 이름 있는 fixture matrix 10개(유효 sector 사례 9개 + `InvalidInputCases`)를 public MAP14_01 API로 구성한다.

MAP14_01은 socket별 side와 MAP08 fixed cell, MAP13 실제 footprint 치수를 게시하지 않는다. 따라서 upstream을 수정하거나 private/CSV를 읽지 않고, MAP14_02 request가 side/rect를 명시하는 작은 public projection adapter를 추가했다. fixture rect는 `REFERENCE ANCHOR PLAN` 전용이며 live world/fixed-cell publication이 아니다. Special source identity에는 region ID, kind, binding, footprint ID와 projection rect 치수를 함께 넣었다.

실제 authoritative 수치는 다음과 같다.

```text
fixture matrix / valid sectors: 10 / 9
canvas: 48x32
anchors: 19
incompatible collision: 0
explicit compatible overlap: 1
plan digest: 5721695eca21e4f4852b4789a749c94db2e1ddf7307bf8cfeec660140a9c0e26
assignment bundle digest: a4ff9a046d5039f6bba8aa57186203b8e0380ac508dced2e118c0e0795ef4599
MAP14_03 handoff ready flag: true
route/access/socket/boundary/site/special mutation: 0/0/0/0/0/0
solver/RNG/tile/canvas/asset mutation: 0/0/0/0/0
cluster candidate/placement: 0/0
Activity placement/Event marker/gameplay spawn/path edge: 0/0/0/0
```

| Anchor kind | Count | 실제 고정한 reference evidence |
|---|---:|---|
| `ExternalRouteSocket` | 4 | Plain fixture의 L/R/U/D public socket ID와 side-only rect |
| `BoundaryFixedSlice` | 1 | MAP08 summary의 right-side pair/candidate reference strip |
| `BoundaryWarning` | 1 | 같은 boundary warning count `1`; failure가 아닌 evidence |
| `SpecialFootprint` | 3 | Core/Forge/Boss reserved reference footprint rect |
| `SpecialEntryReturn` | 3 | Core/Forge/Boss entry-return protected rect |
| `SpecialApronBuffer` | 3 | Core/Forge/Boss apron/buffer protected rect |
| `SiteReservation` | 3 | Core/Forge/Boss site/reservation identity rect |
| `ReferenceOnlyMarker` | 1 | Village non-live/non-progression reference marker |

| Source | Count |
|---|---:|
| Route / Boundary / Site snapshot | 4 / 2 / 3 |
| SpecialRegion snapshot | 10 |
| OptionalRegion / PacingAssignment / ReferenceFixture anchor source | 0 / 0 / 0 |

| Priority | Count |
|---|---:|
| Special reservation / transition | 6 / 6 |
| External route socket | 4 |
| Boundary fixed / warning | 1 / 1 |
| Reference only | 1 |

Priority는 `SpecialReservation(600) > SpecialTransition(500) > ExternalRouteSocket(400) > BoundaryFixedSlice(300) > BoundaryWarning(200) > ReferenceOnly(100)`으로 고정했다. plan ordering은 sector index, priority descending, kind, rect, ordinal anchor ID 순이다.

| Sector fixture | Anchors | 실제 reference rect |
|---|---:|---|
| `PlainTraversalBoundarySector` | 6 | L `(0,14,1,4)`, R `(47,14,1,4)`, U `(22,0,4,1)`, D `(22,31,4,1)`, boundary fixed/warning `(47,4,1,4)` |
| `QuietBufferSector` | 0 | false route/boundary/special claim 없음 |
| `VillageReferenceSector` | 1 | marker `(23,15,1,1)`, placed/progression claim 0 |
| `CoreResourceSector` | 4 | footprint `(18,12,12,8)`, entry `(16,14,2,4)`, buffer `(30,12,2,8)`, site `(20,22,4,2)` |
| `ForgeLandmarkSector` | 4 | Core와 동일한 sector-local reference layout, Forge identity |
| `BossGateSector` | 4 | Core와 동일한 sector-local reference layout, Boss identity |
| `ActivityCompatibleSector` | 0 | Activity/Event 배치 anchor 없음 |
| `DeferredOptionalSector` | 0 | Merchant/Maru placed/footprint/site/buffer anchor 없음 |
| `NeighborInfluencedSector` | 0 | neighbor pacing reason이 path/socket anchor를 만들지 않음 |

boundary fixed/warning 두 label만 동일 rect, 동일 source identity, 동일 explicit compatibility group으로 겹쳐 compatible overlap `1`을 게시했다. 그 외 17개 anchor는 같은 sector 안에서 서로 겹치지 않는다. 다른 source identity의 overlap은 위치를 이동·축소·carve·삭제하지 않고 `IncompatibleOverlap`으로 원자 실패한다.

실제로 고정한 것은 다음과 같다.

- external socket ID별 exactly-one side mapping과 side-only 48x32 rect;
- route type, AccessClass, socket ID를 포함한 route source identity;
- boundary side/pair/candidate/warning identity와 deterministic reference strip;
- Core/Forge/Boss의 reserved footprint/entry-return/buffer와 site/reservation reference rect;
- Village의 non-live reference marker;
- anchor priority/order, compatible-overlap 조건, canonical digest;
- route/boundary/site/special before/after identity digest equality와 모든 mutation counter 0;
- assignment 수/coordinate/current identity가 input sector와 exact match한다는 gate.

아직 고정하지 않은 것은 production socket side import, MAP08 actual fixed cells/final `12x8` slice, live MAP13 footprint geometry, neighbor-side world consistency/rollback, route spine/traversal envelope, cluster candidate/packing/placement, MicroPattern render/cleanup, Activity/Event placement, final canvas ownership/conflict resolution, retry/backtracking/RNG, tile path/physics/collider, gameplay object/reward/NPC/Boss/shop/crafting/save state다. MAP15가 소유하는 world-level neighbor rollback도 수행하지 않는다.

MAP13 contract는 Core/Forge/Boss를 `ReservedMandatory`, Village를 `ReferenceOnly`, Merchant/Maru를 `DeferredOptionalLocal`로만 소비했다. Village marker는 placed/progression `0/0`; Merchant/Maru anchor는 `0`; invalid deferred placed와 Village live claim은 각각 `DeferredPlacedClaim`/`ReferenceLiveClaim`으로 plan/digest 없이 실패했다.

Editor 가시성은 새 EditorWindow/menu/preview가 없으며 기존 MAP13 preview를 변경하지 않았다. 최종 active Scene은 `Assets/_Game/Scenes/MapGenerationProgressTest.unity`, root `3`, dirty `false`, selection `0`이다. 게임 가시성은 없다. Scene/Prefab/Tilemap/Material/Texture/GameObject/component를 만들지 않았고 PlayMode를 실행하지 않았다.

## Responsibility and Added Functions

### Added scripts and exact boundary

| Script | Assembly / namespace | Responsibility | Input → output |
|---|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlan.cs(.meta)` | `Game.Map.Runtime` / `StarNight.Map.WorldGeneration.SectorPlanning` | immutable anchor model, counts, result, digest | public projection/validated anchors → canonical fixed-anchor publication values |
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorFixedAnchorPlanner.cs(.meta)` | same | validation, identity resolution, coverage, priority and overlap gate | `SectorFixedAnchorBuildRequest` → complete plan/digest or stable errors only |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorFixedAnchorPlannerTests.cs(.meta)` | `Game.Map.Tests.EditMode` / `StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning` | exact fixture and negative focused proof | public MAP14_01 input/assignments + reference projections → 10 focused verdicts |

Unity generated matching metas with GUIDs `7447db761b8d7494793a690ce4b8fdf6`, `99646d6444050a84380ae5d64d0bf5f8`, `3a438931b7f86d34782f94f6db172a45` respectively.

### Runtime class and method responsibility

| Class / method | Responsibility | Input → output |
|---|---|---|
| four enums (`SectorFixedAnchorKind`, `Source`, `Priority`, `ErrorCode`) | stable anchor/source/priority/error vocabulary | semantic fact → canonical enum |
| `SectorFixedAnchorRect` constructor | immutable local rectangle | x/y/width/height → exact bounds value |
| `IsInside` | positive 48x32 containment gate | canvas dimensions → bool |
| `TouchesOnly` | L/R/U/D side-only alignment, corner rejection | side+canvas → bool |
| `Overlaps` | strict positive-area overlap | another rect → bool |
| `CompareTo` / equality / `ToString` | culture-invariant rect ordering/identity | rect → order/equality/`x,y,w,h` token |
| `SectorFixedAnchorProjection` constructor | MAP14_01 missing geometry를 보완하는 explicit adapter | anchor/source/side/rect/claim evidence → immutable request projection |
| `SectorFixedAnchor` internal constructor | validated output only | projection + sector index + derived public source identity → immutable anchor |
| `SectorFixedAnchorBuildRequest` constructor | defensive-copy input, assignments, projections and mutation claims | public inputs + label/digest/claims → immutable build request |
| `SectorFixedAnchorError` constructor / comparison / equality | accumulated, deduped, stable errors | code+subject+detail → comparable error evidence |
| `SectorFixedAnchorPlan` internal constructor | canonical sort and exact count publication | input+validated anchors+digests → immutable handoff plan |
| `Count(kind/source/priority)` | exact aggregate lookup | enum → count |
| `CountForSector` | deterministic sector-local count | `SectorCoord` → count or 0 |
| `SectorFixedAnchorBuildResult` internal constructor | atomic publication | plan candidate + errors → plan/digest or errors with zero publication |
| `SectorFixedAnchorCanonicalDigest.Compute` | public digest rebuild | immutable plan → 64 lowercase-hex SHA-256 |
| digest internal `Compute` | canonical material ownership | label/input/assignment/anchors/identity/overlap → SHA-256 |
| `ComputeAssignmentDigest` | assignment bundle identity | matching assignments → stable digest |
| `ComputeRoute/Boundary/Site/SpecialIdentity` | before/after authority identity | MAP14_01 input → four stable source digests |
| `SectorFixedAnchorPlanner.Build` | complete atomic orchestration | request → validated plan or ordered errors only |
| `ValidatePublication/Input/Assignments/MutationClaims` | canonical input, exact assignment, zero-side-effect gates | request facts → errors only |
| `BuildAnchors` / `Resolve*Identity` | public source match and output creation | projection + sector snapshot → derived anchor or source-specific error |
| `ValidateCoverage` / `ValidateSpecialCoverage` | exactly-one required anchor and reference/deferred gates | input + anchors → missing/excess/ownership errors |
| `ValidateOverlaps` | explicit same-source overlap or atomic collision | ordered anchors → compatible count / `IncompatibleOverlap` |
| `ExpectedPriority` | kind-owned priority policy | anchor kind → exact priority |

### Focused test method responsibility

| Test method | Responsibility | Input → asserted output |
|---|---|---|
| `BuildPublishesCanonicalFixedAnchorPlanFromPlannerInput` | immutable full publication | 9 sectors + assignments + 19 projections → 19 anchors, bounds/digest/counts, collision 0 |
| `ExternalRouteSocketsAreSideAlignedAndDoNotMutateRouteAccess` | four-side route gate | L/R/U/D socket projections → four side-only anchors, route/access/socket unchanged |
| `BoundaryAnchorsPreservePairCandidateWarningEvidence` | MAP08 identity/overlap proof | pair/candidate/warning `1` → fixed+warning anchors, compatible overlap 1 |
| `SpecialAnchorsReserveFootprintEntryReturnBufferBeforeClusters` | mandatory reservation proof | Core/Forge/Boss → `4+4+4` anchors before cluster work, cluster count 0 |
| `VillageReferenceAndDeferredOptionalDoNotBecomePlacedProgressionBlockers` | MAP13 boundary | Village/Merchant → marker 1 with claims 0 / placed anchor 0 |
| `ActivityEventAndNeighborFactsDoNotCreateAnchors` | non-owner proof | quiet/activity/event/optional/neighbor facts → sector anchors 0, path/spawn/marker 0 |
| `IncompatibleOverlapAndOutOfBoundsFailAtomically` | no auto-fix proof | foreign overlap + negative-x rect → plan null/digest empty, required errors |
| `AssignmentMismatchAndMutationClaimsFailAtomically` | matching/no-mutation gate | missing/duplicated assignments + five mutation groups → zero publication |
| `DeferredPlacedAndReferenceLiveClaimsFailAtomically` | optional/reference claim gate | Merchant footprint + Village live marker claims → required errors, zero publication |
| `AnchorPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture` | order/culture canonicality | repeat/reversed/new `tr-TR` fixture → same plan digest and anchor order |

`AnchorFixtureSet.Create/Request/BuildValid` owns deterministic test-only MAP14_01 public fixture construction and projections. It does not copy a private producer, read CSV, or publish production world data.

Production changes outside the two new Runtime scripts: 0. Existing Runtime/Editor/test C#, CSV/schema, asmdef/asmref, Scene, Prefab, Tilemap, Material, Texture, Settings, Packages changes: 0. Upstream MAP14_01 modifications: 0. New Editor production C#/PlayMode helper/generated report asset: 0. Downstream owner is locked `MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES`.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
assembly_names: [Game.Map.Tests.EditMode]
category_names: [MAP14_02]
job_id: 45fdc83cb8234c9792483564867230ee
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 1.8121569
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

Only the `MAP14_02` EditMode category was selected. The first focused run passed 10/10; a contract review then added projection rect dimensions to Special source identity inside the task-owned planner, and the final authoritative run above passed 10/10. No focused failure or upstream invariant defect occurred. MAP14_01 or any earlier category, legacy 19347, PlayMode, and unfiltered tests were never selected.

## Finalize and Commit

```text
Commit subject: MAP14_02: fix route boundary and special anchors
Push: NOT PERFORMED
MAP14_03: LOCKED / NOT STARTED
```
