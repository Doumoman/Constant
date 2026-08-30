TASK: MAP14_08_IMPLEMENT_LOCAL_RETRY_AND_RNG_POLICY
STATUS: PASS
MAP14_08: COMPLETE ELIGIBLE only when PASS
MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP14_08은 MAP14_01~07의 immutable sector-local 결과 위에 **local retry 순서, deterministic cap, pass별 RNG trace, typed abort**를 추가했다. 이번 구현은 실패한 pattern/transform/cluster/footprint 후보를 제한된 범위에서 다시 고르는 정책과 그 선택 증거만 소유한다. 169-sector production assembly, debug/graybox export, Tilemap bake, collider/physics/player traversal, Scene/Prefab/GameObject 반영, Activity/Event runtime spawn 또는 gameplay 실행은 하지 않았다.

추가한 Runtime script는 다음 세 개다.

- `SectorPlannerRetryRngPolicy.cs`: retry stage/decision/failure owner/pass scope/error vocabulary, default order와 public cap, policy digest를 소유한다.
- `SectorPlannerAttemptTrace.cs`: failure/input/RNG/attempt/node/request/result/plan immutable model, MAP14_01~07와 MAP12 identity handoff, canonical digest를 소유한다.
- `SectorPlannerRetryExecutor.cs`: typed failure mapping, cap gate, forbidden mutation gate, `RNG_SECTOR_RECIPE` draw, errors-only atomic abort와 terminal decision을 소유한다.

추가한 focused test script는 `SectorPlannerRetryRngPolicyTests.cs` 하나이며 `MAP14_08` category의 11개 test를 가진다. 이 test는 9-sector MAP14_01~07 public builder chain을 실제로 조립한 뒤 MAP14_08 request에 전달한다. private upstream plan field를 읽거나 production source를 수정하지 않았고, Runtime 구현은 오직 public plan/factory surface를 소비한다.

### 실제 reference fixture 수치

```text
MAP14_01~07 reference sectors: 9
MAP14_07 canvas coverage consumed: 13,824 / 13,824
first-pass accept cases: 1
first-pass retry nodes: 0
first-pass MAP14 RNG draws: 0
first-pass retry plan digest:
4e1bad6608402eb340ab632d0f42fd41171e251fcb22f8899b2d1625e4c3ad11

synthetic recoverable cases in the aggregate fixture: 6
retry nodes: 6
terminal decisions: 1 (`AcceptRecovered`)
PatternCandidate retries: 2
PatternTransform retries: 1
ClusterVariant retries: 2
ClusterFootprint retries: 1
MAP14 retry RNG draws: 6
MAP12 upstream Activity draws: 10
MAP12 upstream Event draws: 10
```

첫-pass success는 `AcceptFirstPass`를 게시하고 MAP14 draw를 전혀 만들지 않는다. aggregate retry fixture는 missing pattern, protected transform rejection, cluster ranking, footprint overlap, pattern/Quiet ownership overlap 두 단계를 입력으로 받아 stable-sorted 후보에서 6개 node를 선택하고 마지막에 `AcceptRecovered`를 게시했다. 각 node는 실제 upstream plan을 변형하지 않고 선택된 candidate identity와 trace를 immutable plan에 게시한다.

### retry order와 cap 증거

default recovery order와 cap은 다음과 같다.

```text
PatternCandidate -> PatternTransform -> ClusterVariant -> ClusterFootprint
-> SectorAttempt cap check -> Abort

max pattern candidate attempts / zone: 3
max pattern transform attempts / selected pattern: 2
max cluster variant attempts / sector: 3
max cluster footprint attempts / sector: 3
max retry nodes / sector: 12
max total local attempts / sector: 8
```

small-cap focused matrix는 pattern candidate, pattern transform, cluster variant, cluster footprint, retry node, total local attempt의 6개 cap을 각각 `1`로 낮춰 검증했다. 결과는 `6/6` deterministic `AbortCapReached`, successful plan `0`, 각 case에서 허용된 첫 node의 draw `1` 후 두 번째 attempt 전에 중단, validation relaxation `0`이다. 같은 input의 reverse order도 같은 stable-sorted error를 게시했다.

### 실제 RNG trace

focused RNG evidence 한 건의 실제 값은 다음과 같다.

