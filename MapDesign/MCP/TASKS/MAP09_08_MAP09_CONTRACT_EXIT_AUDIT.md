```yaml
mcp_patch:
  format: single_task_v1
  task_id: MAP09_08_MAP09_CONTRACT_EXIT_AUDIT
  task_file: TASKS/MAP09_08_MAP09_CONTRACT_EXIT_AUDIT.md
  requires_current_task: NONE
  requires_completed_task: MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES
  requires_result:
    path: REPORTS/MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES_RESULT.md
    status: PASS
    sha256: 324a6bb60f5747e950a6f3222ed7b00990b57af08e7245df8772b5e68f3b7467
  requires_installed_task:
    path: TASKS/MAP09_07_EXTEND_CSV_REGISTRY_AND_CREATE_COMPATIBILITY_FIXTURES.md
    sha256: 49aca5871b2c93ab3e002d54c457d08d92abaff1213ce4917a49cad8b7c976e6
  sets_current_task: MAP09_08_MAP09_CONTRACT_EXIT_AUDIT
```

# MAP09_08 — MAP09 Contract Exit Audit

```text
TASK: MAP09_08_MAP09_CONTRACT_EXIT_AUDIT
PHASE: MAP09 — V2 Contracts / CSV / Generated Models
STATUS: CURRENT
NEXT: MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION
NEXT STATUS: LOCKED UNTIL MAP09 EXIT RESULT IS REVIEWED AS PASS
```

## 0. Task Responsibility

이번 Task는 MAP09_01~07이 게시한 계약을 수정하거나 기능을 확장하지 않는다. 하나의 focused Phase Exit fixture에서 공개 API와 live digest를 연결해 다음을 판정한다.

```text
계층 소유권이 겹치지 않는가
→ pass 입력/출력이 완전한가
→ immutable publish와 실패 차단이 유지되는가
→ Authoring/Generated 방향이 뒤집히지 않는가
→ MAP07/MAP08 compatibility가 source를 변경하지 않는가
→ MAP10 구현을 시작할 계약 기준선이 완성됐는가
```

추가되는 기능은 **MAP09 contract phase-exit audit fixture와 승인 evidence**뿐이다. 새 production model, renderer, solver, CSV, content는 추가하지 않는다.

## 1. Binding No-Regression Policy

사용자의 최신 지시가 기존 Master의 “exit마다 19,347 전체 회귀” 문구보다 우선한다.

정상 경로:

```text
MAP09_08 focused only
Prior MAP00~09_07 test selections: 0
Legacy 19347 selections: 0
```

`19,347/19,347`은 MAP08까지 승인된 historical baseline으로만 기록하고 이번 PASS 증거로 재실행하지 않는다.

회귀를 허용하는 실제 문제 trigger:

- MAP09_08 focused audit 실패
- compile/Console error
- 승인된 live digest/count mismatch
- 기존 production/test/Authoring file drift
- asmdef/GUID/ownership/dependency 위반

trigger가 없으면 이전 category와 legacy selection은 금지한다. Trigger가 있으면 먼저 owner와 원인을 Result에 기록하고, 원인 국소화에 필요한 최소 selection만 실행한다. 전 범위 실행을 기본 repair로 사용하지 않는다.

## 2. Preflight

읽기 전용 확인:

1. MAP09_07 Result/설치/Archive Task SHA exact
2. MAP09_08만 CURRENT, inbox candidate 0
3. MAP09_01~07 Result가 모두 PASS/COMPLETE이고 이후 Task Result 없음
4. 모든 approved public catalog/fixture/digest가 compile된 live 값과 일치
5. Authoring CSV/meta `50/50`, Generated CSV `0`
6. existing file hash, asmdef, GUID, compile/Console, dirty worktree

불일치가 있으면 값을 재기준화하거나 이전 파일을 자동 수정하지 말고 `BLOCKED`다.

## 3. Exact MAP09 Live Baseline

Focused audit가 다음 exact 값을 public API에서 다시 계산한다.

