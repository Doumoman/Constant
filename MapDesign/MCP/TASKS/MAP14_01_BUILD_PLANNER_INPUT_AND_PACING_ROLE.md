```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
  task_file: TASKS/MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE.md
  requires_current_task: NONE
  requires_completed_task: MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: 637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2
  requires_installed_task:
    path: TASKS/MAP13_09_MAP13_SPECIAL_REGION_EXIT_TESTS.md
    sha256: efbf8ecbb83217d6ca084b32413b21fafbe4619ab278ff62a4cb9a360429b5db
  sets_current_task: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
```

# MAP14_01 — Build Planner Input and PacingRole

```text
TASK: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
PHASE: MAP14 — Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14 Sector Planner의 첫 입력 계층을 만든다. 이번 Task는 Solver가 아니라, 후속 MAP14_02~10이 소비할 **불변 Sector Planner input snapshot**과 **PacingRole assignment**만 소유한다.

```text
MAP09 layer/pacing/access contracts
MAP10 pattern authority summary
MAP11 TerrainCluster catalog summary
MAP12 Activity/Event availability summary
MAP13 SpecialRegion exit publication
MAP00~08 world/biome/route/boundary/site authority
→ SectorPlannerInputBuilder
→ immutable SectorPlannerInput
→ SectorPacingRolePlanner
→ PacingRole assignment + reasons + stable digest
```

이번 Task는 `48×32` sector 안에 cluster를 배치하지 않는다. boundary anchor 고정, Special footprint 고정, cluster 후보 생성, spine 연결, MicroPattern 렌더링, Activity/Event 배치, ownership conflict, retry/RNG, graybox actual tile path는 모두 다음 Task에서 한다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, 실제 fixture 수치, PacingRole 결정 이유, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| Sector Planner input value model | Sector solve/execution |
| biome/patch/route/boundary/site/optional/neighbor snapshots | cluster placement |
| world progress and landmark-distance facts | fixed boundary/special anchor placement |
| PacingRole candidate scoring and deterministic primary role selection | route spine and traversal envelope |
| input validation, stable errors, canonical digest | MicroPattern render or cleanup |
| no-mutation focused EditMode tests | Activity/Event assignment |
| current public authority compatibility check | canvas ownership/conflict resolver |
| MAP14_02 handoff contract | retry/RNG/failure report/graybox exit |

`PacingRole`은 플레이 리듬 의도다. `AccessClass`, `RouteType`, external socket, required tool, special-entry gate를 바꾸거나 대신하지 않는다.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_01`만 선택한다.

```text
MAP14_01 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_01` category로 제한한다.

신규 task-owned failure는 신규 MAP14_01 allowlist 파일만 수정하고 `MAP14_01` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

## 3. Read-Only Preflight

```text
MAP13_09 Result: PASS
MAP13_09 Result SHA-256:
637fec406f42bf845be5ae9313a036b3ec49f66467539a3552c1f94ad68bd5e2

MAP13_09 installed Task SHA-256:
efbf8ecbb83217d6ca084b32413b21fafbe4619ab278ff62a4cb9a360429b5db

MAP13 PHASE EXIT: APPROVED
MAP13_09 COMPLETE / MAP14_01 CURRENT / MAP14_02 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP09: GenerationLayerCatalog, PacingRole, AccessClass, pass ownership
MAP10: MicroPattern catalog/profile availability summary
MAP11: TerrainCluster authoring catalog, cluster pacing/biome/route compatibility
MAP12: Activity/Event catalog, compatibility/frequency/cap publication
MAP13: SpecialRegionValidationAuditor or preview model publication, exact MAP13 exit facts
MAP00~08: sector coordinate constants, biome patch identity, route graph, boundary pair/candidate authority, site reservation authority
```

If a public accessor is missing, add a small adapter only inside MAP14_01 allowlist when it can read public values without changing upstream ownership. If upstream source must change, `BLOCKED`.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInput.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInputBuilder.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPacingRolePlanner.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerInputAndPacingRoleTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_01
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

필요한 `SectorPlanning` folder와 `.meta`가 이미 존재하지 않으면 `BLOCKED`로 보고한다. 이번 Task에서 새 folder meta를 만들지 않는다.

