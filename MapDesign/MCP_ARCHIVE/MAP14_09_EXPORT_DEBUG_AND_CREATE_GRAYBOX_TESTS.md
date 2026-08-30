```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
  task_file: TASKS/MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS.md
  requires_current_task: NONE
  requires_completed_task: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
  requires_result:
    path: REPORTS/MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY_RESULT.md
    status: PASS
    sha256: 8d3018b85cd1963d3517408f8ded86fa22df0eaa4645e464036310f7b6e4b3d9
  requires_installed_task:
    path: TASKS/MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY.md
    sha256: 7d78db7ba7041b89175bc0afe9bc2e6f8c7d1c688a61458e882ebec869e8029c
  sets_current_task: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
```

# MAP14_09 - Export Debug and Create Graybox Tests

```text
TASK: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01~08의 sector-local planner chain을 사람이 검토 가능한 deterministic debug packet으로 출력하고, 모든 public RouteType, biome, boundary pair, SpecialRegion 조건을 1-sector / 3-sector graybox fixture catalog로 덮는다.

```text
SectorPlannerInput
SectorPacingAssignment
SectorFixedAnchorPlan
SectorClusterPlacementPlan
SectorSpineEnvelopePlan
SectorClusterRolePatternPlan
SectorPatternRenderPlan
SectorQuietActivityEventPlan
SectorCanvasOwnershipPlan
SectorPlannerRetryPlan
→ SectorPlannerDebugExporter
→ SectorPlannerFailureRingExporter
→ SectorPlannerGrayboxFixtureCatalogBuilder
→ immutable SectorPlannerDebugExport + GrayboxFixtureCatalog
→ MAP14_10 exit-test input
```

이번 Task의 "export"는 Runtime immutable debug model과 deterministic text/grid payload를 만드는 것이다. 디스크 파일, Generated asset, EditorWindow, overlay, Scene, Prefab, Tilemap 또는 GameObject를 만들지 않는다.

이번 Task는 MAP14_10 exit approval이 아니다. actual tile reachability, player physics, collider, production world seed, 169-sector solve, Tilemap bake는 여전히 잠겨 있다. MAP14_09는 MAP14_10이 검증할 수 있도록 성공 plan과 실패 1-ring context를 설명 가능한 형태로 준비하는 단계다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, debug section/token/export 수치, failure 1-ring 수치, RouteType/biome/boundary/Special graybox coverage 수치, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| deterministic plan debug export model | MAP14 exit approval |
| success ownership/retry summary sections | actual tile path reachability |
| failure 1-ring context export | collider/physics/player traversal |
| 1-sector graybox fixture catalog | 169-sector production world assembly |
| 3-sector graybox fixture catalog | production seed approval |
| RouteType/biome/boundary/Special coverage audit | Tilemap bake / MicroChunk slice / streaming |
| text/grid token serialization in memory | Scene/Prefab/GameObject mutation |
| focused EditMode tests | EditorWindow/overlay/generated file export |

Debug export must explain, not repair. It cannot change plan state, retry policy, ownership winners, route/access/socket identity, SpecialRegion reservation, pattern selection, Activity/Event marker decisions or RNG draws.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_09`만 선택한다.

```text
MAP14_09 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01~08 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_09` category로 제한한다.

신규 task-owned failure는 신규 MAP14_09 allowlist 파일만 수정하고 `MAP14_09` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_08 Result: PASS
MAP14_08 Result SHA-256:
8d3018b85cd1963d3517408f8ded86fa22df0eaa4645e464036310f7b6e4b3d9

MAP14_08 installed Task SHA-256:
7d78db7ba7041b89175bc0afe9bc2e6f8c7d1c688a61458e882ebec869e8029c

