```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS
  task_file: TASKS/MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS.md
  requires_current_task: NONE
  requires_completed_task: MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
  requires_result:
    path: REPORTS/MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS_RESULT.md
    status: PASS
    sha256: 9f10c4cc57203152d4c769d792164ab3847af9e9e5dbbc95352cc5369c6fab39
  requires_installed_task:
    path: TASKS/MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS.md
    sha256: 9db7e08506f33a6d065ece29a7509d0ea3e526d63c41cc8fea6067fd7c1d83f3
  sets_current_task: MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS
```

# MAP09_03 — Implement MicroPattern Contracts

```text
TASK: MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

---

## 0. 목적과 범위

4×4 `MicroPattern`을 완성형 방이나 12×8 `MicroChunk`가 아닌 **16개 1×1 셀의 local operation brush**로 표현하는 immutable Runtime 계약과 validator를 구현한다.

이번 Task가 고정하는 것:

- 4×4 exact 16-cell 좌표
- operation/layer 호환성
- integer weight와 biome compatibility
- 허용 transform 정책
- protected cell 처리 정책
- immutable catalog entry와 deterministic digest

이번 Task가 하지 않는 것:

- CSV schema/import/export
- 실제 transform 좌표 연산
- Canvas에 operation 적용
- Pattern 선택/RNG/반복 방지/cleanup
- starter Pattern authoring
- Cluster/Spine/Envelope 구현

위 실행 기능은 MAP10에서 별도로 연다.

---

## 1. Preflight

변경 전에 확인하고 Result에 기록한다.

1. MAP09_02 Result/설치 Task/Archive의 상태·SHA가 metadata와 exact 일치
2. MAP09_03만 CURRENT이며 inbox candidate가 0
3. MAP09_01 pass catalog와 MAP09_02 layer catalog digest가 Result와 일치
4. `MicroPattern=4×4`, `MicroChunk=12×8`, `Sector=48×32` 기존 상수/좌표 계약
5. MAP08의 기존 typed biome ID/catalog와 4 biome identity
6. Runtime/Test의 approved `WorldGeneration/MicroPatterns/` root·namespace·assembly
7. Authoring `50/50`과 manifest, meta/GUID, compile/Console, dirty worktree

다음이면 `BLOCKED`다.

- predecessor hash/status mismatch
- 4×4와 12×8 명칭 또는 기존 좌표축을 additive하게 재사용할 수 없음
- 기존 MicroPattern production type이 이미 다른 의미로 존재
- task allowlist와 사용자 변경이 겹침

기존의 대량 MCP_INBOX/Archive dirty state는 읽기 전용으로 보존하고 절대 stage하지 않는다.

---

## 2. Exact MicroPattern Model

구현 위치:

```text
Runtime: Assets/_Game/Map/Runtime/WorldGeneration/MicroPatterns/
Test:    Assets/_Game/Tests/EditMode/Map/WorldGeneration/MicroPatterns/
Runtime namespace: StarNight.Map.WorldGeneration.MicroPatterns
Test namespace: StarNight.Map.Tests.EditMode.WorldGeneration.MicroPatterns
Assembly: Game.Map.Runtime / Game.Map.Tests.EditMode
```

물리 파일 수는 자유지만 다음 semantic type을 제공한다.

```text
MicroPatternId
MicroPatternLayer
MicroPatternOperation
MicroPatternTransform
MicroPatternProtectedPolicy
MicroPatternInstruction
MicroPatternCell
MicroPatternDefinition
MicroPatternValidator
MicroPatternValidationError / Result
```

### 2.1 ID와 좌표

```text
MicroPatternId grammar: ^MP_[A-Z0-9_]+$
Width/Height: 4/4 exact
Cell count: 16 exact
Local origin: 기존 MAP00 tile 좌표 계약 재사용
Canonical cell index: y * 4 + x
Canonical order: cell index 0..15
```

- 16개 셀을 모두 explicit하게 보관한다. 빈 셀도 생략하지 않는다.
- 각 좌표는 `x=0..3`, `y=0..3` 안에 있고 exact one cell만 가진다.
- duplicate/missing/out-of-range coordinate를 거부한다.
- 새 coordinate system 또는 UnityEditor 의존 좌표를 만들지 않는다.

### 2.2 Layer와 operation

exact layer:

```text
Geometry
Surface
Affordance
Material
Hazard
Marker
```

exact operation:

```text
NoChange
AddSolid
CarveAir
SetSurface
SetAffordance
SetMaterial
SetHazard
SetMarker
```

호환 matrix:

| Layer | 허용 operation |
|---|---|
| `Geometry` | `NoChange`, `AddSolid`, `CarveAir` |
| `Surface` | `NoChange`, `SetSurface` |
| `Affordance` | `NoChange`, `SetAffordance` |
| `Material` | `NoChange`, `SetMaterial` |
| `Hazard` | `NoChange`, `SetHazard` |
| `Marker` | `NoChange`, `SetMarker` |

- 한 셀에서 같은 layer instruction은 최대 1개다.
- 생략된 layer는 `NoChange`와 동일한 의미지만 canonical digest에는 한 방식으로만 정규화한다.
- `NoChange`, `AddSolid`, `CarveAir`는 payload가 없어야 한다.
- `Set*` operation은 grammar `^[A-Z][A-Z0-9_]*$`의 non-empty stable payload ID가 필요하다.
- layer/operation mismatch, duplicate layer, invalid payload는 거부한다.
- payload ID의 실제 FK/schema는 MAP09_07/MAP10에서 연결한다. 여기서 임의 asset/prefab을 조회하지 않는다.

### 2.3 Weight와 biome compatibility

```text
Weight: integer 1..10000
Allowed biomes: non-empty, unique, canonical ordinal order
```

- float weight, normalized probability, RNG draw를 도입하지 않는다.
- MAP08의 기존 typed biome identity를 재사용한다.
- exact starter biome set은 `MoonCrater`, `CassiaRoot`, `AbandonedMill`, `MoonDough`다.
- duplicate biome enum/codec/catalog를 만들지 않는다.
- unknown biome와 empty allowlist를 거부한다.

### 2.4 Transform policy

exact transform:

```text
R0
MirrorX
MirrorY
R180
```

- definition은 non-empty unique allowlist를 가진다.
- `R0`는 항상 포함한다.
- `R90`, `R270`, arbitrary rotation/scale/translation은 금지한다.
- 이번 Task는 transform ID와 allowlist만 검증한다. 실제 cell-coordinate 변환은 MAP10_02다.

### 2.5 Protected policy

exact policy:

```text
ForceNoChange
RejectCandidate
```

- `ForceNoChange`: protected cell과 겹친 instruction을 적용 단계에서 `NoChange`로 mask한다.
- `RejectCandidate`: protected write가 하나라도 있으면 해당 Pattern 후보를 거부한다.
- protected cell에 실제 write를 허용하는 policy는 없다.
- 이번 Task는 policy 값과 선언만 구현한다. Spine/Envelope/ProtectedOpen mask 연산은 구현하지 않는다.

`Route Spine`, `Traversal Envelope`, MAP08 boundary `ProtectedOpen`, Special fixed entry는 향후 protected source이며 Pattern보다 우선한다.

---

## 3. Immutability, Validation, Digest

### 3.1 Immutability

- definition/cell/instruction/list/set은 defensive copy 후 read-only 노출
- caller collection mutation이 published 값·digest에 영향 0
- default/undefined enum 거부
- partial definition publish 금지
- static mutable state, Unity lifecycle, filesystem/CSV/clock/RNG 조회 금지

### 3.2 Validator error 최소 구분

```text
MissingInput
InvalidPatternId
InvalidDimensions
InvalidCellCount
DuplicateCell
MissingCell
CellOutOfRange
DuplicateLayerInstruction
InvalidLayerOperation
MissingPayload
UnexpectedPayload
InvalidPayloadId
InvalidWeight
MissingBiome
DuplicateBiome
UnknownBiome
MissingTransform
DuplicateTransform
MissingR0
UnsupportedTransform
InvalidProtectedPolicy
```

오류는 accumulated, stable-sorted, deduplicated하며 invalid input에서 exception·partial output·RNG draw가 없다.

### 3.3 Canonical digest

SHA-256 입력:

```text
Pattern ID
4x4 dimensions
weight
canonical biome IDs
canonical transform IDs
protected policy
cell index 0..15
각 cell의 canonical layer/operation/payload
```

display text, locale, object hash code, 입력 collection order, reflection/file order, timestamp를 제외한다.

같은 의미의 “생략 layer”와 explicit `NoChange`는 동일한 canonical representation/digest를 내야 한다.

---

## 4. 변경 경계

허용:

- `WorldGeneration/MicroPatterns/` 신규 Runtime C# + matching meta
- 대응 EditMode test 신규 C# + matching meta
- Result, 설치/Archive Task, Finalize Status

금지:

- 기존 MAP00~09_02 production/test 수정
- Pipeline/TerrainClusters/Activities/EventOverlays/SpecialRegions 등 다른 V2 root 수정
- CSV/Authoring/Generated/ScriptableObject/Scene/Prefab/Editor Window 수정·생성
- asmdef/asmref, ProjectSettings/Packages 수정
- renderer, transform executor, protected mask 계산, candidate selector, RNG, cleanup 구현
- 12×8 MicroChunk 정의 변경 또는 4×4를 MicroChunk라 명명
- `WorldGenerationRoot` 실행 연결
- unrelated dirty path stage/commit

신규 Runtime scope에서 금지:

```text
UnityEditor
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
class MicroChunk / struct MicroChunk / enum MicroChunk
```

---

## 5. 필수 검증

### 5.1 Focused `MAP09_03`

최소 다음을 자동 검증한다.

1. valid 4×4 definition = 16 explicit cells
2. canonical index/order 0..15
3. missing/duplicate/out-of-range cell 거부
4. exact layer/operation matrix
5. duplicate layer와 payload 규칙
6. ID/weight/biome positive·negative cases
7. transform allowlist와 mandatory R0
8. R90/R270 거부
9. exact protected policies와 allow-write policy 0
10. caller mutation 불변
11. 입력 순서/explicit NoChange 정규화 digest 동일
12. 의미 변경 시 digest 변경
13. accumulated/sorted/deduplicated errors
14. RNG/file/Unity lifecycle 사용 0
15. forbidden symbol 및 duplicate biome/MicroChunk type 0

### 5.2 Regression

최종 코드에서 별도 실행한다.

```text
MAP09_03 focused: discovered/executed >0, all PASS
MAP09_02 exact: 38/38 PASS
MAP09_01 exact: 26/26 PASS

