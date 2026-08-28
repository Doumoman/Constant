```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
  task_file: TASKS/MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL.md
  requires_current_task: NONE
  requires_completed_task: MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER
  requires_result:
    path: REPORTS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER_RESULT.md
    status: PASS
    sha256: f2c93add171cb9b6ee1adeed16af43c1c32a71a8ab6c9b85a14e8dd2f3a93bcf
  requires_installed_task:
    path: TASKS/MAP11_05_IMPLEMENT_CLUSTER_PATTERN_ZONES_AND_RENDERER.md
    sha256: 45bde171c3357c8c9c5f2776566f2e55f4a17cba2d3978323e0a05636a2623b8
  sets_current_task: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
```

# MAP11_06 — Implement Quiet Buffer Cluster Pool

```text
TASK: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
PHASE: MAP11 — TerrainCluster Authoring / Compilation
STATUS: CURRENT
NEXT: MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

## 0. User-Meaning Summary

이번 Task는 랜드마크 전후와 아직 일반 지형이 배치되지 않은 공간에 나중에 사용할 **짧고 조용한 정적 이동 TerrainCluster 후보 pool**을 만든다.

```text
MAP11_05 immutable cluster working canvas/report
→ Quiet Buffer eligibility validation
→ biome/use/socket/pacing/access compatibility index
→ stable compatible-candidate query
→ MAP11_07 content authoring / MAP14 placement input
```

단순한 빈 AIR를 filler로 게시하지 않는다. Entry→Exit를 실제 정적 지형으로 연결하고, 가장 작은 합법 TerrainCluster footprint인 active 2청크를 사용하며, reward/marker가 없는 후보만 Quiet Buffer로 승인한다.

이번 Task는 실제 후보를 랜덤 선택하거나 Sector에 배치하지 않는다. starter 콘텐츠는 MAP11_07, 실제 빈 공간 배치는 MAP14_06 책임이다.

## 1. Responsibility

| 소유 | 소유하지 않음 |
|---|---|
| Quiet Buffer candidate/profile 모델 | starter cluster 제작 |
| MAP11_01~05 artifact chain 검증 | 새 cluster/spine/pattern 저작 |
| short/static/quiet eligibility 판정 | Sector 빈 공간 탐색·예약·배치 |
| biome/use/route/pacing/access pool index | RNG/weight/후보 추첨 |
| stable compatibility query | repetition/density/cleanup |
| immutable pool/query report/digest | Activity/Event/SpecialRegion 조립 |

실제 흐름:

```text
MAP11_05 pattern-rendered full working canvas
→ MAP11_06 Quiet Buffer candidate pool
→ MAP11_07 biome별 starter content 등록
→ MAP14_06 남은 공간 Quiet/Buffer 배치
```

## 2. No-Regression Policy

정상 실행은 category `MAP11_06`만 선택한다.

```text
MAP11_06 focused selection: required
Prior MAP09/MAP10/MAP11_01~05 selections: 0
Legacy 19347 selections: 0
PlayMode selections: 0
```

이전/legacy 회귀는 다음 실제 trigger가 있을 때만 owner와 최소 범위를 기록하고 허용한다.

- compile/Console error가 기존 authority 파일을 가리킴
- MAP11_05 PASS artifact identity/digest 또는 repair evidence drift
- 기존 production/test/CSV/meta의 예상 밖 변경
- 기존 PacingRole/AccessClass/Biome/RouteType authority와 실제 API 불일치
- asmdef/GUID/namespace/authority 위반

Task-owned 코드·fixture 문제는 task-owned 파일만 수정하고 `MAP11_06`만 재실행한다. 기존 authority 변경이 필요하면 수정하지 말고 `STATUS: BLOCKED`로 STOP한다.

## 3. Read-Only Authorities and Preflight

정확히 확인한다.

1. MAP11_05 repaired Result status/SHA와 COMPLETE 상태
2. original MAP11_05 installed/archive Task SHA
3. installed/archive MAP11_05R repair SHA `aa7beb451be6169d4069c3d323c91207d3e53667bc53d1e276a0caa6697463fc`
4. MAP11_06만 CURRENT, MAP11_07 LOCKED, inbox candidate 0
5. MAP11_01 Local Canvas/active chunk ownership
6. MAP11_02 roles/ports/socket compatibility
7. MAP11_03 compiled traversal/protection
8. MAP11_04 Static Shell and baseline/high/recovery witnesses
9. MAP11_05 zone map, working canvas, render report, canonical digest
10. existing typed `MoonpalaceBiomeId`, `PacingRole`, `AccessClass`, integer RouteType `0..4`
11. Authoring 52, MicroPattern `24/453`, Generated CSV 0
12. compile/Console, meta/GUID, dirty/staged paths

다음이면 `BLOCKED`다.

- predecessor/repair/result SHA mismatch
- MAP11_01~05 artifact identity/digest chain 불일치
- existing typed identity를 재정의해야만 구현 가능
- current task allowlist가 사용자 변경과 겹침
- 기존 authority 수정 없이는 구현 불가

## 4. Exact Write Boundary

신규 파일만 허용한다.

```text
Runtime:
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterQuietBuffer.cs(.meta)
Assets/_Game/Map/Runtime/WorldGeneration/TerrainClusters/TerrainClusterQuietBufferPool.cs(.meta)

