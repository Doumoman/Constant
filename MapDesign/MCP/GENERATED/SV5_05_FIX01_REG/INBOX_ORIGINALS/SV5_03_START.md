# SV5_03_START — 기존 코드·데이터 접점 확인

MODE: design_plan_handoff_v1
TASK: SV5_03_BINDINGS
PREVIOUS: SV5_02_RULES
PREVIOUS_RESULT_SHA256: ed4e9ee6fd6f271bd81915040e1d6e103832a3737bd90d5eb90cc9371956a496
PREVIOUS_TASK_ARCHIVE_SHA256: b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1
SOURCE_SPEC: MCP/INPUTS/SV5_03/SV5_03_BINDINGS.md
SOURCE_SPEC_SHA256: 5bdc2b45cc0c4dc51a4ea347ad28be12c57aa2d9c53d17df1c41f2a8fef1bce9

## 시작

실제 AGENTS/MCP·02_PROTOCOL과 SOURCE_SPEC을 읽는다. 이 START는 실행 Task가 아니다.
모든 보조 파일은 MCP/INPUTS/SV5_03에 있다. 프로젝트 루트에 새 CHECK/FILES/VERIFY/HANDOFF를 만들지 않는다.
VERIFY.py --manifest-sha를 python -X utf8로 실행해 배포 입력을 검사한다.
첨부 선행 Result는 SV5_02 CURRENT 때 쓰였다. 실제 SV5_02 Finalize/소유 commit부터 확인한다.
아직 CURRENT이면 같은 PASS Result와 Task/Archive를 확인한 뒤 기존 승인 범위의 정상 Finalize/commit을 마무리한다.
Current NONE·SV5_02 COMPLETE·SV5_03 LOCKED에서만 실제 선행 바이트와 이번 Task를 바인딩한다.
이미 SV5_03 COMPLETE면 정상 증거를 보고하고 종료한다. 다른 Current나 SHA 불일치를 임의로 덮지 않는다.

## 수행

SV5_03_BINDINGS 한 개만 정상 Apply한다. RMAP12~17/SPACE와 실제 소스·데이터를 읽고 REUSE/ADAPT/NEW를 기록한다.
13개 조사 그룹, 8개 핵심 물리 site와 보호·진행 접점, 42개 후속 Task 연결을 OUTPUT_CONTRACT에 맞게 작성한다.
검색 단서는 실제 타입/경로 보장이 아니다. 미래 API를 지금 구현하거나 게임 데이터를 변경하지 않는다.
06_BINDINGS/07_FILE_FLOW를 등록하고 02_PROTOCOL에 지정 블록을 한 번 연결한다.
이번 작업 임시파일을 정리하고 정적 참조·SHA·쓰기 범위 검사를 한다.

## 종료

정상 Result·Finalize·작업 소유 commit 후 실제 상태와 역할별 SHA/commit을 콘솔에 보고한다.
Result와 BINDINGS.csv·CORE_BINDINGS.csv·DATA_SCHEMAS.csv·TASK_COVERAGE.csv를 다음 검토에서 읽을 수 있게 제공한다.
SV5_04 이후·RMAP18/19·VIS 후속·게임 코드·Unity·push는 진행하지 않는다.
