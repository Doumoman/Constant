```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
  task_file: TASKS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER.md
  requires_current_task: NONE
  requires_completed_task: MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES
  requires_result:
    path: REPORTS/MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES_RESULT.md
    status: PASS
    sha256: 226527df3ee9c807e3dd5bdff921034bdb18b7dac165d22429571a0f34980f34
  requires_installed_task:
    path: TASKS/MAP11_04_IMPLEMENT_BASE_HIGH_AND_RECOVERY_ROUTES.md
    sha256: 4ede8aabf1c78ed607d10be0b51e0430cb62a3c4ebdcb5042dbb81b8bb25faa4
  sets_current_task: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
```

# MAP11_05 — Implement Cluster Pattern Zones and Renderer

```text
TASK: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. User-Meaning Summary

이번 Task는 MAP11_04의 통과 가능한 Static Shell 위에 4×4 MicroPattern을 안전하게 적용한다.

```text
Static Shell
→ Add/Carve·Affordance·Marker 허용 zone
→ base/high/recovery 이동 공간의 AbsoluteProtected zone
→ 기존 MAP10 transform/protected planner/ordered renderer
→ immutable cluster pattern working canvas와 delta
```

패턴은 허용된 영역만 바꾸며 기본길·고점길·복귀길에는 실제 write를 남길 수 없다. 아직 pattern 선택 RNG, starter cluster 콘텐츠, SectorCanvas, Tilemap 또는 Scene 출력은 수행하지 않는다.

## 1. Responsibility

| 소유 | 소유하지 않음 |
|---|---|
| cluster-local pattern zone 모델/검증 | pattern 선택 RNG/weight |
| MAP11_03~04 근거 기반 AbsoluteProtected union | 새 Spine/route/protection 계산 |
| caller-selected pattern placement 검증 | starter 16종 pattern 배치 저작 |
| MAP10_02 planner와 MAP10_03 renderer 실제 재사용 | transform/renderer 복제 구현 |
| immutable working canvas/delta/report/digest | quiet buffer, density, cleanup |
| atomic reject와 보호 write 0 증명 | final SectorCanvas/Slice/Tilemap |

파이프라인 위치:

```text
MAP11_01 Local Canvas
→ MAP11_02 role/socket
→ MAP11_03 Spine/Envelope protection
→ MAP11_04 Static Shell + route witnesses
→ MAP11_05 pattern zones + MAP10 renderer application
→ MAP11_06 quiet buffer pool
→ MAP11_07 starter 16 TerrainClusters
```

## 2. No-Regression Policy

정상 실행은 category `MAP11_05`만 선택한다.

```text
MAP11_05 focused selection: required
Prior MAP09/MAP10/MAP11_01~04 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

이전 category 또는 legacy 회귀는 다음 실제 trigger가 있을 때만 허용한다.

- compile/Console error가 기존 authority 파일을 가리킴
- 기존 MAP10 planner/renderer의 계약과 실제 API가 불일치
- MAP11_03 protection 또는 MAP11_04 shell/witness digest drift
- 기존 production/test/CSV/meta의 예상 밖 변경
- asmdef/GUID/namespace/authority 위반

Trigger가 있으면 owner, 원인, 필요한 최소 test selection을 먼저 Result에 기록한다. Task-owned 코드·fixture 문제는 task-owned 파일만 고치고 `MAP11_05`만 재실행한다. 기존 authority 수정이 필요하면 수정하지 말고 `STATUS: BLOCKED`로 STOP한다.

## 3. Read-Only Authorities and Preflight

정확히 확인한다.

1. MAP11_04 Result/Task SHA와 PASS/COMPLETE 상태
2. MAP11_05만 CURRENT, MAP11_06 LOCKED, inbox candidate 0
3. MAP11_01 exact active Local Canvas와 coordinate lookup
4. MAP11_03 RouteSpine/TraversalEnvelope protected coordinates와 provenance
5. MAP11_04 Static Shell, baseline/high/recovery witness, canonical digest
6. MAP10_01 validated MicroPattern definition/catalog authority
7. MAP10_02 transformer, protected-mask builder, application planner, application plan
8. MAP10_03 render request/target/delta/result와 ordered renderer
9. MAP10_06 starter MicroPattern `24 definitions / 453 physical rows`
10. Authoring 52, Generated CSV 0, compile/Console, meta/GUID, dirty/staged paths

다음이면 `BLOCKED`다.

- predecessor identity/digest mismatch
- MAP10 public API 재사용 없이 같은 transform/protected/render logic을 복제해야만 구현 가능
- MAP11_03 보호 근거가 MAP11_04 witness 보호 좌표를 덮지 못함
- task allowlist가 사용자 변경과 겹침
- 기존 authority 수정 없이는 구현 불가