Focused test:
Assets/_Game/Tests/EditMode/Map/WorldGeneration/TerrainClusters/TerrainClusterQuietBufferPoolTests.cs(.meta)

Namespace:
StarNight.Map.WorldGeneration.TerrainClusters

Assembly:
Game.Map.Runtime / Game.Map.Tests.EditMode
```

책임 분리에 꼭 필요하면 신규 Runtime 모델 파일 1개를 추가할 수 있다. 기존 MAP00~MAP11_05 production/test/CSV/meta 파일은 수정하지 않는다. 실제 파일과 public surface를 Result에 기록한다.

## 5. Quiet Buffer Use Contract

exact use kinds:

```text
BeforeLandmark
AfterLandmark
UnplacedSpace
```

의미:

- `BeforeLandmark`: landmark 진입 전 강한 사건을 연속 배치하지 않기 위한 정적 완충 후보
- `AfterLandmark`: landmark 이탈 직후 회복 가능한 정적 완충 후보
- `UnplacedSpace`: 일반 Cluster 배치 후 남는 합법 footprint를 빈 AIR가 아닌 지형으로 채우는 후보

이 enum은 실제 위치나 landmark를 검색하지 않는다. candidate가 지원 가능한 사용 문맥만 선언하며 MAP13/MAP14가 후속 배치에 사용한다.

candidate stable ID grammar:

```text
^QBUF_[A-Z0-9_]+$
```

candidate profile 최소 입력:

```text
stable Quiet Buffer ID
existing typed MoonpalaceBiomeId
supported use kinds 1+
compatible PacingRole set
compatible AccessClass set
MAP11_01~05 compiled artifacts/report
```

RouteType와 Entry/Exit side compatibility는 profile 문자열로 중복 저작하지 않고 MAP11_02 primary ports/socket contract에서 exact derive한다.

## 6. Exact Quiet Eligibility

candidate는 다음을 모두 만족해야 한다.

### 6.1 Smallest Legal TerrainCluster

```text
active chunk count: exact 2
inactive cells: existing Local Canvas 의미 그대로 허용
six-chunk exception: 사용하지 않음
```

MAP09_04/MAP11_01의 일반 TerrainCluster 최소 2청크 계약을 보존한다. Quiet 전용 1청크 예외를 만들지 않는다.

Entry와 Exit primary port의 owning active chunk는 서로 달라야 한다. MAP11_04 baseline witness는 두 owning chunk를 모두 통과해야 하며 synthetic node/edge를 만들지 않는다.

### 6.2 Quiet / No-Tool Compatibility

compatible PacingRole set은 exact `Quiet`를 포함해야 하며 다음 existing roles의 부분집합만 허용한다.

```text
Quiet
Traversal
Recovery
Safe
Flow
```

이 set은 pacing을 배정하지 않고 compatibility만 선언한다. `Discovery/Risk/Machinery/Activity/Narrative/Reward/Landmark/Resource/Boss/Integrated`는 Quiet Buffer compatibility에서 거부한다.

compatible AccessClass set은 exact `MandatoryNoTool`을 포함해야 하며 다음만 허용한다.

```text
MandatoryNoTool
OptionalNoTool
```

`OptionalTool`과 `OptionalEnvironment`는 Quiet Buffer에서 거부한다. existing enum/token/codec은 수정하거나 복제하지 않는다.

### 6.3 Static Traversal Evidence

- MAP11_04 baseline Entry→Exit witness가 성공 상태여야 한다.
- baseline의 모든 node/edge/movement/timing evidence는 source graph에 존재해야 한다.
- high/recovery witness는 MAP11_04 성공 artifact 그대로 보존하며 Quiet compiler가 재계산하지 않는다.
- 모든 active chunk는 final working canvas에서 `Solid >=1`과 `Air >=1`을 각각 가져야 한다.
- full final working canvas coordinate count는 MAP11_01 active tile count와 exact해야 한다.
- AbsoluteProtected renderer write/change count는 exact `0/0`이어야 한다.
- MAP11_04 Static Shell과 MAP11_05 initial/final canvas identity/digest chain이 일치해야 한다.

### 6.4 No Strong Content in Base Candidate

- MAP11_02 `Reward` role anchor count는 exact `0`이어야 한다.
- MAP11_05 final Marker non-default count는 exact `0`이어야 한다.
- MAP11_05 final Hazard non-default count는 exact `0`이어야 한다.
- MAP11_05 Surface/Material은 기존 task 의미를 보존하며 Quiet compiler가 새 값을 만들지 않는다.
- Activity/Event/SpecialRegion 존재 여부를 추론하거나 placeholder를 만들지 않는다.

High-route benefit stable IDs는 실제 reward spawn이 아니므로 보존할 수 있다. Quiet compiler는 benefit ID를 제거·재해석하지 않는다.

## 7. Candidate Publication

성공 candidate 최소 evidence:

```text
Quiet Buffer ID
typed biome
supported use kinds
compatible pacing/access sets
TerrainCluster ID and transform
active chunk coordinates/count
Entry/Exit port, side, RouteType compatibility
baseline route node/edge/chunk coverage
static Solid/Air counts per active chunk
Reward/Marker/Hazard zero evidence
MAP11_01~05 artifact identities/digests
candidate canonical digest
```

candidate collections는 defensive copy/read-only, stable canonical order다. display text, timestamp, locale, object identity, input/reflection/file order는 digest에서 제외한다.

## 8. Pool Compilation and Indexes

pool compiler는 caller-supplied candidate profiles를 검증하고 성공 candidate를 다음 key로 index한다.

```text
typed biome
use kind
Entry side
Exit side
compatible RouteType
compatible PacingRole
compatible AccessClass
```

규칙:

- pool input은 최소 1 candidate다.
- Quiet Buffer ID와 TerrainCluster identity 조합은 unique하다.
- 동일 candidate reference duplicate는 coalesce하지 않고 duplicate error다.
- invalid candidate 하나라도 있으면 pool 전체 atomic failure다.
- 모든 index bucket과 candidate list는 candidate ID ordinal order다.
- input order, dictionary order, culture가 pool/index/digest를 바꾸지 않는다.
- pool digest는 ruleset, every candidate digest, every index key/membership을 포함한다.

이번 Task는 production pool row/CSV를 만들지 않는다. focused test의 in-memory fixture만 사용하고, 실제 biome별 후보는 MAP11_07이 authoring한다.

## 9. Compatibility Query

query 최소 입력:

```text
typed biome
exact use kind
required Entry side
required Exit side
required RouteType
required PacingRole
required AccessClass
optional maximum active chunk count
```

query는 pool index에서 모든 조건을 만족하는 후보를 읽어 candidate ID ordinal order로 게시한다.

- candidate를 하나 선택하지 않는다.
- RNG stream을 만들거나 draw하지 않는다.
- weight/ticket/recent-history/repetition을 계산하지 않는다.
- placement coordinate/free footprint를 검색하지 않는다.
- matching 0은 valid immutable empty query result이며 caller가 후속 retry/failure policy를 결정한다.
- undefined enum, invalid RouteType, maximum chunk count `<2`, pool digest mismatch는 query failure다.

query result는 request, pool digest, matched IDs/candidate digests, match count, canonical query digest를 포함한다.

## 10. Publication, Errors, and Digest

최소 semantic surface:

```text
TerrainClusterQuietBufferUse
TerrainClusterQuietBufferProfile
TerrainClusterQuietBufferCandidate
TerrainClusterQuietBufferPool
TerrainClusterQuietBufferPoolCompileRequest
TerrainClusterQuietBufferPoolCompiler
TerrainClusterQuietBufferQuery
TerrainClusterQuietBufferQueryResult
TerrainClusterQuietBufferErrorCode / Error / Result
```

기존 naming 충돌 시 의미를 보존하는 최소 조정은 가능하다.

publication rules:

- all collections defensive copy/read-only
- errors accumulated, deduplicated, stable-sorted
- any compile error에서 candidate/pool/index/query/digest partial output `0`
- query validation error에서 matches/query digest `0`
- reversed inputs/culture same artifact/digest
- semantic biome/use/socket/pacing/access/artifact mutation different digest 또는 typed error

최소 error distinctions:

```text
MissingInput
ArtifactIdentityMismatch
ArtifactDigestMismatch
InvalidQuietBufferId
DuplicateQuietBufferId
InvalidBiome
InvalidUseKind
InvalidPacingCompatibility
InvalidAccessCompatibility
InvalidFootprintSize
EntryExitChunkMismatch
BaselineCoverageMismatch
WorkingCanvasCoverageMismatch
EmptyChunkTerrain
RewardRoleNotQuiet
MarkerNotQuiet
HazardNotQuiet
ProtectedMutationDetected
DuplicateCandidateIdentity
EmptyPool
InvalidQuery
PoolDigestMismatch
NonCanonicalPublication
```

## 11. Exact Non-Ownership

금지:

- existing MAP09/MAP10/MAP11_01~05 production/test/CSV/meta 수정
- new footprint/role/spine/envelope/route/pattern/working canvas 계산
- Quiet 전용 1청크 authority 예외
- starter 16 TerrainCluster 또는 production pool 데이터 저작
- pattern candidate/weight/RNG selection
- repetition history/local cleanup/density repair
- landmark 탐색·buffer reservation·placement
- SectorCanvas free-space solve
- Activity/Event/SpecialRegion 조립
- final Slice/Tilemap/Scene/Prefab/SO/PlayMode
- EditorWindow/WorldGenerationRoot wiring
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
DeterministicRngStreamFactory
Time.deltaTime
Tilemap
```

