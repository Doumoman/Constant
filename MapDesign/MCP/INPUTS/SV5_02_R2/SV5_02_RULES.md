# SV5_02_RULES — 활성 규칙 등록 원본 명세

FORMAT: source_spec_v1
TASK_ID: SV5_02_RULES
STATUS: PLANNED_NOT_INSTALLED
INPUT_REVISION: R2
PREVIOUS_TASK: SV5_01_APPROVAL_BASELINE
PREVIOUS_RESULT_SHA256: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
PREVIOUS_INSTALLED_ARCHIVE_SHA256: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed
이 파일은 원본 명세다. 현지 실제 single_task_v1을 별도 바인딩·발행해 정상 Apply한다.

## 목표와 범위

SV5_01에서 등록한 승인 기준과 45개 계획에 규칙 원본·우선순위·후속 읽기 경로를 연결한다.
첫 작업의 계획 등록을 다시 수행하지 않는다. 45개 행을 추가하거나 기존 전체 순서를 다시 만들지 않는다.
이번 결과는 활성 규칙 등록이며 게임 코드·지형·물리·맵 Bake 변경이 아니다.

## 선행 확인

1. 실제 AGENTS/MCP 계약과 SV5_01에서 만든 MCP/SV5/02_PROTOCOL_V5.md를 읽는다.
2. 실제 SV5_01 Result를 현지 계약에서 찾고 위 SHA·TASK·독립 STATUS: PASS를 검사한다. 이 패치의 reference 사본은 현지 증거를 대체하지 않는다.
3. MCP/TASKS와 MCP_ARCHIVE의 SV5_01 바이트가 위 SHA와 같고 실제 상태가 COMPLETE인지 확인한다.
4. 사용자 콘솔이 보고한 SV5_01 Finalize commit 9e6a71489fb4881c4112c378f7ca043bbbd85e85와 실제 Status/Current를 현지 확인해 BINDING에 기록한다. Result 원문은 수정하지 않는다.
5. 보고서의 da180344 HEAD는 SV5_01 전 기준이다. 그 HEAD를 새 시작 HEAD로 강제하거나 Result를 amend하지 않는다.
6. 예상 시작은 285 = 239 COMPLETE / 0 CURRENT / 46 LOCKED, SV5_02 LOCKED이다. 숫자를 맞추려고 상태를 수정하지 않는다.
7. 이미 SV5_02가 완료되어 있으면 동일 입력·정상 증거를 확인해 재사용하고 중복 실행하지 않는다.

## 필수 Read

- 현지 AGENTS·MCP 진입점·Task 형식·Apply/Finalize 계약·Master/Status/Current와 실제 선행 증거.
- MCP/SV5/00_APPROVAL_BASELINE.md, 01_SEQUENCE_V5.md, 02_PROTOCOL_V5.md.
- MCP/GENERATED/SV5_01/BASELINE.json, PLAN_LINK.json, BINDING.json.
- MCP/INPUTS/SV5의 SPACE_V5_RULES.md, SPACE_V5_MEMORY.md, SPACE_V5_TASKS.md와 필요한 reference/v4 명세.
- MCP/INPUTS/SV5_02_R2/SOURCE_LOCK.json, candidate/의 3개 파일, PROTOCOL_APPEND.md, 이 원본 명세.
- 규칙을 읽는 실제 문서 진입점 한 곳. 기존 프로토콜 연결을 확인하며 모든 소스 API 조사는 SV5_03에 남긴다.

모든 상대 경로는 MapDesign 모듈 루트 기준이다. GENERATED는 MCP/GENERATED를 사용한다. --map-root에 MapDesign/MCP를 넣어 보정하지 않는다.
MCP/INPUTS/SV5 원본과 첫 입력 manifest는 수정하지 않는다. 새 입력은 SV5_02_R2 폴더에만 추가한다. 기존 SV5_02 입력도 보존한다.

## 허용 Write

- MCP/SV5/03_RULES_V5.md: 활성 규칙 인덱스. candidate와 동일 바이트로 등록한다.
- MCP/SV5/04_RULE_COVERAGE_V5.csv: 원문 문장별 분류·추적표. candidate와 동일 바이트로 등록한다.
- MCP/SV5/05_RULE_READSET_V5.json: 담당 Task별 원문 경로·SHA. candidate와 동일 바이트로 등록한다.
- MCP/SV5/02_PROTOCOL_V5.md: PROTOCOL_APPEND.md의 SV5_02 표식 블록을 한 번만 연결하는 최소 변경.
- MCP/GENERATED/SV5_02/ 이하 RULE_REGISTRATION.json, BINDING.json, validation.json과 정상 Result 경로.
- 현지 정상 도구가 관리하는 이 Task의 Task·Archive·Status·Current 및 필요한 상태 표식.

위 대상이 이미 있으면 읽고 소유/내용을 확인한다. 같은 바이트는 재사용하며 다른 책임의 문서를 무조건 덮지 않는다.
이전 00_APPROVAL_BASELINE·01_SEQUENCE·입력 원본·선행 Task/Archive/Result·SV5_01 출력은 보존한다.
Master의 행 추가나 전체 재생성은 하지 않는다. 정상 현지 계약이 요구하는 SV5_02 관련 최소 상태 변경만 허용한다.
Assets·Scene·Prefab·Player·맵 생성 코드·런타임 CSV와 검증/승인 정책 변경은 Write 범위 밖이다.

