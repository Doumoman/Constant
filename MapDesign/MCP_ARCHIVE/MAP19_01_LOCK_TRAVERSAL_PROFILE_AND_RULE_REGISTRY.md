```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY
  task_file: TASKS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY.md
  requires_current_task: NONE
  requires_completed_task: MAP18_07_MAP18_POPULATION_EXIT_TESTS
  requires_result:
    path: REPORTS/MAP18_07_MAP18_POPULATION_EXIT_TESTS_RESULT.md
    status: PASS
    sha256: 5d5c45a6a9714cdd42c19b3e13a03956d7226b396be531da6c9783e0ffe50881
  requires_installed_task:
    path: TASKS/MAP18_07_MAP18_POPULATION_EXIT_TESTS.md
    sha256: 0aba8a72c532f8449da76d2d2112b86c7dafc0b57ad56c551b4ac1bb9d913ad9
  sets_current_task: MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY
```

# MAP19_01 - Lock Traversal Profile and Rule Registry

```text
TASK: MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY
PHASE: MAP19 - Traversal Validation / Seed QA Preparation
STATUS: CURRENT
NEXT: MAP19_02_BUILD_TILE_MOVEMENT_GRAPH
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. 작업 목적

MAP19의 첫 작업으로, 실제 그래프 탐색이나 대량 seed QA를 시작하기 전에 **Traversal Profile**과 **Validation Rule Registry**를 versioned immutable contract로 잠근다.

이번 Task는 플레이어 물리 코드를 수정하지 않는다.  
또한 tile movement graph, BFS, completion search, seed batch runner를 만들지 않는다.

이번 Task의 책임:

```text
1. player collider, jump, air control, climb, bounce, fall, chase-width 관련 검증 입력값을 하나의 versioned traversal profile로 모은다.
2. MAP09/MAP11/MAP16에서 승인한 MovementKind와 envelope/source 용어를 MAP19 validation rule registry에 연결한다.
3. validation rule의 id, severity, threshold, owner, target phase, failure policy를 typed catalog로 정의한다.
4. rule/profile digest를 만들어 MAP19_02 Tile Movement Graph가 같은 값을 참조하게 한다.
5. 중복 numeric tuning이나 산발적인 hardcoding이 생기지 않도록 profile value source와 test fixture 사용 범위를 보고한다.
```

금지:

```text
PlayerController / Rigidbody / Collider / Physics2D behavior change
actual player movement tuning
tile movement graph construction
stand/climb/bounce node generation
walk/jump/drop/climb/slide/bounce edge generation
BFS or completion search
cluster recovery/density validation execution
worst-case scenario validation
distance/revisit/pacing measurement
failure bundle, screenshot, CSV, headless runner creation
seed batch run, production seed approval
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap / Collider / Rigidbody / Physics2D creation or write
Scene / Prefab / Tilemap mutation
Addressables / Resources / AssetDatabase load
camera, streaming, save integration
optimization rewrite or broad refactor
shared fixture consolidation
MAP19_02 unlock or execution
```

## 1. 사용자 보고 의무

Result의 첫 두 섹션은 반드시 아래 이름으로 작성한다.

```text
## User-Facing Implementation Report
## Responsibility and Added Scripts
```

`## User-Facing Implementation Report`에는 한국어로 다음을 설명한다.

```text
이번 Task가 추가한 기능
Traversal Profile이 실제 플레이어 물리 변경과 어떻게 다른지
어떤 movement capability와 collider/jump/fall/climb/bounce/chase-width 값을 versioning했는지
Validation Rule Registry가 어떤 rule/severity/threshold를 담는지
MAP18 approved audit/handoff digest를 어떻게 보존했는지
MAP19_02에 넘기는 profile/rule registry surface
중복 코드나 하드코딩 후보를 발견했는지
회귀 테스트를 돌리지 않았는지, 돌렸다면 실제 트리거가 무엇이었는지
```

`## Responsibility and Added Scripts`에는 표로 다음을 작성한다.

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| 실제 파일 경로 | 이 파일이 맡은 책임 | 이 파일이 절대 하지 않는 일 |

Result에서 "테스트 PASS"만 쓰고 스크립트 책임 설명을 생략하면 이 Task는 `FAIL`이다.

