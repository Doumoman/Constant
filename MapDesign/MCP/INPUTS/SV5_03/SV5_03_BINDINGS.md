# SV5_03_BINDINGS — 실제 코드·데이터 접점 조사

FORMAT: source_spec_v1
TASK_ID: SV5_03_BINDINGS
STATUS: PLANNED_NOT_INSTALLED
PREVIOUS_TASK: SV5_02_RULES
PREVIOUS_RESULT_SHA256: ed4e9ee6fd6f271bd81915040e1d6e103832a3737bd90d5eb90cc9371956a496
PREVIOUS_INSTALLED_ARCHIVE_SHA256: b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1
실제 single_task_v1은 현지 계약에 맞춰 별도 바인딩한다. 이 원본 명세를 이름만 바꿔 Apply하지 않는다.

## 목표

RMAP12~17·기존 SPACE 이력과 현재 코드/데이터를 읽어 후속 SV5가 재사용할 책임과 수정할 접점을 확정한다.
REUSE·ADAPT·NEW를 실제 파일/타입/API/header·key·소유·현재 SHA 근거와 함께 기록한다.
특히 SV5_04~06이 실제 핵심 site/protection·진행 상태·world/space 자료를 바로 찾을 수 있게 한다.
코드/게임 데이터 변경은 하지 않는다. 새 지역 지형·기능·월드 베이크는 각 후속 Task의 책임이다.

## 선행과 바인딩

1. 실제 AGENTS/MCP·Task 형식·Apply/Finalize·Master/Status/Current와 02_PROTOCOL을 읽는다.
2. 첨부 SV5_02 Result는 PASS지만 작성 당시 CURRENT이다. 실제 Finalize와 Task 소유 commit 여부를 현지에서 먼저 확인한다.
3. SV5_02가 아직 CURRENT이고 같은 원본 Result/Task/Archive가 검증되면, 이미 승인된 SV5_02의 정상 Finalize·소유 commit만 마무리한다. 코드를 재실행/Result amend하지 않는다.
4. SV5_02 COMPLETE·Current NONE·SV5_03 LOCKED를 확인한 후에만 이번 Task를 바인딩/Apply한다. 다른 Current나 충돌하는 증거는 자동 강제로 바꾸지 않는다.
5. SOURCE_LOCK의 실제 파일과 실제 local Result를 검증한다. reference 사본은 현지 선행 증거가 아니다.
6. 기대 시작 상태는 285 = 240 COMPLETE / 0 CURRENT / 45 LOCKED이다. 실제 상태를 기록하고 숫자에 맞추려 수정하지 않는다.
7. SV5_03가 이미 COMPLETE면 기존 정상 증거를 확인해 보고하고 종료한다. 45개 계획 행을 다시 만들지 않는다.
8. 실제 Read/Write 파일을 먼저 좁혀 300줄 이하 실행 Task와 별도 BOUND_TASK_SHA를 발행한다.

## 필수 Read

- 실제 AGENTS/MCP 진입점·Task/Apply/Finalize 계약·현재 상태·SV5_02 실제 Task/Archive/Result/생성 증거.
- MCP/SV5/00_APPROVAL_BASELINE.md, 01_SEQUENCE_V5.md, 02_PROTOCOL_V5.md, 03_RULES_V5.md, 04_RULE_COVERAGE_V5.csv, 05_RULE_READSET_V5.json.
- MCP/INPUTS/SV5의 SPACE_V5_RULES/MEMORY/TASKS 및 담당 reference/v4 상세 계약. 모든 파일을 한 번에 문맥에 넣지 않는다.
- MCP/INPUTS/SV5_03의 SOURCE_LOCK.json, AUDIT_SCOPE.json, OUTPUT_CONTRACT.md, FILE_FLOW.md, PROTOCOL_APPEND.md, 이 명세.
- 실제 RMAP12~17의 Task/Archive/Result 및 producer/consumer 코드·generated export. 필요한 RMAP02~11과 SPACE/SV4 이력은 호출 관계를 따라 읽는다.
- 실제 Player/prefab/scene 연결과 필요한 검사 진입점. 씬을 실행하거나 재생성하지 않고 파일로 조사한다.
- VIS01/VIS02 등이 현지 존재하면 별도 시각화 책임의 위치/범위를 읽는다. 별도 보고의 seed/pool/완료를 SV5 선행으로 대체하지 않는다.

MapDesign 상대 경로와 Unity project 상대 경로를 구분한다. GENERATED는 MapDesign/MCP/GENERATED다.
AUDIT_SCOPE의 이름은 검색 단서다. 없던 경로·타입·함수 시그니처를 실재하는 것으로 작성하지 않는다.

## 허용 Write

