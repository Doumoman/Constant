TASK: MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS
STATUS: PASS
MAP15_06: COMPLETE ELIGIBLE only when PASS
MAP15_07_MAP15_WORLD_ASSEMBLY_EXIT_AUDIT: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 Task는 MAP15_01부터 MAP15_05까지의 공개 world-plan 결과를 읽어, 메모리 안에서만 사용할 수 있는 불변 overlay export와 4개 reference-world batch report로 묶었다. 실제 624×416 월드를 다시 생성하거나 Tilemap, Scene, Prefab, GameObject, authoring asset, gameplay object를 변경하지 않는다.

- `WorldAssemblyOverlayExport.cs`는 169개 sector, 312개 intersector edge, 12개 overlay layer, MAP15_01~05 input/output hash record 10개, 4개 batch case, solver upper bound 10개를 read-only 공개 값으로 제공한다.
- `WorldAssemblyOverlayExporter.cs`는 topology, solve order, boundary, special/cluster reservation, pacing/activity-event cap, rollback/failure, hash chain, mutation proof를 stable token으로 projection한다. 입력 누락, authority chain 불일치, digest/count 불일치, batch label 누락, bound 초과, production 승인 주장 및 금지 mutation은 partial export 없이 typed failure로 거절한다.
- `WorldAssemblyOverlayBatchTests.cs`는 요구된 focused EditMode gate 10개를 실행해 ordering, repeat/reverse/culture 결정성, 원자 실패, mutation zero와 MAP15_07 잠금을 확인한다.

정상 reference fixture에서 world sector `169/169`, internal edge `312/312`, edge endpoint `624/624`를 확인했다. overlay sector/edge도 각각 `169/169`, `312/312`이고, row-major sector id 및 edge-id ordering mismatch는 `0`이다.

12개 required layer는 모두 공개되었다. `Topology`, `SolveOrder`, `IntersectorEdges`, `BoundaryPairs`, `SpecialReservations`, `PacingDensity`, `ActivityEventCaps`, `RollbackScopes`, `FailureReports`, `HashChain`, `MutationProof`는 공개 evidence token을 포함한다. reference fixture에 public cluster reservation이 없으므로 `ClusterReservations`는 빈 layer를 숨기지 않고 `NO_PUBLIC_CLUSTER_RESERVATIONS`라는 명시적 unavailable reason을 제공한다. required/covered/missing layer는 `12/12/0`이다.

hash chain은 MAP15_01, MAP15_02, MAP15_03, MAP15_04, MAP15_05 각각의 input/output을 기록해 required/covered/missing `10/10/0`이다. MAP15_06 자체 input/output은 export digest로 별도 공개한다.

batch labels는 다음 4개로 고정되어 있고 production seed 또는 phase-exit 승인으로 사용하지 않는다.

```text
REFERENCE_WORLD_BASELINE
REFERENCE_WORLD_BOUNDARY_HEAVY
REFERENCE_WORLD_SPECIAL_RESERVATION
REFERENCE_WORLD_ROLLBACK_FAILURE
```

각 case의 graph connected component는 `1`, duplicate sector/edge id `0`, missing required boundary pair `0`, untyped reservation conflict `0`, accepted pacing violation `0`, rollback scope `9 <= 9`이다. batch required/covered/passing/missing은 `4/4/4/0`, production-seed approval count는 `0`이다.

```text
world size: 624x416 tiles
sector size: 48x32 tiles
sector grid: 13x13
world sectors: 169/169
internal edges: 312/312
edge endpoints: 624/624
overlay sectors: 169/169
overlay edges: 312/312
required/covered/missing layers: 12/12/0
required/covered/missing hash records: 10/10/0
required/covered/passing/missing batch cases: 4/4/4/0
required/covered/violated solver upper bounds: 10/10/0
boundary-pair evidence: 1
special reservation transactions: 2
cluster reservation unavailable reasons: 1
pacing windows: 5
sector density budgets: 169
activity/event caps: 2
rollback scope sectors: 9
failure observations: 1
connected components: 1
duplicate ids: 0
missing required boundary pairs: 0
untyped reservation conflicts: 0
accepted pacing violations: 0
input digest: 37928c41c2ec1579c4175d620ddcb705743ea9b15132dacdeb7ecbb9c1b96d7c
output digest: 71a38dd6452b1805244166cc832745c8ba84a939b96dd7b0378712b5b1a52cfb
repeat/reverse/culture digest mismatches: 0
whole-world rerandom: 0
fallback carve: 0
silent widening: 0
generated/file write: 0
Tilemap/Scene/Prefab/GameObject mutation: 0
authoring/upstream plan mutation: 0
Activity/Event/NPC/reward gameplay spawn: 0
production-seed approval: 0
full regression runs: 0
```

