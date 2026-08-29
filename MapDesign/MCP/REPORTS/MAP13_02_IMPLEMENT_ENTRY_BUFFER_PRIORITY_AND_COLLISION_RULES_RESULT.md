TASK: MAP13_02_IMPLEMENT_ENTRY_BUFFER_PRIORITY_AND_COLLISION_RULES
STATUS: PASS
MAP13_02: COMPLETE ELIGIBLE
MAP13_03_SEPARATE_FIXED_SHELL_SLOTS_AND_PERSISTENCE: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP13_01이 발행한 placed site bridge와 MAP11의 exact two-active-chunk Quiet candidate를 호출자 제공 배치 증거로 결합하는 immutable entry-buffer compiler를 추가했다. compiler는 위치 탐색, pool query, RNG, terrain render 또는 tile write를 수행하지 않는다. 선택된 Entry/Return port, MAP03 anchor, 내부 apron, Before/After Quiet 배치를 검증하고 정적 양방향 witness와 canonical digest만 발행한다.

focused fixture에서 Entry port는 `(5,5)/(0,1)/L`, Return port는 `(5,5)/(47,1)/R`이며 anchor exterior sector는 각각 `(4,5)`, `(6,5)`로 exact 일치했다. Entry apron은 minimum `4x4 = 16 cells`, Return apron은 larger `44x4 = 176 cells`, union은 `192 unique cells`, apron overlap은 `0`, fixed-shell/비-port slot overlap은 `0`이다. Before/After는 각각 MAP11 candidate의 `2 chunks / 192 cells`를 보존했고 region footprint, apron, Before, After 사이 금지 overlap은 모두 `0`이다. Entry/Return contact chunk는 각각 exact `1`이며 두 placement identity는 서로 다르다.

정적 witness는 `BeforeQuiet -> EntrySocket -> EntryApron -> RegionInterior`와 `RegionInterior -> ReturnApron -> ReturnSocket -> AfterQuiet`를 발행한다. synthetic edge, teleport, carve, tool requirement, one-way edge 및 runtime physics claim은 모두 `0`이다.

collision compiler는 exact priority `Boss 700 > Forge 600 > CoreResource 500 > Village 400 > RareRegion 300 > TerrainCluster 200 > ActivityStructure 100`을 적용한다. 전체 서로 다른 priority pair `21/21`에서 higher accepted/lower rejected를 확인했다. HardProtected overlap과 same-priority different-owner overlap은 atomic failure이며, committed lower와 later higher의 충돌은 기존 payload를 제거하지 않고 `RequiresReplan`을 발행한다. non-overlap은 양쪽 모두 accepted이고 global layer reorder 및 payload removal은 `0`이다.

추가·수정 script 전체 경로와 책임:

- `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionEntryBuffer.cs`: bridge/port/anchor/apron/MAP11 Quiet placement를 검증하고 immutable entry-buffer plan, chunk binding, bidirectional witness 및 digest를 발행한다.
- `Assets/_Game/Map/Runtime/WorldGeneration/SpecialRegions/SpecialRegionPlacementCollision.cs`: canonical occupancy claim을 검증하고 seven-level local collision decision, accepted/rejected/replan owner 집합 및 digest를 발행한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SpecialRegions/SpecialRegionEntryBufferCollisionTests.cs`: `MAP13_02` category에서 port/anchor, minimum/larger apron, two-chunk preservation, witness, 21-pair priority matrix, HardProtected/same-priority/committed conflict, overlap, determinism 및 atomic failure를 검증한다.

각 script의 matching `.meta`도 신규 추가했으며 기존 script/test/CSV/meta는 수정하지 않았다.

Editor/게임 가시성: 새 Editor window, inspector authoring UI, Scene, Prefab, Tilemap, GameObject 또는 gameplay visual은 없다. 이 변경은 pure runtime contract/decision data와 EditMode verification만 제공하므로 현재 게임 화면에서 보이는 변화는 없다.

미구현 기능: world placement search, Quiet pool selection/RNG, terrain/tile overwrite·delete·carve, collider/physics reachability, 실제 replan 실행, content authoring, MAP13_03 fixed-shell/slot/persistence 분리는 구현하지 않았다. MAP13_03은 계속 LOCKED이며 시작하지 않았다.

## Responsibility and Added Functions

| Field | Evidence |
|---|---|
| Task responsibility | mandatory Entry/Return + internal apron + MAP11 Quiet Before/After binding, static witness, exact priority/collision verdict |
| Added scripts | Runtime 2: `SpecialRegionEntryBuffer.cs`, `SpecialRegionPlacementCollision.cs`; focused test 1: `SpecialRegionEntryBufferCollisionTests.cs`; matching meta 3 |
| Added functions | `SpecialRegionEntryBufferCompiler.Compile`: caller plan validation -> immutable buffer plan; `SpecialRegionEntryBufferCanonicalDigest.Compute`: canonical plan -> SHA-256; `SpecialRegionPlacementCollisionCompiler.Compile`: claims -> local decisions/owner sets; `GetPriority`: owner kind -> exact numeric priority; `SpecialRegionPlacementCollisionCanonicalDigest.Compute`: collision plan -> SHA-256 |
| Added entry types | `SpecialRegionTileCoordinate`, `SpecialRegionEntryApron`, `SpecialRegionQuietChunkRole`, `SpecialRegionQuietChunkPlacement`, `SpecialRegionQuietBufferPlacement`, `SpecialRegionQuietChunkBinding`, `SpecialRegionEntryPortBinding`, `SpecialRegionBidirectionalWitness`, `SpecialRegionEntryBufferPlan`, request/error/result |
| Added collision types | `SpecialRegionPlacementOwnerKind`, `SpecialRegionOccupancyClaim`, `SpecialRegionCollisionKind`, `SpecialRegionCollisionDecision`, `SpecialRegionPlacementCollisionPlan`, request/error/result |
| Inputs consumed | MAP13_01 `SpecialRegionSiteBridge`, MAP03 `SiteEntryAnchor`, MAP11 `TerrainClusterQuietBufferCandidate`, explicit apron/chunk placements, explicit occupancy claims |
| Outputs produced | immutable entry-buffer plan, exact two-chunk bindings, static bidirectional witness, collision decisions, accepted/rejected/replan owner IDs, canonical digests |
| Explicit non-ownership | placement search/RNG, terrain writes, content, gameplay/physics, global layer reorder, replan execution, MAP13_03 |
| Downstream consumer | MAP13_03 may be unlocked only by a separate reviewed patch; it was not started here |

## Focused Verification

Final Unity verification:

```text
MODE: EditMode
FILTER TYPE: category
FILTER: MAP13_02
DISCOVERED: 12
EXECUTED: 12
PASSED: 12
FAILED: 0
SKIPPED: 0
INCONCLUSIVE: 0
UNITY SCRIPT COMPILE ERRORS: 0
RELEVANT CONSOLE ERRORS AFTER CLEAN COMPILE: 0
```

한 task-owned test assertion이 첫 focused 실행에서 지연 열거형에 `Has.Count`를 적용해 `11/12`였고, 해당 신규 테스트 파일만 수정했다. 이후 focused 실행은 `12/12`, adjacency/minimum-apron 보강 후 최종 focused 실행도 `12/12`였다. 실행된 test selection은 모든 회차에서 `category: MAP13_02`, `EditMode`뿐이다.

Negative fixture는 Optional access, minimum 위반 apron, After use 미지원 Quiet candidate, one-chunk placement, anchor와 교집합이 없는 RouteType `4` candidate, Before/After overlap, duplicate claim 및 missing input을 각각 atomic failure로 확인했다.

## Static Scope and Determinism

```text
NEW RUNTIME C#/META: 2/2
NEW FOCUSED TEST C#/META: 1/1
EXISTING C#/TEST/CSV/META MODIFICATIONS: 0
AUTHORING/GENERATED/SCENE/PREFAB/TILEMAP/SETTINGS/PACKAGES CHANGES: 0
NEW GUID DUPLICATES: 0
UNAPPLIED INBOX CANDIDATES: 0
DIFF CHECK ERRORS: 0
UNRELATED STAGED FILES: 0
RNG/TIME/FILESYSTEM/UNITY LIFECYCLE/STATIC MUTABLE CACHE IN RUNTIME: 0
WORLD OR TILE MUTATIONS: 0
GIT PUSH: NOT PERFORMED
```

Reverse input, repeat compile, caller collection mutation 및 `tr-TR` culture에서 entry-buffer/collision canonical digest가 동일했다. collections은 defensive-copy/read-only/canonical order이며 invalid input은 plan `null`, digest empty, sorted/deduped error를 발행했다.

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0

## Task and Commit Handoff

```text
INSTALLED TASK SHA-256: d46b1baa1ba721f78e5c03569e2bc2991c728dc93bb1569336afb5d1b0bfabfa
ARCHIVED TASK SHA-256: d46b1baa1ba721f78e5c03569e2bc2991c728dc93bb1569336afb5d1b0bfabfa
INSTALLED/ARCHIVED BYTE IDENTITY: PASS
STATUS FINALIZE TARGET: MAP13_02 CURRENT -> COMPLETE; Current Task -> NONE
ATOMIC COMMIT SUBJECT: MAP13_02: implement entry buffers and collision priority
PUSH: NOT PERFORMED
```

Commit SHA는 Result를 포함하는 atomic commit의 외부 증거이므로 commit 완료 후 최종 handoff에서 보고한다. 관련 없는 기존 untracked meta 3개는 수정·stage·commit하지 않는다.