## 5. Runtime API Surface

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_01 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorPlannerInput
SectorPlannerInputRequest
SectorPlannerInputBuildResult
SectorPlannerSectorSnapshot
SectorPlannerNeighborSnapshot
SectorPlannerBiomeSnapshot
SectorPlannerRouteSnapshot
SectorPlannerBoundarySnapshot
SectorPlannerSiteSnapshot
SectorPlannerSpecialRegionSnapshot
SectorPlannerOptionalRegionSnapshot
SectorPlannerWorldProgressSnapshot
SectorPlannerAuthorityDigestSnapshot
SectorPacingCandidate
SectorPacingAssignment
SectorPacingReason
SectorPlannerInputErrorCode
SectorPlannerInputError
SectorPlannerInputBuilder.Build
SectorPacingRolePlanner.Assign
SectorPlannerInputCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial `SectorPlannerInput` and publishes accumulated, deduped, stable-sorted errors only.

Minimum error groups:

```text
MissingInput | DuplicateSector | SectorOutOfRange | MissingAuthorityDigest
InvalidBiomePatch | InvalidRouteSnapshot | InvalidBoundarySnapshot
InvalidSiteSnapshot | InvalidNeighborSnapshot | InvalidSpecialRegionSnapshot
InvalidOptionalRegionSnapshot
PacingRoleUndefined | PacingAccessCoupling | PacingRouteMutationClaim
LandmarkDistanceInvalid | WorldProgressInvalid
DigestMismatch | NonCanonicalPublication | MutationClaim
```

## 6. Planner Input Snapshot Contract

`SectorPlannerInputBuilder.Build` consumes explicit request records or public authority projections and publishes one immutable input per sector. It does not inspect private fields, parse CSV again, mutate assets, or run a solver.

Each `SectorPlannerSectorSnapshot` must preserve:

```text
sector coordinate / sector index
48×32 canvas constants
biome patch id and biome id
route type and external side requirements
boundary pair/candidate/warning summary per side
site reservations attached to this sector
SpecialRegion reserved/reference/deferred facts
optional region availability facts
neighbor summaries for L/R/U/D
world progress ordinal and chapter/branch bucket if public
nearest mandatory landmark distance
nearest optional landmark distance
source authority digest bundle
```

The builder must prove that PacingRole assignment cannot modify:

```text
RouteType
AccessClass
external sockets
boundary candidate identity
site reservation identity
SpecialRegion binding
Cluster catalog contents
Activity/Event catalogs
```

If actual 169-sector world assembly data is not public yet, use deterministic focused fixtures that represent exact allowed cases. Those fixtures must be labeled `REFERENCE PLANNER INPUT` and must not claim live world publication.

## 7. PacingRole Assignment Contract

`SectorPacingRolePlanner.Assign` receives only a valid `SectorPlannerInput` and returns a deterministic primary role plus ordered candidate/reason evidence.

Use the existing MAP09 `PacingRole` authority. Do not add new PacingRole enum values and do not treat Activity/Event kind as PacingRole.

Minimum assignment rules:

| Condition | Required role evidence |
|---|---|
| mandatory resource site present | `Resource` primary or candidate with highest hard priority |
| Boss SpecialRegion present | `Boss` primary or highest hard priority |
| Forge or other mandatory landmark present | `Landmark` primary, with `Machinery` candidate for Forge when available |
| Village reference shell only | may publish `Safe` or `Landmark`; must not publish mandatory progression dependency |
| MAP08 boundary/warning on side | `Traversal` and boundary reason candidate present |
| high route or recovery need | `Recovery` or `Traversal` candidate present as applicable |
| Activity-compatible sector with no mandatory blocker | `Activity` candidate present, but no Activity placement |
| quiet buffer or low-pressure sector | `Quiet` candidate present |
| optional Merchant/Maru deferred-local only | no placed landmark claim; optional reason only |

Tie-breaking must be stable and documented:

```text
hard priority class
then world progress suitability
then landmark distance bucket
then role canonical order
then sector coordinate order if needed
```

Scoring may be integer-based. Random draws are forbidden in MAP14_01.

World progress and landmark distance:

- world progress may influence candidate score or reason.
- distance must be bucketed deterministically, for example `SameSector`, `Near`, `Medium`, `Far`, `Unknown`.
- negative distance, inconsistent optional/mandatory distance, or unavailable required distance is an atomic input error.
- distance must never create a route, reservation, boundary, or access claim.

## 8. Focused Fixture Matrix

Create focused test fixtures that cover the planner input and PacingRole logic without needing live world assembly.