MAP14_08 COMPLETE / MAP14_09 CURRENT / MAP14_10 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput, RouteType, AccessClass, PacingRole, biome, boundary and Special snapshots
MAP14_02: SectorFixedAnchorPlan and anchor identities
MAP14_03: SectorClusterPlacementPlan and candidate/placement evidence
MAP14_04: SectorSpineEnvelopePlan, ProtectedOpen, route envelope, node/edge identities
MAP14_05: SectorClusterRolePatternPlan and SectorPatternRenderPlan
MAP14_06: SectorQuietActivityEventPlan, Quiet/Activity/Event marker evidence
MAP14_07: SectorCanvasOwnershipPlan, owner plane, claim/winner/suppression/conflict evidence
MAP14_08: SectorPlannerRetryPlan, retry policy, attempt trace and RNG evidence
MAP13: SpecialRegion fixed shell/Village/Core/Forge/Boss/Merchant/Maru facts where public
MAP08: approved boundary pair/candidate identity where public
MAP09: pass catalog, layer ownership, PacingRole, AccessClass, MicroPattern/MicroChunk constants
```

MAP14_09 must consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_09-side projection only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

Do not create generated debug files. The only user-facing persisted file from this work is the normal `*_RESULT.md` written by the MCP workflow.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerDebugExport.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerFailureRingExporter.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerGrayboxFixtureCatalog.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerDebugGrayboxTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_09
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
EditorWindow / overlay / inspector
debug export file, generated report asset, JSON file, CSV file
Tilemap bake or MicroChunk slice exporter
MAP14 exit test
```

`SectorPlanning` folders and metas were created by MAP09_00. If missing, report `BLOCKED`; do not create folder metas in this Task.

## 5. Runtime API Surface

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_09 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorPlannerDebugExportKind
SectorPlannerDebugSectionKind
SectorPlannerDebugTokenKind
SectorPlannerDebugSeverity
SectorPlannerGrayboxFixtureKind
SectorPlannerGrayboxCoverageKind
SectorPlannerDebugToken
SectorPlannerDebugSection
SectorPlannerDebugExport
SectorPlannerFailureRingSnapshot
SectorPlannerFailureRingSector
SectorPlannerGrayboxFixture
SectorPlannerGrayboxCoverageAudit
SectorPlannerDebugExportRequest
SectorPlannerDebugExportResult
SectorPlannerDebugExportErrorCode
SectorPlannerDebugExportError
SectorPlannerDebugExporter.Export
SectorPlannerFailureRingExporter.ExportFailureRing
SectorPlannerGrayboxFixtureCatalogBuilder.Build
SectorPlannerDebugCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial debug export/catalog and publishes accumulated, deduped, stable-sorted errors only.

Minimum debug section kinds:

```text
SourceIdentity
RouteAccess
AnchorBoundarySpecial
SpineEnvelope
ClusterPattern
QuietActivityEvent
OwnershipPlanes
RetryRng
FailureRing
GrayboxCoverage
MutationProof
```

Minimum token kinds:

```text
Empty
Terrain
Solid
ProtectedOpen
Reservation
Boundary
Special
Spine
Cluster
Pattern
Quiet
ActivityMarker
EventMarker
Suppressed
Conflict
RetryNode
FailureCenter
NeighborContext
```

Minimum fixture kinds:

```text
OneSector
ThreeSector
FailureOneRing
```

Minimum coverage kinds:

```text
RouteType
Biome
BoundaryPair
SpecialRegion
PacingRole
AccessClass
OwnershipPlane
RetryStage
```

Minimum error groups:

```text
MissingInput | MissingRetryPlan | MissingOwnershipPlan | MissingPlannerInput
MissingFailureTrace | SectorMismatch | RingCenterMissing
RingNeighborMismatch | RingCoordinateOutOfBounds | DebugTokenOutOfBounds
DuplicateDebugToken | DuplicateSection | DuplicateFixtureId
CoverageMissingRouteType | CoverageMissingBiome | CoverageMissingBoundaryPair
CoverageMissingSpecialRegion | CoverageMissingOwnershipPlane | CoverageMissingRetryStage
OneSectorFixtureMissing | ThreeSectorFixtureMissing | FailureRingFixtureMissing
ThreeSectorAdjacencyBroken | FixtureOutOfBounds | FixtureUsesPrivateData
UnsupportedFileWriteClaim | GeneratedAssetMutationClaim | EditorWindowMutationClaim
TileMutationClaim | SceneMutationClaim | PrefabMutationClaim | GameObjectMutationClaim
PlayModeClaim | ExitApprovalClaim | NonCanonicalPublication
```

