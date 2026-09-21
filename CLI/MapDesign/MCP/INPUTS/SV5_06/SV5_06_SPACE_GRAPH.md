---
mcp_patch:
  format: single_task_v1
  task_id: SV5_06_SPACE_GRAPH
  task_file: TASKS/SV5_06_SPACE_GRAPH.md
  requires_current_task: NONE
  requires_completed_task: SV5_05_FIX01
  requires_result:
    path: REPORTS/SV5_05_FIX01_RESULT.md
    status: PASS
    sha256: 46d9e5b0dceb2a1efc431602df7d5da068642831e67468042f4f90498e4c1ef3
  requires_installed_task:
    path: TASKS/SV5_05_FIX01.md
    sha256: 4f0fb356be2a2ced028a890047580cec62bec65c19dd035ce1ccb258ae55b9b0
  sets_current_task: SV5_06_SPACE_GRAPH
---

# SV5_06_SPACE_GRAPH — 전체 장소·통로 공간 그래프

TASK: SV5_06_SPACE_GRAPH
NEXT: SV5_07_DIVERSITY — LOCKED / DO NOT START

## 목적과 권한

승인된624×416 구성에 따라 큰 장소·핵심 구역·보통 공간을 먼저 배치하고 실제포트/연결예약으로 묶은 C# 공간plan을 구현한다.
그 뒤12×8청크/4×4패턴의 예약소유로 분해한다. 기존04core를 보존하며05_FIX01의 접촉누락/검증표시를 이06책임에서 보완한다.
일반 지형 세부구현/장치/최종셀합성/Scene Bake는 해당후속Task가 담당한다. 이번 권한은 이 문서와 CONTRACT.md의 현재소유 범위다.
새 Task등록/규약변경은 필요없다.06은이미Status/Master에한번등록되어있어야 한다.

## 필수 READ

- 실제 MCP의00_MCP_ENTRYPOINT,01_PROJECT_LOCKED_RULES,05_CHANGE_CONTROL_RULES,07_PATCH_APPLY_RULES,08_STATUS_FINALIZE_RULES,
  APPLY_PATCH_AND_RUN_CURRENT_TASK,06_IMPLEMENTATION_STATUS,MASTER_IMPLEMENTATION_TASK_LIST를 읽는다.
- MCP/SV5/00~09의 현재규칙·소유/파일흐름. 04_RULE_COVERAGE_V5.csv/05_RULE_READSET_V5.json에서06의승인원문을 확인한다.
- MCP/INPUTS/SV5의SPACE_V5_RULES/MEMORY/TASKS, TASKS.json 및 baseline의regions/connections/validation/BASELINE_SHA,
  승인전체PNG·PDF와reference/v4의관련장소규칙. 이미지의핵심좌표를정본좌표로덮지않는다.
- MCP/GENERATED/SV5_03의BINDINGS/CORE_BINDINGS/DATA_SCHEMAS/TASK_COVERAGE와04의core/route/access/state geometry.
- 실제 선행05_FIX01의설치Task·Archive·PASS Result·BINDING·analysis·contact_checks·obligations·focused XML 및 소유commit.
- MCP/INPUTS/SV5_06/CONTRACT.md 전체, REVIEW_FINDINGS.json, SOURCE_LOCK.json, FILES.json, PROTOCOL_APPEND.md.
- 실제Sv5CoreReservationPlan/RmapSpecialReservationPlanner/RmapClusterAssemblyPlanner, RmapWorldGraphPlanner/Sv5RouteStatePolicy,
  RmapWorldDataContract/GeneratedCompletionSearch와04/05/FIX01/RMAP13직접tests. API를현지코드에서확인한다.
- READ의 파일위치는MapDesign/MCP를기준으로한것이다. SOURCE_LOCK의경로는Unity프로젝트root기준이다.

## 적용 전

STAGE.py의 package/preflight는읽기전용, stage는MapDesign/MCP_INBOX/SV5_06_SPACE_GRAPH.md하나만쓴다.
SOURCE_LOCK/MF/선행Result/Task/Archive/livecommit/상태가맞아야 한다. 기존MD나legacy후보를자동삭제/이동하지 않는다.
검증후stage된단일MD를native07절차로설치·Archive·CURRENT로연다. 이INPUTS의Task복사본을직접실행하지않는다.
기존nativeTask/Archive와bytecollision이면BLOCKED. Status/Master를자동보정하거나추가등록하지않는다.
동일 Task가이미CURRENT이면새Apply없이설치바이트와post-readonly를검증하고재개한다.
이미COMPLETE이면재실행하지않고결과/Finalize/소유commit/검토ZIP존재를읽기전용확인한다.

## WRITE ALLOWLIST — 구현 Phase

