# MAP19_04 Validate Cluster Recovery and Density Result

TASK: MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY
STATUS: PASS

## User-Facing Implementation Report

이번 작업은 MAP19_02의 tile movement graph와 MAP19_03의 naked BFS/completion proof를 읽기 전용 입력으로 받아 terrain cluster의 복구 가능성과 생성 셀 밀도를 함께 검증한다. 기본 route 재합류, high route 복구, 명시적 recovery route, 2~5초 finite recovery, entry-to-spine, spine-to-exit, boundary socket 복구를 실제 `TerrainClusterRouteWitnessReport` binding과 graph 경로로 증명한다.

밀도 검증은 실제 sector canvas, 16개 generated slice, 1536개 cell provenance를 소비해 solid/reachable AIR 비율, contiguous 8x6 AIR window, unreachable pocket, head snag, one-way pit, hard-solid, 비-cluster overprotected envelope, boundary socket obstruction을 판정한다. reachable AIR는 MAP19_03 naked 시작점에서 도달 가능한 graph cell을 seed로 삼은 결정적 4방향 AIR flood로 계산한다. 따라서 열린 수직 공간을 graph stand 좌표 하나로 축소하지 않으면서도 격리 pocket은 데이터만으로 검출한다.

실패는 owner, reason, cluster/case ID, offending key, expected/actual, source digest와 frontier/window evidence를 canonical 순서로 게시한다. 하나라도 실패하면 recovery/density/combined success digest와 MAP19_05 handoff digest를 모두 비워 원자성을 유지한다. 성공 surface는 여섯 개의 lower-hex SHA-256 digest를 게시하지만 MAP19_05를 시작하거나 unlock하지 않는다.

이번 작업에서는 graph/proof/terrain mutation, runtime physics 또는 scene object 조회, player tuning, seed batch, event-removal/worst-case 검증, distance/revisit/pacing 측정, PlayMode 및 prior/legacy/full regression, MAP19_05 실행을 하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedClusterRecoveryDensityValidation.cs(.meta)` | recovery/density input, case, proof, failure, action-audit, aggregate surface와 deterministic digest 계약을 제공한다. | MAP19_02 graph와 MAP19_03 proof를 생성·수정하지 않고 MAP19_05를 시작하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Validation/GeneratedClusterRecoveryDensityValidator.cs(.meta)` | route-witness binding, graph-only finite recovery, AIR 연결성·8x6 window·shape/provenance obstruction, atomic failure를 검증한다. | Unity runtime/physics/scene/tilemap API, seed, 파일 I/O, player behavior를 소유하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Validation/GeneratedClusterRecoveryDensityValidatorTests.cs(.meta)` | 정확히 10개의 `MAP19_04` focused EditMode test로 성공, failure, atomicity, read-only 소비, 순서·문화권·cell 안정성, mutation sensitivity와 MAP19_05 lock을 증명한다. | prior category, PlayMode, legacy/full regression을 선택하지 않는다. 기존 public 계약으로 만든 graph/proof/route fixture는 test-only이다. |
| `MapDesign/MCP/TASKS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md` | 검증된 단일 inbox 후보의 byte-for-byte installed Task 사본이다. | Task 본문이나 다른 inbox 후보를 재작성·설치하지 않는다. |
| `MapDesign/MCP_ARCHIVE/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY.md` | 같은 후보의 byte-for-byte archive 사본이다. | MAP19_05 후보를 이동·설치하지 않는다. |
| `MapDesign/MCP/REPORTS/MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY_RESULT.md` | precondition, 실제 recovery/density 수치와 digest, focused Unity 결과, 금지 경계를 기록한다. | 미실행 회귀나 MAP19_05 완료를 주장하지 않는다. |
| `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` | PASS 확정 후 MAP19_04만 COMPLETE, Current Task를 NONE으로 finalize한다. | MAP19_05와 다른 LOCKED row를 변경하지 않는다. |

## Cluster Recovery and Density Summary

### Preconditions and task installation

- MAP19_03 Result SHA-256 required/actual: `184f11c2610577e4bd2851c21b6c33b9c667e50d2e8743851642989ea756c915` / `184f11c2610577e4bd2851c21b6c33b9c667e50d2e8743851642989ea756c915`
- MAP19_03 installed Task SHA-256 required/actual: `451cdacd8c595a09443c2fa7d432fc584c6b74881e6f39a8c52fe375d71ca4e1` / `451cdacd8c595a09443c2fa7d432fc584c6b74881e6f39a8c52fe375d71ca4e1`
- MAP19_02 graph digest reused: `bf6b682dd5797e6dd8cb469289ca26fc8db3f10175a99f77d6316c5afe65d063`
- MAP19_03 completion proof digest reused: `6bc3bf96b766337011fd08d3436fd46e12b30ae7b0af5cfc0598646e8e338fca`
- MAP19_04 incoming handoff digest reused: `694a70da6977d6cb7948976b8f2a512fa6ecf6c2d15f5f9f83d56dada59c9b2e`
- inbox candidates validated / legacy candidates: `1 / 0`
- MAP19_04 inbox/install/archive SHA-256: `7e6d29b179a839193c343a56bde7796db4f0385da8f955457837d2a4a7c4b188`
- installed/archive byte equality / inbox Markdown candidates after apply: `YES / 0`
- Current Task before/after apply: `NONE / MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY`
- MAP19_03 before apply: `COMPLETE`
- MAP19_04 before apply/task execution: `LOCKED / CURRENT`
- MAP19_05 before/after task execution: `LOCKED / LOCKED`
- unrelated staged files before apply: `0`
- Master task membership count / Master edits: `1 / 0`
- protocol-required Archive path is the only Phase A path outside the Task MCP write list: `YES`

### Required numeric and digest evidence

```text
clusters checked: 1
recovery cases checked: 7
base route recovery cases: 1
high route recovery cases: 1
explicit recovery cases: 1
2-5s recovery cases: 1
recovery cases passed: 7
recovery cases failed: 0
average recovery edge count: 2.8571428571428571428571428571
max recovery edge count: 6
recovery validation input digest lower-hex SHA-256: c7037e11b3c080a0cd987f77f03ca570de741c3b32f9fe6aff67670067c28fc2
recovery proof digest lower-hex SHA-256: b371b67691697eb719309f59131c4b85ec5c6402d64d7f183aec7d03ff1c92b3

