```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
  task_file: TASKS/MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY.md
  requires_current_task: NONE
  requires_completed_task: MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS
  requires_result:
    path: REPORTS/MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS_RESULT.md
    status: PASS
    sha256: e5bcbbd49f33a727223bc217e69a9c568fa2c957abea8664dceecf2a76fc43a8
  requires_installed_task:
    path: TASKS/MAP14_07_IMPLEMENT_CANVAS_OWNERSHIP_AND_CONFLICTS.md
    sha256: 2814b2940d582e6e9ed5937f2e1c337defa24f307ed265fd84d3e3e5b7669dc2
  sets_current_task: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
```

# MAP14_08 - Implement Local Retry and RNG Policy

```text
TASK: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
PHASE: MAP14 - Cluster-first Sector Planner
STATUS: CURRENT
NEXT: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. Goal and User Report

MAP14_01~07의 sector-local planner chain에 실패 복구 정책과 pass별 deterministic RNG trace를 붙인다.

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
existing DeterministicRngStreamFactory / approved stream IDs
→ SectorPlannerRetryRngPolicy
→ SectorPlannerAttemptTraceBuilder
→ SectorPlannerRetryExecutor
→ immutable SectorPlannerRetryPlan
→ MAP14_09 debug/graybox input
```

이번 Task는 **실패했을 때 어떤 단계만 다시 시도할지**, **각 시도가 어떤 deterministic RNG stream/scope/draw를 썼는지**, **attempt/node cap을 넘었는지**, **임의 통로 굴착·전체 sector 재랜덤·validation 완화가 없었는지**를 게시한다.

이 Task는 retry/RNG 정책과 trace를 구현하지만 169-sector production world assembly, debug export, graybox scene, Tilemap bake, collider/physics/player traversal, Scene/Prefab/GameObject 반영, Activity/Event runtime spawn은 하지 않는다. MAP14_09가 debug/graybox 출력을 소유하고 MAP14_10이 MAP14 phase exit를 소유한다.

Result 첫 섹션은 한국어 `## User-Facing Implementation Report`, 두 번째는 `## Responsibility and Added Functions`로 작성한다. 추가한 모든 script, class/method별 책임, 입력→출력, retry stage/attempt/RNG draw 실제 수치, cap evidence, 금지 fallback 0, 회귀 작업 0, 아직 구현하지 않은 범위, Editor/게임 가시성을 반드시 보고한다.

## 1. Responsibility and Non-Ownership

| 소유 | 소유하지 않음 |
|---|---|
| retry stage vocabulary and ordered recovery policy | 169-sector world assembly |
| pass-specific RNG stream/scope trace | production seed approval |
| deterministic attempt/node cap accounting | debug export / graybox fixtures |
| typed failure-to-retry mapping | MAP14 exit approval |
| pattern -> transform -> cluster -> footprint retry order | Tilemap bake / MicroChunk slice / streaming |
| no arbitrary carve / no validation relaxation proof | collider/physics/player traversal |
| immutable retry plan digest and handoff | Scene/Prefab/GameObject mutation |
| focused EditMode tests | Activity/Event/NPC/reward runtime execution |

Retry here means controlled local re-selection of allowed inputs for a failed sector-local attempt. It must not hide defects by creating fallback corridors, relaxing protected masks, changing sockets, rewriting boundaries, moving SpecialRegion reservations, or randomizing the whole sector/world.

## 2. Focused-Only and No-Regression Policy

정상 실행은 EditMode category `MAP14_08`만 선택한다.

```text
MAP14_08 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14_01~07 category selections: 0
legacy 19347: 0
PlayMode: 0
unfiltered tests: 0
```

컴파일 확인과 Console 확인은 허용한다. test selection은 `MAP14_08` category로 제한한다.

신규 task-owned failure는 신규 MAP14_08 allowlist 파일만 수정하고 `MAP14_08` category만 재실행한다.

