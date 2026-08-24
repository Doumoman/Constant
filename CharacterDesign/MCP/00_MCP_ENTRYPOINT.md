# CHARACTER MCP 진입 규칙 v2.0

## 1. 세션 Phase

### PHASE A — PATCH APPLY

`CharacterDesign/MCP_INBOX/`에서 `.APPLIED`가 없는 패키지를 검사하고 manifest에 선언된 payload만 `CharacterDesign/MCP/`에 적용한다.

### PHASE B — TASK EXECUTION

`06_IMPLEMENTATION_STATUS.md`가 지정한 Current Task 정확히 하나만 수행하고 대응 REPORT를 생성한다. Task 자체는 상태 파일을 수정하지 않는다.

### PHASE C — STATUS FINALIZE

REPORT가 정확히 `STATUS: PASS`일 때만 수행한다. 현재 Task를 `CURRENT → COMPLETE`, Current Task를 `NONE`으로 변경한다. 다음 Task는 열지 않는다.

## 2. 기본 파이프라인

사용자가 다음을 요청하면:

```text
CharacterDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md를 수행해.
```

아래 순서로 정확히 한 Task만 처리한다.

```text
PATCH APPLY → TASK EXECUTION → REPORT PASS 확인 → STATUS FINALIZE → STOP
```

어느 Phase라도 FAIL/BLOCKED면 즉시 종료하고 다음 Phase 또는 다음 Task를 시작하지 않는다.

## 3. 규칙 우선순위

1. 사용자의 현재 세션 최신 지시
2. `01_CHARACTER_LOCKED_RULES.md`
3. 현재 Task
4. `02~05`, `07`, `08` 규칙
5. registry와 과거 참고 코드

## 4. 절대 금지

- 다음 Task 선행 구현
- READ/WRITE ALLOWLIST 밖 접근 또는 수정
- 관련 없는 리팩터링
- Scene/Prefab/Packages/ProjectSettings/asmdef 임의 변경
- MAP 내부 구현 직접 수정 또는 Tilemap 직접 소유
- 테스트 완화, 실패 은폐
- git commit/push

## 5. 종료 보고

```text
PHASE:
TASK_OR_PATCH:
STATUS: PASS / FAIL / BLOCKED

READ:
CHANGED:
CREATED:
TEST:
UNITY:
OUT_OF_SCOPE_FINDINGS:
NEXT:
```

STATUS FINALIZE까지 PASS하면:

```text
NEXT:
- Current Task: NONE
- Awaiting next MCP_INBOX patch: YES
```