```text
stream: RNG_SECTOR_RECIPE
local scope: MAP14_PATTERN_CANDIDATE
world seed: 336068609 (0x14080001)
sector: (1,1)
attempt ordinal: 6
node ordinal: 7
draw before -> after: 0 -> 1
draw count: 1
ticket / candidate count: 0 / 3
stable-sorted candidates: MP_A, MP_B, MP_C
chosen candidate: MP_A
initial state digest:
daefa87330a11ab0f38d4e211e5ecdf7ed60e9411be14882eb8f0584aa68c43e
final state digest:
844dd9dffc1491f0b048d266cc326615e356d6d0dbd5a919bf6f1fa1fb0730d8
```

MAP14 local scope는 global registry에 새 stream으로 등록하지 않고 trace label로만 게시한다. 실제 draw는 기존 승인 stream `RNG_SECTOR_RECIPE`와 public `DeterministicRngStreamFactory`/sector reset scope를 사용했다. seed 변경과 attempt 변경은 drawn retry digest를 바꿨고, 같은 seed/input 반복, reverse input, `tr-TR` culture는 digest를 바꾸지 않았다. 실행 전후 `RNG_POPULATION` unrelated first draw도 동일했다. MAP12 Activity/Event draw `10/10`은 upstream evidence로 분리했으며 MAP14 retry draw에 합산하지 않았다.

### forbidden fallback와 no-mutation 증거

```text
forbidden synthetic requests: 8
rejected before RNG draw: 8
successful forbidden plans: 0
forbidden-path MAP14 draws: 0
arbitrary fallback corridor carve: 0
validation relaxation: 0
whole sector rerandom: 0
whole world rerandom: 0
fixed anchor mutation: 0
boundary/socket mutation: 0
SpecialRegion reservation mutation: 0
ProtectedOpen/no-write mask relaxation: 0
Tilemap writes: 0
Scene mutations: 0
Prefab mutations: 0
GameObject mutations: 0
Activity runtime spawns: 0
Event runtime spawns: 0
gameplay execution: 0
debug export writes: 0
```

금지 matrix는 corridor carve, validation relaxation, sector reroll, world reroll, socket mutation, boundary mutation, Special reservation mutation, ProtectedOpen relaxation을 각각 typed error로 거부했다. missing authority, negative attempt, duplicate attempt/node trace, invalid cap, RNG stream/scope/draw mismatch, upstream mutation claim을 포함한 invalid matrix `10`건도 partial retry plan과 digest 없이 atomic failure를 게시했고 신규 draw는 `0`이었다.

accepted plan에서 다음 before/after identity는 모두 exact equality다.

- SectorPlannerInput, PacingAssignment, FixedAnchorPlan
- ClusterPlacementPlan, SpineEnvelopePlan
- SectorClusterRolePatternPlan, SectorPatternRenderPlan
- SectorQuietActivityEventPlan, SectorCanvasOwnershipPlan
- MAP12 Activity/Event authority
- RouteType/AccessClass, external sockets, boundary identity
- SpecialRegion binding/region, cluster/variant/footprint identity
- ProtectedOpen/envelope, MAP10 render/Quiet/marker authority identity

따라서 MAP14_08이 승인하는 범위는 9-sector reference chain 위의 deterministic local retry policy, candidate identity 선택 trace, cap/abort 판정, no-fallback/no-mutation proof와 MAP14_09 handoff-ready immutable plan이다. production seed, 169-sector production solve, 실제 downstream planner 재호출/plan 교체, debug export/graybox, MAP14 exit, Tilemap/physics/gameplay 결과는 아직 승인하지 않는다.

### 아직 구현하지 않은 범위

- 169-sector production world assembly와 production seed approval
- 실제 production planner pass 재호출, world rollback 또는 whole-sector/world reroll
- fallback corridor carve, gap repair, validation relaxation, socket/boundary/Special/ProtectedOpen rewrite
- MAP14_09 debug export, graybox fixture/scene, preview window/overlay/generated report asset
- MAP14_10 MAP14 phase exit validation
- Tilemap bake, MicroChunk slice/export, streaming, collider/physics/player traversal
- Activity/Event/NPC/reward/combat/crafting/inventory runtime spawn/실행과 persistence save/load

### Editor / 게임 가시성