또한 Result에는 아래 섹션을 포함한다.

```text
## Traversal Profile and Rule Registry Summary
## No Graph BFS Regression or Physics Boundary Notes
```

## 2. 선행조건

작업 전에 다음을 확인한다.

```text
MAP18_07 Result exists
MAP18_07 Result STATUS: PASS
MAP18_07 Result SHA-256:
5d5c45a6a9714cdd42c19b3e13a03956d7226b396be531da6c9783e0ffe50881

MAP18_07 installed task SHA-256:
0aba8a72c532f8449da76d2d2112b86c7dafc0b57ad56c551b4ac1bb9d913ad9

MAP18 approved audit digest:
d17fc7aa674e42bbe17b576032d4f03298ecdf53e890dd1ae2352c68078ae6a7

MAP19_01 handoff digest from MAP18:
4fdec72ed7065e10f2a368285af7c86a54bd48afebae6d9b335af18e3788a6d2

MAP18 audited upstream digest count:
16

MAP18 identity collision counts:
0

MAP18 runtime side-effect counters:
0

Current Task before apply: NONE
MAP18_07: COMPLETE
MAP19_01: LOCKED before apply
MAP19_02: LOCKED
unrelated staged files: 0
```

선행 Result나 installed Task SHA가 다르면 임의로 맞추지 않는다. `BLOCKED`로 멈추고 실제 SHA를 Result에 기록한다.

## 3. 입력 계약

다음 산출물을 읽어 사용한다. 실제 타입명은 프로젝트의 현재 public API를 따른다.

```text
TraversalMovementKind
TraversalEnvelopeKind
RouteSpine
TraversalEnvelope
GeneratedFinalRouteValidation
GeneratedPopulationExitAuditSurface
BakingCanonicalDigest
```

프로젝트에 위 이름과 정확히 일치하는 타입이 없으면, 동일 semantic owner를 가진 현재 public 타입을 사용한다. 타입명을 맞추기 위해 MAP09/MAP11/MAP16/MAP18 기존 파일을 대규모 변경하지 않는다.

Read-only MCP references:

```text
MapDesign/MCP/REPORTS/MAP18_07_MAP18_POPULATION_EXIT_TESTS_RESULT.md
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/TASKS/MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS.md
MapDesign/MCP/TASKS/MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE.md
MapDesign/MCP/TASKS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY.md
```

MCP report/task reads are allowed for audit evidence. Production code must not use `System.IO` to read these reports.

기준:

```text
required MovementKind count: 6
required MovementKind values: Walk, Jump, Drop, Climb, Slide, Bounce
required EnvelopeKind values: Centerline, Floor, Clearance, JumpArc, DropColumn, Landing, Recovery
required traversal capability groups: Collider, Jump, AirControl, Climb, Bounce, Fall, ChaseWidth
required rule severities: Critical, Error, Warning, Info
minimum validation rules: 12
tile graph nodes generated in this task: 0
tile graph edges generated in this task: 0
BFS runs in this task: 0
seed batch runs in this task: 0
```

Approved movement/envelope source를 찾을 수 없으면 임의 enum을 만들지 않는다. `BLOCKED`로 멈추고 누락 source를 보고한다.

## 4. 핵심 산출물

프로젝트 패턴에 맞게 파일과 타입을 조정할 수 있지만, 다음 semantic responsibility는 분명해야 한다.

| Required concept | Responsibility |
|---|---|
| `GeneratedTraversalProfileVersion` | profile namespace/version/dataVersion/generatorVersion을 정의한다. |
| `GeneratedTraversalCapabilityGroup` | Collider, Jump, AirControl, Climb, Bounce, Fall, ChaseWidth capability group을 정의한다. |
| `GeneratedTraversalCapabilityValue` | numeric/bool/range capability 값을 source label, unit, threshold policy와 함께 담는다. |
| `GeneratedTraversalProfile` | movement graph가 참조할 모든 traversal capability 값을 immutable profile로 묶는다. |
| `GeneratedTraversalRuleSeverity` | Critical, Error, Warning, Info severity를 정의한다. |
| `GeneratedTraversalValidationRule` | rule id, owner, severity, threshold, target movement/envelope, fail policy를 정의한다. |
| `GeneratedTraversalRuleRegistry` | MAP19 validation rule catalog, digest, profile compatibility surface를 묶는다. |
| `GeneratedTraversalProfileRuleFailure` | missing value, duplicate rule id, invalid threshold, unsupported movement kind, digest mismatch를 deterministic하게 보고한다. |
| `GeneratedTraversalProfileRuleLock` | profile과 registry를 검증하고 MAP19_02 handoff surface를 만든다. |

