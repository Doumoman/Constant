TASK: MAP14_03_BUILD_AND_PLACE_CLUSTER_CANDIDATES
STATUS: PASS
MAP14_03: COMPLETE ELIGIBLE only when PASS
MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP14_03은 TerrainCluster 후보와 `4×4` MicroChunk-grid의 추상 placement reservation을 게시하는 Task다. route spine, traversal envelope, MicroPattern render, tile/collider/GameObject 생성은 하지 않는다. MAP14_01의 immutable `SectorPlannerInput`/`SectorPacingAssignment`, MAP14_02의 immutable `SectorFixedAnchorPlan`, MAP11의 공개 cluster/variant/transform/biome 타입을 소비하는 `REFERENCE CLUSTER CANDIDATE`/`REFERENCE CLUSTER PLACEMENT` fixture만 만들었으며 live 169-sector production seed를 주장하지 않는다.

추가한 script는 네 개다.

- `SectorClusterCandidatePlan.cs`는 source projection, footprint cell/placement, candidate/set, placement/plan, request/result/error, rejection accounting과 canonical digest value surface를 소유한다.
- `SectorClusterCandidateBuilder.cs`는 biome → pacing → route/socket → access → quiet/special → density → `4×4` fit/fixed-anchor avoidance hard gate를 적용하고, 성공 시 sector별 stable candidate set을 원자 게시한다.
- `SectorClusterPlacementPlanner.cs`는 successful candidate set과 matching anchor plan만 받아 SpecialRegion → route/boundary → larger footprint → primary pacing → stable cluster/variant 순으로 한 cluster씩 선택하고 추상 reservation plan을 게시한다.
- `SectorClusterCandidatePlacementTests.cs`는 9개 유효 sector fixture와 invalid cases를 포함하는 정확히 10개 `MAP14_03` focused EditMode test를 소유한다.

### 실제 후보·배치 수치

```text
fixture matrix: 10 (valid sector fixtures 9 + InvalidInputCases 1)
public source projections: 22 (eligible publication 18 + rejection probes 4)
sector/source evaluations: 9 x 22 = 198
published candidate count: 18
candidate count per sector: 2 x 9
candidate-stage rejected: 180
accepted abstract placements: 9
lower-ranked eligible candidates not placed: 9
final rejected candidate accounting: 189
placed footprint cells: 26
hard fixed-anchor MicroChunk cells: 21
free MicroChunk cells: 97
anchor overlap / placement overlap: 0 / 0
candidate set digest: 3e79e15dcd6ba97d57f1df447ffe1064dc9bcff367836f0535877014b61749ff
placement plan digest: c084643048a97ec2d7f8347d87037320f6b99a6185b0af027afd22e6aba0779e
MAP14_04 handoff ready: true
```

후보-stage rejection은 안정 정렬된 이유별로 `BiomeMismatch 142`, `PacingMismatch 34`, `SocketMismatch 1`, `AccessMismatch 1`, `DensityOutOfPolicy 1`, `AnchorOverlap 1`이다. placement 단계는 sector마다 stable winner 하나를 선택해 `LowerRankedCandidate 9`를 더한다. dummy filler, footprint 축소, 승인 origin 밖 이동, anchor carve, retry/backtracking은 없다.

### 선택된 cluster / variant와 이유

| Constraint-large-first 순서 | Sector fixture | 선택 cluster / variant | 선택 이유 |
|---:|---|---|---|
| 1 | `BossGateSector` | `TC_REF_BOSS_GATE / SPINE_BOSS_R0` | mandatory SpecialRegion + primary `Boss`; 유효 후보 중 가장 큰 5-cell footprint가 중앙 Boss anchors를 피함 |
| 2 | `CoreResourceSector` | `TC_REF_CORE_RESOURCE_RING / SPINE_RESOURCE_R0` | mandatory Core + primary `Resource`; 4-cell row가 footprint/site/entry/buffer 우선권을 보존하며 3-cell 대안보다 먼저 정렬됨 |
| 3 | `ForgeLandmarkSector` | `TC_REF_FORGE_MACHINERY / SPINE_LANDMARK_R0` | primary `Landmark`가 secondary `Machinery`보다 우선하고 4-cell footprint가 Forge anchors를 피함 |
| 4 | `PlainTraversalBoundarySector` | `TC_REF_TRAVERSAL_BRIDGE / SPINE_TRAVERSAL_R0` | primary `Traversal`이 `Recovery` 대안보다 우선하며 L/R/U/D route socket과 boundary fixed strip을 덮지 않는 중앙 2-cell placement만 승인됨 |
| 5 | `NeighborInfluencedSector` | `TC_REF_NEIGHBOR_FLOW / SPINE_NEIGHBOR_R0` | public neighbor pacing evidence의 primary `Traversal`; 같은 role의 2-cell 대안보다 3-cell footprint가 우선 |
| 6 | `ActivityCompatibleSector` | `TC_REF_ACTIVITY_SHELL / SPINE_ACTIVITY_R0` | primary `Activity`, 동일 크기 대안과의 tie를 catalog/variant ordinal order로 해결; Activity object는 만들지 않음 |
| 7 | `DeferredOptionalSector` | `TC_REF_DISCOVERY_PASSAGE / SPINE_DISCOVERY_R0` | deferred Merchant fact가 낸 primary `Discovery` compatibility만 소비; optional ownership/anchor는 0 |
| 8 | `QuietBufferSector` | `TC_REF_QUIET_BUFFER / SPINE_QUIET_R0` | primary `Quiet`와 explicit quiet-pool compatibility; mandatory blocker가 없는 stable catalog-first 2-cell 후보 |
| 9 | `VillageReferenceSector` | `TC_REF_VILLAGE_APPROACH / SPINE_SAFE_R0` | primary `Safe`가 secondary `Landmark`보다 우선; Village `ReferenceOnlyMarker`는 non-blocking/non-owning reference로 유지 |