## 수행

1. 패키지를 UTF-8로 검증하고 SOURCE_LOCK의 원본/선행 파일을 정상 시작 상태에서 검사한다.
2. 실제 Read/Write·선행·native 절차를 바인딩한 300줄 이하 실행 Task와 외부 BOUND_TASK_SHA를 발행한다.
3. 정상 Apply로 SV5_02만 시작한다. 원본 명세 SHA를 새 bound Task의 기대값으로 사용하지 않는다.
4. candidate 3개를 지정 경로에 동일 바이트로 등록한다. 규칙 내용을 임의 추가/축소하지 않는다.
5. 02_PROTOCOL에 표식 블록이 없으면 하나 추가한다. 기존 원문 바이트를 앞부분에 그대로 보존하고 필요한 줄바꿈 뒤 UTF-8 블록을 붙인다. 같은 블록이 있으면 재사용하며 중복 삽입하지 않는다.
6. 원문 우선순위·초기 제작 기본값·미검증 점프 후보·고정하지 않은 비율을 구분해 읽을 수 있는지 확인한다.
7. 문서 연결과 원문 SHA를 검사하고 정상 Result·Finalize·현지 소유 commit 절차를 따른다.

## 등록할 내용

- 624×416 승인된 공간 구성, 1×1 타일·4×4 패턴·12×8 청크 및 기존 Player/카메라 규격.
- 핵심 보호·복귀·진행 조건, 일반 방/굴/다중 연결, 샛길·종류 반복 억제와 최종 6×6 검사.
- 점프맵 JUMP-01~14: 실제 SOLID·안전 Grab, 1~2칸 외곽 굴곡과 이동/보호 마스크.
- 일반 +1칸·Jump+Grab 최대 +2칸, 실제 몸체 여유·고체 아래면/옆면·착지·재시도 검사.
- 최소 Grab 1회·외곽 2구간은 AUTHORING_DEFAULT, 가로 3~4칸 후보는 미검증, 고체 비율은 미지정.
- 기존 장소·특수·계단·철도·엘리베이터 상세 계약의 후속 담당 Task와 원문 경로.
- 정적 시안·규칙 등록·실제 Player/진행 PASS의 분리.

## 완료 판정

- 이전 원본·선행 Task/Archive/Result와 승인/순서 문서 SHA 보존. 프로토콜은 허용 표식 변경만 발생.
- active 인덱스에서 동일 SHA의 원문을 찾을 수 있고 candidate 3개와 실제 등록 파일의 바이트가 같음.
- Coverage가 원문의 모든 의미 있는 문장을 빠짐없이 연결하며 JUMP-01~14가 유일하게 존재.
- JUMP-07/10이 사용자 지정 숫자로 승격되지 않고 가로 3~4칸을 성공 보장으로 바꾸지 않음.
- ReadSet에 선언한 원문 파일·SHA가 실제로 맞고 명세 MD는 각각 300줄 이하.
- 45개 SV5 ID·순서·기존 완료 이력이 유지되고 이번 실행은 SV5_02 한 개뿐임.
- Unity launch/compile/test/build/bake 및 게임 기능 구현은 NOT_RUN으로 기록.

## 보고와 종료

정상 Result에 변경 파일·이전/이후 SHA·규칙 연결·선행 확인·검증·미실행 범위를 기록한다.
INBOX_SHA / SOURCE_SPEC_SHA / BOUND_TASK_SHA / INSTALLED_TASK_SHA / ARCHIVE_TASK_SHA를 분리한다.
보고된 시작 상태와 같다면 Apply 후 239 COMPLETE / 1 CURRENT / 45 LOCKED, Finalize 후 240 COMPLETE / 0 CURRENT / 45 LOCKED를 기대한다.
실제 관측값은 정상 도구 결과에서 얻는다. Result 작성 시점에 Finalize/commit이 아직이면 완료된 것처럼 적지 않는다.
Finalize 후 실제 상태와 commit SHA는 최종 콘솔 응답으로 보고한다. immutable Result에 SHA를 넣으려고 amend하지 않는다.
SV5_03 이후와 RMAP18/19는 시작하지 않고 push 없이 종료한다.

## R2 경로 정정과 재시도

- 기존 R1은 Apply 전 경로 불일치로 중단되었다. TASK_ID는 SV5_02_RULES 그대로이며 새 계획 행을 추가하지 않는다.
- 입력 경로와 GENERATED 경로 정정 외에 규칙 candidate 3개·원문·PROTOCOL_APPEND는 R1과 동일 바이트다.
- R1 START/명세/SOURCE_LOCK/검증기/manifest는 보존한다. 이 R2의 새 START와 SOURCE_SPEC을 이번 실행 입력으로 명시한다.
- 잘못된 MapDesign/GENERATED 위치에 복사본·링크·빈 파일을 만들거나 기존 증거 파일을 옮기지 않는다.
- REPAIR.md와 REVISION.json을 읽고 세 경로의 변경 전/후 및 SHA 불변을 확인한다.
- MCP/GENERATED/SV5_02의 정상 산출물 위치도 실제 MCP 계약과 대조해 바인딩한다. 다른 계약이 발견되면 실제 근거와 함께 BLOCKED로 보고한다.
- 성공 시 기존 SOURCE_LOCK의 02_PROTOCOL PRE_APPLY_ONLY 검사를 Finalize 뒤 다시 강제하지 않는다.