Suggested production files:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedTraversalProfile.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedTraversalRuleRegistry.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedTraversalProfileRuleLock.cs(.meta)
```

Suggested focused test file:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedTraversalProfileRuleRegistryTests.cs(.meta)
```

파일을 더 적게 또는 다르게 구성해도 된다. 다만 Result의 `Responsibility and Added Scripts`에서 실제 구성과 책임을 정확히 보고해야 한다.

## 5. Traversal Profile 규칙

### 5.1 Capability groups

다음 group을 모두 표현한다.

```text
Collider
Jump
AirControl
Climb
Bounce
Fall
ChaseWidth
```

각 capability value는 다음 metadata를 가져야 한다.

```text
key
group
value kind: Integer, Decimal, Boolean, Range, Enum
unit
source label
min/max or exact threshold
consumer phase
```

Profile 값은 한 파일 또는 한 registry owner 안에 중앙화한다. 같은 numeric 값을 production 여러 파일에 흩뿌리면 `FAIL`이다.  
테스트 fixture에서 값을 반복해야 한다면 Result에 test-only duplication으로 보고하고, production duplication과 구분한다.

### 5.2 Movement and envelope compatibility

MovementKind는 MAP09/MAP11의 여섯 값을 그대로 따른다.

```text
Walk
Jump
Drop
Climb
Slide
Bounce
```

EnvelopeKind는 다음 값을 support matrix에 연결한다.

```text
Centerline
Floor
Clearance
JumpArc
DropColumn
Landing
Recovery
```

필수 matrix:

```text
Walk requires Floor, Clearance, Landing, Recovery
Jump requires Clearance, JumpArc, Landing, Recovery
Drop requires Clearance, DropColumn, Landing, Recovery
Climb requires Clearance, Landing, Recovery
Slide requires Floor, Clearance, Landing, Recovery
Bounce requires Clearance, JumpArc, Landing, Recovery
```

이번 Task는 matrix를 정의만 한다. 실제 tile graph edge를 만들거나 물리 가능성을 계산하지 않는다.

### 5.3 Validation rule registry

Registry는 최소 12개 rule을 가진다.

필수 rule owner category:

```text
ProfileCompleteness
MovementKindSupport
EnvelopeSupport
ColliderClearance
JumpArcEnvelope
DropRecovery
ClimbReach
BounceArc
FallLimit
ChaseWidth
RouteCriticality
DigestCompatibility
```

각 rule은 다음 값을 가진다.

```text
rule id
owner category
severity
threshold value or range
target movement kinds
target envelope kinds
failure policy
consumer task
```

Critical rule은 후속 MAP19 검증에서 실패 완화 대상이 아니다. Warning/Info는 보고 가능하지만 route-critical 통과 조건을 완화하지 않는다.

### 5.4 Digest and handoff

`BakingCanonicalDigest` 또는 기존 canonical digest primitive를 사용한다.

필수:

```text
LF normalization
UTF-8 no BOM
lower-hex SHA-256
stable repeat
stable reverse input order
stable culture
stable rule order
mutation sensitivity
```

Handoff surface는 다음 digest를 포함한다.

```text
MAP18 approved audit digest
MAP19_01 incoming handoff digest
traversal profile digest
rule registry digest
movement-envelope matrix digest
MAP19_02 handoff digest
```

Digest material에는 machine path, timestamp, frame count, Unity object instance ID를 넣지 않는다.

### 5.5 Failure policy

다음은 atomic failure다.

```text
missing MAP18 approved audit digest
missing required capability group
missing required MovementKind
missing required EnvelopeKind
unsupported MovementKind or EnvelopeKind
duplicate capability key
duplicate rule id
invalid severity
invalid threshold range
Critical rule without failure policy
movement-envelope matrix mismatch
profile digest mismatch
registry digest mismatch
attempted physics/player/controller/tile graph/BFS/seed runner work
```

