TASK: MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION
STATUS: PASS
MAP15_04: COMPLETE ELIGIBLE only when PASS
MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 작업은 MAP15_01 solve order, MAP15_02 intersector edge plan, MAP15_03 reservation plan과 MAP09~13 공개 identity를 받아 **world-level pacing/density/repetition contract**를 구성한다. 결과는 후속 rollback planner가 소비하는 immutable in-memory evidence이며, 실제 sector terrain을 다시 렌더링하거나 624x416 Tilemap, Scene/Prefab/GameObject 또는 gameplay runtime을 변경하지 않는다.

- 새 Runtime model `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlan.cs`는 Quiet/Cluster/Activity/Event/Landmark window, 169-sector abstract density envelope, Pattern/Cluster/Activity signature와 recent-use rule/observation, Activity/Event cap projection, typed violation/failure 및 canonical digest를 공개한다.
- 새 Runtime planner `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlanner.cs`는 upstream identity와 mutation-zero를 검증하고 window/budget/signature/rule/cap을 deterministic하게 평가한다. 유효한 관측 위반은 explicit violation으로 남기며, 잘못된 계약 입력은 partial plan 없이 typed atomic result로 종료한다. silent reroll은 없다.
- 새 focused test `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldPacingDensityPlannerTests.cs`는 명시적인 `REFERENCE WORLD PACING DENSITY PLAN`만 사용한다. 이 fixture는 production seed, 실제 full-world terrain/Tilemap, player traversal, Activity/Event runtime spawn 또는 MAP15 phase exit을 승인한다고 주장하지 않는다.
- 관측된 world sector/internal edge/reservation plan은 `169/169`, `312/312`, `1/1`이다. MAP15_03 reservation output digest는 입력에서 그대로 보존됐다.
- required/covered/missing window kind는 `5/5/0`, window 총수는 `5`다. Quiet, Cluster, Activity, Event, Landmark가 각각 solve-step span, sector ids, min/max/observed count, reason과 source owner를 공개하며 sector reference는 모두 유효하다.
- density budget sector coverage는 `169/169`이고 중복·누락·invalid budget은 `0`, accepted reference fixture의 budget violation은 `0`이다. 모든 budget verdict는 `WithinRange`이며 solid `30..70`/observed `50`, reachable `20..80`/observed `60`의 abstract envelope다. 실제 tile cell count가 아니다.
- fixed MAP15_03 Special sector는 Landmark window가 모두 포함하고, deferred landmark transaction identity `1`개는 reservation plan에 그대로 남았다. reservation priority overwrite와 reservation mutation은 `0`이다.
- required/covered/missing signature kind와 recent-use rule은 각각 `3/3/0`, signature `9`, rule `3`이다. accepted recent-use observation은 `6`, accepted fixture repeat violation은 `0`이며 모든 관측은 world coordinate graph distance와 solve-step distance를 함께 사용했다.
- 합성 Pattern/Cluster/Activity 동일-signature 근접 반복은 각각 1건씩 총 `3`건을 deterministic violation으로 검출했다. violation type은 `RecentPatternRepeat`, `RecentClusterRepeat`, `RecentActivityRepeat`이며 reroll을 수행하지 않았다.
- MAP12 공개 `ActivityFrequencyPolicy`에서 activity target `90 permille`, world strong cap `4`를 projection했고, 공개 `EventOverlayAssignmentPolicy`의 event target `80 permille`와 test-only abstract event cap `2`를 명시했다. accepted fixture Activity/Event cap violation은 `0`; 합성 Event over-cap은 `1`건을 검출했다.
- input digest는 `5be15980b301216f47a8152513bc750d2c47f6a7c5232e689d70a6c561860a7f`, output digest는 `2c028c390c1bb08b2b987f0b42c4e54afb7f4f604e64e40e20329e7403bc44ee`다. repeat, reversed enumeration, `tr-TR` culture replay의 input/output/observation/budget-order mismatch는 모두 `0`이다.
- missing upstream/window/budget, invalid range/signature/rule/cap/digest와 mutation claim은 `Plan = null`, empty input/output result digest와 typed reason으로 원자적으로 종료하는 것을 확인했다.
- 새 RNG draw, fallback carve, sector rerender, generated file write, Tilemap/Scene/Prefab/GameObject mutation, gameplay spawn, authoring/MAP15_01 world/MAP15_02 edge/MAP15_03 reservation mutation은 각각 `0`이다.
- prior task category, legacy 19347, PlayMode 및 unfiltered regression test는 실행하지 않았다. 최종 task-owned compile/test 문제가 없으므로 `REGRESSION TRIGGER DETECTED: NO`다.