## 6. Debug Export Contract

`SectorPlannerDebugExporter.Export` consumes successful MAP14_01~08 public plans and publishes deterministic debug sections.

Required success export sections:

```text
SourceIdentity: task IDs, source digests, version labels
RouteAccess: RouteType, AccessClass, external sockets, pacing reason summary
AnchorBoundarySpecial: fixed anchors, boundary pair/candidate, SpecialRegion facts
SpineEnvelope: node/edge counts, ProtectedOpen count, route envelope summary
ClusterPattern: cluster placement, role cell, pattern zone, render summary
QuietActivityEvent: Quiet/Buffer/no-write count, Activity/Event marker-only count
OwnershipPlanes: owner/plane claim, winner, suppression and coverage summary
RetryRng: retry terminal decision, stage counts, MAP14 RNG draw, MAP12 upstream RNG evidence
MutationProof: all forbidden mutation counters
```

Debug export must include:

```text
section ID
section kind
severity
source task ID
source digest
human-readable summary
stable key-value facts
debug tokens where spatial
canonical section digest
```

Spatial debug tokens must:

- use sector coordinate plus sector-local tile coordinate.
- stay inside `48x32`.
- never claim Tilemap ownership.
- be color-independent; token kind and label must carry meaning.
- preserve source claim/owner/route/anchor identity.

Text/grid output:

- provide a deterministic text grid or compact row payload for each exported sector.
- use stable symbols with a legend.
- do not write the grid to disk.
- do not depend on current culture, object hash order, DateTime, Unity instance ID, Scene selection or filesystem path.

If a section cannot be generated without missing source or private data, fail atomically.

## 7. Failure 1-Ring Export Contract

`SectorPlannerFailureRingExporter.ExportFailureRing` consumes a failed attempt or retry trace plus the nearest available public sector context.

1-ring definition:

```text
center sector = failed sector
ring sectors = all available adjacent sectors in the 8-neighbor Moore ring
exported set = center + available ring sectors
```

For edge or synthetic focused fixtures where all 8 neighbors are not present, the export may include fewer ring sectors only when it records which neighbors are missing and why. The normal 9-sector reference fixture should include `1 center + 8 neighbors`.

Required failure ring data:

```text
failure owner/code/subject/detail
retry decision and next stage
attempt ordinal and node ordinal
RNG trace if any
center sector source identities
neighbor sector identities
external sockets and boundary sides touching the ring
SpecialRegion reservations in the ring
ProtectedOpen and Reservation token summaries
ownership conflicts/suppressions relevant to the failure
forbidden fallback flags
```

Failure export must explain what failed and what would be retried or aborted. It must not:

```text
execute another retry
change RNG state
relax validation
carve a corridor
rerandomize sector/world
move anchors or SpecialRegion reservations
write debug files
open an EditorWindow
```

## 8. Graybox Fixture Catalog Contract

`SectorPlannerGrayboxFixtureCatalogBuilder.Build` creates deterministic fixture descriptors for MAP14_10.

Fixture types:

- `OneSector`: a single sector-local reference case with exact route/biome/boundary/Special/ownership/retry tags.
- `ThreeSector`: a center sector plus two adjacent sectors that prove neighbor/route/boundary/Special context can be inspected without world rollback.
- `FailureOneRing`: a failed or aborted attempt with center + 1-ring debug context.

Every fixture must include:

```text
fixture ID
fixture kind
center sector
neighbor sector list
coverage tags
source task IDs and digests
expected RouteType/AccessClass/PacingRole
expected biome ID
expected boundary pair/candidate IDs where present
expected SpecialRegion kind/binding/region IDs where present
expected ownership plane summary
expected retry terminal/stage summary
debug export digest
```

Coverage requirements:

- every public RouteType used by the current MAP14_01 input appears in at least one `OneSector` and one `ThreeSector` fixture.
- current reference minimum must include Type0, Type1, Type2, Type3, Type4, Boundary and Special route conditions when public snapshots expose them.
- every public biome ID used by the current MAP14_01 input appears in at least one `OneSector` and one `ThreeSector` fixture.
- every MAP08-approved boundary pair consumed by current MAP14 inputs appears in at least one boundary fixture; the approved six-pair set remains the source of truth where public.
- every public SpecialRegion condition consumed by MAP14 inputs appears in at least one Special fixture. Village reference, Core, Forge, Boss and deferred Merchant/Maru facts must be represented when public.
- every ownership plane from MAP14_07 appears in debug coverage.
- every retry stage/terminal from MAP14_08 appears in debug coverage or explicit zero-count evidence.

Fixture descriptors are data, not Scene fixtures. Do not create Unity scenes, prefabs, GameObjects, tilemaps, textures or files.

## 9. Coverage Audit Contract

The coverage audit must publish:

```text
route types required / covered / missing
biomes required / covered / missing
boundary pairs required / covered / missing
SpecialRegion facts required / covered / missing
PacingRole required / covered / missing
AccessClass required / covered / missing
ownership planes required / covered / missing
retry stages required / covered / missing
one-sector fixture count
three-sector fixture count
failure-ring fixture count
coverage digest
```

Any missing required public coverage is atomic failure. Do not mark missing coverage as skipped or optional unless the upstream public input truly has no such condition; in that case publish explicit zero-required evidence.

## 10. Identity and No-Mutation Proof

Build/export must prove before/after equality for:

```text
SectorPlannerInput digest
PacingAssignment digest
FixedAnchorPlan digest
ClusterPlacementPlan digest
SpineEnvelopePlan digest
SectorClusterRolePatternPlan digest
SectorPatternRenderPlan digest
SectorQuietActivityEventPlan digest
SectorCanvasOwnershipPlan digest
SectorPlannerRetryPlan digest
MAP12 Activity/Event authority digests consumed where public
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells
ProtectedOpen coordinates and envelope digest
MAP10 pattern render cell identities
Quiet fill cell identities
Activity/Event marker decision identities
retry RNG trace identities
```

The following counters must remain 0:

```text
retry execution
new RNG draw
fallback corridor carve
validation relaxation
whole sector rerandom
whole world rerandom
fixed anchor mutation
boundary socket mutation
SpecialRegion reservation mutation
ProtectedOpen/no-write mask removal
Tilemap write
Scene/Prefab/Tilemap/GameObject mutation
EditorWindow/overlay/inspector mutation
generated debug file write
Activity runtime spawn
Event runtime spawn
reward/combat/crafting/inventory/NPC execution
MAP14 exit approval claim
```

Debug tokens and text grids are in-memory publication and must be counted separately from file or Unity object writes.

## 11. Focused Fixture Matrix

Reuse the MAP14_01~08 fixture chain through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected MAP14_09 responsibility |
|---|---|
| `SuccessDebugExport` | all success sections, token/grid payload, digests and no-mutation proof |
| `FailureOneRingPattern` | missing pattern or transform failure with center + 8-neighbor context |
| `FailureOneRingCluster` | cluster/footprint/spine failure with retry decision and cap/abort evidence |
| `RouteTypeOneAndThreeSectorCoverage` | all public RouteType/route conditions represented |
| `BiomeOneAndThreeSectorCoverage` | all public biome IDs represented |
| `BoundaryOneAndThreeSectorCoverage` | all MAP08-approved/public boundary pairs represented |
| `SpecialOneAndThreeSectorCoverage` | Village/Core/Forge/Boss/deferred Merchant/Maru facts represented where public |
| `OwnershipAndRetryCoverage` | MAP14_07 planes/owners and MAP14_08 stages represented |
| `InvalidExportCases` | missing source, duplicate token, missing coverage, file-write claim fail atomically |
| `DeterminismCases` | repeat/reverse/`tr-TR` stable export/catalog digest |

