TASK: MAP14_04_BUILD_SECTOR_SPINE_AND_ENVELOPE
STATUS: PASS
MAP14_04: COMPLETE ELIGIBLE only when PASS
MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 Task는 MAP14_01의 `SectorPlannerInput`/`SectorPacingAssignment`, MAP14_02의 `SectorFixedAnchorPlan`, MAP14_03의 `SectorClusterPlacementPlan`을 읽어 sector-local `REFERENCE SPINE GRAPH`와 `REFERENCE TRAVERSAL ENVELOPE`, `ProtectedOpen` 정적 보호 집합을 게시한다. MicroPattern 선택·렌더는 하지 않는다.

추가한 script는 Runtime 3개와 EditMode focused test 1개다.

- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineEnvelopePlan.cs(.meta)`: immutable node/edge/envelope/request/result/error/count/digest surface.
- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorSpineGraphBuilder.cs(.meta)`: public placement·anchor·socket·Special evidence를 node와 ordered route edge로 연결하고 atomic validation을 수행한다.
- `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorTraversalEnvelopeBuilder.cs(.meta)`: graph centerline에서 floor/clearance/landing/recovery/anchor bridge/ProtectedOpen evidence를 도출한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorSpineEnvelopeTests.cs(.meta)`: category `MAP14_04`의 10개 focused fixture/test를 소유한다.

### 실제 reference fixture 수치

```text
sector fixtures: 9
source cluster projections: 22
candidate set: 18
selected placements: 9

nodes total: 34
ExternalSocket 4
BoundaryBridge 1
ClusterEntry 9
ClusterExit 9
SpecialEntry 3
SpecialReturn 3
RecoveryJoin 3
OptionalBranch 2

edges total: 26
MandatoryLow 4
MandatorySpecialConnector 6
BoundaryConnector 1
ClusterConnector 9
OptionalHigh 2
Recovery 1
Return 3

mandatory route edges: 23
optional/high/recovery edges: 3
cluster connectors: 9
Special connectors: 6

envelope evidence total: 3479
Centerline 757
Floor 697
Clearance 748
Landing 52
Recovery 17
ProtectedOpen 1173
ProtectedAnchorBridge 35

unique ProtectedOpen cells: 1173
anchor-compatible protected overlaps: 29
blocking-anchor overlaps: 0
```

`mandatory route edges`는 `MandatoryLow + MandatorySpecialConnector + BoundaryConnector + ClusterConnector + Return` 합계다. `optional/high/recovery edges`는 `OptionalHigh + Recovery` 합계이며, 세 branch의 mandatory 재합류를 게시하는 `Return 3`은 mandatory count에 포함했다. `ProtectedAnchorBridge 35`는 edge별 소유 evidence이고, `anchor-compatible protected overlaps 29`는 sector/coordinate unique ProtectedOpen cell 수라서 서로 다른 의미의 수치다.

선택 cluster/variant는 MAP14_03 public placement identity를 그대로 보존했다.

```text
TC_REF_TRAVERSAL_BRIDGE / SPINE_TRAVERSAL_R0
TC_REF_QUIET_BUFFER / SPINE_QUIET_R0
TC_REF_VILLAGE_APPROACH / SPINE_SAFE_R0
TC_REF_CORE_RESOURCE_RING / SPINE_RESOURCE_R0
TC_REF_FORGE_MACHINERY / SPINE_LANDMARK_R0
TC_REF_BOSS_GATE / SPINE_BOSS_R0
TC_REF_ACTIVITY_SHELL / SPINE_ACTIVITY_R0
TC_REF_DISCOVERY_PASSAGE / SPINE_DISCOVERY_R0
TC_REF_NEIGHBOR_FLOW / SPINE_NEIGHBOR_R0
```

Canonical evidence:

```text
MAP14_01 planner input digest:
93a7182a7ac063c6348fd79b3500a0575643f217b31b8aa4f0d055f113dadbc0