아직 구현하지 않은 범위는 실제 full-world terrain solve/bake, 624x416 Tilemap, MicroChunk 12x8 slice/streaming, collider/physics/player traversal, Activity/Event/NPC/reward/combat/crafting/inventory runtime, production seed 승인, MAP15_05 neighbor rollback/failure report 및 MAP15 phase exit이다. Editor 가시성은 Unity Test Runner focused evidence만 제공하며 새 EditorWindow/overlay/inspector/debug asset은 없다. 게임 가시성은 없다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlan.cs`

- `WorldPacingWindowKind` / `WorldPacingWindow`: five window kind와 stable id, sorted sector ids, solve-step span, min/max/observed count, reason/owner 입력을 immutable comparable window로 변환한다.
- `WorldDensityBudgetKind` / `WorldDensityBudgetVerdict` / `WorldSectorDensityBudget`: sector id와 abstract solid/reachable min/max/observed input을 `WithinRange`/`Warning`/`Violation` verdict를 가진 immutable envelope로 변환한다.
- `WorldContentSignatureKind` / `WorldContentSignature`: Pattern/Cluster/Activity identity, sector, solve step, source owner를 canonical recent-use input으로 보존한다.
- `WorldRecentUseRule`: signature kind별 positive graph/solve-step minimum distance와 provenance를 immutable rule로 보존한다.
- `WorldRecentUseObservation`: earlier/later signature, sector/step, graph availability/distance, solve distance, accepted/violation reason을 deterministic evidence로 보존한다.
- `WorldActivityEventConstraint`: MAP12-derived Activity/Event target permille, abstract maximum count, authority digest와 owner를 명시한다.
- `WorldPacingDensityViolation`: window, density, reachable, repetition, Activity/Event cap violation의 type/subject/sector/signature/reason을 stable order로 보존한다.
- `WorldPacingDensityRequest`: MAP15_01 world/solve, MAP15_02 edge plan, MAP15_03 reservation plan, windows/budgets/signatures/rules/caps, MAP10~14 identity digest와 모든 mutation proof counter를 defensive immutable input으로 묶는다.
- `WorldPacingDensityPlan`: windows/budgets/signatures/rules/observations/violations, `169/312`, reservation identity, coverage/violation counters, input/output digest, downstream owner `MAP15_05`와 automatic-open false를 공개한다.
- `WorldPacingDensityFailure` / `WorldPacingDensityResult`: 누적 typed contract issue를 성공 시에만 plan을 갖는 atomic result로 반환한다.
- `WorldPacingDensityDigest.ComputeInput`: upstream/public authority identity, publication label, mutation counters와 sorted input values를 UTF-8/LF/InvariantCulture lower-hex SHA-256으로 변환한다.
- `WorldPacingDensityDigest.ComputeOutput`: sorted windows/budgets/signatures/rules/caps/observations/violations를 canonical output digest로 변환한다.
- `WorldPacingDensityDigest.HashCanonicalText`: canonical text 입력을 lower-hex SHA-256으로 변환한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldPacingDensityPlanner.cs`

