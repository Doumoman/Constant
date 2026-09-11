---
mcp_patch:
  format: single_task_v1
  task_id: SV5_07_DIVERSITY
  task_file: TASKS/SV5_07_DIVERSITY.md
  requires_current_task: NONE
  requires_completed_task: SV5_06_FIX04
  requires_result:
    path: REPORTS/SV5_06_FIX04_RESULT.md
    status: PASS
    sha256: 59652c300adf9b2d8ddc67eaddc8bc3fb8932793ab75bee80d9814c12d189077
  requires_installed_task:
    path: TASKS/SV5_06_FIX04.md
    sha256: aa8640e5cbffb672653a78312acdfad5ec6094d5b81ab9b3fdb930294175188b
  sets_current_task: SV5_07_DIVERSITY
---

# SV5_07_DIVERSITY — 가까운 독립 지형의 반복 억제

TASK: SV5_07_DIVERSITY
NEXT: SV5_08_INFILL — LOCKED / DO NOT START

## 목적

같은 종류의 독립 동굴·도서관 등이 가까이 몰리는 선택 확률을 줄인다.
장소 삭제·크기 축소·큰 AIR 띠로 개선하지 않는다. 연속된 한 지형의 청크/패턴은 하나로 센다.
실제 PlaceFamily 후보 선택을 바꾸고 624×416 전후 좌표·통로 비교를 제공한다.
`INPUTS/SV5_07/CONTRACT.md` D01~D10과 V01~V12가 이번 완료 계약이다.
DIVERSITY_PROFILE.json의 36칸/100·70·49·34는 운영 기본값이며 사용자가 지정한 수치로 기록하지 않는다.
일반 +1, Jump+Grab 최대 +2, 4×4 패턴/12×8 청크와 정본 core를 유지한다.

## READ

- native 00_MCP_ENTRYPOINT, 01_PROJECT_LOCKED_RULES, 05_CHANGE_CONTROL_RULES,
  07_PATCH_APPLY_RULES, 08_STATUS_FINALIZE_RULES, APPLY_PATCH_AND_RUN_CURRENT_TASK.
- 06_IMPLEMENTATION_STATUS와 MASTER_IMPLEMENTATION_TASK_LIST. 이번 ID는 이미 한 번 등록돼 있다.
- MCP/SV5/00~14의 현재 활성 규칙. 04_RULE_COVERAGE/05_RULE_READSET에서 SV5_07 관련 원문을 찾아 읽는다.
- INPUTS/SV5의 SPACE_V5_RULES/MEMORY/TASKS, TASKS.json 및 승인 전체 예시. 그림 좌표로 정본 core를 덮지 않는다.
- GENERATED/SV5_03의 BINDINGS/CORE_BINDINGS/DATA_SCHEMAS/TASK_COVERAGE 중 이번 실제 접점.
- SV5_06_FIX04의 설치Task/Archive/PASS Result/BINDING/validation/plan/좌표/문/시험XML과 소유commit.
- SV5_04의 core/route/access/state와 현재 Sv5SpaceGraph 계열, GateGeometry/PhysicalMovement/Product 및 직접 시험.
- INPUTS/SV5_07의 CONTRACT/DIVERSITY_PROFILE/SOURCE_LOCK/FILES/STAGE/PROTOCOL_APPEND 전체.
- 모든 프로젝트 경로는 Unity root 기준이며 MCP 상대 READ는 MapDesign/MCP 기준이다.

## 단계와 사전 검증

STAGE.py --mode package/preflight/post-readonly는 읽기 전용이다. --mode stage만 INBOX MD 하나를 생성한다.
패키지/선행Result·설치Task·Archive/실제commit/Status/Master/소스가 맞아야 stage한다.
정상 INBOX 하나를 native single_task_v1 절차로 설치·Archive하고 이 행만 CURRENT로 연다.
INPUTS의 Task 복사본을 직접 구현 지시로 실행하거나 Status/Master 등록을 다시 하지 않는다.
이미 CURRENT이면 동일 Task/Archive와 post-readonly를 검사하고 재Apply 없이 계속한다.
이미 COMPLETE이면 재실행하지 않고 Result/Finalize/소유commit/Review ZIP을 확인한다.
다른 INBOX 후보와 충돌하면 자동 삭제·이동하지 않는다. SHA/EOL/경로/상태 기대값 자동수정은 금지다.

## WRITE ALLOWLIST

`Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/` 아래:
- 기존 Sv5SpaceGraphPlan.cs, Sv5SpaceGraphPlanner.cs, Sv5SpaceGraphExport.cs.
- 신규 Sv5SpaceDiversity.cs 및 같은 이름 .meta.

