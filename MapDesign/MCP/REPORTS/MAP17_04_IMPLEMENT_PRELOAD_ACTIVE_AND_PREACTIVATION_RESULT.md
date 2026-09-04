# MAP17_04 Implement Preload Active and Preactivation Result

TASK: MAP17_04_IMPLEMENT_PRELOAD_ACTIVE_AND_PREACTIVATION
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 13×13 world sector 안에서 현재 중심 sector를 기준으로 Chebyshev 거리 3인 7×7 preload window와 거리 2인 5×5 active window를 계산하는 순수 데이터 planner를 추가했다. 가운데에서는 각각 49/25개, corner에서는 16/9개, 한쪽 edge에서는 28/15개로 world 경계에 맞게 clamp되며 active membership은 항상 preload membership의 부분집합이다.

preactivation은 Camera나 player transform을 직접 읽지 않는다. 호출자가 전달한 sector-local normalized progress와 방향 hint를 low/high threshold `0.12/0.88`, hysteresis `0.04`에 적용해 이웃 sector를 candidate로 표시할 뿐이다. corner 접근은 직교 이웃 둘과 대각 이웃 하나를 합쳐 최대 3개 candidate를 만들 수 있고 world 밖 candidate는 제거한다. candidate에는 MAP17_03 cache key와 기대 state가 포함되지만 실제 Scene activation 실행 수는 0이다.

이전/다음 window 비교는 add/remove preload, promote/demote active, active/preload preserve, evict candidate, SleepingModified preserve를 결정적으로 분류한다. MAP17_03 lifecycle을 그대로 호출하는 immutable transition batch는 `Unloaded -> Preloaded -> Active` 순서를 보장하며 direct `Unloaded -> Active`를 허용하지 않는다. window 밖으로 나간 `SleepingModified` handle은 `Unloaded`로 전이해도 dirty revision과 reason을 계속 보존하고 durable save는 MAP17_05 책임으로 남긴다.

첫 focused run은 9/10으로, transition 순서를 row-major 전체 batch가 아니라 한 active sector의 두 record라고 가정한 test assertion 한 곳이 실패했다. production 계약은 유지한 채 실제 계약인 “모든 Active 전이는 Preloaded 또는 SleepingModified에서 시작”을 검증하도록 test만 수정했다. 이후 동일 category의 두 번의 run이 모두 10/10 PASS했다. prior category, PlayMode, legacy, unfiltered, full regression은 실행하지 않았고 MAP17_05는 열거나 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorStreamingWindow.cs` | 13×13 sector coordinate, window member/kind, request/snapshot, preactivation candidate, window diff/change, transition batch/result/failure와 canonical digest를 정의한다. | Scene object, Camera, Tilemap, Collider, loader 또는 save storage를 소유하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorPreactivationPolicy.cs` | normalized local progress, cardinal/diagonal direction, low/high threshold와 hysteresis를 deterministic candidate intent로 변환하고 world edge를 clamp한다. | Camera/Cinemachine을 읽거나 GameObject를 활성화하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorWindowPlanner.cs` | 7×7/5×5 membership, handle/cache 완전성, preactivation candidate, 이전/다음 diff와 MAP17_03 lifecycle transition batch를 atomic하게 계획한다. | asset loading, streaming thread/job, Scene/Prefab/Tilemap mutation, durable save/load를 실행하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSectorWindowPlannerTests.cs` | 정확히 10개의 `MAP17_04` focused test로 center/edge/corner, preactivation, diff, dirty handoff, lifecycle, digest와 failure boundary를 검증한다. | prior category, PlayMode, legacy/unfiltered/full regression을 선택하지 않는다. |
| matching `.meta` 4개 | 3개 production C# asset과 1개 focused test asset의 Unity GUID를 보존한다. | Scene, Prefab, Tile 또는 generated data asset을 만들지 않는다. |

## Patch Apply and Preconditions

```text
single MCP_INBOX candidate: 1/1
candidate/task/sets_current identity: PASS
MAP17_03 status before apply: COMPLETE
MAP17_04 status before/after apply: LOCKED/CURRENT
MAP17_05 status before/after execution: LOCKED/LOCKED
Current Task before apply: NONE
unrelated staged files before apply: 0
authoritative Master membership: 1/1 in MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
MAP17_03 Result required/actual SHA-256:
f7decc2c2cfd0e2473e7966201403eaf5ecf5998f6b9affe224321f3356bf573
MAP17_03 installed Task required/actual SHA-256:
a7f82c314787dc864e5cc095c4d602980810f7edeafc1be2f53862ca20d7262a
MAP17_04 inbox/installed/archive SHA-256:
4ceadbe998821f206ea33ba90b52fc5c7fd719b618d4282da619f5fdbdfc98c0
installed/archive byte equality: YES
Phase A status delta: COMPLETE 0 / CURRENT +1 / LOCKED -1
```

## Streaming Window and Transition Evidence