MAP14_02 fixed anchor plan digest:
5721695eca21e4f4852b4789a749c94db2e1ddf7307bf8cfeec660140a9c0e26

MAP14_03 cluster placement plan digest:
c084643048a97ec2d7f8347d87037320f6b99a6185b0af027afd22e6aba0779e

spine graph digest:
df7ccaba728fcd8ecf75cbe0394d3165262760a2c2909ecb1b8bcebd73f32b8c

traversal envelope digest:
457b1c5c58d06166d2a47f6db0e31e5930db7f6989ea0cd3d48f9efed5335afb

spine-envelope plan digest:
0282b7596dcfa964ff62e30c1658ab5c7b9c85ea1ccc408d55b0c443571538e4
```

Input/assignment/anchor/cluster, RouteType/AccessClass, external socket ID, boundary pair/candidate ID, SpecialRegion binding/region ID, cluster/variant/transform/footprint identity는 plan의 before/after digest가 exact equality다. solver/RNG/tile/MicroPattern/Activity·Event/retry/final-canvas/Scene/physics counter는 전부 0이다.

MAP13 SpecialRegion은 Core/Forge/Boss의 entry-return/static route evidence로만 소비했다. Village는 `ReferenceOnlyMarker`라 progression blocker나 live ownership으로 승격하지 않았고, deferred Merchant는 optional terrain branch evidence만 만들며 placed Special connector는 만들지 않았다. Activity-compatible sector도 Activity/Event edge나 placement를 만들지 않는다.

### 보장 범위

- 9개 reference sector 각각의 selected cluster entry/exit를 연결한다.
- 존재하는 external socket, boundary bridge, Core/Forge/Boss entry-return endpoint를 동일 sector의 deterministic mandatory component에 연결한다.
- optional/high/recovery edge는 `RecoveryJoin`과 `Return`을 통해 mandatory route에 재합류한다.
- 모든 node/edge/envelope coordinate는 `48x32` 내부다.
- centerline은 ordered edge route cell과 일치하고, ProtectedOpen은 centerline+clearance+landing+recovery+anchor bridge의 sector-local 합집합이다.
- route/boundary/Special transition anchor overlap은 명시적으로 compatible일 때만 허용하며 `SpecialFootprint`, `SiteReservation`, incompatible anchor overlap은 0이다.
- selected placement footprint를 route evidence가 실제로 통과한다. 현재 placement plan에 게시되지 않은 cluster footprint를 새로 주장하지 않는다.
- repeat, reversed source order, `tr-TR` culture에서 graph/envelope/plan digest가 동일하다.
- invalid missing endpoint/out-of-bounds/blocking-overlap/mutation evidence는 partial graph/plan이나 digest 없이 stable-sorted error로 실패한다.

### 아직 보장하거나 구현하지 않은 범위

- live MAP11 traversal edge의 production tile reachability와 전체 16-entry live catalog integration
- 169-sector production world assembly, inter-sector/world completion reachability
- 최종 tile material, terrain cleanup, collider contact, jump tuning, 실제 player execution
- MicroPattern selection/render/pattern zone, Activity/Event placement, reward/combat/inventory execution
- final canvas ownership/conflict resolver, local retry/backtracking/RNG stream
- Scene/Prefab/Tilemap/GameObject 생성·반영, debug overlay/preview window, PlayMode physics
- placement plan이 게시하지 않은 live/unselected candidate의 실제 tile geometry 검증

따라서 이번 결과는 public anchor/placement를 연결하는 static design proof이며 production 플레이 가능성 승인이나 MAP14 전체 승인이 아니다. Downstream owner는 아직 `LOCKED`인 MAP14_05다.

### Editor / 게임 가시성

- Editor: 신규 EditorWindow, overlay, inspector, generated report asset이 없다. Test Runner의 focused result와 Runtime/test code에서만 데이터가 보인다.
- 게임/Scene: Game view 시각 변화가 없고 active scene `Assets/_Game/Scenes/MapGenerationProgressTest.unity`는 `isDirty=false`, roots `3`, selection `0`을 유지했다.
- Scene/Prefab/ScriptableObject/Tilemap/Material/Texture/GameObject/Settings/Packages/asmdef/asmref 변경은 `NONE`이다.

## Responsibility and Added Functions

### `SectorSpineEnvelopePlan.cs`

| Class / method | 책임 | Input -> Output |
|---|---|---|
| `SectorSpineNodeKind`, `SectorSpineEdgeKind`, `SectorTraversalEnvelopeCellKind`, `SectorSpineEndpointRole` | typed graph/envelope vocabulary | semantic category -> stable enum token |
| `SectorSpineEnvelopeErrorCode` | required failure groups와 canvas/scene/physics 범위 위반 코드 | invalid invariant -> typed error code |
| `SectorSpineEnvelopeError` constructor | immutable error value | code+subject+detail -> error |
| `CompareTo/Equals/GetHashCode/ToString` | dedup·stable sort·diagnostic material | error/error -> ordinal ordering/equality/string |
| `SectorSpineNode` constructor / `CompareTo` | node identity, coordinate, route/access, source evidence와 canonical order | endpoint facts -> immutable/sortable node |
| `SectorSpineEdge` constructor / `CompareTo` | ordered from/to, kind, route class, movement label, clearance, exact centerline | endpoint pair+route cells -> immutable/sortable edge |
| `SectorTraversalEnvelopeCell` constructor / `CompareTo` | sector+coordinate+kind+edge ownership evidence | derived cell facts -> immutable/sortable envelope cell |
| `SectorSpineEnvelopeBuildRequest` constructor | all four public inputs, labels, expected digest, fault/mutation proof를 defensive-copy | planner/assignment/anchor/placement+claims -> immutable request |
| `SectorSpineGraph` constructor | nodes/edges를 stable sort하고 node/edge kind counts와 all source identity digests를 게시 | validated nodes+edges+identities -> immutable graph |
| `SectorSpineGraph.Count` overloads | typed node/edge accounting | kind -> exact count |
| `SectorSpineGraph.CountAll` | 모든 enum key를 포함한 immutable count map | values+selector+all keys -> read-only dictionary |
| `SectorSpineGraphBuildResult` constructor | graph atomicity | graph candidate+errors -> graph/digest or errors-only |
| `SectorSpineEnvelopePlan` constructor | graph+envelope+ProtectedOpen, counts, overlaps, identities, handoff flag 게시 | successful graph+derived cells+digests -> immutable final plan |
| `SectorSpineEnvelopePlan.Count` | typed envelope accounting | cell kind -> exact count |
| `SectorSpineEnvelopeBuildResult` constructor | final plan atomicity | plan candidate+errors -> plan/digest or errors-only |
| `SectorSpineEnvelopeCanonicalDigest.ComputeGraph` | complete graph digest rebuild | immutable graph -> lowercase SHA-256 |
| `ComputeEnvelope` | edge-owned envelope와 unique ProtectedOpen digest | envelope cells+protected cells -> lowercase SHA-256 |
| `ComputePlan` | graph/envelope/overlap 결합 digest | immutable plan -> lowercase SHA-256 |
| `Hash/CoordinateMaterial/NodeMaterial/EdgeMaterial/CellMaterial` | culture-invariant canonical material serialization | values -> ordinal text/SHA-256 material |

### `SectorSpineGraphBuilder.cs`

| Class / method | 책임 | Input -> Output |
|---|---|---|
| `SectorSpineGraphBuilder.Build` | validation, node/edge build, identity digest, atomic publication orchestration | `SectorSpineEnvelopeBuildRequest` -> `SectorSpineGraphBuildResult` |
| `ValidateRequest` | missing input, labels, source digest chain, handoff, mutation/fault gate | request -> accumulated errors |
| `ValidateAssignments` | one exact MAP14_01 public assignment per sector | request assignments -> index map/errors |
| `BuildSectorNodes` | external/boundary/cluster/Special/recovery/optional facts projection | sector+placement+anchors -> typed nodes |
| `Node` | canonical ID와 source identity 구성 | endpoint facts -> `SectorSpineNode` |
| `ValidateNodes` | duplicate/bounds/cluster endpoints/external socket equality | request+nodes -> errors |
| `BuildEdges` | mandatory chain, boundary connector, optional/recovery/return publication | nodes+assignment evidence -> edge list |
| `AddEdge` | blocking-aware path와 label/source ownership 결합 | node pair+kind -> `SectorSpineEdge` or error |
| `ValidateEdgesAndConnectivity` | endpoint references, bounds, blocking, mandatory component, recovery/Special connector gate | nodes+edges -> errors |
| `BuildPath` | deterministic cardinal BFS, no teleport | start+end+blocking cells -> exact ordered centerline or empty |
| `FindOpen` | recovery/optional junction의 deterministic open-cell selection | preferred cell+blocking set -> in-sector open cell |
| `Center/Inside/Distance` | anchor center, bounds, stable proximity primitives | rect/coord pair -> coord/bool/int |
| `AssignmentDigest` | sorted assignment bundle identity | assignments -> SHA-256 |
| `RouteAccessIdentity/ExternalSocketIdentity/BoundaryIdentity/SpecialIdentity/ClusterIdentity` | before/after authority identity proof | public input/placement -> stable SHA-256 identities |
| `Subject/Add/Failure` | stable subject/error/atomic failure helpers | sector/error collection -> formatted subject/error/result |
| `SectorSpineEnvelopeAnchorUtility.IsCompatible` | route/boundary/Special transition compatibility policy | anchor -> bool |
| `BlockingCells` | SpecialFootprint/SiteReservation/incompatible cells rasterization | anchor plan+sector -> blocking tile set |
| `Contains` | anchor overlap probe | anchor+tile -> bool |

### `SectorTraversalEnvelopeBuilder.cs`

| Class / method | 책임 | Input -> Output |
|---|---|---|
| `SectorTraversalEnvelopeBuilder.Build` | graph에서 envelope/ProtectedOpen/overlap counts/digests를 atomic publication | request+successful graph -> `SectorSpineEnvelopeBuildResult` |
| `ValidateInput` | graph digest·labels·input/anchor/placement identity chain | request+graph -> errors |
| `ValidateDerivedCells` | bounds, blocking overlap, edge별 clearance/landing evidence | graph+derived cells -> errors |
| `ValidateProtectedSet` | exact ProtectedOpen union과 selected cluster traversal evidence | all cells+protected cells+placements -> errors |
| `TryAddDerived` | bounds/blocking-safe floor/clearance insertion | edge+candidate cell+kind+blocking -> optional cell |
| `AddCell` | edge-owned envelope evidence dedup | edge+coordinate+kind -> cell map |
| `Inside` | sector-local bounds primitive | tile -> bool |
| `CoordinateKey/CellKey/EdgeCellKey` | sector/coordinate/kind/edge stable identity | cell facts -> ordinal key |
| `Subject/Add/Failure` | stable diagnostic와 atomic failure helpers | sector/error collection -> subject/error/result |

### `SectorSpineEnvelopeTests.cs`

| Test / helper method | 책임 | Input -> Output |
|---|---|---|
| `BuildPublishesCanonicalSpineEnvelopePlanFromClusterPlacements` | immutable plan, counts, digests, handoff와 실제 metric output | valid 9-sector fixture -> plan+lower-hex digests |
| `SpineNodesRepresentExternalBoundaryClusterAndSpecialEndpoints` | exact eight node-kind counts/source identities | valid plan -> 34 typed nodes verified |
| `MandatoryLowRoutesConnectRequiredEndpointsInStableOrder` | required endpoints one component, exact order/no-tool class | valid graph -> 23 mandatory edges verified |
| `OptionalHighAndRecoveryRoutesRejoinMandatoryRoute` | RecoveryJoin/Return topology | valid graph -> optional 2/recovery 1/return 3 verified |
| `EnvelopePublishesProtectedOpenClearanceLandingAndRecoveryCells` | all envelope kinds, bounds, exact protected union | valid plan -> 3479 evidence/1173 protected verified |
| `EnvelopeAvoidsBlockingAnchorsAndPreservesCompatibleBridgeOverlaps` | compatible overlap와 blocking failure | valid plan+blocking fault -> 29/0 and atomic failure |
| `SpineEnvelopePreservesInputAnchorAndClusterIdentities` | ten before/after identity equality checks | source plans -> unchanged identities |
| `InvalidMissingEndpointBlockingOverlapAndMutationClaimsFailAtomically` | invalid endpoint/OOB/blocking/mutation matrix | faulted requests -> graph null/digest empty/sorted errors |
| `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture` | order/culture determinism | repeat+reverse+`tr-TR` -> same three digests |
| `BuildDoesNotInvokePatternActivityRetryTileOrPhysicsSystems` | non-ownership counters | valid build -> all side-effect counters 0 |
| `AssertRequiredConnected` | per-sector required-node BFS test oracle | graph+sector -> connected assertion |
| `Key/Join` | protected coordinate/error diagnostics | cell/errors -> canonical test strings |
| fixture `Create/Request/MutationRequest/Build/BuildPlan` | public MAP14_01/02/03 chain assembly and valid/invalid request execution | reverse/fault/claims -> input/anchor/placement/plan |
| fixture `CreateSectors/Sector/Mandatory/CreateAnchors/RouteAnchor/AddSpecial` | nine named sector and 19-anchor reference projections | fixture facts -> public planner/anchor inputs |
| fixture `CreateCatalog/Source/H2/V2/V3/H4/L3/Boss5/Cell/Origins/Digest` | 22 public cluster projections and deterministic footprint helpers | catalog facts -> MAP14_03 candidate/placement input |

Production Runtime C# 신규 `3`, Runtime EditMode test C# 신규 `1`, matching `.meta` 신규 `4`다. 기존 production/test/CSV/meta 수정은 `0`, upstream 수정은 `0`, Editor production/test 추가는 `0`이다. Downstream owner는 `MAP14_05_ASSIGN_CLUSTER_ROLES_AND_RENDER_PATTERNS`이며 이 Task에서 시작하지 않았다.

## Focused Verification

최초 focused job `3df0f2315dfa43149d3c41142e65a063`은 shared landing coordinate가 edge별 소유 evidence를 dedup한 task-owned 결함을 검출했다. `EdgeId`를 cell identity에 포함해 수정했고, 다른 파일이나 category 없이 동일 focused selection만 재실행했다.

```text
Unity: 6000.3.8f1
mode: EditMode
assembly_names: [Game.Map.Tests.EditMode]
category_names: [MAP14_04]
final job: ae60c6b9048e4f24b97f9aa2fdffc462
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 3.1123129
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

Scene/Prefab changes: `NONE`

Commit subject: `MAP14_04: build sector spine and envelope`
Push: `NOT PERFORMED`

## PASS Gate

- MAP14_03 Result/installed Task metadata SHA-256: exact match.
- inbox candidate: exact one `single_task_v1`; legacy candidate/staging: `0/0`.
- installed/archive Task SHA-256: `937faa91439188f170921e2492020f24c666d7784c2446cc2df2c981250cfd4e` / byte-identical.
- compile errors: `0`.
- final focused MAP14_04: `10/10 PASS`.
- Console after clear: error/warning `0/0`.
- next Task MAP14_05: `LOCKED / DO NOT START`.
