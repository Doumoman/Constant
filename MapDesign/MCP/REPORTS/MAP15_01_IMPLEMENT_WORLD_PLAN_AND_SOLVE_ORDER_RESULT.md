TASK: MAP15_01_IMPLEMENT_WORLD_PLAN_AND_SOLVE_ORDER
STATUS: PASS
MAP15_01: COMPLETE ELIGIBLE
MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES: LOCKED / DO NOT START

## User-Facing Implementation Report

이번 Task는 MAP14 phase-exit가 승인한 sector-local planner handoff를 13×13 world graph에서 소비하기 위한 **169-sector abstract solve-order 계약**을 구현했다. 48×32 sector terrain canvas를 다시 렌더링하거나 Tilemap, Scene, Prefab, GameObject, gameplay runtime에 반영하는 단계가 아니다.

- 신규 Runtime model `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSectorPlan.cs`는 624×416 world, 48×32 sector, 13×13/169-sector topology 상수와 immutable node/dependency/retry/step/result/failure/digest public surface를 제공한다.
- 신규 Runtime planner `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSolveOrderPlanner.cs`는 입력을 원자적으로 검증하고 dependency-aware Kahn ordering을 수행한다. ready node tie-break는 priority rank, dependency count descending, route/access/special stable key, sector id ascending 순서다.
- 신규 focused test `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSolveOrderPlannerTests.cs`는 category `MAP15_01`의 9개 gate와 명시적인 `REFERENCE WORLD PLAN` 169-sector fixture를 제공한다. fixture는 production seed 또는 실제 full-world terrain 승인으로 간주하지 않는다.
- topology actual은 sector nodes `169/169`, unique ids `169/169`, unique coordinates `169/169`, out-of-bounds `0`, duplicate nodes `0`이다.
- dependency actual은 `SpecialReservation 3`, `MandatoryRoute 15`, `BoundaryPair 6`, `ExternalSocket 4`, `NeighborContinuity 8`, `PacingWindow 6`, `RetryGuard 3`, 합계 `45`다. Special/Boundary/Mandatory required dependency missing은 `0`이다.
- solve order actual은 steps `169/169`, unsolved `0`, duplicate solve step `0`, cycle `0`, prerequisite-order violation `0`이다.
- constrained priority 첫 10개 evidence는 `SECTOR_000:MandatoryRouteOrBoundary`, `SECTOR_042/028/014:FixedSpecial`, `SECTOR_056~061:MandatoryRouteOrBoundary`다. world-start prerequisite가 먼저 해결된 뒤 ready 상태의 fixed Special이 mandatory/boundary보다 먼저 선택됐다.
- retry envelope는 node별 sector-local attempt max `6`, dependency rollback radius `1`, typed abort `SectorLocalAttemptsExhausted`다. MAP14 local retry loop 실행 `0`, 신규 RNG draw `0`, whole-world rerandom `0`, fallback carve `0`이다.
- canonical input digest는 `adb90ec201665821abf51d5ad54e1a301451832de9359afdabf73d906cab3c33`, output digest는 `7d2bae14b7326410320a11356586afb0e993d037570dd322332d1ed8125e8882`다.
- repeat/reversed input enumeration/`tr-TR` culture 비교에서 input/output/order digest mismatch는 `0/0/0`이다. canonicalization은 UTF-8, LF join, invariant integer formatting, stable enum names, sorted node/edge facts, lower-hex SHA-256을 사용한다.
- invalid input gate는 missing input, 168-sector count, duplicate id/coordinate, out-of-bounds/id mismatch, self dependency, missing endpoint, cycle, missing required dependency, whole-world rerandom을 partial payload 없이 거부했다. 모든 failure result는 input `null`, steps `0`, input/output digest empty다.
- generated file write, Tilemap, Scene, Prefab, GameObject, gameplay spawn, MAP14 sector-planner mutation은 모두 `0`이다. Activity/Event/NPC/reward/combat/crafting/inventory runtime, WorldGenerationRoot wiring도 실행하지 않았다.
- MAP09~14 prior category, legacy 19347, PlayMode, unfiltered regression 선택은 모두 `0`이다. `REGRESSION TRIGGER DETECTED: NO`다.