```text
MAP17_03 cache key digest reused:
e5804aba97511cf73c080ac325d7a428915732981944fcbe1e83c1b0b334c5ca
MAP17_03 collider rebuild plan digest reused:
2ab1b2fa4ca7f7c8e57dbf62456cc5c8f3faa43854c600e7b1c8f7a3ed02e599
MAP17_03 runtime handle digest reused:
0c4ea997c35c04d9386d96e41611cffe9b5b3a9006a2b94222d5883cf8279331
source runtime handle states observed: Unloaded/Preloaded/Active/SleepingModified
source allowed transitions reused: 7/7
source collider cache entries observed: 169
source sector coordinates observed: 169/169

world sectors observed: 169/169
middle preload window count: 49/49
middle active window count: 25/25
corner preload window count: 16/16
corner active window count: 9/9
edge preload/active window counts: 28/15
active subset of preload: YES
active-only outside preload: 0
duplicate preload/active members: 0/0
out-of-world preload/active members: 0/0

preactivation policy threshold/hysteresis published: 0.12/0.88/0.04
preactivation direction probes passed: 4/4
diagonal approach candidates: 3/3
preactivation candidates inside valid window: 3/3
world-edge candidates clamped: 3
executed scene activations: 0
camera/cinemachine integration: 0/0

window diff add/remove/promote/demote probes: 7/7/5/5
window diff preserve active/preload probes: 20/12
window diff evict probes: 7
SleepingModified preserve diff probes: 1
initial transition plan records published/success/failed: 74/74/0
shifted-window transition plan records published: 24
Unloaded -> Preloaded transitions: 49
Preloaded -> Active transitions: 25
transition plan execution side effects: 0
forbidden Unloaded -> Active transition probes passed: 1/1
invalid center/missing cache/missing handle failure probes: 1/1/1
atomic partial window/diff/transition results: 0/0/0
SleepingModified dirty revision preserved: YES (revision 1, PLAYER_MUTATION)
SleepingModified final out-of-window state: Unloaded
durable save writes: 0

window snapshot digest lower-hex SHA-256: YES
window snapshot digest: cb3bd4d7037ced7745cb7080e2e80c35057770e9fa2278743360f659373be07a
window diff digest lower-hex SHA-256: YES
window diff digest: fa5e1f6ddedc374a0399b6fd5c04d5cfb2939e24bc2c03f4f49a91713c47ec2b
shifted-window diff digest: d559696b16f7ffe46cfb6092ca8ae998b183fc2ca608aea43a880bc8206eab88
transition plan digest lower-hex SHA-256: YES
transition plan digest: 4276889b5ba3af471505d26181b902d471e4a6198392afce9c5890b684333489
repeat/reverse/culture/handle-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed: 3/3

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
GameObject instantiate/enable/disable/destroy: 0/0/0/0
Addressables/Resources/AssetDatabase loads: 0/0/0
Generated CSV/assets committed: 0/0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_05 started: NO
```

## Focused Verification

```text
Unity version: 6000.3.8f1
mode: EditMode
category_names: [MAP17_04]
final discovered: 10
final executed: 10
final passed: 10
final failed: 0
final skipped: 0
final inconclusive: 0
final duration seconds: 10.8830682
compile errors after final compile: 0
relevant Console errors after final compile/test cursor: 0
relevant Console warnings after final test cursor: 0
unrelated compile/reload warnings: 2 (legacy empty asmdef, Editor non-automated mode)
Scene/Prefab Changes: NONE

REGRESSION TRIGGER DETECTED: NO
MAP17_04 FOCUSED EDITMODE RUNS: 3 (initial 9/10 + final verification 10/10 + final evidence 10/10)
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

초기 production compile 요청은 domain reload 중 Pipeline 연결이 한 번 재시작되며 완료 대기 timeout을 반환했지만 Editor는 정상 복구됐다. 이후 test 포함 compile을 세 번 확인해 모두 오류 0이었고, 마지막 compile/reload에서 기존 legacy empty asmdef 및 non-automated Editor 경고 2건만 확인했다. 최종 test 시작 cursor 이후 warning/error는 모두 0이었다.

## Static and Write-Boundary Verification

- required focused test names present: 10/10, 각 1회
- production source의 `UnityEngine`, `UnityEditor`, Scene/Physics/file I/O 및 Camera/asset-load API 의존: 0
- 모든 좌표, window, diff, candidate, transition은 immutable pure-data contract
- 모든 새 production/test C# asset에 matching `.meta`가 있으며 새 GUID 중복: 0
- Scene/Prefab/Tilemap changed files: 0/0/0
- task-owned tracked source/status의 `git diff --check`: PASS
- installed/archive Task는 원본의 기존 EOF blank를 포함해 byte-for-byte SHA 계약을 그대로 보존
- MAP17_05 status: `LOCKED`, execution: NOT STARTED
- 기존 `Constant.slnx`, TerrainClusters meta 파일들, root repair instruction, PRE-MAP17 report는 수정하거나 stage하지 않음
- Git push: 0

MAP17_04는 위 evidence가 PASS이므로 이 Result 작성 후에만 Status Finalize와 atomic commit을 수행한다. MAP17_05는 자동 시작하지 않는다.