## 4. Exact Write Boundary

신규 파일만 허용한다.

```text
Runtime:
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterPatternZone.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterPatternRenderer.cs(.meta)

Focused test:
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterPatternRendererTests.cs(.meta)

Namespace:
StarNight.Map.WorldGeneration.TerrainClusters

Assembly:
Game.Map.Runtime / Game.Map.Tests.EditMode
```

필요하면 책임을 명확히 분리하기 위한 Runtime 모델 파일 1개를 추가할 수 있다. 그 경우 반드시 신규 파일이어야 하며 Result에서 이유와 public surface를 보고한다. 기존 MAP00~MAP11_04 production/test/CSV/meta 파일은 수정하지 않는다.

## 5. Pattern Zone Contract

exact zone kinds:

```text
GeometryAdd
GeometryCarve
Affordance
Marker
AbsoluteProtected
```

zone은 Local Canvas의 active coordinate 집합으로 게시한다. 하나의 coordinate는 서로 다른 layer zone에 동시에 속할 수 있지만 `GeometryAdd`와 `GeometryCarve`를 동시에 가질 수 없다.

규칙:

- `GeometryAdd`: Static Shell이 `Air`인 active/unprotected cell에서 `AddSolid`만 허용
- `GeometryCarve`: Static Shell이 `Solid`인 active/unprotected cell에서 `CarveAir`만 허용
- `Affordance`: active/unprotected cell에서 `SetAffordance`만 허용
- `Marker`: active/unprotected cell에서 `SetMarker`만 허용
- `AbsoluteProtected`: 모든 non-`NoChange` operation 금지
- zone 밖 cell: 모든 non-`NoChange` operation 금지
- `NoChange`: 모든 active cell에서 허용
- `SetSurface`, `SetMaterial`, `SetHazard`: MAP11_05에서는 항상 금지
- inactive/out-of-bounds coordinate: 모든 zone과 placement target에서 금지

Geometry/Affordance/Marker zone의 authored input enumeration은 순서와 중복에 독립적으로 canonicalize한다. 동일 의미 duplicate는 coalesce하고 서로 모순되는 zone은 accumulated error로 거부한다.

## 6. AbsoluteProtected Zone

`AbsoluteProtected`는 caller가 임의로 저작하지 않고 다음 read-only evidence의 exact union으로 compiler가 만든다.

```text
MAP11_03 RouteSpine protected coordinates
MAP11_03 TraversalEnvelope protected coordinates
MAP11_04 baseline route covered protected coordinates
MAP11_04 high-route protected coordinates
MAP11_04 recovery-route protected coordinates
MAP11_04 Entry/Exit and recovery anchor coordinates
```

요구사항:

1. unique coordinate별 source kind/source ID/provenance를 lossless하게 보존한다.
2. MAP11_04 witness coordinate는 해당 MAP11_03 protection evidence로 설명 가능해야 한다.
3. authored non-protected zone과 겹치면 `AbsoluteProtected`가 우선하며 해당 authored overlap은 error다.
4. MAP10_02에는 기존 exact protected kinds를 사용한다.

```text
RouteSpine
TraversalEnvelope
BoundaryProtectedOpen
SpecialFixedEntry
```

새 protected kind를 MAP10에 추가하거나 기존 enum을 수정하지 않는다. 이번 Task의 source에서 실제 존재하는 RouteSpine/TraversalEnvelope evidence만 전달한다. witness-only 좌표가 기존 protected evidence로 설명되지 않으면 silent mapping하지 않고 failure다.

성공 결과는 다음을 증명해야 한다.

```text
AbsoluteProtected coordinate에 대한 effective renderer write count = 0
AbsoluteProtected coordinate에 대한 final value change count = 0
```

## 7. Caller-Selected Placement Intent

이번 Task는 pattern 선택을 하지 않는다. caller가 이미 선택한 placement intent만 받는다.

최소 intent:

```text
stable placement ID
validated MicroPattern ID
transform
cluster-local origin
expected definition/application identity
```

- stable placement ID grammar는 `TCP_[A-Z0-9_]+`다.
- pattern은 MAP10_01/06 validated catalog에서 ID로 resolve한다.
- protected policy는 해당 definition의 authority 값을 그대로 사용한다.
- RNG seed, weight ticket, biome candidate selection을 input으로 받거나 생성하지 않는다.
- input order가 결과 순서나 digest를 바꾸지 않는다.

잘못된/중복 placement ID, unknown pattern, invalid transform/origin, catalog digest mismatch는 accumulated error 후 atomic failure다.