- Editor: Test Runner의 `MAP14_08` focused result와 Runtime immutable plan/API에서만 확인 가능하다. 새 EditorWindow, inspector, overlay, generated asset은 없다.
- 활성 Editor scene은 `Assets/_Game/Scenes/MapGenerationProgressTest.unity`로 유지됐고 scene file 변경은 없다.
- 게임/Game View: 시각 변화가 없다. Tilemap, Material, Texture, collider, GameObject, Activity/Event spawn을 만들지 않았다.
- Scene/Prefab/ScriptableObject/Tilemap/Material/Texture/Settings/Packages/asmdef/asmref 변경은 `NONE`이다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicy.cs`

| Class / method | 책임 | Input -> Output |
|---|---|---|
| `SectorPlannerRetryStage` | None/pattern/transform/cluster/footprint/sector/abort vocabulary | recovery phase -> stable stage token |
| `SectorPlannerRetryDecisionKind` | first-pass/recovered/retry/typed abort vocabulary | retry outcome -> stable decision token |
| `SectorPlannerRetryFailureOwner` | MAP14_01~07/RNG/forbidden failure owner vocabulary | source responsibility -> owner token |
| `SectorPlannerRngPassScope` | sector/pattern/cluster/activity/event/retry trace scope vocabulary | pass responsibility -> local scope token |
| `SectorPlannerRetryErrorCode` | missing/cap/forbidden/RNG/duplicate/mutation atomic error groups | invariant violation -> typed error code |
| `SectorPlannerRetryLimit` constructor | six public positive cap을 immutable 보관 | six integer caps -> limit contract |
| `SectorPlannerRetryLimit.ForStage` | retry stage별 해당 cap 조회 | stage -> maximum attempts or `0` |
| `SectorPlannerRetryPolicy` constructor | limits/order/version defensive copy와 digest 게시 | limit+ordered stages+version -> immutable policy |
| `CreateDefault` | reference order와 `3/2/3/3/12/8` caps 제공 | none -> default policy |
| `HasCanonicalOrder` | exact recovery order 판정 | policy order -> bool |
| `SectorPlannerRetryError` constructor | immutable diagnostic 생성 | code+subject+detail -> error |
| `CompareTo/Equals/GetHashCode/ToString` | error stable sort/dedup/culture-invariant text | error/error -> order/equality/diagnostic |

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerAttemptTrace.cs`

| Class / method | 책임 | Input -> Output |
|---|---|---|
| `SectorPlannerRetryFailure` constructor/`CompareTo` | original owner/code/subject/detail/sequence/forbidden code 보존 | typed failure facts -> immutable sortable failure |
| `SectorPlannerAttemptTraceInput` constructor/`CompareTo` | stable-sorted compatible candidates와 attempt/node/recovery outcome 보관 | synthetic/local attempt package -> canonical input |
| `SectorPlannerRngTrace` constructor/`CompareTo` | stream/scope/seed/sector/attempt/node/draw/ticket/chosen/state digest 게시 | one deterministic draw -> immutable trace |
| `SectorPlannerAttemptTrace` constructor/`CompareTo` | original failure와 다음 stage/decision/reason mapping 보존 | failure mapping -> attempt trace |
| `SectorPlannerRetryNodeTrace` constructor/`CompareTo` | attempt, RNG, selected candidate, recovery result 결합 | attempt trace+RNG trace -> retry node |
| `SectorPlannerRetryBuildRequest` constructor | ownership/policy/factory/seed/attempt inputs와 모든 mutation gate defensive-copy | MAP14_07 plan+retry authority+claims -> immutable request |
| `SectorPlannerRetryPlan` constructor | attempts/nodes/RNG/stage counts, terminal, all upstream identity pairs 게시 | successful execution evidence -> immutable handoff plan |
| `SectorPlannerRetryPlan.Count` | stage별 actual retry node 수 조회 | stage -> count |
| `AllUpstreamIdentitiesPreserved` | MAP14_01~07/MAP12 before-after exact equality 집계 | identity pairs -> bool |
| `Map14_09HandoffReady` | ownership handoff+identity+accept terminal gate | retry plan -> bool |
| zero-counter properties | corridor/relax/reroll/anchor/socket/Special/mask/tile/Unity/spawn/gameplay/debug mutation 증명 | accepted plan -> zero counters |
| `SectorPlannerRetryBuildResult` constructor | success plan 또는 errors-only와 diagnostic traces 게시 | plan/traces/errors -> atomic result |
| result `AbortCount/CapAbortCount/ForbiddenAbortCount` | terminal abort actual accounting | terminal decision -> count |
| `SectorPlannerRetryCanonicalDigest.ComputePolicy` | ruleset/order/caps canonical material hash | policy -> lowercase SHA-256 |
| `ComputeRngTrace` | complete per-draw evidence hash | RNG trace -> lowercase SHA-256 |
| `ComputePlan` | upstream/policy/attempt/node/terminal canonical hash | request+traces+terminal -> lowercase SHA-256 |
| `Hash/Append` | UTF-8, length-prefixed, culture-invariant digest helper | canonical values -> lowercase SHA-256 material |

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerRetryExecutor.cs`

| Class / method | 책임 | Input -> Output |
|---|---|---|
| `SectorPlannerAttemptTraceBuilder.Build` | typed failure를 declared retry/abort stage로 매핑하고 original detail 보존 | failure+attempt+node -> attempt trace |
| `StageFor/DecisionFor/Contains` | pattern/transform/cluster/footprint/spine/ownership/forbidden mapping 규칙 | owner+code+sequence -> stage+decision |
| `SectorPlannerRetryExecutor.Execute` | validate -> ordered local retry -> cap/draw/terminal -> plan 또는 errors-only | retry request -> `SectorPlannerRetryBuildResult` |
| `ValidateRequest` | ownership handoff, sector, policy/order/caps/digest, factory, ordinals, duplicate traces, publication 검증 | request -> accumulated errors |
| `ValidateSourceRngTraces` | approved stream/local scope/draw accounting/canonical digest와 duplicate 검증 | published RNG traces -> errors or pass |
| `ValidateMutationClaims` | all forbidden fallback/mutation counters를 draw 전에 거부 | request counters/claims -> typed errors |
| `Draw` | `RNG_SECTOR_RECIPE` sector stream 생성, stable candidate ticket 선택, before/after/state digest 기록 | seed+sector+attempt+node+candidates -> RNG trace |
| `CreatePlan/Success/Failure` | canonical digest와 atomic publication 조립 | evidence+terminal+errors -> success/error result |
| `AddAbortError/TerminalFor/IsForbidden` | original failure/error group을 exact terminal abort로 환원 | failure/errors -> typed abort |
| `PassScope/ScopeLabel` | stage를 MAP14 local trace label로 투영 | retry stage/scope -> pass scope/string |
| `Count/Key/CountError/IsLowerSha/Add` | cap accounting, duplicate key, counter/error/digest validation helper | values -> deterministic gate evidence |

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerRetryRngPolicyTests.cs`

