TASK: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
STATUS: PASS
MAP14_01: COMPLETE ELIGIBLE only when PASS
MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP14_01은 Sector solver나 world placement를 구현한 Task가 아니다. 이번 변경은 MAP00~13의 공개 요약을 다시 해석하거나 CSV를 재파싱하지 않고, 이후 MAP14_02~10이 읽을 수 있는 immutable `SectorPlannerInput`과 deterministic `PacingRole` assignment evidence를 만든다. 모든 focused fixture는 `REFERENCE PLANNER INPUT`으로 명시되며 live generated-world publication을 주장하지 않는다.

추가된 production script는 세 개다.

- `SectorPlannerInput.cs`는 sector/biome/route/boundary/site/SpecialRegion/optional/neighbor/world-progress/authority snapshot, build result/error, pacing candidate/assignment, canonical digest value model을 소유한다.
- `SectorPlannerInputBuilder.cs`는 공개 authority digest bundle과 명시적 sector record를 검증하고, 성공 시에만 canonical immutable input을 게시한다. 실패 시 accumulated/deduped/stable-sorted error만 게시하며 input/digest는 `null/empty`다.
- `SectorPacingRolePlanner.cs`는 유효 input만 받아 hard priority → world-progress suitability → landmark-distance bucket → MAP09 role canonical order → sector coordinate 순으로 deterministic primary/candidate/reason을 게시한다. RNG와 solver를 호출하지 않는다.

추가된 test script는 `SectorPlannerInputAndPacingRoleTests.cs` 하나다. 정확히 10개 `MAP14_01` EditMode test와 10개 이름 있는 fixture matrix(유효 sector 9개 + `InvalidInputCases`)만 포함한다.

실제 authoritative fixture 수치는 다음과 같다.

```text
fixture matrix: 10
valid sector snapshots: 9
invalid build groups: 4 (missing / duplicate / undefined / mutation-coupled)
canvas: 48x32 tiles
world constants preserved: 13x13 sectors / 169 total
published candidates / reasons: 12 / 12
input digest: 93a7182a7ac063c6348fd79b3500a0575643f217b31b8aa4f0d055f113dadbc0
authority bundle digest: d75b0e4cf25d5df65045665755d386416d5758021fd194a3e69c6068001160c3
CSV reparse / generated write / Scene mutation / asset mutation: 0 / 0 / 0 / 0
solver invocation / RNG draw: 0 / 0
route / access / socket / boundary / site / catalog mutation: 0 / 0 / 0 / 0 / 0 / 0
Activity placement / marker / spawn: 0 / 0 / 0
```

공개 authority snapshot은 MAP09 `GenerationLayerCatalog.StableDigest`, MAP13 `CoreResourceRegionStarterCatalog` 3개와 `SpecialLandmarkRegionStarterCatalog` 4개의 현재 public digest/count를 직접 캡처했다. 명시적으로 전달된 승인 summary는 MAP10 MicroPattern `24`, MAP11 TerrainCluster `16`, MAP12 Activity/Event `7/5`, MAP13 audit digest `a7ab6fd571425c4c8e64d7eecad5dd246a3d9a8a08044801800948fc2fa03e4e`다. private reflection, physical CSV read, Generated write는 없다.

| Fixture | Primary | Ordered candidates | Reasons | Candidate/reason | Assignment digest |
|---|---|---|---|---:|---|
| `PlainTraversalBoundarySector` | Traversal | Traversal, Recovery | boundary warning, high/recovery route | 2/2 | `efcba19047789fad1da608e39b09cc98859a2582181ff7bb410e10feae8ce340` |
| `QuietBufferSector` | Quiet | Quiet | quiet-compatible low pressure | 1/1 | `1e2e45bb7e58ec493e22aa225c61ab6e6fb616b757f744c10c609bafd572cf3a` |
| `VillageReferenceSector` | Safe | Safe, Landmark | Village reference shell; no progression ownership | 2/1 | `2aa918d4e67358238670b58006c7061e2c946901fff4070fb950429044be7bad` |
| `CoreResourceSector` | Resource | Resource | mandatory resource hard priority 90 | 1/1 | `1c991fccf7d5658b16d336467ad35fdac578c473ea6d698b4f5a7aa6ae2de4b6` |
| `ForgeLandmarkSector` | Landmark | Landmark, Machinery | mandatory landmark 80 + Forge compatibility 70 | 2/2 | `de63c6fb8058e0315e8867bf689f126654b02c88f1947a223cb3195d3fec13b8` |
| `BossGateSector` | Boss | Boss | mandatory Boss hard priority 100 | 1/1 | `1317396c89fad061aa5586310fa1ce40d737e3c17da8dbad481304c890bf2615` |
| `ActivityCompatibleSector` | Activity | Activity | Activity/Event catalogs available; placement absent | 1/2 | `29aaed9857cc83a231c83d6e0454447208687fef7056d5f46d0b79e02b70817d` |
| `DeferredOptionalSector` | Discovery | Discovery | Merchant deferred-local availability only | 1/1 | `62bee382e6f2ca2d301eb3ba76131c528f30d7ec9a8662851d071cc8ac14831f` |
| `NeighborInfluencedSector` | Traversal | Traversal | exact L/R/U/D neighbor context only | 1/1 | `f21a368160cf800cc94969e03803766c8378be50da62397fe85c7731592505d0` |
| `InvalidInputCases` | none | none | four atomic failure groups | 0/0 publication | empty |

