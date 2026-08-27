```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
  task_file: TASKS/MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES.md
  requires_current_task: NONE
  requires_completed_task: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
  requires_result:
    path: REPORTS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL_RESULT.md
    status: PASS
    sha256: 0fbd1448b6bac27ff51774aac8d5198cc19f34d7ff97ad11be9b31ace5e43d8a
  requires_installed_task:
    path: TASKS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL.md
    sha256: 35185c5ea8a584cf89e97928e16fcf88c14684e5aaa7e6658a33e12aa741fd2f
  sets_current_task: MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
```

# MAP09_01 — Freeze Baseline and Register V2 Passes

```text
TASK: MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS
NEXT STATUS: LOCKED UNTIL THIS RESULT IS REVIEWED AS PASS
```

---

## 0. 작업 목적

MAP08 Exit 승인 상태를 변경 불가능한 MAP09 기준선으로 고정하고, 새 광역 지형 생성기가 사용할 V2 pass 순서·artifact 흐름·실패 소유권을 등록한다.

이 Task는 **새 Sector Solver를 구현하지 않는다.**

이 Task가 완료되면 이후 MAP09~21 구현은 다음 두 기준을 동시에 참조해야 한다.

1. MAP00~08에서 승인된 좌표·CSV·Site·Biome·Route·MicroChunk·Boundary 계약
2. `Cluster-first → Pattern-second → Chunk-slice-last` V2 pass 계약

---

## 1. 작업 전 읽기 전용 감사

코드를 변경하기 전에 실제 프로젝트에서 다음을 확인하고 Result에 기록한다.

1. Unity 버전과 Editor 상태
2. `Game.Map.Runtime`, `MapAuthoring.Editor`, EditMode/PlayMode test assembly 경계
3. MAP00에서 승인된 WorldGeneration runtime/test 폴더와 namespace
4. 현재 WorldGeneration pass/root/catalog 관련 타입과 실제 의존 방향
5. MAP08_14 Result와 MAP05~08 required test category 선택 방식
6. Authoring CSV 실제 root와 현재 sorted relative-path SHA-256 manifest
7. 현재 global Assets meta 수, Map subtree meta 수, duplicate GUID group 수
8. unapplied MCP patch 또는 기존 compile/Console error 유무
9. 작업 시작 전 dirty worktree와 이 Task 밖 사용자 변경 파일

감사 결과가 문서의 기준선과 다르면 임의로 맞추지 않는다. 차이가 기존 사용자 변경인지, 누락된 patch인지, 기준선 drift인지 판정하고 `BLOCKED`로 보고한다.

---

## 2. MAP08 승인 기준선

다음 값은 MAP08_14 승인 Result에서 가져온 exact baseline이다.

```text
Unity: 6000.3.8f1

MAP08_14 focused:       840/840 PASS
MAP08 required union:  9220/9220 PASS
MAP07 required:        5422/5422 PASS
MAP06 required:        2746/2746 PASS
MAP05 required:        1959/1959 PASS
Required subset total: 19347/19347 PASS

Boundary candidates:              31
Boundary microchunks:              31
Boundary tile rows:              2976
Boundary socket rows:              62
Directional projections:       62/62
Mandatory tool_requirement NONE: 31/31
Boundary pair count:                6

Boundary aggregate digest:
f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68

Authoring CSV count: 50
Authoring meta count: 50
Authoring manifest:
f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb

Generated CSV created by MAP08_14: 0
Scene/Prefab changes by MAP08_14: 0/0
ProjectSettings/Packages changes by MAP08_14: 0/0
asmdef/asmref changes by MAP08_14: 0/0
Duplicate GUID groups: 0
Unapplied MCP patches: 0
```

### 2.1 기준선 저장 원칙

- 테스트 실행 수치·digest·manifest는 Runtime gameplay 상수에 넣지 않는다.
- MAP09 baseline 전용 test fixture 또는 test data artifact에 둔다.
- 파일 경로는 기존 approved test fixture/data 규칙을 따른다.
- 새 JSON/CSV fixture가 필요하면 Test data로만 추가하고 Authoring/Generated 경로에는 쓰지 않는다.
- MAP08 production code나 authoring CSV를 수정하지 않는다.
- 기준 수치를 다시 계산한 실제 값과 위 exact 값이 모두 일치해야 한다.

---

## 3. V2 아키텍처 불변 규칙

다음 항목을 baseline documentation/test fixture에 명시한다.

### 3.1 용어

```text
4×4  = MicroPattern
12×8 = MicroChunk
48×32 = Sector Canvas
```

