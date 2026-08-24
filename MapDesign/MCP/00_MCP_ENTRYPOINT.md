# 《별을 물어오는 밤》 MCP 진입 규칙 v1.3

# 1. 세션 모드

MCP 세션은 아래 4개 Phase로 구분한다.

## PHASE A — PATCH APPLY

`MapDesign/MCP_INBOX/`의 패치를 검증하고 `MapDesign/MCP/`에 적용한다.

## PHASE B — TASK EXECUTION

현재 `06_IMPLEMENTATION_STATUS.md`가 지정한 Current Task 하나만 수행한다.

TASK 자신은 `06_IMPLEMENTATION_STATUS.md`를 수정하지 않는다.

## PHASE C — STATUS FINALIZE

TASK 결과가 PASS일 때만 수행한다.

읽기:
1. `00_MCP_ENTRYPOINT.md`
2. `05_CHANGE_CONTROL_RULES.md`
3. `08_STATUS_FINALIZE_RULES.md`
4. `06_IMPLEMENTATION_STATUS.md`
5. 방금 수행한 Current Task
6. 해당 TASK의 Result 문서

허용 수정:
- `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`만

상태를:
- 현재 TASK `CURRENT -> COMPLETE`
- `Current Task -> NONE`
으로 바꾼다.

다음 TASK는 자동 시작하지 않는다.

## PHASE D — TASK COMMIT

STATUS FINALIZE까지 PASS한 Task는 반드시 Git commit한다.

커밋 범위:
- Phase A에서 적용한 해당 Task patch
- Phase B의 구현, matching meta, test, Result
- Phase C의 status finalize

기존에 존재하던 무관한 uncommitted change는 포함하지 않는다.

커밋 메시지는 Task ID를 제목에 포함하고 구현 파일·동작·테스트·정적 gate를 본문에 상세히 기록한다.

자동 push는 수행하지 않는다.

# 2. 기본 실행 파이프라인

사용자가:

```text
MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

라고 하면:

```text
PHASE A PATCH APPLY
    ↓ PASS
PHASE B CURRENT TASK
    ↓ PASS
PHASE C STATUS FINALIZE
    ↓ PASS
PHASE D TASK COMMIT
    ↓ PASS
STOP
```

어느 Phase라도 FAIL/BLOCKED면 즉시 종료한다.

# 3. 규칙 우선순위

1. 사용자의 현재 세션 최신 지시
2. `01_PROJECT_LOCKED_RULES.md`
3. 현재 TASK
4. `02~05`, `07`, `08` 규칙
5. 과거 GDD/참고 문서

# 4. 절대 금지

허용 범위 밖:
- 다음 TASK 선행 구현
- 관련 없는 리팩토링
- CSV schema 임의 변경
- Scene/Prefab 임의 변경
- package 설치/삭제
- Phase D 밖의 임의 Git commit
- Git push/branch/reset/rebase/force
- 테스트 규칙 완화
- 실패 결과를 임의 후처리로 숨기기

# 5. 종료 보고

```text
PHASE:
TASK_OR_PATCH:
STATUS: PASS / FAIL / BLOCKED

READ:
CHANGED:
CREATED:
TEST:
UNITY:
COMMIT:
OUT_OF_SCOPE_FINDINGS:
NEXT:
```

TASK COMMIT까지 PASS하면:

```text
NEXT:
- Current Task: NONE
- Awaiting next MCP_INBOX patch: YES
```