Fixtures are debug/graybox descriptors, not production seeds, Scene assets or MAP14 exit approval.

## 12. Required Tests

`SectorPlannerDebugGrayboxTests` must include 9~12 focused tests in category `MAP14_09`.

Minimum assertions:

1. `DebugExportPublishesSuccessfulPlanSectionsTokensAndDigest`
   - valid MAP14_01~08 chain publishes immutable debug export, required sections, lower-hex digest and token/grid payload.
2. `FailureRingExportsCenterAndAvailableNeighborContextWithoutRepair`
   - failure ring includes center + available 1-ring, failure owner/code, retry decision and no repair/mutation.
3. `GrayboxCatalogCoversEveryRouteTypeInOneAndThreeSectorFixtures`
   - all public RouteType/route-condition tags have one-sector and three-sector coverage.
4. `GrayboxCatalogCoversEveryBiomeInOneAndThreeSectorFixtures`
   - all public biome IDs have one-sector and three-sector coverage.
5. `GrayboxCatalogCoversEveryBoundaryPairInOneAndThreeSectorFixtures`
   - all public MAP08 boundary pairs consumed by MAP14 have coverage and missing count 0.
6. `GrayboxCatalogCoversEverySpecialConditionInOneAndThreeSectorFixtures`
   - Village/Core/Forge/Boss/deferred public Special facts have coverage where required.
7. `CoverageIncludesOwnershipPlanesAndRetryStages`
   - ownership planes/owners and retry stage/terminal tags are covered or explicitly zero-required.
8. `DebugExportPreservesAllUpstreamIdentityAndDoesNotDrawRng`
   - all MAP14_01~08 digests and identities are before/after equal; new RNG draw count 0.
9. `InvalidMissingSourceDuplicateTokenMissingCoverageAndFileWriteClaimsFailAtomically`
   - invalid requests publish null export/catalog, empty digest, stable-sorted errors and zero mutation.
10. `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
    - repeat/reverse/`tr-TR` produce identical debug export, failure ring and graybox catalog digests.
11. `NoTilePhysicsScenePreviewGameplayOrExitApprovalMutation`
    - Tilemap, physics, Scene/Prefab/GameObject, EditorWindow/overlay, generated file, gameplay spawn and MAP14 exit approval counters are 0.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 13. Expected Result Report

Result must begin:

```text
TASK: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
STATUS: PASS | FAIL | BLOCKED
MAP14_09: COMPLETE ELIGIBLE only when PASS
MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 in-memory debug export + graybox fixture catalog이며 Tilemap/Scene/gameplay/MAP14 exit가 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 debug export count, section count, token count, text/grid payload count
- failure 1-ring export count, center/ring sector count, missing-neighbor count
- one-sector / three-sector / failure-ring fixture count
- RouteType/biome/boundary/Special coverage required/covered/missing 수치
- ownership plane/owner와 retry stage coverage 수치
- MAP14_01~08 identity가 변하지 않았다는 증거
- new RNG draw 0, retry execution 0, file export 0, Tilemap/Scene/Prefab/GameObject/spawn 0
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input -> output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_10

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_09]
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
Commit subject: MAP14_09: export debug and create graybox tests
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_10.

## 14. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS.md
MCP_ARCHIVE/MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS.md
MCP/REPORTS/MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerDebugExport.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerDebugExport.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerFailureRingExporter.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerFailureRingExporter.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerGrayboxFixtureCatalog.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerGrayboxFixtureCatalog.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerDebugGrayboxTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerDebugGrayboxTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_10: do not start
STOP after Result and optional PASS finalize commit
```