- MCP/SV5/06_BINDINGS_V5.md: 조사 요약과 산출물 진입점.
- MCP/SV5/07_FILE_FLOW_V5.md: FILE_FLOW.md와 동일 바이트 등록.
- MCP/SV5/02_PROTOCOL_V5.md: 지정한 SV5_03 표식 블록을 정확히 한 번 추가.
- MCP/GENERATED/SV5_03/: OUTPUT_CONTRACT의 CSV/JSON, 이번 조사 검증기·명시적 임시 _work 파일.
- 실제 정상 Result 경로, 이번 Task/Archive 및 정상 도구가 관리하는 최소 상태 표식.
- Apply 전 SV5_02 완료 처리가 필요한 경우에만 기존 승인된 정상 Finalize/소유 commit 절차를 수행한다. 별도 단계로 증거를 기록한다.

MCP/INPUTS의 기존 원본·기존 결과/Archive·활성 문서 중 허용되지 않은 부분은 보존한다.
기존 00_APPROVAL/01_SEQUENCE/03_RULES/04_COVERAGE/05_READSET을 다시 쓰지 않는다.
Assets·Scene·Prefab·.meta·Player·runtime CSV·ProjectSettings·Packages·게임 소스·검증/승인 정책은 변경하지 않는다.
Master에 행 추가/재정렬하지 않는다. 정상 Task 상태 갱신에 필요한 최소 변경만 따른다.
보조 파일은 MCP 하위에만 둔다. 과거 루트 패키지 자료는 참조 여부 조사/정리 판정을 남기고 경로 고정 증거를 무단 이동/삭제하지 않는다.

## 조사 절차

1. 현재 HEAD/dirty 상태를 기록하고 rg --files와 좁힌 rg 검색으로 실제 파일/소유/호출자를 찾는다.
2. 먼저 B01~04에서 실제 RMAP12~15와 SV5_04~06의 단위·핵심·진행 정본을 연결한다.
3. B05~13을 책임 단위로 읽는다. 오래된 소스와 새 기능이 함께 있으면 현재 활성 호출/참조와 각 완료 증거를 구별한다.
4. existing source/key/시그니처·입출력 schema·현재 SHA를 조사 표에 넣고 REUSE/ADAPT/NEW를 결정한다.
5. NEW는 실제 검색 결과와 소유 확장점을 기록한다. 미래 구현의 부재는 정상 조사 결과이며 빈 dummy API를 만들지 않는다.
6. 8개 물리 core site와 보호 셀·port·slot·진행 조건을 현재 정본에서 추출해 CORE_BINDINGS에 연결한다.
7. 모든 SV5_04~45 Task가 어느 접점을 읽고 무엇을 구현해야 하는지 TASK_COVERAGE로 연결한다.
8. 06_BINDINGS와 07_FILE_FLOW를 등록하고 프로토콜에 지정 표식 블록을 추가한다. 원문을 그대로 prefix로 보존한다.
9. 실재 경로/SHA/header/symbol·CSV/JSON 참조·작성 범위·MD 길이·임시파일 정리를 정적으로 확인한다.
10. 정상 Result를 작성하고 이번 Task만 Finalize·작업 소유 commit 후 멈춘다.

## 완료 기준

- 13개 조사 그룹에 실제 근거와 판정이 있고 42개 후속 Task가 유일하게 연결된다.
- 실제 8개 core site ID와 보호/접근/복귀/상태 접점이 확인된다. 그림의 좌표로 대체하지 않는다.
- SV5_04~06의 핵심 정본/소유/상태 접점에 미해결 충돌이 없다. critical 정본을 못 찾으면 BLOCKED 근거를 남긴다.
- REUSE/ADAPT는 실재 원소를 가리키고 NEW는 검색 근거/후속 책임을 가진다. 제안 이름을 기존 API 칸에 쓰지 않는다.
- 원문 입력과 선행 증거, 조사한 게임 소스/설정/scene/prefab 바이트가 보존된다.
- 기능 부재·정적 조사·이전 보고의 PASS·실제 실행을 구별한다. 이번 Unity/compile/tests/build/bake/Player는 NOT_RUN이다.
- 새 루트 보조 파일은 0개이며 이번 생성 임시파일은 정리 또는 보존 근거가 있다.
- 지정 프로토콜 블록은 한 번만 연결되고 07_FILE_FLOW는 원문과 동일 바이트다.

## 보고와 종료

Result에 변경 파일 SHA, 실제 선행 완료/commit, 핵심 binding, 검증, 남은 구현 책임을 기록한다.
INBOX_SHA / SOURCE_SPEC_SHA / BOUND_TASK_SHA / INSTALLED_TASK_SHA / ARCHIVE_TASK_SHA를 구분한다.
정상 시작 상태와 같다면 Apply 후 240 COMPLETE / 1 CURRENT / 44 LOCKED, Finalize 후 241 COMPLETE / 0 CURRENT / 44 LOCKED를 기대한다.
Finalize 전 Result를 완료 뒤로 꾸미지 않는다. 실제 Finalize 후 상태/commit은 콘솔에 별도로 보고하고 Result amend는 하지 않는다.
SV5_04 이후·RMAP18/19·VIS 후속 작업·Unity·push를 시작하지 않는다. 묶음 실행 전환도 하지 않는다.