Failure는 owner, reason, offending key, expected/actual value를 deterministic하게 보고한다. Failure 이후 profile lock이나 MAP19_02 handoff digest가 게시되면 `FAIL`이다.

## 6. 명시적 금지 범위

이번 Task에서 다음을 하지 않는다.

```text
PlayerController / Rigidbody / Collider / Physics2D behavior change
actual movement tuning
actual input handling
tile movement graph construction
stand/climb/bounce node generation
walk/jump/drop/climb/slide/bounce edge generation
BFS or completion search
cluster recovery/density validation execution
event removal validation execution
worst-case scenario validation
distance/revisit/pacing measurement
failure bundle, screenshot, CSV, headless runner creation
seed batch run
production seed approval
GameObject / Prefab instantiate, enable, disable, destroy
Unity Tilemap component write
Tilemap.SetTile / SetTiles / SetTilesBlock / ClearAllTiles / CompressBounds
TilemapCollider2D / CompositeCollider2D / Collider2D creation
Rigidbody2D creation
Physics2D simulation or query
NavMesh or pathfinding setup
Scene mutation
Prefab mutation
Camera or streaming loader integration
Addressables / Resources / AssetDatabase load
System.IO file write/read for production
PlayerPrefs write/read
actual optimization rewrite
large refactor of generated terrain pipeline
shared fixture consolidation
MAP19_02 unlock or execution
PlayMode tests
legacy 19347 regression
unfiltered test runs
full regression runs
```

## 7. Focused-only 검증 정책

정상 검증은 EditMode category `MAP19_01`만 선택한다.

```text
MAP19_01 EditMode: required
MAP09/MAP10/MAP11/MAP12/MAP13/MAP14/MAP15/MAP16/MAP17/MAP18 selections: 0
legacy 19347 selections: 0
PlayMode selections: 0
unfiltered test selections: 0
full regression runs: 0
```

Compile check와 relevant Console check는 허용한다.

실제 문제가 발생해 더 넓은 검증이 필요하다고 판단되면 조용히 회귀를 돌리지 않는다. Result에 다음을 기록하고 멈춘다.

```text
REGRESSION TRIGGER DETECTED: YES
trigger owner:
broken invariant:
why focused proof is insufficient:
requested wider verification:
```

문제가 없다면 Result에 반드시 기록한다.

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## 8. 필수 Focused Tests

다음 test name을 그대로 포함한다. 프로젝트 test framework에 맞춰 클래스/파일은 조정할 수 있다.

```text
TraversalProfilePublishesVersionedCapabilityGroups
TraversalProfileUsesCentralizedValuesWithoutProductionDuplication
TraversalRuleRegistryPublishesRequiredOwnersSeveritiesAndThresholds
MovementEnvelopeMatrixMatchesApprovedWalkJumpDropClimbSlideBounceContract
TraversalProfileRuleLockPreservesMap18ApprovedAuditAndIncomingHandoffDigests
TraversalProfileRuleDigestsAreStableAcrossRepeatReverseCultureAndRuleOrder
TraversalProfileRuleFailuresAreAtomicAndReportOwnerReasonExpectedActual
TraversalProfileRuleLockRejectsDuplicateMissingInvalidMovementRuleAndThresholds
TraversalProfileRuleLockDoesNotBuildGraphsRunBfsMutatePhysicsOrRunRegressions
Map19HandoffKeepsMap19_02Locked
```

Expected focused result:

```text
mode: EditMode
category_names: [MAP19_01]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
```

If the project already has equivalent focused tests and the exact count differs, explain why in Result. Silent substitution is not allowed.

## 9. Result 필수 증거

Result에는 아래 값을 실제 수치로 기록한다.

