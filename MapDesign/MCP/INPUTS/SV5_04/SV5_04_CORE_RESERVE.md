# SV5_04_CORE_RESERVE — 핵심 구역과 복귀 통로 선예약

FORMAT: source_spec_v1
TASK_ID: SV5_04_CORE_RESERVE
STATUS: PLANNED_NOT_INSTALLED
PREVIOUS_TASK: SV5_03_BINDINGS
PREVIOUS_RESULT_SHA256: 0b6f9c0a8ba658de8e911781f3b5a3e614c27333ff25faf3c63720e7486ae422
PREVIOUS_INSTALLED_ARCHIVE_SHA256: b8f6f5865dae26d512a50a3f62061c1370daec8bc12e9c56578ebda3565677b3
이 원본 명세는 현지 single_task_v1을 별도 바인딩해 실행한다. 원본 SHA를 bound Task SHA로 사용하지 않는다.

## 목표

실제 RMAP15 8개 core site·고정/보호 셀·port/slot/상태를 먼저 예약하고, 필수 접근·복귀 통로의 셀/지지/여유까지 소비자가 보호하도록 구현한다.
SV5_03 연결표의 REUSE/ADAPT 결정을 출발점으로 사용한다. 이번에는 작동하는 예약 API·소비 검사·실제 입력 기반 focused 증거를 남긴다.
SV5_05 진행 상태 규칙 확장, SV5_06 랜덤 장소 그래프, 후속 지형/장치·전체 월드 합성/Scene Bake는 시작하지 않는다.

## 시작과 선행

1. 실제 AGENTS/MCP 계약·02_PROTOCOL·Master/Status/Current를 읽는다.
2. SV5_03 Result는 작성 당시 CURRENT이다. 실제 Finalize/Task 소유 commit을 먼저 확인한다.
3. 아직 SV5_03 CURRENT면 동일 PASS Result·Task/Archive를 검증하고 기존 승인 범위의 정상 Finalize·소유 commit을 마무리한다. Result/조사 결과를 재작성하지 않는다.
4. SV5_03 COMPLETE·Current NONE·SV5_04 LOCKED에서만 이번 Task 바인딩/Apply를 진행한다. SV5_04가 이미 COMPLETE면 정상 증거를 보고하고 종료한다.
5. SOURCE_LOCK의 실제 파일·실제 선행 Result를 검사한다. INPUTS 사본은 현지 증거가 아니다.
6. 기대 시작 상태는 285 = 241 COMPLETE / 0 CURRENT / 44 LOCKED다. 숫자/이전 Status SHA로 상태를 강제하지 않는다.
7. BINDINGS/CORE_BINDINGS/DATA_SCHEMAS/TASK_COVERAGE를 실제로 열어 B01~08 및 SV5_04 관련 현재 source/API/schema를 확인한다.
8. 실제 readiness가 BLOCKED거나 정본/소유/소스 SHA 충돌이면 원인과 정확한 경로를 보고한다. 보고서 숫자로 빈 source를 만들어 대체하지 않는다.
9. 현지 최신 source와 SV5_03 snapshot을 대조하고 이번 Task의 정확한 Read/Write/호출·테스트를 300줄 이하 실행 Task에 바인딩한다.

## Read

- 실제 AGENTS/MCP Task/Apply/Finalize 계약, 현재 상태, SV5_03 local Task/Archive/Result/commit.
- MCP/SV5/00_APPROVAL_BASELINE.md, 01_SEQUENCE_V5.md, 02_PROTOCOL_V5.md, 03_RULES_V5.md, 06_BINDINGS_V5.md, 07_FILE_FLOW_V5.md.
- MCP/GENERATED/SV5_03의 네 CSV, SOURCE_SNAPSHOT.json, BINDING.json, validation.json.
- MCP/INPUTS/SV5 원본 규칙/계획과 관련 reference/v4의 SPECIALS/NETWORK/MOVEMENT/FILL 계약.
- MCP/INPUTS/SV5_04의 SOURCE_LOCK.json, RESERVATION_CONTRACT.md, VALIDATION_CONTRACT.md, OUTPUT_CONTRACT.md, 이 명세.
- 실제 RMAP12 definition/RNG, RMAP13 graph/state, RMAP14 owner, RMAP15 special/protection, RMAP16 route spine 생산/소비 코드와 generated 자료.
- 실제 Player 크기/이동/Grab의 정적 계약, source가 가리키는 focused test/asmdef/Unity 실행 접점.

MapDesign 상대 경로와 project 상대 코드 경로를 구분한다. 증거 위치는 MapDesign/MCP/GENERATED다.
CSV 원본은 이 패키지에 첨부되지 않았다. SOURCE_LOCK으로 고정한 현지 CSV 내용에서 정확한 경로·타입·시그니처를 읽어야 한다.
RmapSpecialReservationPlan.EvaluateTerrainCells와 RmapClusterAssemblyPlanner는 과거 RMAP15/16 보고의 검색 단서이며 현재 실제 선언을 확인한다.

## 허용 Write와 구현 경계