모든 선택 variant는 fixture의 승인 transform `R0`다. reverse input/catalog와 `tr-TR` culture에서도 위 후보/배치 digest와 순서가 동일했다.

### 기존 권위 보존과 승인 경계

- fixed anchor plan은 build 전후 동일 object/digest/source identity 배열을 유지했다. route/boundary/site/Special identity before/after 네 쌍이 모두 exact equality이며 candidate/placement의 anchor overlap은 `0`이다.
- PacingRole은 compatibility와 ordering evidence로만 사용했다. 9개 assignment의 route/access/socket mutation counter 합은 `0`; builder와 placer의 solver/RNG/tile mutation도 `0/0/0`이다.
- MAP13 SpecialRegion은 Core/Forge/Boss의 hard anchor obstacle, Village의 non-blocking reference marker, Merchant의 deferred fact로만 소비했다. Special ownership, Village progression blocker, Merchant/Maru anchor, Activity/Event placement ownership은 추가하지 않았다.
- MAP14_03이 승인하는 범위는 immutable candidate/value/error model, public projection adapter, deterministic filter/score/order, approved-origin `4×4` abstract placement, `12×8` tile-rect derivation, hard-anchor avoidance, failure evidence/digest, MAP14_04 handoff readiness까지다.

아직 구현하거나 승인하지 않은 범위는 live MAP11 16-entry catalog → live 169-sector production projection, world seed/assembly, route spine/path edge, traversal envelope/recovery proof, MicroPattern selector/render/cleanup, Activity/Event 실제 배치, final canvas ownership/conflict resolver, retry/backtracking/RNG stream, tile write/physics/collider, debug overlay/preview, Scene/Prefab/Tilemap/GameObject, PlayMode reachability다. spine/envelope의 downstream owner는 아직 잠긴 `MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE`다.

Editor 가시성은 새 EditorWindow/menu/preview가 없고 기존 MAP13 preview도 변경하지 않았다. 최종 active Scene은 `Assets/_Game/Scenes/MapGenerationProgressTest.unity`, root `3`, dirty `false`, selection `0`이다. 게임 가시성은 없다. Scene/Prefab/Tilemap/Material/Texture/GameObject/component를 만들지 않았고 PlayMode를 실행하지 않았다.

## Responsibility and Added Functions

### Added scripts and exact boundary

| Script | Assembly / namespace | Responsibility | Input → output |
|---|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidatePlan.cs(.meta)` | `Game.Map.Runtime` / `StarNight.Map.WorldGeneration.SectorPlanning` | immutable source/candidate/placement/request/result/error/accounting/digest surface | public MAP11-style projection + validated facts → defensive canonical values |
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterCandidateBuilder.cs(.meta)` | same | atomic compatibility, footprint, density and anchor-avoidance filtering | `SectorClusterCandidateBuildRequest` → candidate set/digest/rejection counts or sorted errors only |
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorClusterPlacementPlanner.cs(.meta)` | same | constraint-large-first abstract placement | `SectorClusterPlacementRequest` → immutable placement plan/digest/handoff evidence or sorted errors only |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorClusterCandidatePlacementTests.cs(.meta)` | `Game.Map.Tests.EditMode` / `StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning` | exact fixture, failure, determinism and non-owner proof | test-owned reference input/catalog → 10 focused verdicts and actual metrics |

