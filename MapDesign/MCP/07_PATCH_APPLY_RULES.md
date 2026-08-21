# MCP Patch Apply Rules v1.1

# 1. 구조

```text
MapDesign/
├─ MCP/
├─ MCP_INBOX/
└─ MCP_ARCHIVE/
```

# 2. 적용 전 조건

manifest에서:
- patch_id
- requires_status
- sets_current_task
- copy_operations
- forbidden_operations

을 검사한다.

`requires_status`가 실제 상태와 다르면 BLOCKED.

# 3. 적용 허용

PATCH APPLY에서 manifest에 적힌 copy만 허용한다.

# 4. 적용 후 검증

1. destination 존재
2. 빈 파일 아님
3. 새 `06_IMPLEMENTATION_STATUS.md` 확인
4. `Current Task`가 manifest `sets_current_task`와 일치
5. Current Task 파일 존재
6. 새 Current Task 상태가 `CURRENT`

# 5. 적용 후 실행

사용자가 `APPLY_PATCH_AND_RUN_CURRENT_TASK`를 요청한 경우:

```text
PATCH APPLY PASS
-> TASK EXECUTION
-> TASK Result PASS
-> STATUS FINALIZE
-> STOP
```

TASK가 FAIL/BLOCKED면 STATUS FINALIZE 금지.

# 6. Archive

가능하면 `MCP_ARCHIVE/<PATCH_ID>/`로 이동.
불확실하면 INBOX에 `.APPLIED` 생성.
