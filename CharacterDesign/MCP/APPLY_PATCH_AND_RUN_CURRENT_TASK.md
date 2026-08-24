# APPLY PATCH + RUN CURRENT TASK + FINALIZE

`CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`를 먼저 읽는다.

## Phase A — Patch Apply

`CharacterDesign/MCP_INBOX/`에서 `.APPLIED`가 없는 패키지를 찾는다.

- 정확히 1개: `07_PATCH_APPLY_RULES.md`에 따라 적용한다.
- 0개 + Current Task 존재: Phase B로 이동한다.
- 0개 + Current Task NONE: 실행할 작업 없음으로 종료한다.
- 2개 이상: BLOCKED한다.

## Phase B — Current Task

1. 전역 규칙을 읽는다.
2. Current Task 하나만 읽고 수행한다.
3. READ/WRITE ALLOWLIST를 지킨다.
4. 지정 REPORT를 생성한다.
5. 정확한 `STATUS: PASS`와 모든 gate를 확인한다.

FAIL/BLOCKED면 종료한다.

## Phase C — Status Finalize

REPORT가 PASS일 때만 `08_STATUS_FINALIZE_RULES.md`에 따라 현재 Task를 COMPLETE, Current Task를 NONE으로 바꾼다.

다음 Task는 자동 시작하지 않는다.