## 12. Focused Verification

category `MAP11_06`만 실행하고 최소 다음을 검증한다.

1. exact use kinds and ID grammar
2. repaired MAP11_05 Result/Task/repair SHA preflight evidence
3. MAP11_01~05 artifact identity/digest chain
4. exact active 2-chunk eligibility; 1/3+ rejection
5. Entry/Exit different owning chunk and baseline both-chunk coverage
6. Quiet 포함 allowed pacing subset; strong pacing rejection
7. MandatoryNoTool 포함/no-tool-only access; tool/environment rejection
8. per-active-chunk Solid/Air evidence and full canvas coverage
9. Reward role, Marker, Hazard exact zero
10. protected write/change exact zero
11. typed biome/use/side/RouteType/pacing/access indexes
12. multi-condition query stable matching
13. valid empty query result with RNG/draw 0
14. duplicate/invalid candidate atomic pool rejection
15. immutable/canonical publication and deterministic candidate/pool/query digests
16. reversed input/culture stability and semantic sensitivity
17. accumulated errors with partial output 0
18. no RNG/placement/cleanup/starter/sector/Tilemap side effects

Task-owned 실패는 task-owned 파일만 고치고 `MAP11_06`만 재실행한다.

## 13. Static Gates

```text
Unity compile / Console error / relevant warning: 0 / 0 / 0
MAP11_06 focused: all discovered executed and PASS; skip/inconclusive 0
MAP11_05 Result SHA: f2c93add... exact
MAP11_05R repair SHA: aa7beb45... exact
existing MAP09/MAP10/MAP11_01~05 production/test/meta modifications: 0
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
MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL_RESULT.md
```