| Owner | Exact evidence |
|---|---|
| MAP09_01 Pipeline | pass `10`, digest `90a2614f9a95c29f1546f350190010524672d4b4aa2d1ad1dfe7dbd431be50d5` |
| MAP09_02 Layers | layer `7`, digest `d0888c865cbdcc0884dc8abab9fac92900addd662a12a1ec30dc930f9cf4c94e` |
| MAP09_03 MicroPattern | `4×4/16`, digest `42c88cdb30154f098593d0e3be65063111613612fe5e9e1b9b11f2d9f1297a3d` |
| MAP09_04 TerrainCluster | fixture digest `e8c3228e6f9df360637023d68e9c243cb70df4122342a3251740054bbcc8f9f1` |
| MAP09_05 Activity | fixture digest `7a5357320d8e2634ab9416ae7c90fb80a83c1c7f799a8df7689ba37b8a0903bc` |
| MAP09_05 EventOverlay | fixture digest `722a490f054e5bfc5a75ac81e03eee4978cd7f51d34e01fa1e01818c9d4ce904` |
| MAP09_06 SpecialRegion | fixture digest `73fd2085ecf65057f25eec8b2ff4fceb1a4d1a1a0eadfd60b7595071936a7066` |
| MAP09_06 SectorCanvas | digest `7c26d2d12d418a6f203e793bffd49216c003a6c0fc6f6f2bea06d210d3bded0c` |
| MAP09_06 ValidationStamp | digest `cb909e6a1fc2a14bbd4e8b5a6ab103b5926e0428f535163f428f8dafda38a9f6` |
| MAP09_06 GeneratedSlice | digest `2066f58b09e3ac8ef0118c54e243008f54bcefe1e3bb032fa67dbe5d25156368` |
| MAP09_07 CSV schema | `15 tables / 83 columns / 13 FK / 2 approved legacy FK`, digest `272ec4f449a17179158720c94e92f6982cb5a32427ce6f6ea8ffc5eb92050621` |
| MAP08 compatibility | `6 pairs / 31 candidates / 62 projections`, digest `f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68` |

하나라도 다르면 expected 값을 바꾸지 않는다.

## 4. Cross-Contract Audit

### 4.1 Pass and ownership chain

Exact chain:

```text
Pacing
→ SpecialReservation
→ TerrainCluster
→ RouteSpine
→ TraversalEnvelope
→ MicroPattern
→ TerrainCleanup
→ ActivityEventOverlay
→ TileValidation
→ MicroChunkSlice
```

- 각 pass ID/order/input/output/failure owner는 unique하고 cycle이 없다.
- `SpecialRegion`은 TerrainCluster보다 먼저 예약된다.
- `TerrainCluster`가 TraversalGraph/Spine/Envelope를 소유한다.
- `ActivityStructure`가 MechanismGraph/ProgressionGraph를 소유한다.
- `EventOverlay`는 marker-only이며 graph를 소유하지 않는다.
- `MicroPattern=4×4 brush`, `MicroChunk/GeneratedSlice=12×8 output` 책임을 혼합하지 않는다.
- PDF 설명용 색 선을 coordinate graph source로 참조하지 않는다.

### 4.2 Safety and publication

- Pattern 적용 전 Spine/Envelope/ProtectedOpen/Special fixed entry가 보호된다.
- Activity/Event 제거 상태에서도 static shell과 필수 traversal이 유지된다.
- invalid contract는 partial artifact/index/digest를 publish하지 않는다.
- collection external mutation이 live artifact/digest를 바꾸지 않는다.
- validation failure escalation은 `Pattern → Cluster → Footprint`이며 임의 통로 생성 fallback이 없다.

### 4.3 Canvas, slice, and data direction

- SectorCanvas는 exact `48×32/1536 cells`다.
- GeneratedSlice는 `4×4 slices`, 각 `12×8/96`, 합계 1536 exact-once다.
- `Unvalidated Canvas`는 slice source가 될 수 없다.
- slice가 cell/provenance/persistence key를 변형·손실하지 않는다.
- Authoring schema 15개는 approved 5 roots에만 있고 physical V2 CSV는 아직 없다.
- Authoring FK는 Generated artifact를 target으로 가질 수 없다.
- Generated artifact를 Authoring source로 역승격하지 않는다.

### 4.4 Compatibility and forbidden dependency

허용된 compatibility:

```text
V2 terrain_cluster_cells.source_microchunk_id
  -> MAP07 microchunk_catalog.microchunk_id
V2 terrain_cluster_cells.source_boundary_chunk_id
  -> MAP08 boundary_chunk_catalog.boundary_chunk_id
```

두 edge는 read-only provenance이며 production type 소유권 이전이 아니다.

MAP09 신규 Runtime production에서 다음 직접 의존/hit은 0이어야 한다.