- 기존 WorldGeneration/SpecialRegions 또는 SV5_03이 지정한 실제 확장 위치의 이번 core reservation 전용 최소 source 파일.
- 후보 신규 이름 Sv5CoreReservationPlan.cs / Sv5CoreReservationPlanner.cs는 제안이다. 같은 책임이 이미 있으면 재사용하고 실제 파일 목록을 bound Task에 먼저 확정한다.
- 기존 보호 판정은 호출해 재사용하고 새 adapter는 접근/복귀 예약과 통합 조회/거부 책임만 맡는다. 별도 WorldDefinition/graph/site generator를 복제하지 않는다.
- 실제 기존 assembly 아래 SV5_04 focused test 파일과 Unity가 요구하는 대응 .meta.
- export에 별도 Editor adapter가 꼭 필요하면 이번 plan의 출력만 담당하는 파일 하나를 실제 경로로 바인딩한다. Generator Window/Scene builder를 확장하지 않는다.
- MCP/SV5/08_CORE_RESERVE_V5.md와 MCP/GENERATED/SV5_04의 명시된 출력·focused 증거·검토 ZIP·추적된 임시 _work 파일.
- 정상 MCP의 이번 Task/Archive/Result·최소 상태 표식과 필요 시 기존 SV5_03 정상 Finalize/소유 commit 단계.

기존 RMAP12~17 정본 site/보호 데이터·이전 입력/Archive/Result·SV5_03 조사 표·기존 활성 문서를 덮지 않는다.
기존 파일 변경이 꼭 필요하다는 근거가 발견되면 실제 SV5_03 ADAPT 접점과 이 기능에 필요한 최소 파일/변경을 Apply 전에 bound Write로 명시한다.
그 변경도 기존 site identity/보호·논리 상태/Player 능력의 의미를 바꿀 수 없다. 범위를 벗어난 재설계는 진단을 남긴다.
Scene/Prefab·Player 설정·카메라·NPC/전투/저장·BuildSettings/Addressables/Packages/ProjectSettings·공용 승인/검사 정책은 Write 밖이다.
Master 45개 계획 재등록/재정렬, 새 후속 Task 실행, 기존 무관한 dirty 변경의 revert/stage는 하지 않는다.

## 수행

1. 정상 선행을 확인하고 역할별 SHA가 분리된 bound Task를 발행해 SV5_04만 Apply한다.
2. RESERVATION_CONTRACT C01~C03에 따라 실제 immutable core plan과 셀/소유/protection을 재사용한다.
3. C04~C06에 따라 실제 필수 접근/복귀 spine·여유·지지·port flow·gate 조건의 예약을 연결한다.
4. C07의 결정적 조회/소비 판정과 실패 진단을 구현한다. 작동하지 않는 설정 표시나 종점 목록만으로 완료하지 않는다.
5. 검사한 동일 객체에서 C08/OUTPUT_CONTRACT의 자료를 생성한다.
6. VALIDATION_CONTRACT T01~T08 책임을 focused 시험으로 확인한다. 실제 필터/명령/XML과 통과·실패 수를 기록한다.
7. 원본/기존 입력 보호, 정확한 diff, 역할별 SHA, 파생 출력 일치, MD 길이, 임시 정리를 확인한다.
8. 정상 Result를 작성하고 이번 Task만 Finalize·소유 commit한다. 이후 동일 자료로 검토 ZIP을 만들어 최종 상태/commit/ZIP SHA를 콘솔에 보고한다.

## 완료 기준

- 실제 8개 core site와 대표 정본 2,432개 원본 셀 및 ID/slot/patch/port/state geometry가 보존된다.
- 필수 접근/복귀 요구마다 연속 셀 경로와 지지/여유·허용 방향·조건의 예약 근거가 있다.
- 일반 지형 후보의 core/air/slot/gate/route 보호 침범을 실제 소비 API가 거부하며 정상 후보는 허용한다.
- 같은 입력/기여 순서 변화에 결정적 결과이고, 진단 없는 relocation/fallback carve는 0이다.
- 기존 definition/RNG/Player 능력 변경 없이 작동하고 출력이 검사한 동일 plan과 일치한다.
- 필요한 focused 시험을 실제 실행해 통과했다. 미실행/실패를 PASS로 승격하지 않는다.
- 임시 자료는 정리됐고 모든 보조 파일이 MCP 하위에 있다. 정적 예약과 실제 Player 통과를 구분한다.

## 보고와 종료

독립 TASK/STATUS, source/code/test/generated 파일 경로·SHA, 실제 선행 완료/commit, 구현/미실행 범위, 테스트 명령/수치를 Result에 기록한다.
INBOX_SHA / SOURCE_SPEC_SHA / BOUND_TASK_SHA / INSTALLED_TASK_SHA / ARCHIVE_TASK_SHA를 별도 필드로 기록한다.
정상 시작과 같다면 Apply 후 241 COMPLETE / 1 CURRENT / 43 LOCKED, Finalize 후 242 COMPLETE / 0 CURRENT / 43 LOCKED를 기대한다.
Result 작성 시점의 상태를 정확히 기록한다. Finalize 후 상태·commit은 콘솔로 보고하고 immutable Result amend는 하지 않는다.
SV5_05 이후·RMAP18/19·VIS 후속·무필터/전체 회귀·PlayMode·Player build·Scene Bake·push는 진행하지 않는다.
현재는 한 Task 후 종료이며 묶음 자동 실행으로 전환하지 않는다.
