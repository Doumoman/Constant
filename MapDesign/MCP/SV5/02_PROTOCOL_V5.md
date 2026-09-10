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