upstream public API defect, 기존 data contradiction, 기존 Result/Task SHA mismatch, 또는 기존 production code 수정이 필요한 문제를 발견하면 기존 파일을 고치지 않는다. owner/invariant/reason/minimum verification을 Result에 기록하고 `BLOCKED`로 STOP한다.

문제가 실제로 발생하지 않은 상태에서 prior category, legacy regression, PlayMode, unfiltered test를 돌리지 않는다. 회귀가 필요하다고 판단한 경우에도 이 Task 안에서 임의로 실행하지 말고 `REGRESSION TRIGGER DETECTED: YES`와 정확한 원인을 Result에 기록한 뒤 STOP한다.

## 3. Read-Only Preflight

```text
MAP14_07 Result: PASS
MAP14_07 Result SHA-256:
e5bcbbd49f33a727223bc217e69a9c568fa2c957abea8664dceecf2a76fc43a8

MAP14_07 installed Task SHA-256:
2814b2940d582e6e9ed5937f2e1c337defa24f307ed265fd84d3e3e5b7669dc2

MAP14_07 COMPLETE / MAP14_08 CURRENT / MAP14_09 LOCKED
inbox candidate / unrelated staged: 0 / 0
Unity compile / relevant Console error: 0 / 0
```

Required public authority:

```text
MAP14_01: SectorPlannerInput and SectorPacingAssignment
MAP14_02: SectorFixedAnchorPlan and anchor identities
MAP14_03: SectorClusterPlacementPlan, candidate/placement failure identities where public
MAP14_04: SectorSpineEnvelopePlan, ProtectedOpen, route envelope, failure identities where public
MAP14_05: SectorClusterRolePatternPlan, SectorPatternRenderPlan, pattern/render failure identities where public
MAP14_06: SectorQuietActivityEventPlan, Activity/Event marker decisions and MAP12 RNG evidence where public
MAP14_07: SectorCanvasOwnershipPlan, ownership/conflict failure identities where public
MAP10_04/MAP12_03/MAP12_04: existing deterministic RNG stream usage patterns where public
MAP02_02: approved DeterministicRngStreamFactory and registered stream IDs where public
MAP09: pass catalog, layer ownership, PacingRole, AccessClass, MicroPattern/MicroChunk constants
```

MAP14_08 must consume public values. Do not reparse physical CSV and do not inspect private fields. If a public accessor is missing, add a small MAP14_08-side projection only when it can read public values without changing upstream source. If upstream source must change, `BLOCKED`.

Do not modify the global RNG registry or MAP02 source in this Task. If a new globally registered stream is required, report `BLOCKED`; otherwise use existing approved stream IDs with MAP14-owned scope labels and publish trace evidence locally.

## 4. Exact Write Boundary

정상 범위는 Runtime production 3개, focused Runtime EditMode test 1개와 matching meta다.

```text
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicy.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerAttemptTrace.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryExecutor.cs(.meta)
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicyTests.cs(.meta)
```

```text
Runtime assembly: Game.Map.Runtime
Runtime namespace: StarNight.Map.WorldGeneration.SectorPlanning
Tests assembly: Game.Map.Tests.EditMode
Tests namespace: StarNight.Map.Tests.EditMode.WorldGeneration.SectorPlanning
Category: MAP14_08
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
global RNG registry modification
MAP02 deterministic RNG source modification
Tilemap bake or MicroChunk slice exporter
```

`SectorPlanning` folders and metas were created by MAP09_00. If missing, report `BLOCKED`; do not create folder metas in this Task.

## 5. Runtime API Surface

프로젝트 naming/style에 맞추되 다음 semantic surface를 제공한다. 기존 type 이름과 충돌하면 MAP14_08 Result에 이유를 기록하고 같은 책임을 가진 충돌 없는 이름을 사용한다.