```text
StageMapGenerator
GridWorld
RoomTemplate
RoomGridTransform
TileMutationService
SectorRecipeResolver
UnityEditor
```

## 5. Implementation Boundary

허용:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Pipeline/
  Map09ContractPhaseExitTests.cs
  Map09ContractPhaseExitTests.cs.meta
```

필요하면 같은 파일 안에 helper/fixture를 둔다. production C#이나 두 번째 test file을 만들지 않는다.

추가 허용:

- 설치/Archive Task
- Result
- PASS 후 Finalize Status

금지:

- 기존 C#/test/CSV/meta 수정
- Runtime/Editor production 추가
- physical V2 Authoring/Generated CSV 생성
- solver/renderer/importer/slicer/content 구현
- asmdef/Scene/Prefab/Settings/Packages 변경
- 문제 trigger 없는 이전/legacy test 실행
- Master 또는 다음 Task를 미리 변경/생성
- unrelated stage/commit, Git push

## 6. Focused Exit Validation

Category `MAP09_08`만 실행한다.

최소 audit cases:

1. MAP09_01~07 exact live count/digest table
2. pass dependency order/unique/cycle/failure owner
3. seven-layer ownership and graph separation
4. Special-first reservation and protected traversal sources
5. 4×4 Pattern vs 12×8 Slice distinction
6. Activity/Event removal-safe and marker-only boundary
7. Canvas/stamp/slice dimensions and exact-once/provenance
8. 15-table schema PK/FK/index/Generated separation
9. MAP07/MAP08 compatibility counts/digest
10. immutable atomic publish and deterministic digest
11. forbidden production dependency hit 0
12. legacy Authoring 50-file manifest unchanged, Generated CSV 0

Result 필수 기록:

```text
MAP09_08 focused: discovered/executed/pass/fail/skip
REGRESSION TRIGGER DETECTED: NO | YES(reason)
PRIOR TASK TEST SELECTIONS: 0 (정상 경로)
LEGACY TEST SELECTIONS: 0 (정상 경로)
HISTORICAL LEGACY BASELINE: 19347/19347 (NOT RERUN)
```

Static gate:

```text
compile/Console/relevant warning: 0/0/0
new production C#: 0
new focused test C#/meta: 1/1
legacy Authoring CSV/meta: 50/50 unchanged
legacy Authoring manifest: f630219... exact
physical V2 Authoring/Generated CSV: 0/0
existing MAP00~09_07 modifications: 0
asmdef/Scene/Prefab/Settings/Packages changes: 0
duplicate GUID/unapplied candidate/diff-check: 0/0/0
unrelated staged/included: 0
```

## 7. Required Result Report

Result:

```text
MAP09_08_MAP09_CONTRACT_EXIT_AUDIT_RESULT.md
```

상단:

```text
TASK: MAP09_08_MAP09_CONTRACT_EXIT_AUDIT
STATUS: PASS | FAIL | BLOCKED
MAP09 PHASE EXIT: APPROVED | NOT APPROVED
MAP10_01_IMPLEMENT_PATTERN_CELL_SCHEMA_AND_VALIDATION: LOCKED / DO NOT START
```

첫 구현 섹션은 반드시 `Responsibility and Added Functions`다.

| 필드 | 필수 내용 |
|---|---|
| Task responsibility | MAP09 전체 contract surface와 Phase Exit 판정 책임 |
| Added functions | 새 focused exit fixture와 실제 audit 기능 |
| Inputs consumed | MAP09_01~07 public contracts/digests와 MAP07/MAP08 compatibility |
| Outputs produced | phase approval evidence와 다음 Phase 기준선 |
| Explicit non-ownership | production/gameplay/content/CSV/solver 미구현 |
| Downstream consumers | MAP10 이후가 신뢰할 approved contract surface |

그 뒤 다음을 보고한다.

1. predecessor/status/dirty preflight
2. exact live baseline 표
3. cross-contract ownership/pass/safety/data-direction audit
4. focused 결과와 regression trigger/selection 수
5. Unity/static/change scope/out-of-scope
6. MAP09 Phase Exit decision
7. atomic commit handoff

PASS일 때만 MAP09_08을 Finalize하고 task-owned 파일만 atomic commit한다.

```text
Subject: MAP09_08: approve MAP09 contract phase exit
Push: NOT PERFORMED
```

Result가 PASS여도 MAP10_01을 자동 시작하지 않는다. 사용자가 Result를 전달하고 별도 검수받을 때까지 계속 LOCKED다.
