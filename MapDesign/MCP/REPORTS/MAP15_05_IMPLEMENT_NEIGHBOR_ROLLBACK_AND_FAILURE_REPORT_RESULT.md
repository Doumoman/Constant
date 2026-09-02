TASK: MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT
STATUS: PASS
MAP15_05: COMPLETE ELIGIBLE only when PASS
MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 Task는 실패 sector의 **center + in-bounds Moore 1-ring rollback scope**와 **first contradiction failure report**를 만드는 world-level containment contract다. 실제 state rollback, sector terrain rerender, 624x416 Tilemap bake, Scene/Prefab/GameObject 변경 또는 gameplay spawn은 수행하지 않는다.

- `WorldNeighborRollbackPlan.cs`를 추가해 corner/edge/interior scope, rollback sector, contradiction kind/source/evidence, failure report, bounded retry/abort/blocked-owner decision, typed atomic failure/result, public authority identity와 canonical SHA-256 digest를 불변 값으로 공개했다.
- `WorldNeighborRollbackPlanner.cs`를 추가해 MAP15_01 solve order, MAP15_02의 312 edges, MAP15_03 reservation transaction/claim/edge-lock evidence, MAP15_04 pacing window/budget/recent-use/cap 및 candidate signature, MAP14 retry/debug projection을 공개 값으로만 소비한다.
- `WorldNeighborRollbackPlannerTests.cs`를 추가해 `REFERENCE NEIGHBOR ROLLBACK REPORT` fixture로 요구된 focused gate 10개를 검증했다. 이 fixture는 production seed, 실제 full-world terrain solve, 실제 rollback 실행, Tilemap/player traversal 또는 MAP15 phase exit을 승인하지 않는다.

검증된 rollback scope는 corner `4/4`, world edge `6/6`, interior `9/9` sectors다. 모든 scope는 failed sector를 정확히 한 번 포함하며 `abs(dx) <= 1`, `abs(dy) <= 1`, radius `1`, maximum `9`를 만족했다. scope max exceeded `0`, out-of-radius sector `0`, failed sector missing `0`이다. 순서는 failed sector first, 이후 solve step ascending, sector id ascending이다.

first contradiction은 `lowest solve step -> Special > Boundary > MandatoryRoute > IntersectorSocket > Reservation > PacingDensity > ClusterCandidate > Retry > Unknown -> sector id -> stable contradiction id` 순서로 `1/1` 선택됐다. reverse input과 `tr-TR` culture에서도 동일했다. linked report gate에서 related edge `1/1`, reservation `1/1`, pacing `1/1`, candidate `1/1`, retry label `1/1`이 각 공개 authority에 존재함을 확인했다.

decision gate는 bounded retry `1/1`, retry-cap abort `1/1`, upstream blocked owner `1/1`을 확인했다. whole-world rerandom decision `0`, fallback carve decision `0`, silent widening decision `0`이며 해당 요청은 partial plan 없이 typed failure로 거부된다.

```text
world sectors observed: 169/169
internal edges observed: 312/312
reservation plan observed: 1/1
pacing-density plan observed: 1/1
corner scope sectors: 4/4
edge scope sectors: 6/6
interior scope sectors: 9/9
scope max exceeded: 0
scope out-of-radius sectors: 0
failed sector missing from scope: 0
first contradiction selected: 1/1
related edge references valid: 1/1
related reservation references valid: 1/1
related pacing references valid: 1/1
related candidate/retry references valid: 2/2
bounded retry decisions: 1/1
abort decisions: 1/1
blocked owner decisions: 1/1
whole-world rerandom decisions: 0
fallback carve decisions: 0
silent widening decisions: 0
input digest: 37105fe7de0b9e4be00d76e87a70bdfe4ed47c6f8ae22366fbe8ea7c07873fdb
output digest: 61cb74316f205f8044fb3a34092c60407efa6fdd8056b593c592f2d4bb707595
repeat/reverse/culture digest mismatches: 0
new RNG draws: 0
sector rerender: 0
generated/file write: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
authoring/upstream plan mutation: 0
Activity/Event/NPC/reward gameplay spawn: 0
```

아직 구현하지 않은 범위는 실제 rollback 실행, candidate reroll, fallback terrain carve, full-world terrain solve, Tilemap bake/streaming, Scene/Prefab/GameObject, collider/player traversal, Activity/Event/NPC/reward spawn, production seed 승인과 MAP15 phase exit이다. Editor와 게임 화면에 새 시각 요소는 없으며, MAP15_06이 이 plan/report를 overlay와 batch test world plan으로 표시하는 downstream owner다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlan.cs`

- `WorldRollbackSector`, `WorldRollbackScope`: failed sector id/coordinate와 solve-step ordered in-bounds 1-ring을 read-only collection으로 공개한다. 입력 `failed node + public solve steps` -> 출력 `Corner/Edge/Interior scope 4/6/9`다.
- `WorldContradictionEvidence`, `WorldFailureReport`: edge/reservation/pacing/candidate/retry reference를 복사·정렬하고 first contradiction 및 aggregate evidence를 공개한다. 입력 `typed observations` -> 출력 `stable first contradiction + related evidence`다.
- `WorldRollbackDecision`, `WorldRollbackPolicyRequest`, `WorldNeighborRollbackPlan`: MAP14/MAP15_01~04 identity, retry attempt/cap, 금지 counters와 downstream handoff를 불변 계약으로 묶는다. 입력 `public upstream artifacts + one failed sector + observations` -> 출력 `scope + report + decision + digests`다.
- `WorldNeighborRollbackFailure`, `WorldNeighborRollbackResult`: missing/failed authority, invalid sector/scope/evidence/digest, forbidden rerandom/carve/widening/mutation을 partial plan 없이 반환한다.
- `WorldNeighborRollbackDigest.ComputeInput/ComputeOutput/HashCanonicalText`: UTF-8, LF, InvariantCulture, enum name, sorted evidence 및 lower-hex SHA-256로 canonical digest를 생성한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlanner.cs`

