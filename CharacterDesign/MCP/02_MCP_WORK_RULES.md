# MCP 작업 실행 규칙 v2.0

## 1. 한 세션 한 Current Task

TASK EXECUTION에서는 `06_IMPLEMENTATION_STATUS.md`의 Current Task 하나만 수행한다. Current Task가 없거나 둘 이상이면 BLOCKED다.

## 2. Task와 상태 변경 분리

```text
TASK EXECUTION → REPORT 생성 → PASS 확인 → STATUS FINALIZE
```

Task 자체의 WRITE ALLOWLIST에는 상태 파일을 포함하지 않는다.

## 3. READ/WRITE

- 현재 Task의 READ ALLOWLIST만 읽는다.
- 현재 Task의 WRITE ALLOWLIST만 수정·생성한다.
- 범위 밖 문제는 직접 고치지 않고 REPORT의 `OUT_OF_SCOPE_FINDINGS`에 기록한다.
- 해결되지 않은 토큰이나 복수 후보 경로가 있으면 BLOCKED다.

## 4. 테스트

- 문서·감사 Task: 고정 gate와 무변경 증빙
- 순수 로직: compile, focused EditMode, 불변식, 직전 회귀
- Unity 의존 로직: compile, EditMode, 필요한 PlayMode/scene verification
- 테스트 감소, Ignore/Explicit, 조건부 컴파일, 조기 return으로 실패를 숨기지 않는다.

## 5. REPORT

Task 상단 `status_control.result_file` 단 하나만 사용한다. 후보가 없거나 둘 이상이면 BLOCKED다.

성공은 독립된 정확한 한 줄 `STATUS: PASS`다. FAIL/BLOCKED 표기가 함께 있으면 PASS가 아니다.

## 6. 다음 Task

PASS여도 같은 세션에서 다음 Task를 열거나 읽지 않는다. 다음 Task는 새 MCP_INBOX patch package로만 CURRENT가 된다.
