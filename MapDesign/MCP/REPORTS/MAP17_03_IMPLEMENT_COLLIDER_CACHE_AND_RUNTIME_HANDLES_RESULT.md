# MAP17_03 Implement Collider Cache and Runtime Handles Result

TASK: MAP17_03_IMPLEMENT_COLLIDER_CACHE_AND_RUNTIME_HANDLES
STATUS: PASS

## User-Facing Implementation Report

이번 Task는 MAP17_02의 7개 logical layer와 10752개 bake record를 읽어, 실제 Unity Collider를 만들지 않는 순수 데이터 충돌 계획으로 변환했다. Terrain의 solid/ground/blocked, Affordance의 traversable/ground, Hazard의 hazard/blocked, Protection의 protected 의미만 기존 layer semantic에서 분류하며 임의의 Tile 의미를 새로 발명하지 않는다. 48x32 occupancy mask 5종과 결정적인 가로 run span을 발행하고, 실제 실행 주체가 아닌 후속 adapter가 소비할 command만 계획한다.

collider cache key는 geometry, MAP17_02 bake/seam, registry, sector, generator/data version, mutation revision, collision policy를 묶는다. cache snapshot은 entry를 key digest 순서로 정렬해 insertion order와 무관한 digest를 만들고 hit/miss/invalidate/evict 누계를 새 immutable snapshot으로 반환한다. bake, seam, mutation revision, policy 중 하나가 바뀌면 기존 key와 일치하지 않는다.

runtime handle은 `Unloaded`, `Preloaded`, `Active`, `SleepingModified` 네 상태와 문서화된 7개 전이만 허용한다. `Active -> SleepingModified`는 clean cache key를 invalidate하고 증가한 dirty revision/reason을 메모리에 보존한다. durable save는 수행하지 않으며 MAP17_05의 책임으로 남겼다. `SleepingModified -> Active`는 같은 sector와 upstream digest를 유지하면서 dirty revision에 맞춰 다시 생성된 cache entry가 있어야 한다. 따라서 MAP17_04는 scene/streaming 구현 전에 검증된 pure-data handle과 cache evidence를 입력으로 받을 수 있다.

이번 범위에서 Tilemap, Collider, Rigidbody, Physics2D, Scene, Prefab, GameObject, 파일 저장을 실행하지 않았다. 회귀 trigger는 발견되지 않았고 `MAP17_03` EditMode category만 1회 실행해 10/10 PASS했다. MAP17_04는 열거나 실행하지 않았다.

## Responsibility and Added Scripts

| Script or file | Added or changed responsibility | Explicit non-ownership |
|---|---|---|
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedColliderCachePlan.cs` | collision mask kind/occupancy, integer span, adapter command, rebuild request/plan/result/failure와 canonical digest를 정의한다. | Unity Collider/Tilemap/Rigidbody/Physics 실행, Scene 또는 asset mutation을 소유하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedColliderCache.cs` | MAP17_02 bake의 완전성을 검증하고 mask/span/command를 atomic하게 계획하며 stable cache key/entry/snapshot과 hit/miss/invalidate/evict 연산을 제공한다. | 실제 collider rebuild, streaming window, cache 파일 저장을 수행하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorRuntimeHandle.cs` | stable handle ID, 네 runtime state, immutable handle, transition request/record/result/failure와 handle digest를 정의한다. | runtime GameObject, camera, save/load storage, stable spawn ID를 소유하지 않는다. |
| `Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedSectorRuntimeHandleLifecycle.cs` | 문서화된 7개 전이, stale key/sector/revision 검증과 dirty 전이 시 cache invalidation을 순수 데이터로 수행한다. | MAP17_04 preload/active 반경과 preactivation, MAP17_05 durable modification storage를 구현하지 않는다. |
| `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Baking/GeneratedSectorRuntimeHandleTests.cs` | 정확히 10개의 `MAP17_03` focused test로 mask/span/cache/digest/state/side-effect/handoff 계약을 검증한다. | prior category, PlayMode, legacy, unfiltered, full regression을 선택하지 않는다. |
| matching `.meta` 5개 | 4개 production C# asset과 1개 focused test asset의 Unity GUID를 보존한다. | Scene, Prefab, Tile, generated data asset을 만들지 않는다. |

## Patch Apply and Preconditions

```text
single MCP_INBOX candidate: 1/1
candidate/task/sets_current identity: PASS
MAP17_02 status before apply: COMPLETE
MAP17_03 status before/after apply: LOCKED/CURRENT
MAP17_04 status before/after execution: LOCKED/LOCKED
Current Task before apply: NONE
unrelated staged files before apply: 0
MAP17_02 Result required/actual SHA-256:
8e3c6e7c61d0e359d91085de7c71e6fc92f8f95cfa0b460610d1c5d33038dc19
MAP17_02 installed Task required/actual SHA-256:
78ce28910ba94eb56b8e77ebd93b2adeb91fb5db7c82dec05170e8822f8eb57b
MAP17_03 inbox/installed/archive SHA-256:
a7f82c314787dc864e5cc095c4d602980810f7edeafc1be2f53862ca20d7262a
installed/archive byte equality: YES
Phase A status delta: COMPLETE 0 / CURRENT +1 / LOCKED -1
```

## Collider Cache and Runtime Handle Evidence

```text
MAP17_02 logical bake digest reused:
139465f70d40e6b9a3fdd4bb55696c38e89d1856912f4bec2644edb4c6b47602
MAP17_02 seam report digest reused:
d1a1febd5c9c10481817e5e6c027071fe2890bf2ea79c34252c2a7caaedc7fda
source logical layer count observed: 7/7
source layer records consumed: 10752/10752
source sector cells covered: 1536/1536
source seam pairs preserved: 928/928
source socket references preserved: 64/64
source marker slots preserved: 24/24