아직 구현하지 않은 범위는 MAP15_02 inter-sector socket/boundary resolution, 실제 169-sector terrain solve, Tilemap bake, MicroChunk 12×8 slice/streaming, collider/physics/player traversal, Scene/Prefab/GameObject 반영, production seed 승인, MAP15 phase exit, Activity/Event/NPC/reward/combat/crafting/inventory runtime 실행이다. 이 downstream 범위의 다음 owner는 `MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES`이며 이번 Task에서 시작하지 않았다.

Editor 가시성은 Unity Test Runner의 9개 `MAP15_01` focused result와 Console/execute-code로 확인한 digest·dependency·priority 수치뿐이다. EditorWindow, overlay, inspector, generated visualization asset은 없다. 게임 가시성은 없다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSectorPlan.cs`

- `WorldSectorId`: stable row-major integer identity를 `SECTOR_000` 형식의 invariant token으로 제공한다.
- `WorldSectorCoordinate`: sector x/y와 13×13 bounds, row-major ID projection을 제공한다.
- `WorldSectorNode`: coordinate, primary biome token, RouteType, AccessClass, PacingRole, Special/Boundary/external-socket/world-start facts를 immutable snapshot으로 보관한다.
- `WorldDependencyEdge`: prerequisite `FromSector` -> dependent `ToSector`와 typed kind, reason, source owner를 stable comparable value로 보관한다.
- `WorldRetryEnvelope`: sector-local attempt cap, dependency rollback radius, typed abort, whole-world rerandom/RNG/fallback evidence를 보관한다.
- `WorldPlanInput`: node/edge enumeration을 defensive sorted read-only copy로 만들고 MAP14 phase-exit digest, publication label, mutation proof와 canonical input digest를 발행한다.
- `WorldSolveStep`: solve index, sector ID, priority, prerequisite sector IDs, reason digest를 immutable하게 발행한다.
- `WorldSolveFailure`: typed atomic failure code/subject/reason을 stable sort/equality contract로 제공한다.
- `WorldSolveOrderResult.Pass` / `Fail`: 성공 시에만 169 steps/input/output digest를 발행하고 실패 시 partial input/steps/digest를 발행하지 않는다.
- `WorldSolveDigest.ComputeInput`: world constants + MAP14 handoff + retry/mutation + row-major node + sorted edge facts -> lower-hex SHA-256 input digest를 만든다.
- `WorldSolveDigest.ComputeReason`: node + incoming dependency facts -> step reason digest를 만든다.
- `WorldSolveDigest.ComputeOutput`: input digest + all dependency facts + topological steps -> lower-hex SHA-256 output digest를 만든다.
- `WorldSolveDigest.IsLowerHexSha256`: public digest 형식을 검증한다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/WorldSolveOrderPlanner.cs`