density clusters checked: 1
solid ratio min/max: 13/13 basis points (0.13%/0.13%)
reachable air ratio min/max: 10000/10000 basis points (100%/100%)
8x6 AIR windows required/found: 1/1075
unreachable pockets found: 0
head snag candidates: 0
one-way pit candidates: 0
hard-solid obstructions: 0
overprotected envelope obstructions: 0
boundary socket obstructions: 0
density validation input digest lower-hex SHA-256: e40d0cace823a277dc25e20647181dfceaf56308b54809fd25598e82bd6eba0c
density validation digest lower-hex SHA-256: a33ccb810d8ec39168fe222958850176821792525563e472d7eac3ad07b25b11

cluster recovery+density combined digest lower-hex SHA-256: 6e393133118683be5104af6e4a4f2eff39cee61dc2c1dc8c240fba969512b816
MAP19_05 handoff digest lower-hex SHA-256: 0bdcc4ff5e24e91c11156a64b04acd18508cb42e4f26e72c90a5705793db7087

repeat digest mismatch count: 0
reverse cluster/order digest mismatch count: 0
culture digest mismatch count: 0
cell order digest mismatch count: 0
mutation sensitivity probes passed: 4/4 (recovery input / recovery proof / combined / MAP19_05 handoff)

missing proof/binding failure probes: 3/3 (MAP19_03 proof / cluster binding / recovery target)
digest mismatch failure probes: 1/1
unreachable recovery failure probes: 1/1
recovery bound failure probes: 2/2 (2-5s duration / graph edge bound)
density window failure probes: 1/1
pocket/head-snag/pit failure probes: 3/3
hard-solid/boundary obstruction failure probes: 3/3 (hard-solid / overprotected envelope / boundary socket)
forbidden API or mutation failure probes: 1/1
atomic failure success digests published: 0
atomic failure MAP19_05 handoff digests published: 0

