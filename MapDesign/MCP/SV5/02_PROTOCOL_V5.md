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
