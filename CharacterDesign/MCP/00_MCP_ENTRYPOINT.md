# CHARACTER MCP ENTRYPOINT

이 파일이 캐릭터 작업의 유일한 시작점이다.

## 시작 순서

1. `01_CHARACTER_LOCKED_RULES.md`를 읽는다.
2. `02_MCP_WORK_RULES.md`를 읽는다.
3. `04_UNITY_MCP_RULES.md`를 읽는다.
4. `05_CHANGE_CONTROL_RULES.md`를 읽는다.
5. `06_IMPLEMENTATION_STATUS.md`에서 `current_task`를 읽는다.
6. CURRENT 작업의 TASK 파일만 읽는다.
7. TASK가 허용한 INPUT과 프로젝트 파일만 읽는다.

## 현재 시작점

최초 설치 상태의 유일한 CURRENT는 `CHAR00_01`이다.

## 중단 조건

- CURRENT가 없거나 둘 이상임
- 선행 RESULT 파일이 없거나 정확한 `STATUS: PASS`가 아님
- TASK 또는 상태 파일의 경로가 존재하지 않음
- READ/WRITE ALLOWLIST에 해결되지 않은 토큰이 있는데 구현 작업을 요구함
- Unity 컴파일 오류가 기존 오류인지 신규 오류인지 분리할 수 없음
- 고정 규칙과 사용자 요청이 충돌함

중단 시 추측으로 진행하지 않고 `STATUS: BLOCKED` 결과를 작성한다.
