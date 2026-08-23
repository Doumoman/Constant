# CHAR01_02 — 충돌 질의·지지체 추적·걷기·달리기·가감속 구현

TASK ID: CHAR01_02  
PHASE: CHAR01  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: CHAR01_01

## 목표

입력·상태·충돌·지상 모터·점프를 하나의 플레이어 이동 코어로 구현한다.

현재 작업 범위는 **충돌 질의·지지체 추적·걷기·달리기·가감속 구현**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`
- `CharacterDesign/MCP/01_CHARACTER_LOCKED_RULES.md`
- `CharacterDesign/MCP/02_MCP_WORK_RULES.md`
- `CharacterDesign/MCP/04_UNITY_MCP_RULES.md`
- `CharacterDesign/MCP/05_CHANGE_CONTROL_RULES.md`
- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `CharacterDesign/MCP/TASKS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR.md`
- `{{CHARACTER_RUNTIME_READ_PATHS}}`
- `{{CHARACTER_TEST_READ_PATHS}}`
- `{{TASK_SPECIFIC_INTEGRATION_READ_PATHS}}`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md`
- `{{TASK_SPECIFIC_RUNTIME_WRITE_PATHS}}`
- `{{TASK_SPECIFIC_TEST_WRITE_PATHS}}`

ALLOWLIST NOTE: LOCKED 템플릿. OPEN 패치에서 토큰을 실제 repo-relative 파일/폴더로 좁혀야 하며 토큰 상태로 실행할 수 없다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘충돌 질의·지지체 추적·걷기·달리기·가감속 구현’이다.
- 현재 TASK에 필요하지 않은 기존 코드를 리팩터링하거나 공개 API 이름을 변경하지 않는다.
- 새 전역 싱글톤, 새 asmdef, 새 입력 프레임워크를 임의로 추가하지 않는다.
- 게임플레이 로직을 Animator 이벤트 또는 렌더 프레임에 종속시키지 않는다.
- 고정 규칙과 충돌하거나 ALLOWLIST가 부족하면 확장하지 말고 BLOCKED로 보고한다.
- 장치 키와 논리 명령을 분리한다.
- 입력 소비 틱·잠금 사유·상태 전환 우선순위를 추적 가능하게 한다.

## 금지사항

- WRITE ALLOWLIST 밖 파일 수정
- 다음 TASK 선행 구현
- 기존 파일 삭제·이동·이름 변경
- 테스트 수 감소, Ignore 처리 또는 통과 결과 조작
- 사용자 지시 없는 git push

## 고정 테스트

TEST COUNT: 4

1. UnityCompileClean: 변경 후 컴파일 오류 0
2. TaskPrimaryTest: 현재 TASK의 정상 경로 테스트 PASS
3. TaskBoundaryTest: 현재 TASK의 경계 또는 거부 경로 테스트 PASS
4. PriorRegressionTest: 직전 완료 단계의 대표 회귀 테스트 PASS

## RESULT 계약

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 4개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR01_02: 충돌 질의·지지체 추적·걷기·달리기·가감속 구현`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