아직 구현하지 않은 범위는 실제 파일 JSON/CSV export, Editor overlay window, runtime UI/Tilemap 시각화, production seed 승인, full-world generation 및 MAP15 phase-exit audit이다. 사용자 화면에 새로 보이는 요소는 없고, 이번 결과는 후속 audit이 읽을 수 있는 코드 기반 in-memory overlay/report 계약이다. MAP15_07은 시작하지 않았다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExport.cs`

- `WorldAssemblyOverlaySector`, `WorldAssemblyOverlayEdge`, `WorldAssemblyOverlayLayer`: public world-plan 값을 sector row-major/edge-id 순서의 불변 overlay record로 복사하며 token kind, severity, stable token과 unavailable reason을 공개한다.
- `WorldAssemblyHashRecord`: MAP15_01~05 task id별 input/output SHA-256를 명시적으로 기록한다.
- `WorldBatchPlanCase`, `WorldBatchPlanReport`: 4개 abstract reference label에 graph/reservation/pacing/rollback/solver verdict와 집계 수를 제공한다.
- `WorldSolverUpperBound`: solve step, internal edge, endpoint, rollback sector, retry cap 및 금지 rerandom/carve/widening/file/mutation 10개 bound의 actual/limit/pass를 제공한다.
- `WorldAssemblyOverlayRequest`, `WorldAssemblyOverlayExport`: upstream plan chain과 zero-mutation/zero-approval claim을 입력으로 받고 12-layer/10-hash/4-case/10-bound snapshot과 MAP15_07 locked handoff를 출력한다.
- `WorldAssemblyOverlayFailure`, `WorldAssemblyOverlayResult`: missing/failed authority, invalid dimension/count/digest/label/layer/token, upper-bound violation, production claim 및 mutation을 partial export 없이 반환한다.
- `WorldAssemblyOverlayDigest`: UTF-8, LF, InvariantCulture, canonical ordering 및 lowercase SHA-256로 input/output identity를 계산하고 free-form token을 path separator가 없는 canonical text로 정규화한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldAssemblyOverlayExporter.cs`

- `Export`: MAP15_01 solve order, MAP15_02 intersector plan, MAP15_03 reservation plan, MAP15_04 pacing plan, MAP15_05 rollback plan의 공개 identity chain을 검증한 후 모든 output을 한 번에 생성한다.
- sector projection은 id/coordinate/solve step/route/access/pacing과 special/reservation, pacing window/budget, rollback/failure marker count를 공개한다.
- edge projection은 endpoint sector, orientation, socket compatibility, boundary/mandatory/external summary, edge digest와 stable token을 공개한다.
- layer projection은 required 12종을 정확히 한 번씩 만들며, public evidence가 없는 layer에는 명시적 unavailable reason을 남긴다.
- batch validation은 grid connectivity, id uniqueness, boundary binding, typed reservation conflict, pacing violation, rollback cap과 10개 upper bound를 검증한다.
- forbidden count 또는 validation failure가 하나라도 있으면 export/hash/batch의 partial publication 없이 `WorldAssemblyOverlayResult.Fail`을 반환한다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldAssemblyOverlayBatchTests.cs`

- category `MAP15_06`의 요구 gate 10개를 추가했다.
- test-owned fixture는 공개 planner 순서 `MAP15_01 -> MAP15_02 -> MAP15_03 -> MAP15_04 -> MAP15_05`로 169-sector/312-edge reference plan을 만든 뒤 MAP15_06 export만 검증한다.
- topology/layer/hash coverage, row-major/edge ordering, 4개 label, graph/reservation/pacing/rollback verdict, 10개 bound, forbidden input rejection, repeat/reverse/tr-TR 결정성, immutability/mutation zero 및 MAP15_07 locked handoff를 검증한다.

## Solver Upper Bounds

```text
solve steps: 169 <= 169
internal edges: 312 <= 312
edge endpoints: 624 <= 624
rollback sectors per failure: 9 <= 9
sector-local retry attempts: 6 <= 6
whole-world rerandom: 0 <= 0
fallback carve: 0 <= 0
silent widening: 0 <= 0
file writes: 0 <= 0
combined Scene/Prefab/Tilemap/GameObject mutations: 0 <= 0
```

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_06]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
durationSeconds: 2.7971892
final compile errors: 0
task-owned Console errors: 0
task-owned Console warnings: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

최종 Unity CLI run은 raw `-testCategory MAP15_06` selection으로 이 fixture의 10개만 발견·실행했고 result `Passed`, exit `0`을 반환했다. 초기 focused 시도에서 새 모델의 기존 enum namespace import 누락을 컴파일러가 보고했으며 import를 보정했다. 최종 코드로 반복한 focused run은 compile/test 모두 PASS였다. 기능 문제나 regression trigger는 발생하지 않아 prior category, PlayMode, unfiltered 및 full regression은 실행하지 않았다.

## Static and Workflow Verification

- 단일 inbox candidate `MAP15_06_EXPORT_OVERLAY_AND_BATCH_TEST_WORLD_PLANS.md`만 검증·적용했다.
- installed Task와 archive SHA-256은 모두 `9572300253a1e33829eb2ac14f7fa4a3ebaf3e24600517a6990df02f06d81156`으로 byte-identical이고 inbox 원본은 archive로 이동했다.
- predecessor MAP15_05 Result SHA-256 `fa409cc525e2755e990d7ca444cb165f0fb86d2ae43100ab119348f3e54b7cee`와 installed Task SHA-256 `e5b401d922dc5c1af4ce3152bc9f07499e0dbf37fdbbb34359a7eda26ee040b6`은 patch metadata와 일치했다.
- task 시작 조건은 MAP15_05 `COMPLETE`, MAP15_06 `CURRENT`, MAP15_07 `LOCKED`, status row `215`, unrelated staged `0`이었다.
- Runtime source에는 `System.IO`, UnityEngine/UnityEditor 객체 API, RNG/time API 또는 filesystem write가 없다. Scene/Prefab/Tilemap/GameObject 명칭은 zero-bound/mutation-proof 계약에만 존재한다.
- 새 `.meta` GUID 3개는 형식이 유효하고 프로젝트 전체에서 각각 1회만 발견된다.
- 관련 없는 기존 `Constant.slnx`와 TerrainClusters matching-directory meta 변경은 stage하지 않는다.

Commit subject: `MAP15_06: export overlay and batch test world plans`

Push: NOT PERFORMED