MAP08 required: 9220/9220 PASS
MAP07 required: 5422/5422 PASS
MAP06 required: 2746/2746 PASS
MAP05 required: 1959/1959 PASS
Distinct total: 19347/19347 PASS
```

각 selection의 discovered/executed/pass/fail/skip을 분리 보고한다. timeout, zero-selection, 이전 결과 재사용은 PASS 근거가 아니다.

### 5.3 Unity/Static gate

```text
compile/Console/relevant warning: 0/0/0
Editor ready, compiling false, play mode stopped
Authoring CSV/meta: 50/50
Authoring manifest: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
Authoring/Generated changes: 0/0
Scene/Prefab: 0/0
ProjectSettings/Packages task-owned: 0/0
asmdef/asmref: 0/0
existing MAP00~09_02 modifications: 0
other V2 root changes: 0
duplicate GUID groups: 0
unapplied MCP candidates: 0
git diff --check errors: 0
unrelated staged/included: 0
```

meta before/after와 task-owned 증가 원인을 Result에 기록한다.

---

## 6. Result와 완료 조건

Result 파일:

```text
MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS_RESULT.md
```

상단 필수:

```text
TASK: MAP09_03_IMPLEMENT_MICROPATTERN_CONTRACTS
STATUS: PASS | FAIL | BLOCKED
MAP09_03: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_04_IMPLEMENT_CLUSTER_SPINE_ENVELOPE_CONTRACTS: LOCKED / DO NOT START
```

Result는 다음만 명확히 보고한다.

1. predecessor/Status/dirty preflight
2. 신규 파일 inventory
3. 16-cell·operation/layer·weight/biome·transform·protected 계약과 digest
4. focused 및 MAP09_01~02 regression
5. required `19347` regression
6. Unity/static/change-scope gate
7. out-of-scope dirty paths
8. atomic commit과 다음 Task 잠금

PASS 조건은 모든 계약·focused·regression·static gate 통과와 task-only commit이다. 실패 시 MAP09_04를 열지 않고 같은 MAP09_03 repair만 보고한다.

Commit:

```text
Subject: MAP09_03: implement MicroPattern contracts
Push: NOT PERFORMED
```

설치/Archive Task, task-owned Runtime/Test/meta, Result, Finalize Status만 commit한다. 기존 대량 MCP dirty state를 포함하지 않는다. PASS 후에도 MAP09_04를 자동 시작하지 않는다.
