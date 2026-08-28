```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
  task_file: TASKS/MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES.md
  requires_current_task: NONE
  requires_completed_task: MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE
  requires_result:
    path: REPORTS/MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE_RESULT.md
    status: PASS
    sha256: 5d92d816fbe6570a75d76d89554a8b8c5a780236bf737a187f635c8bc043a8c1
  requires_installed_task:
    path: TASKS/MAP11_03_COMPILE_ROUTE_SPINE_AND_TRAVERSAL_ENVELOPE.md
    sha256: cbd56cd697bc0674c1e58f3fef47e14ca52bf18a696d5d7c118b55d41f3b177f
  sets_current_task: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
```

# MAP11_04 — Implement Base, High, and Recovery Routes

```text
TASK: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. User-Meaning Summary

이번 Task가 끝나면 TerrainCluster는 4×4 MicroPattern이 하나도 적용되지 않은 상태에서도 다음 구조적 증거를 가진다.

```text
Entry에서 Exit까지 통과하는 기본 경로
선택적으로 올라가는 고점 경로
고점 경로 실패 후 기본 경로로 돌아오는 2~5초 복구 경로
기본 이동을 성립시키는 최소 Solid/Air Static Shell
```

이는 실제 Unity 물리 플레이 검증이 아니라 compiled graph와 authored timing evidence를 사용하는 deterministic route witness다. 최종 물리·타일 검증은 MAP16/MAP19에서 다시 수행한다.

## 1. Responsibility

| 소유 | 소유하지 않음 |
|---|---|
| pattern-removed 최소 Static Shell | MicroPattern 적용 |
| baseline Entry→Exit path witness | 새 Spine/edge 저작 |
| high route intent/path/benefit 검증 | 실제 보상·아이템 배치 |
| 2000~5000ms recovery witness | 플레이어 물리 시뮬레이션 |
| immutable route report/digest | starter 16종 콘텐츠 |

흐름:

```text
MAP11_03 compiled traversal
→ pattern-removed static shell
→ baseline path witness
→ high-route witness
→ recovery-to-base witness
→ MAP11_05 pattern-safe working canvas input
```

## 2. No-Regression Policy

정상 실행은 category `MAP11_04`만 선택한다.

```text
MAP11_04 focused selection: required
Prior MAP09/MAP10/MAP11_01~03 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

실제 trigger:

- compile/Console error가 기존 authority에서 발생
- MAP11_03 graph/protection behavior drift
- 기존 production/test/CSV/meta 예상 밖 변경
- asmdef/GUID/namespace/authority 위반

Task-owned 코드·fixture 문제는 task-owned 파일만 고치고 `MAP11_04`만 재실행한다. 기존 authority 결함이면 이전 파일을 수정하지 말고 owner·원인·최소 범위를 보고한 뒤 `STATUS: BLOCKED`로 STOP한다.

## 3. Read-Only Authorities

Preflight에서 exact 확인한다.

1. MAP11_03 Result status/SHA와 installed/archive Task SHA
2. MAP11_04만 CURRENT, MAP11_05 LOCKED, inbox candidate 0
3. MAP11_01 Active/Inactive Local Canvas와 exact tile lookup
4. MAP11_02 Entry/Exit/Recovery role anchors and variant-node links
5. MAP11_03 compiled variants/nodes/edges/seven envelopes/protected provenance
6. exact one baseline SpineVariant from MAP09_04 authority
7. movement kinds `Walk/Jump/Drop/Climb/Slide/Bounce`
8. Authoring `52`, MicroPattern `24/453`, Generated CSV 0
9. compile/Console, meta/GUID, dirty/staged paths

다음이면 `BLOCKED`다.

- predecessor mismatch 또는 MAP11_03 미완료
- Local Canvas/role socket/traversal compilation identity·digest 불일치
- 기존 graph/protection authority 수정 없이는 구현 불가
- task allowlist가 사용자 변경과 겹침

## 4. Exact Write Boundary

신규 파일만 허용한다.

```text
Runtime:
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterStaticShell.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRouteWitness.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterRouteWitnessCompiler.cs(.meta)

Focused test:
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterRouteWitnessCompilerTests.cs(.meta)

Namespace:
StarNight.Map.WorldGeneration.TerrainClusters

Assembly:
Game.Map.Runtime / Game.Map.Tests.EditMode
```

