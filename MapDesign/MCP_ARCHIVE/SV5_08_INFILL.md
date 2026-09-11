---
mcp_patch:
  format: single_task_v1
  task_id: SV5_08_INFILL
  task_file: TASKS/SV5_08_INFILL.md
  requires_current_task: NONE
  requires_completed_task: SV5_07_DIVERSITY
  requires_result:
    path: REPORTS/SV5_07_DIVERSITY_RESULT.md
    status: PASS
    sha256: 2dcf0fa8a0da9d342c5c6ee9577feced6299bec15ffe7f716428e44128983810
  requires_installed_task:
    path: TASKS/SV5_07_DIVERSITY.md
    sha256: bd7940748dd4e25234f724db118cb7226bf13177488176e194a185045ee5ffcd
  sets_current_task: SV5_08_INFILL
---

# SV5_08_INFILL — 실제 작은 방·굴·계단참으로 사이 공간 구성

TASK: SV5_08_INFILL
NEXT: SV5_09_LOOPS — LOCKED / DO NOT START

## 목적 / 완료 계약

624×416의 기존 큰 장소와 핵심 진행을 보존하며 일반 공간을 추가한다.
이름표와 사각 예약만 추가하면 완료가 아니다. 바닥·벽·2칸 개구·짧은 연결·실제 1×1 셀과 4×4 패턴 payload를 만든다.
INPUTS/SV5_08/CONTRACT.md의 I01~I12 및 T01~T12가 이번 구현 계약이다.
INFILL_PROFILE.json은 운영 기본값이다. 목표256/최소128개와 공간 분포 기준은 사용자 원문 수치로 기록하지 않는다.
그 수치는 전체 맵 최종 밀도 승인이 아니다. 남은 미설계 면적과 구역별 결과를 함께 보여준다.
보상 없는 공간을 허용한다. Player/씬/특수 장치/다른 두 지역을 잇는 지름길은 각 후속 Task의 책임이다.

## READ

- native 00_MCP_ENTRYPOINT / 01_PROJECT_LOCKED_RULES / 05_CHANGE_CONTROL_RULES,
  07_PATCH_APPLY_RULES / 08_STATUS_FINALIZE_RULES / APPLY_PATCH_AND_RUN_CURRENT_TASK.
- 06_IMPLEMENTATION_STATUS / MASTER_IMPLEMENTATION_TASK_LIST의 기존 등록을 확인한다.
- MCP/SV5/02_PROTOCOL_V5 및 03_RULES/04_RULE_COVERAGE/05_RULE_READSET에서 SV5_08의 전체 원문.
- INPUTS/SV5/SPACE_V5_RULES / SPACE_V5_MEMORY / SPACE_V5_TASKS / TASKS.json,
  reference/v4/SPACE_FILL / SPACE_MOVEMENT / SPACE_NETWORK 및 승인된 624×416 예시.
- GENERATED/SV5_03의 BINDINGS/CORE_BINDINGS/DATA_SCHEMAS/TASK_COVERAGE 중 이번 직접 접점.
- SV5/10~15 공간·접촉·gate·product·반복 억제의 활성 보완 계약.
- SV5_07 Task/Archive/Result/BINDING/XML, default·repeat ON의 실제 plan·cells·ports·connections·gates.
- SV5_04 정본 core/route/access/state 및 현재 Sv5SpaceGraph 계열과 FIX04 physical validators.
- INPUTS/SV5_08의 CONTRACT/INFILL_PROFILE/SOURCE_LOCK/FILES/STAGE/PROTOCOL_APPEND 전체.
- 프로젝트 경로는 Unity root 기준, MCP 상대 READ는 MapDesign/MCP 기준이다.

## STAGE / APPLY / 재개

STAGE.py --mode package/preflight/post-readonly는 읽기 전용, --mode stage만 INBOX MD 하나를 쓴다.
선행 실제 commit와 parent, raw·blob SHA, 현지 상태와 등록을 검증한 뒤 정상 single_task_v1 Apply를 수행한다.
이번 ID는 이미 등록돼 있다. Status/Master 새 행 추가·가짜 Archive·강제 CURRENT 변경을 하지 않는다.
CURRENT면 같은 설치Task/Archive와 post-readonly를 검사하고 재Apply 없이 구현을 계속한다.
COMPLETE면 재구현하지 않고 PASS/Finalize/소유commit/Review를 확인한다.
INBOX가 다른 후보로 차 있으면 자동 정리하지 않고 경로를 보고한다. 입력 SHA/EOL 기대값 자동수정은 금지다.

## WRITE ALLOWLIST

Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/ 아래:
- 기존 Sv5SpaceGraphPlan.cs / Sv5SpaceGraphPlanner.cs / Sv5SpaceGraphExport.cs.
- 신규 Sv5SpaceInfill.cs / Sv5InfillPatterns.cs / Sv5InfillExport.cs 및 같은 이름 .meta.
- 기존 Plan 결과/07 의미를 보존하고 PlanWithInfill을 생산 후속 진입점으로 추가한다.
- Sv5SpaceDiversity.cs, GateGeometry, StateProjection, PhysicalMovement/Product, RMAP13 FSM은 읽기 전용이다.
- Planner 내 기존 gate repair/validator 조건은 완화하지 않는다. 일반 연결용 재사용 접점 노출과 재검증 호출은 허용한다.