Minimum fixture set:

| Fixture | Purpose |
|---|---|
| `PlainTraversalBoundarySector` | biome + route + MAP08 boundary side; expects Traversal evidence |
| `QuietBufferSector` | no mandatory blocker, quiet-compatible cluster pool; expects Quiet evidence |
| `VillageReferenceSector` | Village reference shell; expects non-mandatory Safe/Landmark evidence |
| `CoreResourceSector` | one required resource site; expects Resource hard priority |
| `ForgeLandmarkSector` | Forge placed mandatory site; expects Landmark/Machinery evidence |
| `BossGateSector` | Boss placed mandatory site; expects Boss hard priority |
| `ActivityCompatibleSector` | Activity/Event catalogs available but no placement; expects Activity candidate only |
| `DeferredOptionalSector` | Merchant or Maru deferred-local only; expects no placed ownership claim |
| `NeighborInfluencedSector` | L/R/U/D neighbor summaries affect reasons but not sockets |
| `InvalidInputCases` | missing/duplicate/undefined/coupled/mutation claims return zero publication |

These fixtures are test-owned `REFERENCE PLANNER INPUT` examples, not production world seeds.

## 9. Required Tests

`SectorPlannerInputAndPacingRoleTests` must include 10~14 focused tests in category `MAP14_01`.

Minimum assertions:

1. `BuildPublishesImmutableCanonicalSectorPlannerInput`
   - exact fixture count, sector constants `48×32`, authority digest publication, defensive copy, lower-hex digest.
2. `BuildConsumesCurrentPublicAuthoritiesWithoutReparsingOrMutation`
   - MAP09~13 public summaries are consumed; CSV reparse/generated write/Scene dirty/asset mutation counts are 0.
3. `BuildRejectsInvalidDuplicateMissingAndMutationClaimInputsAtomically`
   - errors sorted/deduped; partial input/digest publication 0.
4. `PacingRoleAssignmentKeepsAccessRouteAndBoundaryIdentityUnchanged`
   - Pacing changes do not mutate `AccessClass`, `RouteType`, sockets, boundary or site IDs.
5. `MandatoryResourceBossAndLandmarkReceiveHardPriorityRoles`
   - Resource/Boss/Landmark fixtures get expected primary/candidate evidence.
6. `VillageAndOptionalDeferredDoNotBecomeProgressionBlockers`
   - Village mandatory dependency 0; Merchant/Maru placed claim 0.
7. `BoundaryRouteRecoveryAndNeighborFactsProduceReasonsOnly`
   - boundary/neighbor/recovery facts add reasons but do not create anchors or paths.
8. `ActivityEventAvailabilityProducesCandidateOnly`
   - Activity/Event catalogs can create `Activity` candidate but no placement/marker/spawn.
9. `WorldProgressAndLandmarkDistanceInfluenceTieBreakDeterministically`
   - progress/distance changes role order as expected, no RNG draw.
10. `PacingPublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`
    - repeat/reverse/`tr-TR` stable input digest and assignment digest.

Add more focused tests only if needed to cover the required semantic surface. Do not add broad regression selections.

## 10. Expected Result Report

Result must begin:

```text
TASK: MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE
STATUS: PASS | FAIL | BLOCKED
MAP14_01: COMPLETE ELIGIBLE only when PASS
MAP14_02_FIX_ROUTE_BOUNDARY_AND_SPECIAL_ANCHORS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 Solver가 아니라 planner input + PacingRole assignment라는 점
- 추가한 script와 각 script의 책임
- 실제 fixture 수, sector constants, role assignments, candidate/reason counts, digest 수치
- PacingRole이 AccessClass/RouteType/socket/boundary/site를 바꾸지 않았다는 증거
- MAP13 SpecialRegion은 reference/deferred contract로만 소비됐다는 증거
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input→output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_02

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_01]
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
Commit subject: MAP14_01: build SectorPlanner input and pacing roles
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_02.

## 11. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE.md
MCP_ARCHIVE/MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE.md
MCP/REPORTS/MAP14_01_BUILD_PLANNER_INPUT_AND_PACING_ROLE_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInput.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInput.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInputBuilder.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerInputBuilder.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPacingRolePlanner.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPacingRolePlanner.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerInputAndPacingRoleTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerInputAndPacingRoleTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_02: do not start
STOP after Result and optional PASS finalize commit
```