| Test / helper method | 책임 | Input -> Output |
|---|---|---|
| `BuildReferenceCanvas` | MAP14_01~07 public chain normal/reverse fixture를 1회 조립 | public authority fixtures -> two ownership plans |
| `FirstPassSuccessPublishesAcceptDecisionWithZeroRetryDraws` | first-pass terminal/digest/handoff/zero draw 검증 | valid canvas, no failures -> accept plan |
| `RetryPolicyDeclaresOrderedPatternTransformClusterFootprintStages` | exact order, six positive caps, stable policy digest 검증 | default policy -> declared contract |
| `RecoverablePatternFailuresRetryPatternBeforeClusterOrFootprint` | missing/application/render mapping 순서 검증 | three pattern failures -> candidate/transform/candidate nodes |
| `RecoverableClusterAndSpineFailuresRetryClusterVariantThenFootprint` | cluster/footprint/spine ordered recovery 검증 | four failures -> variant/footprint/variant/footprint |
| `CapsAbortDeterministicallyWithoutValidationRelaxation` | six small-cap cases와 reverse-order abort 검증 | cap=1 matrices -> 6 cap aborts/no plan |
| `ForbiddenFallbackCarveRerollSocketAndMaskRelaxationAbort` | eight forbidden actions의 pre-draw rejection 검증 | forbidden requests -> typed abort/zero draw |
| `RngTraceUsesApprovedStreamsScopesAndDrawAccounting` | exact stream/scope/seed/ordinal/draw/ticket/chosen/state digest 검증 | three candidates -> one exact trace |
| `RetryPlanIsDeterministicAcrossRepeatReverseAndTurkishCulture` | repeat/reverse/`tr-TR` plan+trace determinism 검증 | equivalent request order/culture -> identical digests |
| `SeedAndAttemptMutationChangeDrawnRetryDigestAndKeepUnrelatedStreamsIsolated` | seed/attempt sensitivity와 population stream isolation 검증 | mutated seed/attempt -> changed retry digest, unchanged unrelated draw |
| `InvalidInputDuplicateTraceNegativeAttemptAndMutationClaimsFailAtomically` | missing/negative/duplicate/bad-cap/RNG/mutation invalid matrix 검증 | 10 invalid requests -> null plan/empty digest/sorted errors/zero draw |
| `NoTilePhysicsSceneDebugExportOrGameplayMutation` | aggregate actual retry/stage/RNG/MAP12/no-mutation 수치 검증 | six retry inputs -> accepted immutable plan and zero side effects |
| `MetricsInputs/Attempt/Forbidden/CapCase/Policy/Request` | stable synthetic failure, forbidden, cap, request fixture 조립 | compact fixture facts -> public retry inputs/requests |
| `BuildCanvas` | MAP14_01~07 builders/renderer/resolver public chain 실행 | normal/reverse flag -> resolved ownership plan |
| `RetryRngFactory/Definition/Hex/SetAutoProperty` | test-only approved RNG definition fixture 조립 | known stream IDs/salts/scopes -> deterministic factory |
| `Require/Errors/AssertLowerSha/Hash` | fixture success, diagnostics, canonical digest assertions | result/material -> test oracle |
| nested `Fixture.Create/Fill/Place/CreateAuthorities` | MAP14_01~06 public input/pacing/anchor/cluster/spine/pattern/quiet/activity/event chain 재사용 | stable 9-sector facts -> upstream public plans |
| nested sector/anchor/catalog/pattern/ownership/RNG helpers | reference sectors, anchors, candidates, patterns, MAP12 authorities 작성 | stable facts -> public builder inputs |
| nested `AuthorityPackage.Request` | MAP12 Activity/Event evidence를 placement request로 결합 | compiled authorities+quiet plan -> MAP14_06 request |