- `Plan`: public authority chain과 zero-mutation preconditions를 검증하고 scope/report/decision을 원자적으로 생성한다.
- `BuildScope`: failed coordinate 입력에서 in-bounds Moore 1-ring을 만들고 corner/edge/interior `4/6/9`, radius `1`, max `9`, failed-first ordering을 보장한다.
- `ValidateContradictions`, `ValidateEvidenceReferences`: observation sector/solve step과 MAP15_02 edge, MAP15_03 transaction/claim/lock, MAP15_04 window/budget/recent-use/cap/signature, MAP14 retry label의 공개 membership을 검증한다.
- `Decide`: local retry 가능 + cap 미소진이면 `BoundedRetry`, cap 소진/범위 밖이면 `Abort`, upstream invariant owner가 필요하면 `BlockedOwner`를 반환한다.
- `PacingBudgetEvidenceId`: 공개 sector id 입력 -> stable pacing budget evidence id 출력 projection을 제공한다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldNeighborRollbackPlannerTests.cs`

- category `MAP15_05`의 요구 gate 10개와 `ReferenceRollbackFixture`를 추가했다.
- fixture는 public MAP15_01 planner -> MAP15_02 integrator -> MAP15_03 planner -> MAP15_04 planner 순서로 169 sectors, 312 internal edges, reservation/pacing plan을 생성한다.
- corner/edge/interior, ordering, first contradiction priority, full evidence links, 세 decision, forbidden policy, repeat/reverse/culture determinism, atomic invalid inputs, mutation zero, MAP15_06 locked handoff를 검증한다.

소비한 public authority는 MAP14 phase-exit/retry-debug identity, `WorldPlanInput`, `WorldSolveOrderResult`, `WorldIntersectorEdgePlan`, `WorldMultiSectorReservationPlan`, `WorldPacingDensityPlan`이다. 기존 production/test/meta, Editor production, CSV/schema/cache/generated output, Scene/Prefab/Tilemap/ScriptableObject, asmdef/asmref, ProjectSettings, Packages 수정은 `0`이고 upstream 수정도 `0`이다. downstream owner는 `MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS`이며 열거나 시작하지 않았다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_05]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 3.2477645
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

최종 Unity CLI run은 raw `-testCategory MAP15_05` selection으로 10개만 발견·실행하고 result `Passed`, exit `0`을 반환했다. 전용 Editor log의 compile error/warning은 `0/0`이고 task-owned error/warning도 `0/0`이다. 선행 focused 시도는 invalid-sector fixture가 out-of-range id의 solve step을 먼저 조회해 `9/10`이었으며, 허용된 test-owned helper만 수정한 뒤 같은 category 최종 실행이 `10/10` PASS했다. 회귀 trigger는 발생하지 않았고 prior category, legacy 19347, PlayMode 및 unfiltered test는 실행하지 않았다.

## Static and Workflow Verification

- 단일 inbox candidate `MAP15_05_IMPLEMENT_NEIGHBOR_ROLLBACK_AND_FAILURE_REPORT.md`만 적용했다.
- installed Task와 archive SHA-256은 모두 `e5b401d922dc5c1af4ce3152bc9f07499e0dbf37fdbbb34359a7eda26ee040b6`로 byte-identical이다.
- predecessor MAP15_04 Result SHA-256 `07814c976bdb18eaef0148bbae3c5a4cfd0ee44389538f8a7e78e3609060280b`와 installed Task SHA-256 `56873c09160278f14e2c17e1d4572de504c93891e328c6da3311997ed2634990`는 patch metadata와 일치했다.
- task 시작 조건은 MAP15_04 `COMPLETE`, MAP15_05 `CURRENT`, MAP15_06 `LOCKED`, unrelated staged `0`이었다.
- Runtime source에는 UnityEngine, UnityEditor, System.IO, filesystem write, random/time API 의존성이 없다. Tilemap/GameObject 문자열은 mutation proof counter/property에만 존재한다.
- 관련 없는 기존 `Constant.slnx` 및 TerrainClusters matching-directory meta 변경은 수정하거나 stage하지 않았다.

Commit subject: `MAP15_05: implement neighbor rollback failure report`

Push: NOT PERFORMED