- `Plan`: immutable request -> upstream/count/digest/no-mutation validation -> window/budget/signature/rule/cap validation -> explicit observation/violation -> atomic result와 output digest를 만든다.
- `ValidateUpstream` / `HasMutationClaim`: MAP15_01/02/03 success/identity chain, exact `169/312`, MAP10~14 digests, publication label와 모든 reroll/write/mutation counter zero를 검증한다.
- `ValidateWindows` / `ValidateLandmarkCoverage`: five kind coverage, unique identity, solve span/sector/count envelope와 fixed MAP15_03 Special sector preservation을 검증한다.
- `ValidateBudgets`: 169 sectors 각각의 single abstract budget, min/max/provenance를 검증한다. observed out-of-range는 contract failure가 아니라 explicit violation으로 분리한다.
- `ValidateSignatures` / `ValidateRecentUseRules`: Pattern/Cluster/Activity identity가 public world sector/solve step과 일치하고 rule distance가 positive인지 검증한다.
- `ValidateActivityEventConstraints`: Activity/Event projection의 public digest, frequency/cap와 window maximum이 모순되지 않는지 검증한다.
- `AddWindowViolations` / `AddBudgetViolations` / `AddActivityEventCapViolations`: valid observation의 under/over/budget/cap 위반을 deterministic typed evidence로 만든다.
- `BuildRecentUseObservations`: kind별 solve order의 인접 signature를 world Manhattan graph distance와 solve-step distance로 비교하고 accepted evidence 또는 typed recent-repeat violation을 만든다.
- public authority consumed: MAP09 `PacingRole`/`AccessClass`, MAP10 `MicroPatternId`, MAP11 `TerrainClusterId`/`SpineVariantId`, MAP12 `ActivityStructureId`/`ActivityFrequencyPolicy`와 `EventOverlayId`/`EventOverlayAssignmentPolicy`, MAP13 public starter catalog digests, MAP14 phase-exit digest, MAP15_01/02/03 public plans/digests다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldPacingDensityPlannerTests.cs`

- 필수 gate 10개는 public MAP15_01/02/03 planner chain으로 reference input을 만들고 five windows, 169 budgets, recent-use/cap evidence, synthetic violations, atomic invalid cases, determinism, no-mutation 및 MAP15_05 lock을 검증한다.
- `ReferencePacingFixture`: 169-sector solve order, 312-edge intersector plan과 MAP15_03 fixed/deferred reservation을 public API로 생성하고 MAP10~13 typed identities를 digest projection으로 연결한다.
- production Runtime C#/meta 추가 `2/2`, Runtime EditMode test C#/meta 추가 `1/1`이다. 기존 production/test/meta 수정 `0`, Editor production `0`, CSV/schema/cache/generated output `0`, Scene/Prefab/Tilemap/ScriptableObject `0`, asmdef/asmref/ProjectSettings/Packages `0`, upstream 수정 `0`이다.
- downstream owner는 `MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT`이며 이번 작업은 이를 열거나 시작하지 않는다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_04]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 4.4006986
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

Unity MCP final test job `93c1053020134e299a1e0cfadd735463`은 category `MAP15_04`에서 10개를 발견·실행했고 summary `Passed`, passed `10`, failed/skipped `0/0`을 반환했다. 새 asset을 처음 import한 직후의 선행 시도는 Unity cleanup verifier가 테스트 파일을 run 중 생성으로 오인해 test result payload 없이 종료됐으며, full AssetDatabase refresh 후 동일 category 최종 실행이 정상 완료됐다. 세 task-owned script의 최종 Unity standard validation diagnostics는 error/warning `0/0`이고 최종 Console clear 후 error/warning은 `0/0`이다.

## Static and Workflow Verification

- 단일 inbox candidate `MAP15_04_IMPLEMENT_WORLD_PACING_DENSITY_AND_REPETITION.md`만 적용했으며 installed Task와 archive SHA-256은 모두 `56873c09160278f14e2c17e1d4572de504c93891e328c6da3311997ed2634990`로 byte-identical이다.
- predecessor MAP15_03 Result PASS SHA-256 `1e2adf481b3a9dce03d0ef1cf3450b7ba60359ab297ee53c0bc9b687b7cb7187`와 installed Task SHA-256 `32a553a012dfc8b795ad879939246b7780b784013db3fd0882723e34a095c782`는 patch metadata와 일치했다.
- task 시작 조건은 MAP15_03 COMPLETE, MAP15_04 CURRENT, MAP15_05 LOCKED, unrelated staged `0`이었다.
- Runtime/test source에는 UnityEngine, UnityEditor, System.IO, filesystem write, random/time API 의존성이 없다. Scene/Prefab/Tilemap/GameObject 문자열은 mutation counter/property/assertion에만 존재한다.
- 관련 없는 기존 worktree 변경은 수정하거나 stage하지 않았다.

Commit subject: `MAP15_04: implement world pacing density repetition`

Push: NOT PERFORMED