PacingRole 결정 이유는 다음과 같다.

- Boss, required resource, mandatory landmark가 각각 hard class `100/90/80`으로 일반 pacing evidence보다 먼저 결정된다.
- Forge는 `Landmark`를 primary로 유지하면서 MAP09에 이미 존재하는 `Machinery`를 compatible candidate로만 더한다.
- boundary warning은 `Traversal`, high/recovery fact는 `Recovery`, four-neighbor summary는 `Traversal` reason만 추가한다. 이 evidence는 route, socket, boundary anchor를 만들지 않는다.
- Village는 `ReferenceOnly`이므로 Safe/Landmark tie에서 progress ordinal `2`의 Safe suitability가 이기며 reservation/placed/global dependency는 모두 0이다.
- Activity/Event 공개 가능성은 `Activity` candidate와 두 reason만 게시하며 placement/marker/spawn은 0이다.
- Merchant/Maru deferred-local은 optional `Discovery` reason만 내고 footprint, reservation, placed ownership은 게시하지 않는다.
- 같은 hard class에서는 progress suitability, candidate에 관련된 mandatory/optional distance bucket(`SameSector/Near/Medium/Far/Unknown`), role enum order가 순서대로 tie를 푼다. early/late progress와 반대 distance probe가 각각 Safe/Landmark로 뒤집혔고 RNG draw는 0이었다.

no-mutation 검증은 assignment 전후 `RouteType`, `AccessClass`, external socket list, boundary pair/candidate ID, site/reservation ID, SpecialRegion binding의 identity 문자열을 비교하고, assignment source identity에 authority bundle digest를 포함한다. 모든 mutation counter는 0이었다. 따라서 PacingRole은 compatibility/evidence일 뿐 route/access/anchor/catalog authority가 아니다.

MAP13 PASS가 승인한 범위는 SpecialRegion reference contract canonical publication, placed-reference site/buffer/fixed-layer 무모순, Village의 non-mandatory local shell/state 경계, CoreResource reward/recovery/persistence proof, Forge/Boss seal/reset proof, Merchant/Maru deferred-local handoff, MAP13 preview publication을 planner input이 읽을 수 있다는 경계까지다. MAP14_01은 그중 Village를 `ReferenceOnly`, Core/Forge/Boss를 `ReservedMandatory`, Merchant/Maru를 `DeferredOptionalLocal` snapshot으로만 소비했다.

MAP13과 MAP14_01이 아직 승인하거나 구현하지 않은 범위는 live 169-sector world assembly/publication, boundary/special anchor 실제 고정, cluster candidate 생성/배치, spine/envelope 연결, MicroPattern render/cleanup, Activity/Event 실제 배치, ownership/conflict resolver, retry/RNG/failure report, graybox/tile path, player physics reachability, reward/save/inventory/crafting, Boss AI/combat, NPC/shop/hint gameplay object, production CSV/schema/importer, Scene/Prefab/Tilemap/bake/streaming이다. downstream owner는 검수 후에도 잠긴 `MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS`이며 이번 실행에서는 시작하지 않았다.

Editor 가시성은 새 EditorWindow/menu/preview가 없으며 기존 MAP13 `Tools/MapDesign/Special Region Validator & Preview`를 변경하지 않았다. 최종 active Scene은 `Assets/_Game/Scenes/MapGenerationProgressTest.unity`, root `3`, dirty `false`, selection `0`이다. 게임 가시성은 없다. 새 GameObject, component, Scene, Prefab, Tilemap, Material, Texture, generated asset을 만들지 않았고 PlayMode를 실행하지 않았다.

## Responsibility and Added Functions

