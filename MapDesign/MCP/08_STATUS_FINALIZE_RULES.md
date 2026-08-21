# STATUS FINALIZE 규칙 v1.0

# 1. 목적

TASK의 구현 권한과 상태 파일 수정 권한을 분리한다.

TASK가 PASS한 뒤에만 별도 Phase로:

```text
CURRENT -> COMPLETE
Current Task -> NONE
```

을 수행한다.

# 2. READ

반드시 읽는다.

- `00_MCP_ENTRYPOINT.md`
- `05_CHANGE_CONTROL_RULES.md`
- 이 파일
- `06_IMPLEMENTATION_STATUS.md`
- 현재 Current Task
- 해당 TASK의 Result 문서

# 3. Result 찾기

우선 Current Task의 메타데이터:

```yaml
status_control:
  task_key:
  result_file:
```

를 사용한다.

없으면 TASK 본문에 명시된 단 하나의 `REPORTS/*_RESULT.md` 경로를 사용한다.

후보가 없거나 2개 이상이면 BLOCKED.

# 4. FINALIZE Preconditions

아래를 모두 확인한다.

1. Current Task가 존재
2. 상태 표에서 해당 task key가 `CURRENT`
3. Result 파일 존재
4. Result에 정확히 `STATUS: PASS`
5. Result의 TASK ID와 Current Task 일치
6. DONE CONDITIONS가 있으면 모두 완료
7. FAIL/BLOCKED 동시 표기 없음

하나라도 실패하면 상태 변경 금지.

# 5. WRITE ALLOWLIST

오직:

```text
MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

만 수정 가능.

# 6. 정확한 변경

Current Task:

```text
TASKS/MAPXX_YY_NAME.md
```

를:

```text
NONE
```

으로 변경.

상태 표:

```text
| MAPXX_YY_NAME | CURRENT |
```

를:

```text
| MAPXX_YY_NAME | COMPLETE |
```

로 변경.

가능하면 추가/갱신:

```text
## Last Completed Task

MAPXX_YY_NAME

## Last Result

REPORTS/MAPXX_YY_NAME_RESULT.md
```

다른 TASK 상태는 변경하지 않는다.
LOCKED를 자동 CURRENT로 바꾸지 않는다.

# 7. Idempotency

이미 `COMPLETE` + `Current Task = NONE`이면 PASS로 종료하고 추가 변경하지 않는다.

# 8. 다음 TASK

STATUS FINALIZE는 다음 TASK를 열지 않는다.
다음 TASK는 새 MCP_INBOX patch가 열어야 한다.

# 9. 보고

```text
PHASE: STATUS FINALIZE
TASK: ...
STATUS: PASS

VERIFIED RESULT:
- ...

CHANGED:
- 06_IMPLEMENTATION_STATUS.md

STATE:
- <task> = COMPLETE
- Current Task = NONE

NEXT:
- Awaiting next patch = YES
```
