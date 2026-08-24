# STATUS FINALIZE 규칙 v2.0

## 1. 목적

Task 구현 권한과 상태 파일 수정 권한을 분리한다. REPORT가 PASS한 뒤에만 `CURRENT → COMPLETE`, `Current Task → NONE`을 수행한다.

## 2. 필수 READ

- `00_MCP_ENTRYPOINT.md`
- `05_CHANGE_CONTROL_RULES.md`
- 이 파일
- `06_IMPLEMENTATION_STATUS.md`
- Current Task 파일
- Task가 지정한 REPORT

## 3. Preconditions

다음을 모두 확인한다.

1. Current Task가 정확히 하나 존재한다.
2. 상태 표의 같은 task key가 CURRENT다.
3. Task의 `status_control.result_file`가 하나다.
4. REPORT가 존재한다.
5. REPORT에 정확히 `STATUS: PASS`가 있다.
6. REPORT의 TASK가 Current Task와 일치한다.
7. DONE CONDITIONS와 고정 gate가 전부 PASS다.
8. FAIL/BLOCKED 동시 표기가 없다.

하나라도 실패하면 상태 파일을 바꾸지 않는다.

## 4. WRITE ALLOWLIST

```text
CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
```

## 5. 정확한 변경

- Current Task 행: `CURRENT → COMPLETE`
- `## Current Task`: 해당 경로 → `NONE`
- `## Last Completed Task`: 해당 task key
- `## Last Result`: 해당 REPORT 상대 경로
- 다른 Task 상태는 변경하지 않는다.
- 다음 LOCKED Task를 CURRENT로 바꾸지 않는다.

## 6. Idempotency

이미 해당 Task가 COMPLETE이고 Current Task가 NONE이면 추가 변경 없이 PASS로 종료한다.

## 7. 보고

```text
PHASE: STATUS FINALIZE
TASK: <task key>
STATUS: PASS

STATE:
- <task key> = COMPLETE
- Current Task = NONE

NEXT:
- Awaiting next patch = YES
```