- 4×4 단위를 `MicroChunk`로 이름 붙이지 않는다.
- 12×8 MicroChunk는 일반 작은 방이 아니다.
- 12×8은 저장·스트리밍·검증·MAP08 경계 투영 단위다.
- 일반 지형은 완성된 12×8 청크 16개를 독립 추첨해 만들지 않는다.

### 3.2 생성 철학

```text
Cluster-first
→ Pattern-second
→ Chunk-slice-last
```

- TerrainCluster의 footprint와 실제 이동 골격을 먼저 결정한다.
- Route Spine과 Traversal Envelope를 보호한 뒤 4×4 MicroPattern을 적용한다.
- 검증이 끝난 48×32 Canvas만 12×8 slice 16개로 절단한다.
- 실패를 임의 통로 굴착, 전체 Sector 무작위 재생성, validation 완화로 숨기지 않는다.

### 3.3 그래프 책임

PDF의 초록·주황 설명선은 authoring/runtime graph data가 아니다.

향후 MAP09_04에서 다음을 서로 다른 계약으로 구현해야 한다.

1. `TraversalGraph`: 플레이어 Walk/Jump/Drop/Climb/Slide/Bounce
2. `MechanismGraph`: 달돌·절구·도탄·기어·장치 작동
3. `ProgressionGraph`: Trigger/Reward/Reset/Exit 상태 순서

MAP09_01에서는 타입을 미리 구현하지 않는다. Pass baseline이 이 분리를 위반하는 이름이나 artifact를 등록하지 않는지만 검사한다.

### 3.4 바이옴 경계 정본

- PDF의 4-pair 요약표는 정본이 아니다.
- MAP08에서 승인된 **6 pair / 31 candidate / 62 directional projection**이 정본이다.
- MAP08 fixed boundary content는 일반 4×4 Pattern보다 우선한다.
- ProtectedOpen 밖에서만 후속 Pattern 변형이 가능하다.

### 3.5 다양성 책임

- 4×4 Pattern만으로 큰 지형 구조를 만들지 않는다.
- 큰 구조 다양성은 `Cluster Footprint + Spine Variant + Sector Composition`이 담당한다.
- 4×4 Pattern은 보호된 경로를 유지하며 외곽·표면·이동 리듬을 변주한다.
- 단순 재질/장식 차이는 서로 다른 구조로 계산하지 않는다.

---

## 4. 구현할 V2 Pass Catalog

기존 approved runtime folder와 `StarNight.Map.WorldGeneration.*` namespace 안에 V2 pass catalog를 추가한다.

새 asmdef를 만들지 않는다. 기존 assembly dependency를 변경하지 않는다.

물리 파일 수는 자유지만 다음 semantic type/contract가 분명해야 한다.

### 4.1 Pass ID

정확히 다음 pass를 stable order로 등록한다.

| Order | Pass ID | 책임 | 주 출력 artifact |
|---:|---|---|---|
| 10 | `Pacing` | Sector/region의 PacingRole 계획 | `PacingPlan` |
| 20 | `SpecialRegionReservation` | 필수·희귀 SpecialRegion footprint 선예약 | `SpecialRegionReservationPlan` |
| 30 | `TerrainClusterReservation` | 남은 공간에 Cluster footprint 계획 | `TerrainClusterPlacementPlan` |
| 40 | `RouteSpine` | entry/exit와 cluster role을 잇는 이동 골격 계획 | `RouteSpinePlan` |
| 50 | `TraversalEnvelope` | 이동 종류별 보호 tile volume 계획 | `TraversalEnvelopePlan` |
| 60 | `MicroPattern` | 보호영역 밖 4×4 Add/Carve·surface 계획 | `PatternApplicationPlan` |
| 70 | `TerrainCleanup` | noise·head snag·pit 정리와 affordance/material 확정 | `CleanTerrainCanvas` |
| 80 | `ActivityEventOverlay` | Activity/Event marker 후순위 배치 | `ActivityEventPlacementPlan` |
| 90 | `TileValidation` | route·복구·밀도·ownership 검증 | `ValidatedSectorCanvas` |
| 100 | `MicroChunkSlice` | 검증된 48×32를 12×8×16으로 절단 | `GeneratedMicroChunkSlices` |

`SpecialRegionReservation`과 `TerrainClusterReservation`을 하나의 generic `Reservation` pass로 합치지 않는다.

`MicroChunkSlice`는 `TileValidation` 이전에 실행할 수 없다.

### 4.2 Pass Contract 필수 필드

각 catalog entry는 최소 다음 정보를 immutable/read-only 형태로 제공한다.

- Pass ID
- stable numeric order
- 입력 artifact ID 목록
- 출력 artifact ID 목록
- failure owner
- retry scope metadata
- deterministic RNG stream 사용 여부 또는 `NONE`
- Runtime/Editor 무관한 설명 ID

