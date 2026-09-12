---
mcp_patch:
  format: single_task_v1
  task_id: SV5_09_LOOPS
  task_file: TASKS/SV5_09_LOOPS.md
  requires_current_task: NONE
  requires_completed_task: SV5_08_FIX02
  requires_result:
    path: REPORTS/SV5_08_FIX02_RESULT.md
    status: PASS
    sha256: 91d75b8a00c14a971cd24783b8eee01c00ff1896d495b0cb962cfdd53dae37d2
  requires_installed_task:
    path: TASKS/SV5_08_FIX02.md
    sha256: 9cbc7801daf56d604f9313f2ea7935ca86a5285c4ba7bb57f82d316cd70f61db
  sets_current_task: SV5_09_LOOPS
---

# SV5_09_LOOPS — 실제 타일 재합류 통로와 지름길

NEXT: SV5_10_SIDEPATH LOCKED / DO NOT START
이 Task는 추상 그래프 선을 추가하는 작업이 아니다. SV5_08의 실제 방 사이에 1×1 타일로 된 약 2칸 높이의 통로를 만들고,
그 통로가 기존 경로와 함께 실제 순환을 이루는지 증명한다. 사용자 승인 규칙은 동봉 CONTRACT L01~L18이다.

## 선행과 적용

현지 MCP 00/01/05/07/08/APPLY/Finalize, SV5 활성 규칙, 이전 06~08 Task·Result·활성 문서 및 동봉
CONTRACT/LOOP_PROFILE/SOURCE_LOCK/PROTOCOL_APPEND를 읽는다. STAGE가 선행 FIX02 Result·Task·상태와 FIX01 구현 blob을
검증한 뒤 정상 Apply한다. FIX02는 감사 전용이므로 게임 구현 기준점은 검증된 FIX01 commit이다.

## Write allowlist

- Runtime SectorPlanning: 기존 Sv5SpaceGraphPlan.cs, Sv5SpaceGraphPlanner.cs, Sv5SpaceGraphExport.cs,
  Sv5SpacePhysicalMovement.cs, Sv5InfillExport.cs의 최소 통합 변경.
- 같은 폴더 신규 Sv5SpaceLoops.cs, Sv5LoopExport.cs 및 신규 파일의 matching meta.
- EditMode/Map/SV5 신규 Sv5SpaceLoopTests.cs 및 matching meta.
- 같은 폴더 기존 11개 tests.cs는 출력 경로를 이번 GENERATED/SV5_09_LOOPS/_work로 격리하는 변경만 허용한다.
- MapDesign/MCP/SV5/17_LOOPS_V5.md 신규, 02_PROTOCOL_V5.md에 동봉 suffix 정확히 1회 append.
- MapDesign/MCP/GENERATED/SV5_09_LOOPS/, REPORTS/SV5_09_LOOPS_RESULT.md, 정상 Task/Archive/Status 생명주기.
- 동봉 INPUTS/SV5_09_LOOPS는 불변이며 BINDING.json만 GENERATED에 기록한다.

Sv5SpaceInfill.cs/Patterns, gate geometry/FSM/product/state projection/diversity, Player, Scene/Prefab, Packages,
ProjectSettings, 과거 INPUTS·GENERATED·Task·Archive·Result, 레거시 파일, SV5_10+ 및 무관 dirty는 읽기 전용이다.
기존 gate를 넓히거나 원격 cut을 추가하지 않는다. 새 전역 solver/추상 region 계층을 만들지 않는다.

## 구현 순서

1. 실제 production `PlanWithInfill` 결과에서 방 경계·AIR 내부·SOLID 지지·보호 셀을 읽어 loop 후보를 만든다.
2. 서로 다른 기존 방/가지의 두 경계를 선택한다. 이미 직접 연결된 쌍, 조상-자식 한 간선, 같은 출입구 반복은 제외한다.
3. 1×1 셀 중심선을 생성하고 각 보행 칸의 AIR 두 칸, 바닥 SOLID, 회전/상승 머리 여유를 검증한다.
4. 입구로 뚫는 기존 SOLID는 명시적 aperture override로만 기록한다. 역사적 Infill 객체/산출물은 덮어쓰지 않는다.
5. 양 끝 포함 중심선은 4~24칸이다. 상하는 지지된 +1 계단만 허용한다. +2 Jump+Grab 및 20~50칸 샛길은 10으로 보류한다.
6. 후보를 하나씩 실제 physical movement union에 넣고 모든 합법 FSM 상태와 6 resource order를 재검증한다.
7. 진행 gate 우회, Type0/예약/고정 core 침범, stub, 외부 AIR 의존, 중복, 자기교차 후보는 reject한다.
8. 새 연결을 제거해도 두 끝점의 기존 경로가 남아야 한다. 새 연결을 넣으면 cycle rank가 정확히 1 증가해야 한다.
9. 기존 동일 상태 최단거리보다 짧을 때만 RANDOM_SHORTCUT, 그 외 유효 순환은 LOOP로 분류한다.
10. 기본 seed1304와 repeat seed를 생성한다. profile 목표를 best-effort로 채우되 minimum/분산 조건은 반드시 만족한다.
11. production API, export, 독립 checker, focused EditMode를 돌리고 실제 타일 확대도/전체 지도/지표를 만든다.
12. 실패는 허용 범위에서 수정·재시험한다. source/state/hash/권한 충돌만 BLOCKED로 종료한다.

## 완료 증거

동봉 CONTRACT의 필수 export와 검사를 모두 PASS한다. 기존 102개 시험 이름·책임을 보존하고 신규 시험을 더한다.
XML은 total/passed/failed/skipped/inconclusive와 raw SHA를 기록한다. default/repeat 각각 accepted>=16, 12개 이상 sector,
endpoint-inclusive max<=24, illegal bypass=0, dangling=0, duplicate=0을 Result에 기록한다.
ComposedGeometryReady=false, PlayerVerified=false이며 실제 씬/플레이 완료로 승격하지 않는다.

Finalize 전 post-readonly, 실제 PASS 후 native Finalize, Finalize 후 post-readonly, task-owned atomic commit, commit 기반 Review ZIP
순서다. 최종 292=254 COMPLETE/0 CURRENT/38 LOCKED, Current NONE, SV5_10 LOCKED다. push하지 않는다.
Review ZIP은 GENERATED/SV5_09_LOOPS/SV5_09_LOOPS_REVIEW.zip 하나이며 자기 자신·_work·구형 복제를 제외한다.
다음 Result/Review가 올라오면 ChatGPT가 검토와 다음 패키지·인라인 명령을 한 번에 제공한다.