```text
SectorPlannerRetryStage
SectorPlannerRetryDecisionKind
SectorPlannerRetryFailureOwner
SectorPlannerRngPassScope
SectorPlannerRetryLimit
SectorPlannerRetryPolicy
SectorPlannerRngTrace
SectorPlannerAttemptTrace
SectorPlannerRetryNodeTrace
SectorPlannerRetryPlan
SectorPlannerRetryBuildRequest
SectorPlannerRetryBuildResult
SectorPlannerRetryErrorCode
SectorPlannerRetryError
SectorPlannerAttemptTraceBuilder.Build
SectorPlannerRetryExecutor.Execute
SectorPlannerRetryCanonicalDigest
```

All public models are immutable, defensive-copy collections, stable-sorted where order is semantic, and culture-invariant. Any error returns no partial retry plan and publishes accumulated, deduped, stable-sorted errors only.

Minimum retry stages:

```text
None
PatternCandidate
PatternTransform
ClusterVariant
ClusterFootprint
SectorAttempt
Abort
```

Minimum decision kinds:

```text
AcceptFirstPass
RetryPatternCandidate
RetryPatternTransform
RetryClusterVariant
RetryClusterFootprint
AbortCapReached
AbortUnownedFailure
AbortForbiddenFallback
AbortNonDeterministicTrace
```

Minimum failure owners:

```text
Input
Anchor
ClusterPlacement
SpineEnvelope
PatternSelection
PatternApplication
PatternRender
QuietActivityEvent
CanvasOwnership
RngPolicy
ForbiddenFallback
Unknown
```

Minimum pass scopes:

```text
SectorPlan
PatternCandidate
PatternTransform
ClusterVariant
ClusterFootprint
ActivitySelection
EventSelection
RetryDecision
```

Minimum error groups:

```text
MissingInput | MissingOwnershipPlan | MissingRetryPolicy | MissingRngAuthority
SectorMismatch | InvalidRetryOrder | InvalidRetryLimit | RetryCapExceeded
NodeCapExceeded | UnknownFailureOwner | UnretryableFailure
ForbiddenFallbackAttempt | ValidationRelaxationAttempt | WholeSectorRerandomAttempt
WholeWorldRerandomAttempt | SyntheticCorridorAttempt | SocketMutationAttempt
BoundaryMutationAttempt | SpecialReservationMutationAttempt | ProtectedMaskRelaxationAttempt
NonDeterministicRngTrace | RngStreamMismatch | RngScopeMismatch
RngDrawMismatch | NegativeAttemptOrdinal | DuplicateAttemptTrace
DuplicateNodeTrace | MissingTerminalDecision | NonCanonicalPublication
UpstreamMutationClaim | PatternMutationClaim | ClusterMutationClaim
FootprintMutationClaim | OwnershipMutationClaim | TileMutationClaim
SceneMutationClaim
```

## 6. Retry Policy Contract

`SectorPlannerRetryPolicy` must publish deterministic limits and retry order. The default recovery order is:

```text
1. PatternCandidate
2. PatternTransform
3. ClusterVariant
4. ClusterFootprint
5. SectorAttempt cap check
6. Abort
```

Interpretation:

- Pattern candidate retry changes only the selected MicroPattern candidate for a failing zone/role.
- Pattern transform retry changes only transform choice for the same source pattern where compatible transforms exist.
- Cluster variant retry changes only the chosen TerrainCluster variant for the same sector/pacing/anchor envelope.
- Cluster footprint retry changes only an allowed footprint origin/shape candidate within the same sector and fixed anchors.
- Sector attempt means a bounded local attempt ordinal. It is not a full world reroll.

The policy must define positive integer caps for at least:

```text
max pattern candidate attempts per zone
max pattern transform attempts per selected pattern
max cluster variant attempts per sector
max cluster footprint attempts per sector
max retry nodes per sector
max total local attempts per sector
```

Caps may use conservative defaults selected by implementation, but they must be explicit public values, validated as positive, and included in the canonical digest. Tests must include a small-cap fixture proving each cap aborts deterministically.