같은 책임을 더 적은 신규 C# 파일로 구현할 수 있다. 기존 MAP00~MAP11_03 production/test/CSV/meta 파일은 수정하지 않는다. 실제 inventory와 public surface를 Result에 기록한다.

## 5. Pattern-Removed Static Shell

Static Shell은 final terrain이 아니라 MicroPattern이 제거된 상태의 최소 immutable geometry snapshot이다.

exact occupancy:

```text
Inactive: Local Canvas inactive tile; shell 밖
Air:      active tile의 기본값
Solid:    compiled Floor protection이 요구한 tile
```

active tile 전체를 exact 한 번 게시한다.

1. 모든 Active Local Canvas tile을 explicit `Air`로 시작한다.
2. MAP11_03의 모든 compiled `Floor` tile을 `Solid`로 설정한다.
3. `Centerline/Clearance/JumpArc/DropColumn/Landing/Recovery`는 explicit `Air`를 요구한다.
4. 같은 tile이 RequiredSolid와 RequiredAir를 동시에 요구하면 atomic failure다.
5. Inactive tile에는 occupancy를 게시하거나 암묵적 Solid를 만들지 않는다.

각 cell 최소 필드:

```text
compiled LocalTileCoord
owning ClusterChunkCoord
Air | Solid
source variant/edge/envelope provenance
protected-open fact
```

- pattern operation count는 exact `0`이다.
- Surface/Affordance/Material/Hazard/Marker payload는 아직 만들지 않는다.
- shell은 모든 variant 요구의 union을 보존한다.
- 같은 tile의 동일 요구는 coalesce하며 모든 unique provenance를 보존한다.

## 6. Route Witness Intent

starter content는 MAP11_07에서 저작하므로, 이번 Task는 immutable intent 모델과 compiler/validator를 구현한다.

input intent:

```text
baseline SpineVariantId
high-route definitions 1+
edge estimated duration evidence in integer milliseconds
designated high-route failure nodes
recovery target base-route node or Recovery role node
```

timing evidence는 actual player physics가 아니라 authored estimate다.

- edge duration은 integer `>0` ms다.
- float seconds, clock, frame time, animation state를 사용하지 않는다.
- timing provenance에 variant ID, edge ID, ruleset ID를 기록한다.
- MAP19 actual traversal profile 검증 전에는 물리적 확정값으로 승격하지 않는다.

## 7. Baseline Route Witness

baseline variant는 source authority의 exact baseline variant와 같아야 한다.

compiler는 stable BFS로 Entry node→Exit node path를 선택한다.

deterministic ordering:

```text
minimum edge count
then edge ID ordinal sequence
```

baseline witness 최소 필드:

```text
baseline variant ID
Entry/Exit role, port, node identity
ordered node IDs
ordered edge IDs and MovementKinds
compiled coordinates
total estimated duration ms
covered protected tiles
pattern operation count = 0
```

규칙:

- path는 contiguous directed edges다.
- Entry port→Entry role→Entry node에서 시작한다.
- Exit node→Exit role→Exit port에서 끝난다.
- 모든 mandatory BuildUp/Core/Recovery role requirement를 source graph 의미대로 보존한다.
- shell의 Floor/Air 요구와 충돌하지 않는다.
- path가 없거나 disconnected/mismatched면 publish하지 않는다.

## 8. High-Route Witness

high route definition 최소 필드:

```text
stable high-route ID
SpineVariantId
base divergence node ID
ordered directed edge IDs
base rejoin node ID
designated high-point node ID
stable benefit IDs
designated failure node IDs
```

benefit은 이번 Task에서 gameplay enum을 새로 만들지 않고 stable ID로 보존한다.

```text
Benefit ID grammar: ^BENEFIT_[A-Z0-9_]+$
Distinct benefit IDs per high route: >=2
```

예시 ID는 test fixture에서만 `BENEFIT_HEIGHT_ADVANTAGE`, `BENEFIT_REWARD_ACCESS`처럼 사용할 수 있다. 실제 콘텐츠 의미는 MAP11_07 authoring 책임이다.

검증:

- high route는 baseline path의 divergence node에서 시작한다.
- ordered edges는 contiguous하고 rejoin node에 도달한다.
- 적어도 한 edge 또는 node sequence가 baseline subpath와 구조적으로 달라야 한다.
- high-point node는 해당 high path에 존재한다.
- distinct benefit ID가 2개 이상이다.
- failure node는 high path 위에 있고 Entry/Exit가 아니다.
- source compiled graph 밖 ID, duplicate edge, broken direction을 거부한다.

좌표 y만 보고 “높음”을 추론하지 않는다. authored high-point designation과 alternate route structure를 증거로 사용하며 실제 높이/물리는 MAP16/MAP19가 재검증한다.

## 9. Recovery-to-Base Witness

각 designated high-route failure node에서 baseline path의 node로 directed recovery path가 있어야 한다.

projected Recovery role node는 그 node가 baseline path 위에 있을 때 우선 target으로 사용할 수 있다. baseline 밖 Recovery node에서 멈추는 것은 복귀 완료로 계산하지 않는다.

recovery path selection:

```text
minimum total estimated milliseconds
then minimum edge count
then edge ID ordinal sequence
```

exact duration gate:

```text
minimum: 2000 ms inclusive
maximum: 5000 ms inclusive
```

각 witness 최소 필드:

```text
high-route ID / failure node
ordered recovery nodes/edges
MovementKinds
target base/recovery node
total estimated duration ms
rejoined baseline node identity
compiled coordinate/protection provenance
```

- 1999ms 이하와 5001ms 이상을 거부한다.
- recovery는 Exit로 순간 이동하거나 좌표를 생략할 수 없다.
- source graph에 없는 synthetic edge를 만들지 않는다.
- 모든 designated failure node가 valid witness를 가져야 한다.
- recovery path도 Static Shell 요구와 충돌하지 않아야 한다.

## 10. Publication, Errors, and Digest

최소 semantic surface:

```text
TerrainClusterShellOccupancy
TerrainClusterStaticShellCell / StaticShell
TraversalEdgeDurationEvidence
TerrainClusterRouteWitnessIntent
TerrainClusterBaselineRouteWitness
TerrainClusterHighRouteWitness
TerrainClusterRecoveryRouteWitness
TerrainClusterRouteWitnessReport
TerrainClusterRouteWitnessCompileError / Result
TerrainClusterRouteWitnessCompiler
```

기존 naming 충돌 시 의미를 보존하는 최소 이름 조정은 가능하다.

publication rules:

- all collections defensive copy/read-only
- errors accumulated, deduplicated, stable-sorted
- failure에서 partial shell/baseline/high/recovery/report/digest `0`
- digest는 ruleset, MAP11_03 digest, shell cells/provenance, intent, durations, all path nodes/edges/benefits/recovery timings를 포함
- display text, timestamp, locale, object hash, input/reflection/file order는 제외
- reversed intent/evidence enumeration과 culture change는 같은 artifact/digest

최소 error distinctions:

```text
MissingInput
ArtifactIdentityMismatch
ArtifactDigestMismatch
StaticShellConflict
ShellCoverageMismatch
InvalidBaselineVariant
MissingBaselinePath
DisconnectedBaselinePath
InvalidDurationEvidence
MissingHighRoute
InvalidHighRouteId
InvalidHighRoutePath
HighRouteNotDistinct
InvalidHighPoint
InsufficientHighRouteBenefits
InvalidFailureNode
MissingRecoveryPath
RecoveryTargetMismatch
RecoveryTooShort
RecoveryTooLong
ShellRouteMismatch
NonCanonicalPublication
```

## 11. Exact Non-Ownership

금지:

- existing MAP09_04/MAP11_01~03 production/test 수정
- new graph nodes/edges/envelopes authoring
- 실제 collider/velocity/jump arc/air control simulation
- y coordinate만으로 high route 자동 분류
- 실제 reward/item/NPC 배치
- MAP10 pattern planner/renderer/selector 실행
- Pattern zone, density, cleanup
- starter 16 cluster/CSV/Authoring/Generated 제작
- sector placement/planner/world assembly
- Activity/Event/SpecialRegion 조립
- final SectorCanvas/Slice/Tilemap/Scene/Prefab/SO
- EditorWindow/PlayMode/WorldGenerationRoot wiring
- RNG/variant selection/weight
- asmdef/asmref/Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- unrelated path 수정/stage/commit, Git push