Unity generated matching GUIDs `1b7d3e7074cc7bb48897cdae732d84a1`, `a8e4615bd77c91a42b58a41b5a934866`, `e204392cd7e51374faf5059786d68037`, `ef99f86d668c62246b8ce4c782a286c6`; duplicate GUID group은 `0`이다.

### Runtime class and method responsibility

| Class / method | Responsibility | Input → output |
|---|---|---|
| `SectorClusterCandidateReason`, `SectorClusterCandidateErrorCode` | required compatibility/rejection/error vocabulary와 `LowerRankedCandidate` accounting | semantic evidence/failure → canonical enum |
| `SectorClusterFootprintCell` constructor | immutable sector-local MicroChunk coordinate | integer x/y → value cell |
| `ToTileRect` | locked constant를 사용한 mapping | one MicroChunk cell → exact `12×8` tile rect |
| `CompareTo` / equality / `GetHashCode` / `ToString` | culture-invariant ordering/identity | cell → stable order/equality/`x,y` token |
| `SectorClusterFootprintPlacement` internal constructor | approved origin의 absolute cells, tile rect, proximity를 defensive-sort | origin + cells + penalty → immutable placement option |
| `SectorClusterSourceProjection` constructor | upstream 변경 없이 MAP11 public type/facts를 읽는 adapter | cluster/variant/transform/biome + compatibility + footprint/origins/policy/order → immutable projection |
| `SectorClusterCandidate` internal constructor | one sector/source의 accepted evidence publication | sector + assignment + source + approved placements/reasons/score → immutable candidate |
| `SectorClusterCandidate.Compare` | stable per-sector candidate ordering | two candidates → primary pacing, larger footprint, lower anchor penalty, catalog/variant/ID order |
| `SectorClusterCandidateBuildRequest` constructor | defensive request boundary and mutation claims | input/assignments/anchor/catalog/label/digest/counters → immutable build request |
| `SectorClusterCandidateSet` internal constructor | global canonical sort and exact candidate/rejection aggregates | accepted candidates + reason counts → immutable candidate publication |
| `CandidatesForSector` | exact sector lookup without exposing mutable storage | `SectorCoord` → read-only ordered candidates |
| `SectorClusterCandidateError` constructor / comparison / equality / `ToString` | accumulated, deduped, stable errors | code+subject+detail → comparable error evidence |
| `SectorClusterCandidateBuildResult` internal constructor | atomic candidate publication | candidate set candidate + errors → set/digest or null/empty + sorted errors |
| `SectorClusterPlacement` internal constructor | selected candidate reservation projection | candidate + approved footprint + constraint class → immutable selected cluster/variant/cells/rects |
| `SectorClusterPlacementRequest` constructor | matching placement boundary and mutation claims | candidate set + anchor plan + label/digest/counters → immutable request |
| `SectorClusterPlacementPlan` internal constructor | placement/rejection/free-cell/handoff publication | ordered placements + rejection counts + hard/free cells + digest → immutable plan |
| `SectorClusterPlacementBuildResult` internal constructor | atomic placement publication | plan candidate + errors → plan/digest or null/empty + sorted errors |
| `SectorClusterCandidateCanonicalDigest.Compute` | public candidate digest rebuild | immutable candidate set → 64 lowercase-hex SHA-256 |
| `SectorClusterPlacementCanonicalDigest.Compute` | public placement digest rebuild | immutable placement plan → 64 lowercase-hex SHA-256 |
| `SectorClusterCanonicalMaterial.ComputeCandidateSet` / `ComputePlacementPlan` | canonical semantic material ownership | sorted candidates/placements/rejections/aggregates → SHA-256 material |
| `Append` / `Hash` | invariant token serialization and UTF-8 SHA-256 | typed values / string → canonical material / lowercase digest |
| `SectorClusterCandidateBuilder.Build` | validation/filter/score/publication orchestration | build request → successful set or atomic failure |
| builder `ValidateRequest` | input/anchor/label/identity/mutation gate | request → accumulated errors only |
| builder `ValidateCatalog` / `IsConnected` | stable ID, 2..5 unique connected cells, `4×4`, density/order/public compatibility gate | source projections → valid sorted projection list + errors |
| builder `ValidateAssignments` | exact current MAP14_01 assignment identity | input + assignments → sector-index map + errors |
| builder `BuildPlacements` | approved origins만 4×4 fit/anchor filter하고 proximity sort | sector + source + anchor plan → read-only approved placements + reject evidence |
| builder `Count` / `Increment` / `SectorSubject` / `Add` / `Failure` | deterministic rejection/error support | counts/facts/errors → stable accounting/atomic result |
| `SectorClusterAnchorUtility.IsBlocking` | Village marker/boundary warning과 hard anchors 구분 | fixed anchor → blocking bool |
| `BlockingCells` | tile rect overlap을 MicroChunk footprint mask로 projection | anchor plan + sector index → unique blocking cells |
| `SectorClusterPlacementPlanner.Place` | sector winner selection, constraint-large-first order, capacity/digest publication | placement request → successful plan or atomic failure |
| placer `ValidateRequest` | candidate digest/anchor identity/label/mutation gate | request → accumulated errors only |
| placer `ValidatePlacements` | `4×4`, `12×8`, anchor/placement overlap, canonical order proof | selected placements + anchors → errors only |
| placer `ConstraintClass` | Special before route/boundary before plain 분류 | sector index + anchors → class `0/1/2` |
| placer `Subject` / `Add` / `Failure` | stable diagnostic support | placement/error facts → subject/error/atomic result |