## 8. Required MAP10 Reuse

직접 재구현하지 말고 실제 MAP10 public authority를 호출한다.

placement마다:

1. MAP10_01 validated definition을 resolve한다.
2. MAP10_02 `MicroPatternTransformer`로 transform한다.
3. MAP10_02 protected-mask builder에 MAP11_03 provenance를 전달한다.
4. MAP10_02 `MicroPatternApplicationPlanner`로 immutable application plan을 만든다.
5. plan의 각 effective non-`NoChange` instruction을 target zone permission과 대조한다.
6. 전체 batch가 valid일 때만 MAP10_03 render request를 만든다.
7. MAP10_03 `MicroPatternOrderedRenderer`로 한 번의 atomic batch render를 수행한다.

MAP10_02 policy 의미를 보존한다.

- `ForceNoChange`: protected hit는 plan에서 `NoChange`; renderer write 0
- `RejectCandidate`: protected hit가 있는 placement/batch는 실패; partial output 0

MAP10_03 의미를 보존한다.

```text
stage order: Geometry → Surface → Affordance → Material → Hazard → Marker
AddSolid: Solid=true
CarveAir: Solid=false
Set*: own layer only
identical same-layer writes: coalesce with provenance
conflicting same-layer writes: atomic reject
```

이번 Task가 금지한 Surface/Material/Hazard instruction은 renderer 호출 전에 zone validation error로 거부한다. renderer stage/order/conflict code를 복사하지 않는다.

## 9. Working Canvas and Publication

initial MAP10 render target은 MAP11_04 Static Shell의 active cells exact union으로 만든다.

```text
Geometry.Solid = shell occupancy == Solid
Surface/Affordance/Material/Hazard/Marker = empty/default
cell provenance = shell identity/provenance
inactive cells = target에 없음
```

성공 publication 최소 내용:

```text
canonical zone map and provenance
AbsoluteProtected union and evidence
canonical selected placement intents
MAP10_02 application plans and digests
MAP10_03 render delta and digest
initial/final immutable cluster working canvas
changed coordinates and per-layer before/after evidence
protected write/change counts
cluster pattern render canonical digest
```

`TerrainClusterPatternRenderer`는 MAP11_04 shell을 mutate하지 않는다. final working canvas는 render delta를 initial target에 적용한 immutable snapshot이며 active cell exact coverage를 유지한다. 이 output은 final SectorCanvas/Tilemap이 아니다.

publication rules:

- all collections defensive copy/read-only
- canonical stable ordering by coordinate/layer/ID
- errors accumulated, deduplicated, stable-sorted
- any error에서 zone/report/plans/delta/canvas/digest partial output `0`
- digest는 predecessor identities/digests, zone/provenance, intents, MAP10 plan/render digests, initial/final cells, changes를 포함
- timestamp, display text, object hash, culture, input/reflection/file order, RNG는 제외
- reversed inputs/culture change는 same result/digest; semantic zone/placement change는 different digest

최소 semantic surface:

```text
TerrainClusterPatternZoneKind
TerrainClusterPatternZoneCell / PatternZoneMap
TerrainClusterPatternPlacementIntent
TerrainClusterPatternRenderRequest
TerrainClusterPatternWorkingCanvas
TerrainClusterPatternRenderReport
TerrainClusterPatternRenderError / Result
TerrainClusterPatternRenderer
```

기존 naming 충돌 시 의미를 보존하는 최소 이름 조정은 가능하다.

## 10. Minimum Error Distinctions

```text
MissingInput
ArtifactIdentityMismatch
ArtifactDigestMismatch
InvalidZoneCoordinate
ConflictingGeometryZone
ProtectedZoneOverlap
ProtectedEvidenceMismatch
InvalidPlacementId
DuplicatePlacementId
UnknownPattern
InvalidPlacement
ApplicationPlanRejected
UnauthorizedZoneOperation
UnsupportedLayerOperation
RenderConflict
ProtectedWriteDetected
WorkingCanvasCoverageMismatch
NonCanonicalPublication
```

MAP10의 detailed error를 문자열로 뭉개지 말고 stable source/error evidence로 감싼다. 기존 MAP10 error enum은 수정하지 않는다.

## 11. Exact Non-Ownership

금지:

- existing MAP09/MAP10/MAP11_01~04 production/test/CSV/meta 수정
- MicroPattern transform/protected planner/renderer 복제
- candidate profile/weight/RNG selection
- repetition signature/local cleanup/density repair
- 새 Spine/Envelope/route/static-shell 계산
- quiet buffer cluster pool
- starter 16 TerrainCluster/CSV/Authoring/Generated 제작
- Activity/Event/SpecialRegion 조립
- sector placement/world assembly
- final SectorCanvas/MicroChunk Slice/Tilemap/Scene/Prefab/SO
- collider/velocity/jump physics/PlayMode
- EditorWindow/WorldGenerationRoot wiring
- asmdef/asmref/Settings/Packages 변경
- 문제 trigger 없는 prior/legacy test 실행
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
Tilemap
```

## 12. Focused Verification

category `MAP11_05`만 실행하고 최소 다음을 검증한다.

1. exact zone kinds와 active/inactive bounds validation
2. Add zone은 shell Air, Carve zone은 shell Solid에만 성립
3. Affordance/Marker cross-layer overlap 허용, Add/Carve overlap 거부
4. AbsoluteProtected exact union/provenance와 authored overlap 거부
5. MAP11_04 witness protection이 MAP11_03 evidence로 전부 설명됨
6. actual MAP10_02 transformer/protected planner 호출과 plan digest 보존
7. `ForceNoChange` protected write/change exact 0
8. `RejectCandidate` protected hit atomic failure
9. AddSolid/CarveAir/SetAffordance/SetMarker zone-local success
10. zone 밖 operation과 Surface/Material/Hazard 거부
11. actual MAP10_03 ordered renderer 사용, identical coalesce/conflict reject 보존
12. initial shell과 final active-cell coverage, immutable before/after delta
13. reversed zone/intent/source enumeration과 culture deterministic
14. semantic zone/placement change digest difference
15. accumulated errors와 all partial outputs 0
16. RNG/cleanup/quiet-buffer/starter/sector/Tilemap side effect 0

Task-owned 실패는 task-owned 파일만 고치고 `MAP11_05`만 재실행한다.

## 13. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_05 focused: all discovered executed and PASS; skip/inconclusive 0
MAP11_04 Result SHA: 226527df... exact
existing MAP10 and MAP11_01~04 production/test/meta modifications: 0
MicroPattern definitions / physical rows: 24 / 453 unchanged
Catalog CSV SHA: f9d9e9cc... unchanged
Cells CSV SHA: e702ae5d... unchanged
Full 52-file Authoring manifest: 4415ae4a... unchanged
Generated CSV: 0
other roots/asmdef/Scene/Prefab/Settings/Packages changes: 0
new C#/meta valid; duplicate GUID 0
unapplied candidate / diff-check / unrelated staged paths: 0 / 0 / 0
```

## 14. Required Result

```text
MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER_RESULT.md
```

상단:

```text
TASK: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
STATUS: PASS | BLOCKED
MAP11_05: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL: LOCKED / DO NOT START
```

### Required first section: User-Facing Implementation Report

Result의 첫 섹션은 반드시 한국어 `## User-Facing Implementation Report`다. 다음 항목을 실제 구현 기준으로 명확하게 쓴다.

| 필드 | 필수 보고 내용 |
|---|---|
| 이번 작업의 목적 | 플레이어/맵 생성 관점에서 한 문단 |
| 추가된 스크립트 | 모든 신규 C# 파일명과 각 한 줄 책임 |
| 새로 가능해진 기능 | 작업 전에는 불가능했고 지금 가능해진 것 |
| 실제 파이프라인 위치 | MAP11_04 입력과 MAP11_06~07 소비 관계 |
| 아직 안 된 것 | 선택/RNG/quiet buffer/content/sector/Tilemap 명시 |
| 게임에서 보이기 시작하는 시점 | 현재 데이터/working canvas인지 화면 출력인지 |

그 다음 `## Responsibility and Added Functions`를 둔다.

| Field | Required report |
|---|---|
| Task responsibility | zone/protection/MAP10 application/working canvas |
| Added functions | 실제 public surface와 함수별 책임 |
| Inputs consumed | MAP11_03~04와 MAP10_01~03 authority |
| Outputs produced | immutable zone/report/plans/delta/canvas/digest 또는 atomic errors |
| Explicit non-ownership | RNG/cleanup/quiet buffer/starter/sector/Tilemap |
| Downstream consumers | MAP11_06~09 and later sector validation |

이후 predecessor/Status, exact file/public surface, zone/protected evidence, MAP10 reuse, working canvas/delta, immutability/digest/error, focused/no-regression, static/change scope, commit handoff를 기록한다.

```text
MAP11_05 focused: discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

PASS일 때만 Finalize하고 task-owned production/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_05: implement cluster pattern zones and renderer
Push: NOT PERFORMED
```

Result가 PASS여도 MAP11_06을 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