Production Runtime C# 신규 `3`, Runtime EditMode test C# 신규 `1`, matching `.meta` 신규 `4`다. 기존 production C#, 기존 test, CSV, 기존 meta, Editor production/test, asmdef/asmref, Scene/Prefab/Tilemap/asset 수정은 `0`이다. upstream source 수정도 `0`이다. Downstream owner는 `MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS`이며 이 Task에서 시작하지 않았다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
assembly_names: [Game.Map.Tests.EditMode]
category_names: [MAP14_08]
final job: a73a57be489e46b1a23b628d18d05e0c
discovered: 11
executed: 11
passed: 11
failed: 0
skipped: 0
inconclusive: 0
duration: 5.5994552 seconds
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

동일 focused selection의 작업 이력은 다음과 같다.

```text
ee178799d18c47cface88b31ee748ba6: asset discovery 전이라 0 discovered / 0 executed
c016c7ed7aa54082981ad0690b3dfc32: 11 executed, 10 passed, 1 failed
- 신규 invalid fixture helper가 null policy/RNG를 default로 치환하던 test-only 문제 수정
a73a57be489e46b1a23b628d18d05e0c: 11 executed, 11 passed, 0 failed
```

모든 invocation은 `EditMode + Game.Map.Tests.EditMode + MAP14_08`만 사용했다. 이전 Task category, legacy 19347, PlayMode, unfiltered test는 한 번도 선택하지 않았다.

## Static and Change-Control Gates

```text
test methods: 11
MAP14_08 Category attributes: 1
other Category attributes: 0
matching new metas: 4 / 4
unique new meta GUIDs: 4 / 4
existing production C# modified: 0
existing test C# modified: 0
CSV/schema modified: 0
Scene/Prefab/Tilemap/asset modified: 0
global RNG registry / MAP02 source modified: 0
upstream MAP14_01~07 source modified: 0
unrelated staged files: 0
installed/archive Task SHA-256 equality: PASS
installed/archive Task SHA-256:
7d78db7ba7041b89175bc0afe9bc2e6f8c7d1c688a61458e882ebec869e8029c
git diff --check: PASS
```

Commit subject: MAP14_08: implement local retry and RNG policy
Push: NOT PERFORMED

## PASS Decision

DONE CONDITIONS를 충족했다.

- MAP14_01~07 public ownership chain에 deterministic retry order, six caps, typed failure mapping과 immutable trace를 추가했다.
- first-pass는 0 draw로 accept했고 six synthetic retry node는 approved `RNG_SECTOR_RECIPE`를 사용해 stable candidate identity를 선택했다.
- six cap cases와 eight forbidden fallback cases가 deterministic errors-only abort를 게시했다.
- MAP12 Activity/Event RNG와 MAP14 retry RNG가 분리됐고 repeat/reverse/culture/seed/attempt/unrelated-stream invariant를 검증했다.
- accepted attempts의 upstream identity가 전부 유지됐고 tile/Unity object/spawn/gameplay/debug mutation은 0이다.
- focused MAP14_08 EditMode test 11/11 PASS, compile error 0, final relevant Console error/warning 0/0이다.
- 회귀 trigger가 없고 prior/legacy/PlayMode/unfiltered selection은 모두 0이다.

따라서 `STATUS: PASS`이며 MAP14_08은 Status Finalize 및 atomic commit 자격이 있다. MAP14_09는 `LOCKED`로 유지하고 시작하지 않는다.