### Focused test method responsibility

| Test method | Responsibility | Input → asserted output |
|---|---|---|
| `BuildPublishesStableClusterCandidatesFromPlannerInputAndAnchors` | count, per-sector, defensive copy, digest | mutable 22-source list + 9 sectors → immutable `18`, `2×9`, rejected `180`, lowercase digest |
| `CandidatesRespectBiomePacingRouteSocketAccessAndFootprintCompatibility` | all hard-gate reasons and exact rejection matrix | public input/assignments/catalog → base reasons + quiet/special/constraint evidence, `142/34/1/1/1/1` rejects |
| `CandidatesAvoidFixedAnchorsWithoutMutatingAnchors` | no hard-anchor overlap/no source mutation | approved placements + 19 anchors → every overlap false, anchor digest/source identities unchanged |
| `PlacePublishesConstraintLargeFirstClusterPlacementPlan` | deterministic selected order and digest | successful candidate set + anchors → exact 9 cluster IDs, class `0/0/0/1/2...`, handoff true |
| `PlacedFootprintsStayInsideFourByFourGridAndDoNotOverlap` | grid/tile/capacity proof | 9 placements → `26/21/97` placed/hard/free, overlap `0/0` |
| `SpecialVillageOptionalAndActivityBoundariesRemainNonOwningWhereRequired` | MAP13/Activity/Event ownership boundary | Special/Village/Merchant/Activity fixtures → Special anchors respected, reference/deferred/activity ownership 0 |
| `NoCandidateCollisionAndMutationClaimsFailAtomically` | no-candidate/all-collide/duplicate/solver/RNG/tile failures | invalid catalogs/claims → set/plan null, digest empty, sorted required errors, mutation counters 0 |
| `CandidateAndPlacementPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture` | repeat/input-order/culture stability | repeat + reverse + `tr-TR` → exact same candidate/placement digests |
| `BuildAndPlaceDoNotInvokeSpinePatternActivityRetryOrTileSystems` | non-owner counters | successful build/place → spine/envelope/pattern/activity/event/retry/solver/RNG/tile all 0 |
| `CandidateAndPlacementAccountingPublishesExactMap14_04HandoffEvidence` | authoritative metrics and selected identities | full fixture → sectors/candidates/placements/rejections/cells/digests/IDs exact |

`Fixture.Create/CreateSectors/Sector/Mandatory/CreateAnchors/RouteAnchor/AddSpecial/CreateCatalog/Source`는 public MAP14_01/02 API와 MAP11 public value types만 이용해 named reference fixture를 만든다. `H2/V2/V3/H4/L3/Boss5/Cell/Origins/Digest`는 deterministic test data를 구성하며 production seed, physical CSV, private reflection을 사용하지 않는다. `AssertAtomicFailure/BlockingCells/Join`은 focused assertion support만 소유한다.

Production 변경은 신규 Runtime 3개뿐이다. 기존 Runtime/Editor/test C#, CSV/schema, asmdef/asmref, Scene, Prefab, Tilemap, Material, Texture, Settings, Packages 수정은 `0`; upstream MAP14_01/02/MAP11 수정도 `0`; 새 Editor production C#/PlayMode helper/generated report asset도 `0`이다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
assembly_names: [Game.Map.Tests.EditMode]
category_names: [MAP14_03]
job_id: 4967a1cd5e6e475a9db1c86df6ce2622
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 2.3120089
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

`MAP14_03` EditMode category만 한 번 선택했고 첫 run이 `10/10 PASS`했다. MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01/MAP14_02 category, legacy 19347, PlayMode, unfiltered test는 선택하지 않았다. domain reload 직후 Unity Pipeline package의 기존 automated-mode warning 1건이 있었으나 task 관련 warning/error는 없었고, 최종 Console clear 후 error/warning은 `0/0`이다.

## Finalize and Commit

```text
Commit subject: MAP14_03: build and place cluster candidates
Push: NOT PERFORMED
MAP14_04: LOCKED / NOT STARTED
```