Assets/_Game/Tests/EditMode/Map/SV5/ 아래:
- 신규 Sv5SpaceInfillTests.cs / Sv5InfillPatternTests.cs 및 .meta.
- 필요시 신규 Sv5TestOutputPaths.cs 및 .meta.
- 기존 9개 파일(CoreReservationPlan/RouteStatePolicy/RouteStateFix01/SpaceGraphPlan/
  SpaceGraphFix01~04/SpaceDiversity Tests)은 이번 _work로 출력 경로 격리만 변경한다.
- 기존82개 assertion·입력 fixture·이름·분기·skip 조건은 변경하지 않는다. 기존 읽기 경로는 유지한다.

MapDesign/MCP/ 아래:
- 신규 SV5/16_INFILL_V5.md, GENERATED/SV5_08/, REPORTS/SV5_08_INFILL_RESULT.md.
- SV5/02_PROTOCOL_V5.md는 PROTOCOL_APPEND.md의 정확한 suffix를 한 번 append한다.
- 동봉 INPUTS/SV5_08의 7개 파일은 불변. 추가 helper가 필요하면 그 하위 tools/만 소유 기록 후 사용한다.
- Apply/Finalize 권한은 이번 Task/Archive와 Status의 이번 행/Current 블록뿐이다.
Master·이전Task/Archive/Result/INPUTS/GENERATED·기존meta·무관한 dirty를 보존한다.
Player/Camera/Packages/ProjectSettings/Scene/Prefab/기존500패턴 데이터는 변경하지 않는다.

## STEPS

1. 선행·등록·소스 검증, 정상 Apply, BINDING에 read/write/API/비소유 dirty와 실제 소스 SHA를 기록한다.
2. 모든 기존 SV5 시험의 새 출력을 GENERATED/SV5_08/_work/legacy_exports/<fixture>/로 격리한다.
3. I03의 네 일반 공간 recipe와 기존6개 ordinary 내부를 실제 셀/4×4 payload로 구현하고 벽·바닥·출입구·왕복 반례부터 확인한다.
4. 기본/반복07 ON plan을 고정하고 기존 배치·큰 장소·core·gate를 보존하는 연결된 infill 생성을 붙인다.
5. 새 AIR까지 포함한 contact/physical product와 새 SOLID 충돌·로컬 이동 검사를 수행한다.
6. default/repeat의 실제 데이터, 전체624×416 전후, A1~D4, 4×4 확대와 미계획 잔여 지도를 같은 plan에서 내보낸다.
7. I09/T01~T12와 기존82개 실제 Unity focused를 실행한다. 일반 구현 실패는 허용 범위에서 수정·재시험한다.
8. obligations.csv의 07 완료 및 08 실제 부분 상태를 I10대로 바로잡는다. 과거 export 파일은 수정하지 않는다.
9. 이전 증거/비소유 dirty를 대조하고 이번 _work만 정리, 단일 Result와 활성 문서를 작성한다.
10. PASS 후 native Finalize, post-readonly, 이번 소유만 atomic commit, commit 기반 SV5_08_REVIEW.zip을 만든다.

## DONE / 범위

실제 방·굴·계단참·막다른 공간과 2칸 연결이 생산 plan에 포함되고 셀·패턴·출입구·접근/복귀가 일치한다.
default/repeat 두 고정 사례가 최소 개수·면적·분포, 기존 배치 보존, 모든 새 공간 연결, gate/product를 통과한다.
4×4/12×8 경계에서 셀 재구성이 동일하고 SOLID에 막힌 가짜 AIR 연결·1셀 구멍·우회가 음성 시험에서 거부된다.
구역별 새 공간 수·실제 셀 증가·미계획 잔여·목표 미달 이유를 보고하고 전체 완성 밀도와 혼동하지 않는다.
기존82개+새 시험의 failed/skipped=0, 최종 XML과 실제 tested source SHA가 일치한다.
ComposedGeometryReady=false, PlayerVerified=false다. 이번 local tile/static 결과만 명시한다.

## RESULT / FINALIZE / STOP

REPORTS/SV5_08_INFILL_RESULT.md에 TASK: SV5_08_INFILL, 실제 STATUS: PASS/FAIL/BLOCKED를 독립 행으로 쓴다.
패키지 검사/현지 Unity 실행/정적 타일/실제 Player를 구분하고 시험 명령·수치·XML raw와 blob SHA를 기록한다.
최종 상태: 290 = 251 COMPLETE / 0 CURRENT / 39 LOCKED, Current NONE, SV5_09_LOOPS LOCKED.
commit 뒤 GENERATED/SV5_08/SV5_08_REVIEW.zip을 만들고 실제 commit/parent/ZIP SHA/경로를 콘솔에 보고한다.
09+, RMAP18/19, VIS, 전체무필터시험, PlayMode/build/Bake/Player/push는 시작하지 않는다.