collider mask kinds published: 5
solid mask cells: 768
platform mask cells: 715
hazard mask cells: 0
protection mask cells: 4
debug/non-colliding mask cells: 53
non-colliding/ignored records: 9265
mask duplicate/out-of-bounds cells: 0/0
collider spans published: 66
span cells published: 1540
span cells match masks: YES
span out-of-bounds cells: 0
adapter commands planned: 55
adapter commands executed: 0

cache key lower-hex SHA-256: YES
cache key digest: e5804aba97511cf73c080ac325d7a428915732981944fcbe1e83c1b0b334c5ca
collider rebuild plan digest lower-hex SHA-256: YES
collider rebuild plan digest: 2ab1b2fa4ca7f7c8e57dbf62456cc5c8f3faa43854c600e7b1c8f7a3ed02e599
runtime handle digest lower-hex SHA-256: YES
runtime handle digest: 0c4ea997c35c04d9386d96e41611cffe9b5b3a9006a2b94222d5883cf8279331
cache snapshot digest: 79ec30a4e291298d2bccf09ac1f4bf73cde9f3af3ac82b1e0182f3165f930cb1
cache hit/miss/invalidate/evict probes: 1/1/1/1
repeat/reverse/culture/cache-order digest mismatches: 0/0/0/0
mutation sensitivity probes passed: 2/2

runtime handle states published: Unloaded/Preloaded/Active/SleepingModified
allowed transition probes passed: 7/7
forbidden transition probes passed: 5/5
stale cache key transition failures: 1
mismatched sector transition failures: 1
dirty revision preserved in SleepingModified: YES (revision 1, PLAYER_MUTATION)
durable save writes: 0

Unity Tilemap component writes: 0
Tilemap.SetTile/SetTiles/SetTilesBlock/ClearAllTiles calls: 0/0/0/0
TilemapCollider2D/CompositeCollider2D/Collider2D creations: 0/0/0
Rigidbody2D creations: 0
Physics2D queries/simulations: 0/0
Scene/Prefab/Tilemap mutation: 0/0/0
GameObject/Prefab instantiation: 0/0
Generated CSV/assets committed: 0
stable spawn ids created: 0
runtime objects spawned: 0
production seed approvals: 0
MAP17_04 started: NO
```

## Focused Verification

```text
Unity version: 6000.3.8f1
mode: EditMode
category_names: [MAP17_03]
discovered: 10
executed: 10
passed: 10
failed: 0
skipped: 0
inconclusive: 0
duration seconds: 15.53
compile errors after final compile: 0
relevant Console errors after final compile/test cursor: 0
Scene/Prefab Changes: NONE

REGRESSION TRIGGER DETECTED: NO
MAP17_03 FOCUSED EDITMODE RUNS: 1 (10/10 PASS)
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
FULL REGRESSION RUNS: 0
```

구현 중 첫 compile check는 새 test의 constructor named argument 3곳을 잘못 표기한 것을 발견했다. production invariant 문제는 아니었으며 해당 test source만 수정한 뒤 최종 compile은 오류 0, focused run은 10/10 PASS, test 시작 cursor 이후 relevant Console error는 0이었다.

## Static and Write-Boundary Verification

- required focused test names present: 10/10, 각 1회
- production source의 `UnityEngine`, `UnityEditor`, Physics/Scene/file I/O 의존: 0
- collision 분류는 기존 logical layer와 `FinalCanvasCellKind`/protection 값만 사용
- 모든 새 production/test C# asset에 matching `.meta`가 있으며 새 GUID 중복: 0
- Scene/Prefab/Tilemap changed files: 0/0/0
- task-owned source/status의 `git diff --check`: PASS
- MAP17_04 status: `LOCKED`, execution: NOT STARTED
- 기존 `Constant.slnx`, TerrainClusters meta 파일들, root repair instruction, PRE-MAP17 report는 수정하거나 stage하지 않음
- Git push: 0

MAP17_03은 위 evidence가 PASS이므로 이 Result 작성 후에만 Status Finalize와 atomic commit을 수행한다. MAP17_04는 자동 시작하지 않는다.