cluster recovery/density validations run: 27 focused test invocations; 1 canonical success evidence surface
BFS/completion searches run: 11/11 test-only focused helper invocations; 0/0 production validator invocations
event removal/worst-case validations run: 0/0
distance/revisit/pacing measurements run: 0/0/0
seed batch runs: 0
production seed approvals: 0
PlayerController/Rigidbody/Collider/Physics2D behavior changes: 0/0/0/0
Physics2D queries/simulations: 0/0
runtime objects spawned: 0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
System.IO file write/read calls: 0/0
PlayerPrefs writes/reads: 0/0
Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
Scene/Prefab/Tilemap mutation: 0/0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
optimization rewrites/broad refactors: 0/0
MAP19_05 started: NO
```

Production validator는 공급된 graph/naked/completion/route-witness/canvas/slice 객체를 참조로 보존하고 정렬된 공개 collection만 읽는다. focused test는 동일한 MAP19_02 public fixture graph를 cache하고 MAP19_03 pure-data naked/completion search를 각 test scenario의 local evidence helper로만 호출했다. production graph builder, naked BFS 또는 completion search 호출은 모두 `0`이다.

### Focused Unity verification

- Unity Editor version: `6000.3.8f1`
- execution surface: already-running live Editor for this exact worktree; no headless runner was created or used
- mode/category: `EditMode / MAP19_04`
- discovered / executed / passed / failed / skipped / inconclusive: `10 / 10 / 10 / 0 / 0 / 0`
- final job id / result: `c011ff683ca742bfb0ab1a7f9501fd40 / Passed`
- final duration: `3.9036436 seconds`
- exact required test names present: `10 / 10`
- compile errors: `0`
- relevant Console errors after final clear/recheck: `0`
- final run failures: `0`

```text
REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

## No Seed Regression or Physics Boundary Notes

Production implementation은 `UnityEngine`, `UnityEditor`, `System.IO`, Physics2D, GameObject, Transform, MonoBehaviour, Tilemap, Collider, Rigidbody, NavMesh, Scene, Prefab, Camera, Addressables, Resources, AssetDatabase, PlayerPrefs, Unity Input, PlayerController, SetTile/SetTiles/SetTilesBlock/ClearAllTiles/CompressBounds, Instantiate, Destroy API를 참조하지 않는다. 필수 계약명 `ClusterRecoveryValidationInput`과 `ClusterDensityValidationInput`의 `Input`은 Unity Input API가 아니며 외부 상태를 읽지 않는다.

Recovery 검증은 허용 movement edge와 명시적 socket link만 순회하고 실제 route witness의 baseline/high/recovery/entry/exit binding을 검사한다. Density 검증은 generated cell/provenance의 read-only snapshot만 사용한다. graph, proof, tilemap, scene, prefab, collider, rigidbody, player tuning 또는 seed 상태를 변경하지 않는다.

No broader verification trigger was detected. Prior task category, legacy 19347, PlayMode, unfiltered/full regression은 모두 선택하지 않았다. event removal, worst-case, distance/revisit/pacing, seed approval, runtime spawning과 MAP19_05는 범위 밖이며 실행하지 않았다. MAP19_05는 `LOCKED / NOT STARTED`다.

## Final Status Evidence

- Result decision: `PASS`
- PASS condition audit: predecessor hashes 일치; exact graph/completion/handoff digest 재사용; base/high/explicit/2~5s 및 entry/spine/exit/socket recovery 7/7 통과; density 성공 source의 8x6 AIR window 1075개 및 obstruction 0; deterministic failure atomicity; 여섯 digest 안정성·민감도; focused tests 10/10 PASS; compile/relevant Console error 0; forbidden boundary와 wider selections 0
- expected status finalize: `MAP19_04_VALIDATE_CLUSTER_RECOVERY_AND_DENSITY = COMPLETE`
- expected Current Task after finalize: `NONE`
- expected MAP19_05 status after finalize: `LOCKED`
- atomic commit subject: `MAP19_04: validate cluster recovery and density`
- git push: `NOT PERFORMED`