No retry stage may:

```text
carve a fallback corridor
relax validation
remove ProtectedOpen or no-write masks
move fixed anchors or SpecialRegion reservations
mutate boundary sockets or route access
change Activity/Event runtime state
rewrite Authoring/Generated CSV
rerandomize the whole sector/world
```

Those actions must map to forbidden fallback errors and abort without publishing a successful retry plan.

## 7. RNG Policy Contract

MAP14_08 must record pass-specific RNG trace without changing global RNG registration.

Allowed stream usage:

- Use existing approved stream IDs exposed by current public runtime, especially `RNG_SECTOR_RECIPE` for sector/pattern/cluster/footprint retry decisions.
- Preserve MAP12 public RNG evidence for Activity/Event decisions: `RNG_SECTOR_RECIPE` and `RNG_POPULATION` may appear as consumed upstream evidence but must not be reclassified as MAP14 retry draws.
- Use MAP14-owned scope labels such as `MAP14_PATTERN_CANDIDATE`, `MAP14_PATTERN_TRANSFORM`, `MAP14_CLUSTER_VARIANT`, `MAP14_CLUSTER_FOOTPRINT`, `MAP14_RETRY_DECISION` only as local scope strings when supported by the existing factory.

Every RNG trace must publish:

```text
stream ID
scope
world seed
sector coordinate
attempt ordinal
node ordinal
draw ordinal before
draw ordinal after
draw count
ticket or selected ordinal
candidate count or weight total
chosen candidate ID
initial state digest
final state digest
```

Determinism requirements:

- same seed, sector, attempt and inputs produce identical retry plan digest and trace.
- seed change changes the digest when at least one draw occurs.
- attempt ordinal change changes the digest when at least one draw occurs.
- reverse input order and `tr-TR` culture do not change the digest.
- invalid input must publish zero new MAP14 RNG draws.
- unrelated RNG streams must keep their first-draw evidence unchanged.

If no retry is needed, the plan must still publish an `AcceptFirstPass` terminal decision with MAP14 retry draw count `0`.

## 8. Failure Mapping Contract

`SectorPlannerAttemptTraceBuilder.Build` maps upstream typed failures into retry decisions.

Minimum mapping:

| Failure source | Retry stage |
|---|---|
| missing/invalid input, SHA mismatch, sector mismatch | AbortUnownedFailure |
| anchor/boundary/Special reservation conflict | AbortUnownedFailure |
| cluster candidate ranking failure with compatible alternatives | RetryClusterVariant or RetryClusterFootprint |
| cluster placement footprint overlap with allowed alternatives | RetryClusterFootprint |
| spine/envelope cannot connect fixed anchors | RetryClusterVariant, then RetryClusterFootprint, then AbortCapReached |
| missing pattern candidate | RetryPatternCandidate |
| transform/protected-mask rejection | RetryPatternTransform, then RetryPatternCandidate |
| MAP10 application/render rejection | RetryPatternCandidate, then RetryPatternTransform |
| Quiet/Activity/Event marker frequency failure | AbortUnownedFailure unless MAP12 public authority exposes safe local selection alternatives |
| ownership same-plane conflict caused by pattern/Quiet overlap | RetryPatternCandidate, then RetryClusterVariant |
| forbidden fallback request | AbortForbiddenFallback |

The trace must preserve original error owner/code/subject/detail and the chosen next retry stage. If a failure owner is unknown, abort atomically as `UnknownFailureOwner`.

## 9. Retry Executor Contract

`SectorPlannerRetryExecutor.Execute` consumes:

```text
valid first-pass success package from MAP14_01~07
optional synthetic failed attempt packages for focused tests
SectorPlannerRetryPolicy
DeterministicRngStreamFactory or existing public RNG authority
```

Success cases:

- first-pass success publishes `AcceptFirstPass`, zero MAP14 retry draw, zero retry nodes and handoff-ready digest.
- recoverable synthetic failures publish bounded retry nodes in the declared order and a final success or deterministic abort.
- every retry node publishes stage, attempt ordinal, selected candidate identity, consumed RNG trace, source failure and resulting decision.
- all attempts are stable-sorted and digest material is culture-invariant.

Failure cases:

- cap exceeded publishes `AbortCapReached` with exact cap and attempted count.
- forbidden fallback publishes `AbortForbiddenFallback` and zero success plan.
- non-deterministic or mismatched RNG trace publishes `NonDeterministicRngTrace` or stream/scope/draw mismatch.
- mutation claims fail atomically and keep draw count 0 unless the mutation is detected after an intentional public draw; in that case the trace must report the exact draw count before abort.

The executor must not call production world assembly, Tilemap bake, Scene APIs, Prefab APIs, physics, gameplay spawn or debug export.

## 10. Identity and No-Mutation Proof

Build/execute must prove before/after equality for:

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
MAP12 Activity/Event authority digests consumed where public
RouteType and AccessClass identities
external socket IDs
boundary pair/candidate IDs
SpecialRegion binding and region IDs
cluster IDs, variant IDs and footprint cells for accepted attempts
ProtectedOpen coordinates and envelope digest
MAP10 pattern render cell identities for accepted attempts
Quiet fill cell identities
Activity/Event marker decision identities
```

The following counters must remain 0:

```text
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
Activity runtime spawn
Event runtime spawn
reward/combat/crafting/inventory/NPC execution
debug export or generated report asset write
```

MAP14 retry RNG draws are allowed only in retry-policy paths and must be counted. MAP12 upstream Activity/Event draws must be reported separately as upstream evidence.

## 11. Focused Fixture Matrix

Reuse the MAP14_01~07 fixture chain through public APIs where practical. Do not copy private implementation or re-run prior categories.

Minimum fixture coverage:

| Fixture | Expected MAP14_08 responsibility |
|---|---|
| `FirstPassOwnershipSuccess` | terminal `AcceptFirstPass`, zero retry nodes, zero MAP14 retry draw |
| `MissingPatternCandidate` | PatternCandidate retry before transform/cluster/footprint |
| `PatternTransformProtectedRejection` | PatternTransform then PatternCandidate ordering |
| `ClusterVariantConflict` | ClusterVariant retry with bounded RNG trace |
| `ClusterFootprintOverlap` | ClusterFootprint retry with bounded RNG trace |
| `SpineEnvelopeCannotConnect` | cluster retry sequence then deterministic cap abort |
| `OwnershipConflictFromPatternQuietOverlap` | PatternCandidate before ClusterVariant retry |
| `ForbiddenFallbackRequests` | corridor carve/validation relaxation/socket mutation/world reroll abort |
| `RngDeterminismCases` | same seed repeat, reverse input, culture, seed/attempt mutation, unrelated stream isolation |
| `InvalidInputCases` | missing authority, negative attempt, duplicate trace, bad cap fail atomically |

Fixtures are retry/RNG policy examples, not production world seeds or MAP14 exit approval.

## 12. Required Tests

`SectorPlannerRetryRngPolicyTests` must include 9~12 focused tests in category `MAP14_08`.

Minimum assertions:

1. `FirstPassSuccessPublishesAcceptDecisionWithZeroRetryDraws`
   - valid MAP14_01~07 chain publishes immutable retry plan, lower-hex digest, terminal accept, zero MAP14 retry draw.
2. `RetryPolicyDeclaresOrderedPatternTransformClusterFootprintStages`
   - policy order, positive caps and canonical digest are stable.
3. `RecoverablePatternFailuresRetryPatternBeforeClusterOrFootprint`
   - missing pattern/application/render failures choose pattern candidate/transform stages before cluster stages.
4. `RecoverableClusterAndSpineFailuresRetryClusterVariantThenFootprint`
   - cluster/spine failures choose cluster variant/footprint stages in declared order.
5. `CapsAbortDeterministicallyWithoutValidationRelaxation`
   - pattern/cluster/node/attempt caps publish `AbortCapReached` and no success plan.
6. `ForbiddenFallbackCarveRerollSocketAndMaskRelaxationAbort`
   - corridor carve, whole sector/world reroll, socket mutation and protected-mask relaxation are rejected.
7. `RngTraceUsesApprovedStreamsScopesAndDrawAccounting`
   - stream/scope/world seed/sector/attempt/node/draw before-after/chosen candidate evidence is exact.
8. `RetryPlanIsDeterministicAcrossRepeatReverseAndTurkishCulture`
   - repeat/reverse/`tr-TR` produce identical retry plan and trace digests.
9. `SeedAndAttemptMutationChangeDrawnRetryDigestAndKeepUnrelatedStreamsIsolated`
   - seed/attempt mutation changes digest when draws occur and unrelated stream first draw remains unchanged.
10. `InvalidInputDuplicateTraceNegativeAttemptAndMutationClaimsFailAtomically`
    - invalid requests publish null plan, empty digest, stable-sorted errors and zero or reported draw count as contract requires.
11. `NoTilePhysicsSceneDebugExportOrGameplayMutation`
    - Tilemap, Scene/Prefab/GameObject, physics, debug export, spawn and gameplay mutation counters are 0.

Add more focused tests only if needed to cover the semantic surface. Do not add broad regression selections.

## 13. Expected Result Report

Result must begin:

```text
TASK: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
STATUS: PASS | FAIL | BLOCKED
MAP14_08: COMPLETE ELIGIBLE only when PASS
MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS: LOCKED / DO NOT START
```

`## User-Facing Implementation Report` must include:

