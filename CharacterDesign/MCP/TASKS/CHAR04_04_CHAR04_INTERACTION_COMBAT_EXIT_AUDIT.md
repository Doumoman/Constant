# CHAR04_04 — 상호작용·접촉 전투 종료 감사

TASK ID: CHAR04_04  
PHASE: CHAR04  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: CHAR04_03

## 목표

들기·내려놓기·던지기와 밟기·기절·제거·피격을 하나의 상호작용 전투 코어로 구현한다.

현재 작업 범위는 **상호작용·접촉 전투 종료 감사**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`
- `CharacterDesign/MCP/01_CHARACTER_LOCKED_RULES.md`
- `CharacterDesign/MCP/02_MCP_WORK_RULES.md`
- `CharacterDesign/MCP/04_UNITY_MCP_RULES.md`
- `CharacterDesign/MCP/05_CHANGE_CONTROL_RULES.md`
- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `CharacterDesign/MCP/TASKS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT.md`
- `{{CHARACTER_RUNTIME_READ_PATHS}}`
- `{{CHARACTER_TEST_READ_PATHS}}`
- `{{TASK_SPECIFIC_INTEGRATION_READ_PATHS}}`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md`
- `{{TASK_SPECIFIC_RUNTIME_WRITE_PATHS}}`
- `{{TASK_SPECIFIC_TEST_WRITE_PATHS}}`

ALLOWLIST NOTE: LOCKED 템플릿. OPEN 패치에서 토큰을 실제 repo-relative 파일/폴더로 좁혀야 하며 토큰 상태로 실행할 수 없다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘상호작용·접촉 전투 종료 감사’이다.
- 현재 TASK에 필요하지 않은 기존 코드를 리팩터링하거나 공개 API 이름을 변경하지 않는다.
- 새 전역 싱글톤, 새 asmdef, 새 입력 프레임워크를 임의로 추가하지 않는다.
- 게임플레이 로직을 Animator 이벤트 또는 렌더 프레임에 종속시키지 않는다.
- 고정 규칙과 충돌하거나 ALLOWLIST가 부족하면 확장하지 말고 BLOCKED로 보고한다.
- 놓을 공간이 없으면 겹쳐 놓지 않는다.
- 별도 일반 공격 기능을 추가하지 않고 밟기 방향과 하강 속도를 검증한다.

## 금지사항

- WRITE ALLOWLIST 밖 파일 수정
- 다음 TASK 선행 구현
- 기존 파일 삭제·이동·이름 변경
- 테스트 수 감소, Ignore 처리 또는 통과 결과 조작
- 사용자 지시 없는 git push

## 고정 테스트

TEST COUNT: 6

1. PhaseTaskResultsComplete: 해당 단계의 선행 RESULT가 모두 STATUS: PASS
2. UnityCompileClean: 해당 단계 최종 코드 기준 컴파일 오류 0
3. PhaseRegressionSuite: 단계 로드맵에 지정된 회귀 테스트 전체 PASS
4. AllowlistAudit: 단계 중 WRITE ALLOWLIST 위반 0
5. LockedRuleAudit: 고정 규칙 위반 0
6. NextTaskStillLocked: 다음 단계 또는 작업이 아직 LOCKED

## RESULT 계약

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 6개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR04_04: 상호작용·접촉 전투 종료 감사`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
