# SV5_05_FIX01_START — 검토에서 발견한 접근 조건·digest 누락 보완

MODE: design_plan_handoff_v1
TASK: SV5_05_FIX01
PREVIOUS: SV5_05_ROUTE_STATE
PREVIOUS_RESULT_SHA256: 2a8da38b83e4f124ed8059c071a8d7a718ba19178582d935657fe2b3a5d13222
PREVIOUS_TASK_ARCHIVE_SHA256: 38c86cfa5411bb4e991c1cc0ed55116ea9de153e9131e262308b0459d676acb1
SOURCE_SPEC: MCP/INPUTS/SV5_05_FIX01/SV5_05_FIX01.md
SOURCE_SPEC_SHA256: 96e43d745f5a0bf2057b54c2bbd2007530293211b87e84bb5f76b4b0afb5330b

첨부 검토에서 봉인 전 Boss 진입 허용과 후보 입력이 빠진 digest를 확인했다.
SV5_06 전에 보완 작업 하나만 정상 등록·바인딩·Apply·검증·Finalize한다.
실제 등록 및 single_task_v1 형식은 현지 계약으로 확정한다. 완료 SV5_05와 원래 45개를 덮지 않는다.

MCP/INPUTS/SV5_05_FIX01의 REVIEW/REVIEW_FINDINGS/REPAIR_CONTRACT/SOURCE_LOCK/명세를 읽는다.
외부 명령의 FILES.json SHA로 python -X utf8 VERIFY.py --manifest-sha 검사를 먼저 수행한다.
package PASS는 local git/native MCP/Unity PASS가 아니다. 실제 선행 Result와 두 루트를 지정해 local-precheck도 한다.
변경 후에는 --post-readonly를 사용하고 변경 허용 source의 이전 SHA를 다시 강제하지 않는다.
입력 사본을 native Result로 쓰거나 SHA/경로를 바꿔 검증을 우회하지 않는다.

보조 파일은 이번 MCP/INPUTS, 생성 증거는 MCP/GENERATED/SV5_05_FIX01, 임시 소유 자료는 그 _work 아래에 둔다.
구 테스트의 SV5_05 export를 이번 scoped 경로로 분리해 과거 증거를 보호한다.
원본 Result는 유지하고 보완 Result를 새로 만든다. 최종 SV5_05_FIX01_REVIEW.zip을 제공하고 종료한다.
