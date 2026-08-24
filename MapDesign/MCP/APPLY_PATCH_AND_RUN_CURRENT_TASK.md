# APPLY PATCH + RUN CURRENT TASK + FINALIZE

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`를 먼저 읽는다.

## Phase A — Patch Apply

`MapDesign/MCP_INBOX/`에서 `.APPLIED`가 없는 패치를 확인한다.

정확히 1개라면:
1. `07_PATCH_APPLY_RULES.md` 읽기
2. manifest precondition 검증
3. manifest의 copy만 수행
4. 적용 검증

0개면:
- Current Task가 존재하면 Phase B
- Current Task도 NONE이면 종료

2개 이상이면 BLOCKED.

## Phase B — Current Task

1. 전역 규칙 읽기
2. Current Task 읽기
3. READ/WRITE ALLOWLIST 준수
4. TASK 수행
5. Result 생성
6. Result `STATUS: PASS` 확인

FAIL/BLOCKED면 종료.

## Phase C — Status Finalize

TASK Result가 PASS일 때:

1. `08_STATUS_FINALIZE_RULES.md` 읽기
2. Result와 Current Task 일치 확인
3. `06_IMPLEMENTATION_STATUS.md`에서
   - CURRENT -> COMPLETE
   - Current Task -> NONE
4. 검증

## Phase D — Task Commit

TASK 수행과 STATUS FINALIZE가 모두 PASS라면:

1. `05_CHANGE_CONTROL_RULES.md`의 commit 범위 확인
2. 기존 무관한 dirty 파일 제외
3. patch, 구현·테스트·matching meta, Result, status finalize만 stage
4. Task ID가 포함된 제목과 구현·테스트·gate 상세 본문으로 commit
5. commit SHA와 제목 검증

commit 실패 시 BLOCKED로 보고한다. 자동 push는 수행하지 않는다.

## 종료

다음 TASK를 자동 시작하지 않는다.