- 이번 Task가 local retry/RNG policy and trace이며 debug/graybox/Tilemap/gameplay가 아니라는 점
- 추가한 script와 각 script의 책임
- 실제 first-pass accept count, synthetic retry case count, retry node count, terminal decision count
- stage별 retry count와 cap abort count
- RNG stream/scope/draw before-after/selected ordinal evidence
- MAP12 upstream RNG draw와 MAP14 retry RNG draw를 분리한 증거
- forbidden fallback attempt 0 or rejected count, validation relaxation 0, arbitrary corridor carve 0
- whole sector/world rerandom 0, socket/boundary/Special/ProtectedOpen mutation 0
- accepted attempts의 MAP14_01~07 identity가 변하지 않았다는 증거
- Tilemap/Scene/Prefab/GameObject/spawn/debug export 0
- 회귀를 돌리지 않았다는 증거
- 미구현 범위와 Editor/게임 가시성

`## Responsibility and Added Functions` must include:

- exact script paths
- class/method별 책임
- 각 method의 input -> output
- production/Editor/CSV/Scene/Prefab/Tilemap 변경 여부
- upstream 수정 여부
- downstream owner: MAP14_09

Focused verification block:

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP14_08]
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
Commit subject: MAP14_08: implement local retry and RNG policy
Push: NOT PERFORMED
```

If FAIL or BLOCKED, do not finalize Status and do not open MAP14_09.

## 14. Finalize Rules

PASS일 때만 Status Finalize와 atomic commit을 수행한다.

Commit에 포함 가능한 파일:

```text
MCP/TASKS/MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY.md
MCP_ARCHIVE/MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY.md
MCP/REPORTS/MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY_RESULT.md
MCP/06_IMPLEMENTATION_STATUS.md
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicy.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicy.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerAttemptTrace.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerAttemptTrace.cs.meta
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryExecutor.cs
Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryExecutor.cs.meta
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicyTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicyTests.cs.meta
```

관련 없는 dirty/untracked/staged 파일은 수정·stage·commit하지 않는다.

```text
Git push: forbidden
Next task MAP14_09: do not start
STOP after Result and optional PASS finalize commit
```
