# MCP 작업 실행 규칙 v1.1

# 1. 한 세션 한 Current Task

TASK EXECUTION Phase에서는 `06_IMPLEMENTATION_STATUS.md`가 지정한 Current Task 하나만 구현한다.

# 2. TASK와 상태 변경 분리

TASK 자체는 `06_IMPLEMENTATION_STATUS.md`를 수정하지 않는다.

```text
TASK EXECUTION
-> Result 생성
-> PASS 확인
-> STATUS FINALIZE가 상태만 변경
```

# 3. READ 범위

현재 TASK의 READ ALLOWLIST만 파일 내용을 읽는다.

# 4. WRITE 범위

현재 TASK의 WRITE ALLOWLIST만 수정/생성한다.

# 5. 구현 중 범위 밖 문제

직접 고치지 말고 Result에 `OUT_OF_SCOPE_FINDING`으로 기록한다.

# 6. 테스트

순수 생성/데이터 로직:
1. EditMode
2. Determinism
3. Invariant

Unity 의존:
1. Compile
2. EditMode
3. 필요한 경우 PlayMode

# 7. TASK PASS 조건

TASK의 DONE CONDITIONS가 모두 PASS해야 한다.

# 8. TASK Result

미래 TASK는 상단에 가능하면 다음 메타데이터를 가진다.

```yaml
status_control:
  task_key: MAPXX_YY_NAME
  result_file: REPORTS/MAPXX_YY_NAME_RESULT.md
```

구형 TASK에 이 블록이 없으면
TASK 본문에 명시된 단 하나의 `REPORTS/*_RESULT.md`를 사용한다.
후보가 2개 이상이면 BLOCKED.