프로젝트root기준. 새파일·같은이름.meta는허용하고다른family/runtime전체의포괄수정은허용하지않는다.
- Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlan.cs (+.meta)
- Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlanner.cs (+.meta)
- Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphStateProjection.cs (+.meta)
- Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphExport.cs (+.meta)
- 같은SectorPlanning/Sv5RouteStatePolicy.cs: 공용접촉쌍열거/진실한검증상태/동일FSM소비접점/역탐색index의필수보완만.
- 같은SectorPlanning/RmapWorldGraphPlanner.cs: 기존FSM/action조건불변의순수공용분석접점 최소확장만.
- Assets/_Game/Tests/EditMode/Map/SV5/Sv5SpaceGraphPlanTests.cs (+.meta)
- 같은SV5/Sv5RouteStatePolicyTests.cs와Sv5RouteStateFix01Tests.cs: 접촉표시assertion정정과06출력격리, 기존핵심회귀보존.
- Assets/_Game/Tests/EditMode/Map/RMAP13/RmapWorldGraphPlannerTests.cs: 실제변경한분석접점의직접회귀만.
- MapDesign/MCP/SV5/10_SPACE_GRAPH_V5.md; 02_PROTOCOL_V5.md는동봉suffix만정확히한번append.
- MapDesign/MCP/GENERATED/SV5_06/ (새산출물, preview, legacy_exports, 임시_work, 최종검토ZIP).
- MapDesign/MCP/INPUTS/SV5_06/의동봉파일은불변; 필요한추가exporthelper만이번하위tools/에소유기록후생성가능.
- MapDesign/MCP/REPORTS/SV5_06_SPACE_GRAPH_RESULT.md

Apply/Finalize별도권한: 이Task설치/Archive, Status의Current블록과이Task행두필드만. Master쓰기없음.
과거RMAP/SV5 Input·Task·Archive·Result·GENERATED불변. Player/WorldDefinition/Packages/ProjectSettings/Scene/Prefab수정없음.
위목록밖의필수구조변경이필요하면원인·정확한경로를제시하고중단한다. 임의allowlist확장금지.

## 구현 순서

1. BINDING에실제Read/Write/API·최신선행/원본SHA·패키지/Task SHA·기존증거목록을기록한다.
2. CONTRACT C01/C05의공용접촉누락과잘못된checked표시를수정한다. 이전proofPASS와접촉미검증을분리한다.
3. C02/C03의core를보존하는세계좌표장소plan/authoring profile/보통공간/잔여소유를구현한다.
4. C04의실제port/Start EXIT/Village왕복/통로예약/조건경계를생성한다.
5. C05/C06의접촉전수검사·기존FSM기반실제공간투영·6순서·도달상태복귀검사를구현한다.
6. C07의chunk/pattern소유·결정성·JSON/CSV와C09전체/16확대도·HTML을같은plan에서출력한다.
7. C08집중검증을실제Unity에서실행한다.05/FIX01test출력을06하위로먼저격리하고옛증거불변을검증한다.
8. C10의후속책임·정리·활성문서·Result를작성한다. core/rawhistory를다시쓰지않는다.

## DONE CONDITIONS

- 계약C01~C10의이번소유범위와T01~T10책임이실제근거로충족된다.
- 같은624×416공간plan의장소·port·ordered통로·guard·소유·그림이일치한다.
- 기존core셀/identity/필수상태불변, 실제Start/Village접근결정과정상복귀가있다.
- 공통route때문에다른pair를놓치지않으며predicate나열을state검사로표시하지않는다.
- 실제공간의6순서/정상복귀/모든관련도달상태안전성및접촉검사를수행하고반례negative가거부된다.
- 기존추상11edge를몰래붙여삭제된물리예약경로를성공으로숨기지않는다.
- 최종Unityfocused failed/skipped0, 실제명령/버전/수치/XML SHA기록. Player/합성geometry완료주장없음.
- 기존evidence보존, 이번_work정리, 프로젝트root보조파일증가없음.

## RESULT / 정상종료

단일출력: MapDesign/MCP/REPORTS/SV5_06_SPACE_GRAPH_RESULT.md
첫독립line은TASK: SV5_06_SPACE_GRAPH 및실제STATUS: PASS/FAIL/BLOCKED이다.
선행livecommit, 역할별SHA, 구현API, 실제graph규모/분포/검사결과, 미구현소유, 집중XML, 불변·정리결과를포함한다.
PASS인경우만nativeFinalize와이번소유atomiccommit후GENERATED/SV5_06/SV5_06_REVIEW.zip을만든다.
최종286=245COMPLETE/0CURRENT/41LOCKED, CurrentNONE, SV5_07_LOCKED를확인한다.
ZIP구성과source manifest는CONTRACT C10을따른다. ZIP/commit의최종값은콘솔보고한다.
무관한dirty변경을stage하지않는다. 다음Task/전체회귀/PlayMode/build/Bake/RMAP18/19/VIS/push를시작하지않는다.