```text
MAP18_07 Result SHA-256 required/actual:
MAP18_07 installed Task SHA-256 required/actual:
MAP18 approved audit digest reused:
MAP19_01 incoming handoff digest reused:

traversal profile version:
capability groups published:
Collider capability values:
Jump capability values:
AirControl capability values:
Climb capability values:
Bounce capability values:
Fall capability values:
ChaseWidth capability values:
total capability values:
centralized production value owners:
production duplicated numeric tuning values:
test-only duplicated fixture values:

MovementKind values:
EnvelopeKind values:
movement-envelope matrix entries:
matrix mismatch count:

rule registry version:
rule owners published:
validation rules published:
Critical/Error/Warning/Info rule counts:
threshold values published:
duplicate rule ids:
invalid thresholds:
Critical rules without failure policy:

traversal profile digest lower-hex SHA-256: YES
traversal profile digest:
rule registry digest lower-hex SHA-256: YES
rule registry digest:
movement-envelope matrix digest lower-hex SHA-256: YES
movement-envelope matrix digest:
MAP19_02 handoff digest lower-hex SHA-256: YES
MAP19_02 handoff digest:
repeat/reverse/culture/rule-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed:

missing profile/group/movement/envelope failure probes:
duplicate capability/rule failure probes:
invalid severity/threshold failure probes:
matrix mismatch failure probes:
digest mismatch failure probes:
attempted physics/player/tile-graph/BFS/seed-runner failure probes:
atomic failure profile locks published:
atomic failure handoff digests published:

PlayerController/Rigidbody/Collider/Physics2D behavior changes: 0/0/0/0
tile graph nodes generated: 0
tile graph edges generated: 0
BFS/completion searches run: 0/0
cluster recovery/density validations run: 0/0
event removal/worst-case validations run: 0/0
distance/revisit/pacing measurements run: 0
seed batch runs: 0
production seed approvals: 0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
PlayerPrefs writes/reads: 0/0
Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
NavMesh/pathfinding setup: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Camera reads/writes: 0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
optimization rewrites/broad refactors: 0/0
MAP19_02 started: NO
```

## 10. Write boundary

Allowed production source roots:

```text
Assets/_Game/Map/Runtime/WorldGeneration/Validation/
```

Allowed test roots:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/
```

Allowed MCP files:

```text
MapDesign/MCP/TASKS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY.md
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
MapDesign/MCP/REPORTS/MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY_RESULT.md
```

Allowed read-only references:

```text
MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST_V2.md
MapDesign/MCP/REPORTS/MAP18_07_MAP18_POPULATION_EXIT_TESTS_RESULT.md
MapDesign/MCP/TASKS/MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS.md
MapDesign/MCP/TASKS/MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE.md
MapDesign/MCP/TASKS/MAP16_03_VALIDATE_FINAL_ROUTE_AND_RECOVERY.md
Assets/_Game/Map/Runtime/WorldGeneration/
Assets/_Game/Tests/EditMode/Map/WorldGeneration/
```

Do not edit Master unless the current MCP protocol requires current-task bookkeeping there. If Master must be edited, Result must report the exact reason and changed lines.

If editing outside the write boundary seems necessary, stop as `BLOCKED`.

## 11. Completion and commit

PASS 조건:

```text
MAP19_01 focused tests PASS
compile errors 0
relevant Console errors 0
Result includes user-facing implementation report
Result includes responsibility/scripts table
Result includes traversal profile and rule registry summary
Result includes no graph BFS regression or physics boundary notes
Traversal Profile publishes all required capability groups
Validation Rule Registry publishes required owners, severities and thresholds
MovementKind and EnvelopeKind matrix matches MAP09/MAP11 contract
MAP18 approved audit and MAP19_01 incoming handoff digests are preserved
MAP19_02 handoff digest is created
production traversal values are centralized and duplication is reported
no player physics/controller/tuning changes
no tile graph, BFS, seed batch, failure bundle, CSV or headless runner work
no actual Unity object, Tilemap, Collider, Rigidbody, Physics2D, NavMesh, Scene or Prefab mutation
no optimization rewrite or broad refactor
no regression runs unless explicitly triggered and reported
MAP19_02 remains LOCKED / NOT STARTED
```

PASS일 때만 status finalize를 수행한다.

Expected final status:

```text
MAP19_01_LOCK_TRAVERSAL_PROFILE_AND_RULE_REGISTRY: COMPLETE
MAP19_02_BUILD_TILE_MOVEMENT_GRAPH: LOCKED
Current Task: NONE
```

Atomic commit subject:

```text
MAP19_01: lock traversal profile and rule registry
```

Git push는 하지 않는다.

관련 없는 dirty worktree 변경은 수정하거나 stage하지 않는다.