`Assets/_Game/Tests/EditMode/Map/SV5/` 아래:
- 신규 Sv5SpaceDiversityTests.cs 및 .meta.
- 필요한 경우 신규 Sv5TestOutputPaths.cs 및 .meta.
- 기존 Sv5CoreReservationPlanTests.cs, Sv5RouteStatePolicyTests.cs, Sv5RouteStateFix01Tests.cs,
  Sv5SpaceGraphPlanTests.cs, Sv5SpaceGraphFix01Tests.cs, Sv5SpaceGraphFix02Tests.cs,
  Sv5SpaceGraphFix03Tests.cs, Sv5SpaceGraphFix04Tests.cs는 출력 경로 격리만 허용한다.
- 기존 70개 시험의 assertion/분기/이름/카테고리 완화·삭제·skip은 허용하지 않는다.

`MapDesign/MCP/` 아래:
- 신규 SV5/15_DIVERSITY_V5.md.
- SV5/02_PROTOCOL_V5.md는 PROTOCOL_APPEND.md의 suffix만 정확히 한 번 append.
- 신규 GENERATED/SV5_07/ 및 REPORTS/SV5_07_DIVERSITY_RESULT.md.
- 동봉 INPUTS/SV5_07은 불변. 필요한 추가 helper는 그 하위 tools/에 소유 기록 후 생성한다.

Apply/Finalize 별도 권한은 이번 installedTask/Archive와 Status의 이번 행/Current 블록뿐이다.
Master·이전Task/Archive/Result/INPUTS/GENERATED·기존meta는 보존한다.
GateGeometry/PhysicalMovement/PhysicalProduct/StateProjection/RMAP13 FSM과 정본 core는 읽기 전용이다.
Player/Camera/Packages/ProjectSettings/Scene/Prefab은 변경하지 않는다. 범위 밖 필수 수정은 정확한 경로/원인으로 보고한다.

## STEPS

1. 실제 선행과 post-readonly를 확인하고 BINDING에 읽기/쓰기/API/역할별 SHA와 비소유 dirty 목록을 기록한다.
2. 기존 시험의 새 출력부터 GENERATED/SV5_07/_work/legacy_exports/<fixture>/로 격리한다.
3. D02~D04의 family/formation·거리·가중치·결정적 후보 선택을 생산 PlaceFamily에 연결한다.
4. D07의 고정 selector fixture와 실제 repeat profile을 정의하고 ON/OFF 입력을 고정한다.
5. 기본/반복 profile의 실제 624×416 plan을 만들고 FIX04 validators/product로 재검증한다.
6. 같은 plan의 CSV/JSON/전체 전후 SVG/격자 확대/HTML을 D08에 따라 출력한다.
7. 기존70+새focused를 실제 Unity에서 실행한다. 일반 구현 실패는 허용 범위에서 수정·재시험한다.
8. 이전 증거/비소유 dirty 불변을 확인하고 이번 임시 _work만 정리한다. 활성 문서와 단일 Result를 작성한다.
9. PASS 후 native Finalize, post-readonly, 이번 소유만 atomic commit, commit 기반 Review ZIP을 만든다.

## DONE

- 실제 후보 선택이 변화하고 같은 family/formation을 올바르게 집계한다.
- 중복이 없는 기본 예시를 개선 효과로 포장하지 않는다. 반복 fixture의 가까운 쌍이 감소한다.
- 전후 장소 수·개별 크기·종류별 수·core가 같고 기존 물리 연결/문/FSM 안전성이 보존된다.
- 고정 seed/입력 순서 독립성·fallback·음성 fixture·실제 export/digest를 검증한다.
- 기존70개와 새시험의 실제 failed/skipped=0이며 소스 SHA/XML이 일치한다.
- 이전 GENERATED 불변, 이번 _work 정리, 중복 legacy export/루트 보조파일 누적 없음.
- ComposedGeometryReady=false, PlayerVerified=false다.

## RESULT / FINALIZE / STOP

단일 Result: MapDesign/MCP/REPORTS/SV5_07_DIVERSITY_RESULT.md.
독립 행 TASK: SV5_07_DIVERSITY 및 실제 STATUS: PASS/FAIL/BLOCKED를 기록한다.
패키지 검증과 Unity 실행, 좌표 계획과 실제 Player를 구분한다. 최종 시험 명령/수치/XML raw·blob SHA를 남긴다.
이번 PASS 후만 native Finalize하고 소유 파일만 commit한다. Result에 자신의 최종 commit SHA를 억지로 자기참조시키지 않는다.
최종 290 = 250 COMPLETE / 0 CURRENT / 40 LOCKED, Current NONE, SV5_08_INFILL LOCKED.
commit 후 GENERATED/SV5_07/SV5_07_REVIEW.zip을 만들고 commit/parent/ZIP SHA/위치를 콘솔에 보고한다.
SV5_08+, RMAP18/19, VIS, 전체무필터시험, PlayMode/build/Bake/Player/push는 시작하지 않는다.