### Added scripts and exact boundary

| Script | Assembly / namespace | Responsibility | Input → output |
|---|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInput.cs(.meta)` | `Game.Map.Runtime` / `StarNight.Map.WorldGeneration.SectorPlanning` | immutable input/value/error/candidate/digest surface | explicit public projections → defensive, ordinal, culture-invariant planner values |
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInputBuilder.cs(.meta)` | same | atomic validation/publication | `SectorPlannerInputRequest` → `SectorPlannerInputBuildResult` with input+digest or sorted errors only |
| `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPacingRolePlanner.cs(.meta)` | same | deterministic role candidate/reason ordering | valid input or input+coord → one or ordered-many `SectorPacingAssignment` |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerInputAndPacingRoleTests.cs(.meta)` | `Game.Map.Tests.EditMode` / `StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning` | exact 10 fixture contract tests | test-owned `REFERENCE PLANNER INPUT` → focused assertions/evidence only |

Unity generated matching script metas with GUIDs `71e7caa3067ea904bb55063f1fabbedc`, `134fec364beaba44db04daa7bfaa9ea2`, `7856b726d3dc1924ab98b945b190ac8d`, `b74c85b4d7171884cbfb2a43df3fbaa3` respectively.

### Runtime class and method responsibility

| Class / method | Responsibility | Input → output |
|---|---|---|
| six task-owned enums (`SectorPlannerSide`, `SectorPlannerSpecialRegionKind`, `SectorPlannerSpecialRegionBinding`, distance bucket, reason, error code) | stable semantic tokens without changing MAP09 enums | approved semantic fact → canonical enum value |
| `SectorPlannerInputError` constructor / `CompareTo` / equality / `ToString` | immutable, dedupable, stable error ordering | code+subject+detail → comparable error evidence |
| `SectorPlannerAuthorityDigestSnapshot` constructor | preserve MAP00~13 digest/count bundle and its digest | nine public source digests + six counts → immutable authority snapshot |
| `CaptureCurrentPublicAuthorities` | adapt current public MAP09/MAP13 authorities without upstream edit | MAP00~08/MAP10~12/MAP13 audit summaries → bundle using current layer/resource/landmark public digests |
| `EnumerateDigests` | stable builder validation projection | authority snapshot → nine source digest values |
| `SectorPlannerBiomeSnapshot` constructor | preserve biome patch and biome identity | patch ID + biome ID → immutable pair |
| `SectorPlannerRouteSnapshot` constructor | preserve route/access/external sockets and high/recovery facts | route type + AccessClass + sockets + flags → ordinal immutable route snapshot |
| `SectorPlannerBoundarySnapshot` constructor | preserve MAP08 side pair/candidate/warning identity | side + pair ID + candidate ID + warning count → immutable boundary fact |
| `SectorPlannerSiteSnapshot` constructor | preserve site/reservation/mandatory fact | site ID/kind + reservation ID + flag → immutable site fact |
| `SectorPlannerSpecialRegionSnapshot` constructor / `None` | preserve none/reference/reserved/deferred MAP13 ownership | region/kind/binding/footprint/claims → immutable special fact |
| `SectorPlannerOptionalRegionSnapshot` constructor | preserve Merchant/Maru availability without placement | optional region/kind/availability/deferred/claim → immutable optional fact |
| `SectorPlannerNeighborSnapshot` constructor | preserve per-side neighbor route/access/socket/role summary | side+coord+route/access+sockets+role → immutable neighbor fact |
| `SectorPlannerWorldProgressSnapshot` constructor | preserve ordinal/chapter/branch and distances | progress buckets + two distances → immutable tie-break facts |
| `SectorPlannerSectorSnapshot` constructor | combine and ordinal-sort one sector's public facts | coordinate/index/48x32 and all snapshots/compatibility flags → immutable sector snapshot |
| `SectorPlannerInputRequest` constructor | defensive-copy explicit build request and zero-claim counters | sector sequence+authority+publication/digest/claims → immutable request |
| `SectorPlannerInput` constructor / `TryGetSector` | canonical sector-index publication and coordinate lookup | validated sectors+authority+digest / coordinate → immutable input / exact sector lookup |
| `SectorPlannerInputBuildResult` constructor | enforce atomic result | input candidate + errors → success input/digest or failure errors with zero publication |
| `SectorPacingCandidate` constructor | publish one ordered scoring witness | role+hard class+suitability+distance bucket+reason → immutable candidate |
| `SectorPacingAssignment` constructor | publish primary, candidates, reasons, identity/digest and zero side effects | ordered candidate/reason evidence → immutable assignment |
| `SectorPlannerInputCanonicalDigest.Compute` | rebuild canonical SHA-256 | immutable input → 64 lowercase-hex digest |
| digest internal `Compute/ComputeIdentity/Hash/Append` | canonicalize sorted input/identity material invariantly | snapshots/identity fields → canonical material and SHA-256 |
| `SectorPlannerInputBuilder.Build` | orchestrate all validation and atomic publication | request → successful input/digest or accumulated errors only |
| builder validation methods | publication/authority/claims/coord/48x32/biome/route/boundary/site/special/optional/neighbor/progress/role compatibility gates | request facts → ordered errors; no mutation |
| `SectorPacingRolePlanner.Assign(input)` | assign every sector in index order | valid input → read-only ordered assignment list |
| `SectorPacingRolePlanner.Assign(input, coord)` | assign an exact sector | valid input + published coordinate → one assignment or argument error for absent coord |
| planner `AddCandidate/Compare/Suitability/DistanceFor/Bucket` | build and tie-break evidence without random draws | immutable sector facts → unique role candidates in documented deterministic order |

### Focused test method responsibility

| Test method | Responsibility | Input → asserted output |
|---|---|---|
| `BuildPublishesImmutableCanonicalSectorPlannerInput` | exact count/constants/defensive copy/digest | mutable 9-sector source → immutable 9-sector 48x32 input, lowercase digest |
| `BuildConsumesCurrentPublicAuthoritiesWithoutReparsingOrMutation` | MAP09~13 public adapter and zero claims | current public digests/counts → exact authority bundle, six build side-effect counters 0 |
| `BuildRejectsInvalidDuplicateMissingAndMutationClaimInputsAtomically` | atomic accumulated validation | four invalid groups → failure, input null, digest empty, sorted/deduped required errors |
| `PacingRoleAssignmentKeepsAccessRouteAndBoundaryIdentityUnchanged` | no-mutation proof | 9 valid sectors → 9 assignments, source identities unchanged, six mutation counters 0 |
| `MandatoryResourceBossAndLandmarkReceiveHardPriorityRoles` | mandatory priority rules | Core/Forge/Boss → Resource 90, Landmark 80+Machinery 70, Boss 100 |
| `VillageAndOptionalDeferredDoNotBecomeProgressionBlockers` | MAP13 handoff boundary | Village/Merchant facts → Safe/Discovery evidence, reservation/placed/dependency/placement 0 |
| `BoundaryRouteRecoveryAndNeighborFactsProduceReasonsOnly` | route/boundary/neighbor evidence-only rule | boundary+recovery and four neighbors → Traversal/Recovery reasons, anchor/path mutation 0 |
| `ActivityEventAvailabilityProducesCandidateOnly` | availability is not placement | public availability flags → Activity candidate + two reasons, placement/marker/spawn 0 |
| `WorldProgressAndLandmarkDistanceInfluenceTieBreakDeterministically` | documented tie-break | early/late and opposite-distance Village probes → Safe/Landmark flips, RNG 0 |
| `PacingPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture` | repeat/order/culture canonicality and actual evidence output | repeat/reverse/`tr-TR` → identical input/assignment digests, candidate/reason 12/12 |

`PlannerFixtureSet.Create/Request/BuildValid/BuildVillageProbe` owns only deterministic test data construction: approved summary constants and named records → focused requests/builds/probes. It is internal to the EditMode test assembly and is not a production world seed.

Production changes outside the three new Runtime scripts: 0. Existing Runtime/Editor/test C#, CSV, schema, asmdef/asmref, Scene, Prefab, Tilemap, Material, Texture, Settings, Packages changes: 0. Upstream source modifications: 0. New Editor production C#/PlayMode helper/generated report asset: 0.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
assembly_names: [Game.Map.Tests.EditMode]
category_names: [MAP14_01]
job_id: 9f7619391ac94971b6c58727cffc2672
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 1.8883859
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

Only the `MAP14_01` EditMode category was selected. No MAP09/MAP10/MAP11/MAP12/MAP13 category, legacy 19347, PlayMode, or unfiltered test invocation was issued. The first focused run passed 10/10; a read-only review then tightened ordinal creation-time sorting and required-role compatibility inside task-owned files, after which the final authoritative run above passed 10/10. No test or production invariant failure occurred.

## Finalize and Commit

```text
Commit subject: MAP14_01: build SectorPlanner input and pacing roles
Push: NOT PERFORMED
MAP14_02: LOCKED / NOT STARTED
```