신규 Runtime 금지 symbol:

```text
UnityEditor
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
System.Random
UnityEngine.Random
Time.deltaTime
```

## 12. Focused Verification

category `MAP11_04`만 실행하고 최소 다음을 검증한다.

1. exact active-tile shell coverage; inactive publication 0
2. default Air and Floor Solid projection
3. all protected movement volumes remain Air
4. Solid/Air conflict atomic rejection
5. exact baseline variant binding
6. deterministic Entry→Exit baseline path
7. port→role→node chain and BuildUp/Core/Recovery evidence
8. pattern operation count 0
9. valid alternate high-route contiguous path
10. high point on path and structural distinction
11. distinct benefit IDs >=2
12. every designated failure node recovery witness
13. recovery target is on baseline path; Recovery role is preferred only when on baseline
14. 2000/5000ms inclusive success
15. 1999/5001ms rejection
16. missing/duplicate/unknown timing evidence rejection
17. immutable/canonical publication and deterministic digest
18. reversed input/culture stability; semantic change digest difference
19. atomic accumulated failure with partial output 0
20. no physics/pattern/starter/sector side effects

Task-owned 실패는 task-owned 파일만 고치고 `MAP11_04`만 재실행한다.

## 13. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_04 focused: all discovered executed and PASS; skip/inconclusive 0
MAP11_03 Result SHA: 5d92d816... exact
existing MAP11_01~03 production/test/meta modifications: 0
MicroPattern definitions / physical rows: 24 / 453 unchanged
Catalog CSV SHA: f9d9e9cc... unchanged
Cells CSV SHA: e702ae5d... unchanged
Full 52-file Authoring manifest: 4415ae4a... unchanged
Generated CSV: 0
existing MAP00~MAP11_03 production/test/CSV/meta modifications: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new C#/meta valid; duplicate GUID 0
unapplied candidate / diff-check / unrelated staged paths: 0 / 0 / 0
```

## 14. Required Result

```text
MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES_RESULT.md
```

상단:

```text
TASK: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
STATUS: PASS | BLOCKED
MAP11_04: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER: LOCKED / DO NOT START
```

### Required first section: User-Facing Implementation Report

Result의 첫 섹션은 반드시 한국어 `## User-Facing Implementation Report`다. 전문 용어만 나열하지 말고 아래 표와 설명을 실제 구현 기준으로 작성한다.

| 필드 | 필수 보고 내용 |
|---|---|
| 이번 작업의 목적 | 플레이어/맵 생성 관점에서 한 문단 |
| 추가된 스크립트 | 파일명별 한 줄 책임 |
| 새로 가능해진 기능 | 작업 전에는 불가능했고 지금 가능해진 것 |
| 실제 파이프라인 위치 | 어떤 이전 출력을 받고 어떤 다음 작업이 소비하는지 |
| 아직 안 된 것 | 물리/렌더/콘텐츠 등 명시적 비소유 |
| 게임에서 보이기 시작하는 시점 | 현재는 데이터/검증인지 실제 화면 출력인지 |

그 다음 `## Responsibility and Added Functions`를 둔다.

| Field | Required report |
|---|---|
| Task responsibility | Static Shell과 base/high/recovery witness |
| Added functions | shell/intent/witness/compiler/result/digest 실제 기능 |
| Inputs consumed | MAP11_01~03 compiled artifacts |
| Outputs produced | immutable shell와 route witness report 또는 atomic errors |
| Explicit non-ownership | physics/pattern/starter/sector 미구현 |
| Downstream consumers | MAP11_05 pattern zones/renderer와 later validation |

이후 predecessor/Status, file/public surface, Static Shell, baseline/high/recovery witness, timing, immutability/digest/error, focused/no-regression, static/change scope, commit handoff를 기록한다.

```text
MAP11_04 focused: discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

PASS일 때만 Finalize하고 task-owned production/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_04: implement base high and recovery routes
Push: NOT PERFORMED
```

Result가 PASS여도 MAP11_05를 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