문자열 display name을 dependency key로 사용하지 않는다.

### 4.3 Artifact dependency

다음 선행 관계를 자동 검사한다.

```text
Pacing
→ SpecialRegionReservation
→ TerrainClusterReservation
→ RouteSpine
→ TraversalEnvelope
→ MicroPattern
→ TerrainCleanup
→ ActivityEventOverlay
→ TileValidation
→ MicroChunkSlice
```

- 모든 input artifact는 기존 MAP00~08 input 또는 앞선 V2 pass output이어야 한다.
- output artifact ID는 중복될 수 없다.
- 순환 dependency를 금지한다.
- unused intermediate output을 금지한다. 단, 최종 output은 예외다.
- pass 등록 순서는 CSV row order나 reflection discovery order에 의존하지 않는다.

### 4.4 Failure owner와 retry scope

MAP09_01에서는 retry 실행기를 만들지 않는다. Catalog metadata만 등록한다.

최소한 다음 의미를 구분한다.

- configuration/schema/baseline 오류: 즉시 실패, silent fallback 금지
- Pattern 후보 실패: Pattern 범위 재선택 가능
- Cluster 후보 실패: Cluster 범위 재선택 가능
- footprint 실패: footprint/reservation 범위로 escalation 가능
- final validation 실패: `Pattern → Cluster → Footprint` 순서 이외의 임의 복구 금지
- Slice 실패: 앞선 validated Canvas mutation 금지, 즉시 오류

실제 retry limit과 실행 정책은 후속 Planner Task에서 구현한다.

### 4.5 Determinism

- Catalog enumeration은 매 실행에서 동일한 order와 동일한 digest를 낸다.
- Stable ID 정렬 뒤 hash를 계산한다.
- pass display text, 파일 작성 시각, reflection order가 digest를 바꾸지 않는다.
- 아직 RNG를 실행하지 않는다.
- catalog entry에는 향후 사용할 stream ownership만 선언한다.

---

## 5. 구현 경계

### 5.1 허용

- 기존 approved WorldGeneration runtime folder의 신규 C#과 matching meta
- 기존 approved EditMode test folder의 신규 C#과 matching meta
- 필요 시 test-only fixture/data와 matching meta
- 이 Task Result 문서

### 5.2 금지

- MAP00~08 production C# 수정
- MAP00~08 기존 test fixture 수정
- Authoring CSV/meta 수정
- Generated CSV 생성
- Scene/Prefab 수정
- ProjectSettings/Packages 수정
- asmdef/asmref 생성·수정
- Editor window/debug overlay 구현
- ScriptableObject asset 생성
- Sector solver, footprint placer, Pattern renderer, graph compiler 구현
- Runtime Tilemap/Collider/Streaming/Save 구현
- Activity/SpecialRegion production model 선행 구현
- 기존 `WorldGenerationRoot`에 V2 pass 실행 연결
- 전체 프로젝트의 legacy 타입 삭제·rename
- unrelated dirty worktree 포함

### 5.3 신규 파일에서 금지되는 이름·의존성

신규/변경 V2 production scope에서 다음을 구현 기반 또는 신규 타입명으로 사용하지 않는다.

```text
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
UnityEditor   // Runtime production dependency
```

기존 프로젝트 전체에 이미 존재하는 legacy symbol은 삭제 대상이 아니다. Static gate는 이 Task change scope와 신규 V2 production 경로에 한정한다.

---

## 6. 필수 테스트

### 6.1 Focused MAP09_01 tests

최소 다음을 자동 검증한다.

1. exact pass count `10`
2. exact Pass ID와 stable order `10..100`
3. Pass ID·order·output artifact 중복 `0`
4. dependency cycle `0`
5. input artifact가 존재하지 않는 entry `0`
6. `SpecialRegionReservation < TerrainClusterReservation`
7. `RouteSpine < TraversalEnvelope < MicroPattern`
8. `TerrainCleanup < ActivityEventOverlay`
9. `TileValidation < MicroChunkSlice`
10. `MicroChunkSlice`가 최종 pass
11. catalog collection과 entry가 외부에서 mutation 불가
12. 동일 catalog digest 반복 일치
13. display text/reflection order가 digest에 포함되지 않음
14. silent fallback failure policy 없음
15. final validation escalation 순서가 Pattern→Cluster→Footprint로만 표현됨
16. test baseline의 MAP08 pair/candidate/projection/digest exact 일치
17. test baseline의 Authoring manifest exact 일치
18. Authoring 파일 변화 `0`
19. Generated CSV `0`
20. 신규 V2 production scope의 forbidden symbol hit `0`

