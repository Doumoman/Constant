# SV5_02_R2_START — 경로 정정 후 규칙 등록 재시도

MODE: design_plan_handoff_v1
TASK: SV5_02_RULES
INPUT_REVISION: R2
PREVIOUS: SV5_01_APPROVAL_BASELINE
PREVIOUS_RESULT_SHA256: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
PREVIOUS_TASK_ARCHIVE_SHA256: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed
SOURCE_SPEC: MCP/INPUTS/SV5_02_R2/SV5_02_RULES.md
SOURCE_SPEC_SHA256: 4b371aaddbfd4f1946fbf32b1da2096e522a7dacef45bd4958f0cb477653a321
SUPERSEDES_INBOX_SHA256_FOR_THIS_ATTEMPT: c27f7a0d34694b79264cbc5155b68abaf4d9b13e9c09af9829c9715ba5396c46

## 시작

SV5_02_R2_README.md, 새 입력 REPAIR.md/REVISION.json과 실제 AGENTS/MCP 계약을 읽는다.
이 START는 인계문이고 실제 single_task_v1 실행 Task는 별도 바인딩한다.
R1은 입력 경로 오류로 Apply 전 중단되었다. 기존 입력은 보존하고 R2를 이번 실행 입력으로 선택한다.
원본/Archive/Result SHA를 바꾸거나 MapDesign/GENERATED에 가짜 경로를 만들지 않는다.
경로의 기준은 MapDesign이며 선행 증거는 MCP/GENERATED/SV5_01이다.

1. 실제 상태부터 확인한다. COMPLETE면 정상 증거를 보고하고 종료하며 중복 실행하지 않는다.
2. 새 실행은 Current NONE, SV5_01 COMPLETE, SV5_02 LOCKED 및 정상 선행 충족이 필요하다.
3. python -X utf8로 R2 패키지와 실제 선행 바이트를 각각 검사한다. 실제 Result는 입력 reference 사본으로 대체하지 않는다.
4. 보고된 SV5_01 Finalize commit 9e6a71489fb4881c4112c378f7ca043bbbd85e85와 실제 상태/증거를 현지 확인한다.
5. SOURCE_SPEC의 실제 Read/Write/API·정상 절차를 바인딩하고 새 BOUND_TASK_SHA를 별도 발행한다.

## 수행과 종료

기존 45개 계획을 재등록하지 않고 SV5_02_RULES만 정상 Apply·수행·Finalize한다.
규칙 candidate 3개를 MCP/SV5에 등록하고 기존 02_PROTOCOL에 지정 블록을 한 번 추가한다.
이번 generated 증거는 MCP/GENERATED/SV5_02에 기록한다. 실제 계약과 다른 경로를 강제하지 않는다.
규칙 내용·점프 한계·작성 기본값/미검증 후보 구분은 R1과 동일하다.
INBOX/SOURCE_SPEC/BOUND/INSTALLED/ARCHIVE SHA, 실제 전후 상태 및 commit을 보고한다.
Finalize 이전 Result와 이후 콘솔 관측을 구분하며 이전 Result를 amend하지 않는다.
SV5_03 이후·RMAP18/19·Unity·게임 코드·맵 Bake·push는 시작하지 않고 종료한다.