상단:

```text
TASK: MAP11_06_IMPLEMENT_QUIET_BUFFER_CLUSTER_POOL
STATUS: PASS | BLOCKED
MAP11_06: COMPLETE ELIGIBLE | NOT COMPLETE
MAP11_07_AUTHOR_STARTER_16_TERRAIN_CLUSTERS: LOCKED / DO NOT START
```

### Required first section: User-Facing Implementation Report

첫 섹션은 반드시 한국어 `## User-Facing Implementation Report`이며 다음을 실제 구현 기준으로 보고한다.

| 필드 | 필수 보고 내용 |
|---|---|
| 이번 작업의 목적 | 랜드마크 전후/미배치 공간을 왜 빈 AIR 대신 정적 지형으로 채우는지 |
| 추가된 스크립트 | 모든 신규 C# 파일명과 각 한 줄 책임 |
| 새로 가능해진 기능 | Quiet 후보 판정, pool/index/query 중 실제 구현된 기능 |
| 실제 파이프라인 위치 | MAP11_05 입력, MAP11_07/MAP14 후속 소비 관계 |
| 아직 안 된 것 | starter content/RNG/placement/Activity/Sector/Tilemap |
| 게임에서 보이는 시점 | 현재 pool 데이터인지 화면 출력인지 |