### 6.2 Required regression

기존 category selection을 변경하지 않고 다음을 실제 실행한다.

```text
MAP08 required union:  9220/9220 PASS
MAP07 required:        5422/5422 PASS
MAP06 required:        2746/2746 PASS
MAP05 required:        1959/1959 PASS
Required distinct:    19347/19347 PASS
```

기존 category에 새 MAP09 test를 섞어 baseline count를 바꾸지 않는다.

Focused와 regression의 discovery 수·실행 수·PASS/FAIL/SKIP을 분리 보고한다.

timeout, zero-selection, 이전 job replay는 PASS 근거로 사용하지 않는다.

### 6.3 Unity gate

- compile error `0`
- Console error `0`
- 이 Task 관련 warning `0`
- Editor idle / ready_for_tools=true / is_compiling=false

---

## 7. Static Gate

Result 작성 직전에 다음을 검사한다.

- Authoring CSV/meta `50/50`
- sorted Authoring-relative SHA-256 manifest exact 일치
- Generated CSV created `0`
- Scene/Prefab tracked changes `0/0`
- ProjectSettings/Packages tracked changes `0/0`
- asmdef/asmref tracked changes `0/0`
- duplicate GUID groups `0`
- unapplied MCP patches `0`
- `git diff --check` error `0`
- out-of-scope existing dirty files 포함 `0`
- MAP00~08 production/test modification `0/0`
- forbidden symbol hit in new V2 production scope `0`

Global Assets/Map meta count는 신규 파일 수만큼 증가할 수 있다. before/after와 증가 원인을 정확히 보고한다.

---

## 8. 실패 처리

다음 중 하나라도 발생하면 다음 Task를 열지 않는다.

- 기준 manifest/digest/count 불일치
- MAP05~08 required regression 실패 또는 skip
- compile/Console error
- existing MAP00~08 production/test 수정 필요
- Authoring/Generated/Scene/Prefab/asmdef 변경 발생
- pass order·dependency·immutability 위반
- forbidden legacy dependency 발생
- 실제 프로젝트 구조가 문서와 달라 안전한 additive 위치를 확정할 수 없음

Result STATUS는 `FAIL` 또는 `BLOCKED`로 작성하고 같은 `MAP09_01` repair만 제안한다.

---

## 9. Result 문서 계약

작업 완료 후 다음 파일을 작성해 사용자에게 반환한다.

```text
MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES_RESULT.md
```

Result 맨 위에 반드시 다음을 포함한다.

```text
TASK: MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
STATUS: PASS | FAIL | BLOCKED
MAP09_01: COMPLETE ELIGIBLE | NOT COMPLETE
MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS: LOCKED / DO NOT START
```

### 필수 Result 섹션

1. `Preflight Audit`
   - Unity/assembly/namespace/folder/pass root
   - dirty worktree와 unapplied patch
2. `Baseline Evidence`
   - MAP08 digest, 6/31/62, Authoring hash, regression baseline
3. `Implemented File Inventory`
   - 신규/수정/삭제 파일과 matching meta
4. `V2 Pass Catalog Evidence`
   - exact order, artifacts, failure owner, retry scope, RNG ownership, catalog digest
5. `Focused Tests`
   - job ID, discovered/executed/pass/fail/skip
6. `Required Regression`
   - MAP05/06/07/08와 distinct total
7. `Unity Verification`
   - compile/Console/warning/Editor state
8. `Static Gates`
   - Authoring/Generated/meta/GUID/forbidden symbols/diff check/change scope
9. `Out-of-Scope Findings`
   - 읽기 전용으로 발견한 기존 문제; 없으면 `None`
10. `Commit and Phase Decision`
    - atomic commit subject/hash, push 여부, 다음 Task 잠금 상태

수행하지 않은 검증을 PASS로 기록하지 않는다. Discovery arithmetic와 실제 test execution을 구분한다.

---

## 10. 완료 판정

다음 조건을 모두 만족할 때만 `STATUS: PASS`다.

- MAP08 exact baseline 재현
- V2 pass 10개와 dependency/failure metadata 등록
- Focused test 전부 PASS
- required `19347/19347` 실제 PASS
- compile/Console/warning `0/0/0`
- Authoring/Generated/Scene/Prefab/Settings/asmdef 변화 없음
- MAP00~08 production/test 변화 없음
- forbidden dependency 없음
- atomic Result와 commit 범위가 이 Task에 한정됨

PASS Result가 사용자 검수를 통과한 뒤에만 `MAP09_02_DEFINE_LAYER_PACING_AND_ACCESS_CONTRACTS`를 별도 Task로 연다.
