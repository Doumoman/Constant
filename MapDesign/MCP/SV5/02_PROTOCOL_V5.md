# SV5 Normal Issuing Protocol

SV5_01 is the sole first-registration boundary. From SV5_02 onward, a bound
Task must use the exact existing `single_task_v1` schema and verified SHA-256
values from the committed predecessor Result and installed Task.

- Exactly one new bound `mcp_patch` Task is opened at a time.
- Current must be `NONE`; its predecessor must be COMPLETE and its successor
  must be the exactly-one LOCKED Status/Master row.
- Apply installs and archives the same bytes, opens only the new row, and
  Finalize closes only that row after a matching `STATUS: PASS` Result.
- Every Result distinguishes static/design evidence from Unity and Player
  evidence. No later SV5 task may imply a completed feature before its own
  validation.
- RMAP18/19 are separate registered plans. This protocol never opens them.
- No push is part of any SV5 Apply, Finalize, or atomic task commit.

<!-- SV5_02_RULES_BEGIN -->
## SV5 활성 규칙 진입점

다음 SV5 Task를 바인딩하기 전에 MCP/SV5/03_RULES_V5.md를 읽는다.
규칙 원문은 MCP/INPUTS/SV5/SPACE_V5_RULES.md이며, 기존 승인 범위와 45개 순서는 유지한다.
MCP/SV5/04_RULE_COVERAGE_V5.csv와 05_RULE_READSET_V5.json으로 해당 Task의 원문·SHA·상세 읽기 범위를 확인한다.
문서 규칙 등록과 Player/Unity 구현 완료를 구분한다.
<!-- SV5_02_RULES_END -->

<!-- SV5_03_BINDINGS_BEGIN -->
## SV5 코드·데이터 접점과 파일 배치

다음 SV5 Task의 실제 Read/Write/API를 바인딩하기 전에 MCP/SV5/06_BINDINGS_V5.md를 읽는다.
MCP/GENERATED/SV5_03의 BINDINGS·CORE_BINDINGS·DATA_SCHEMAS·TASK_COVERAGE를 담당 범위만 찾아 읽는다.
현재 Task가 변경할 소스의 최신 바이트/소유를 다시 확인한다. 과거 조사 SHA를 모든 미래 변경 뒤에도 강제하지 않는다.
새 패키지와 임시파일 정리는 MCP/SV5/07_FILE_FLOW_V5.md를 따른다. 프로젝트 루트에 작업별 보조 파일을 추가하지 않는다.
한 Task 후 종료 범위와 기존 정상 검증 계약은 유지한다.
<!-- SV5_03_BINDINGS_END -->

<!-- SV5_05_FIX01_BEGIN -->
## SV5 진행 상태 보완 결과 소비

SV5_06/09/41을 바인딩하기 전에 MCP/SV5/09_ROUTE_STATE_FIX01.md와 SV5_05_FIX01의 실제 Result/Finalize를 확인한다.
SV5_05의 과거 6순서 PASS만으로 새 후보집합의 접근 조건·복귀 안전성이 확인됐다고 간주하지 않는다.
현재 candidate payload/digest와 보완된 상태 검사 결과를 사용한다. 미해결 geometry와 Player 검증은 별도로 남긴다.
SV5_41은 합성 geometry, SV5_44는 전체 Player 검증을 담당한다. 기존 45개 ID/순서와 한 Task 실행 원칙을 유지한다.
<!-- SV5_05_FIX01_END -->

<!-- SV5_06_SPACE_GRAPH_BEGIN -->
## SV5 공간 그래프 소비

SV5_07~10 및 SV5_41은 MCP/SV5/10_SPACE_GRAPH_V5.md와 MCP/GENERATED/SV5_06의 실제 plan/validation을 읽는다.
장소·통로·접촉·보호 예약은 동일한 공간 plan digest로 연결한다. 과거 FIX01의 contact checked 표시를 물리 증거로 승격하지 않는다.
후속 배치 변경은 06의 접촉 전수검사와 현재 FSM 기반 전체 연결 검사를 다시 호출한다.
PLANNED_LAYOUT / LOGICAL_STATE / CONTACT_STATE / COMPOSED_GEOMETRY / PLAYER 검증을 분리한다.
SV5_06 완료는 세부 지형·기능·Scene Bake 완료가 아니다. 다음 Task 하나씩만 새 정상 입력으로 연다.
<!-- SV5_06_SPACE_GRAPH_END -->

<!-- SV5_06_FIX01_BEGIN -->
## SV5 공간 예약 검증 보완 소비

SV5_07~10/41의 다음 바인딩은 SV5_06_FIX01 실제 Result/Finalize와 SV5/11_SPACE_GRAPH_FIX01.md를 읽는다.
SV5_06의 과거 contact PASS는 새 clearance를 모두 포함한 검증이 아니다. 보완 결과의 정본 reservation/projection을 소비한다.
미확정 gate 또는 보호 셀 충돌을 state segment의 guard만으로 안전하다고 바꾸지 않는다.
공간 밀도·단일 순환 구조의 미완 항목은 승인된 전체 예시와 대조해07~09와최종REVIEW에서 계속 검토한다.
이 규칙은 다음 Task를 자동으로 열거나45개 본 작업의범위를임의로교체하지않는다.
<!-- SV5_06_FIX01_END -->