그 다음 `## Responsibility and Added Functions`를 둔다.

| Field | Required report |
|---|---|
| Task responsibility | candidate eligibility/pool/index/query |
| Added functions | 실제 public type/function별 책임 |
| Inputs consumed | MAP11_01~05와 typed biome/pacing/access/RouteType authority |
| Outputs produced | immutable candidates/pool/index/query/digests 또는 atomic errors |
| Explicit non-ownership | content/RNG/placement/cleanup/sector/Tilemap |
| Downstream consumers | MAP11_07 and MAP14_03/06 |

이후 predecessor/Status, file/public surface, eligibility, static terrain evidence, pool/index/query, immutability/digest/error, focused/no-regression, static/change scope, commit handoff를 기록한다.

```text
MAP11_06 focused: discovered/executed/pass/fail/skip/inconclusive
REGRESSION TRIGGER DETECTED: NO | YES(owner/reason/minimum scope)
PRIOR TASK TEST SELECTIONS: 0 (normal path)
LEGACY TEST SELECTIONS: 0 (normal path)
PLAYMODE TEST SELECTIONS: 0
```

PASS일 때만 Finalize하고 task-owned production/test/meta/protocol 파일만 atomic commit한다.

```text
Subject: MAP11_06: implement quiet buffer cluster pool
Push: NOT PERFORMED
```

PASS여도 MAP11_07을 자동 시작하지 않고 STOP한다.