- `WorldSolveOrderPlanner.Plan`: immutable `WorldPlanInput` -> validated dependency graph -> stable 169-step topological result를 만든다. cycle/invalid input에서는 partial plan 없이 typed failure만 반환한다.
- `WorldSolveOrderPlanner.Priority`: Special -> Mandatory/Boundary -> ExternalSocket -> pacing constraint -> ordinary terrain의 public priority를 계산한다.
- `Validate`: exact topology, ID/coordinate uniqueness와 bounds, node facts, MAP14 digest, edge endpoints/self/duplicate, required Special/Boundary/Mandatory dependency, retry envelope와 mutation proof를 누적 검증한다.
- `HasIncoming` / `MissingRequired`: required dependency kind의 presence와 typed missing failure를 만든다.
- `CompareReady`: priority rank -> dependency count descending -> stable constraint key -> sector ID의 deterministic tie-break를 제공한다.
- public handoff는 `DownstreamOwner = MAP15_02_INTEGRATE_INTERSECTOR_SOCKETS_AND_BOUNDARIES`, `OpensDownstreamTask = false`, `NewRngDrawCount = 0`으로 고정했다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/WorldSolveOrderPlannerTests.cs`

- `WorldPlanInputPublishesExact169SectorTopologyAndDigests`: topology/uniqueness/bounds/immutability/input-output digest를 검증한다.
- `SolveOrderContainsEachSectorExactlyOnce`: 169 sector가 정확히 한 번씩 0..168 step에 나타나는지 검증한다.
- `DependencyGraphIsAcyclicAndPrerequisitesPrecedeDependents`: 45 edges의 acyclic ordering과 prerequisite-before-dependent를 검증한다.
- `SpecialRouteBoundaryConstraintsHavePriorityReasons`: Special/Route/Boundary priority와 7 dependency kind actual count, reason digest를 검증한다.
- `SolveOrderIsDeterministicAcrossRepeatReverseAndCulture`: repeat/reverse/`tr-TR` input -> 동일 input/output/order digest를 검증한다.
- `RetryEnvelopeDoesNotExecuteRngOrWholeWorldRerandom`: attempt cap/radius/abort와 RNG/rerandom/fallback zero를 검증한다.
- `InvalidWorldInputsFailAtomicallyWithoutPartialPlan`: invalid topology/dependency/retry cases -> typed failure and empty partial payload를 검증한다.
- `WorldPlanDoesNotMutateSectorPlannerOrAuthoringAssets`: input node/edge/digest identity와 모든 mutation counter zero를 검증한다.
- `Map15HandoffKeepsMap15_02Locked`: downstream owner identity와 automatic open false를 검증한다.
- `ReferenceWorldPlanFixture`: approved `WorldGenConstants`, `AccessClass`, `PacingRole`, MAP14 public debug handoff digest -> deterministic test-only 169 nodes/45 edges/retry envelope를 만든다. production seed approval claim은 하지 않는다.

소비한 public authority는 `WorldGenConstants`의 624×416/48×32/13×13/169 topology, MAP09 `AccessClass`/`PacingRole` codecs, MAP05/MAP08/MAP13/MAP14 ownership labels와 MAP14 phase-exit debug digest `5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82`다. production 169-sector facts가 아직 공개되지 않은 범위에는 task-owned reference fixture만 사용했다.

신규 Runtime production C#/meta는 `2/2`, 신규 Runtime EditMode test C#/meta는 `1/1`이다. 기존 production/test/meta 수정 `0`, Editor production `0`, CSV/schema/cache/generated output `0`, Scene/Prefab/Tilemap/ScriptableObject `0`, asmdef/asmref/ProjectSettings/Packages `0`, upstream 수정 `0`이다. 신규 meta GUID occurrence는 각각 `1`이다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
category_names: [MAP15_01]
job_id: 07527bfd3ce84fd6ae4eeafd64019c7b
durationSeconds: 4.0400207
discovered: 9
executed: 9
passed: 9
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

첫 MCP test job `f3c30b453cb34326a820fceabc7ceb1c`은 Editor가 test initialization callback을 시작하기 직전에 adapter의 30초 init timeout이 먼저 닫혔다. 같은 category의 underlying Unity Test Runner 결과는 9/9 PASS였고, 연결 복구 후 init timeout 120초로 실행한 위 final authoritative job이 9/9 PASS를 정상 반환했다. test code failure나 regression selection은 없었다.

## Static and Workflow Verification

- single inbox candidate만 적용했고 installed Task/archive SHA-256은 모두 `6e942509e2a459854554176d4235cb28d871c6cdd9914713a9c81895a1105676`로 byte-identical이다.
- 시작 조건은 MAP14_10 Result PASS/SHA 일치, installed predecessor Task SHA 일치, MAP14_10 COMPLETE, MAP15_01 CURRENT, MAP15_02 LOCKED, unrelated staged `0`이었다.
- 신규 script validation diagnostic은 error/warning `0/0`, Unity compile error `0`, final clear 후 relevant Console error/warning `0/0`이다.
- 신규 Runtime/Test 파일은 `UnityEngine`, `UnityEditor`, `System.IO`, random/time/filesystem API를 사용하지 않는다. Tilemap/Scene/Prefab/GameObject 문자열은 mutation evidence property/test assertion에만 존재한다.
- 관련 없는 기존 worktree 변경은 수정하거나 stage하지 않는다.

Commit subject: `MAP15_01: implement world plan solve order`

Push: NOT PERFORMED
