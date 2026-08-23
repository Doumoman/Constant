# CHAR03_03 — MAP·방 전환 종료 감사

TASK ID: CHAR03_03  
PHASE: CHAR03  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: CHAR03_02

## 목표

MAP 좌표·월드 질의·방 준비 게이트와 카메라룸 전환을 캐릭터에 연결한다.

현재 작업 범위는 **MAP·방 전환 종료 감사**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`
- `CharacterDesign/MCP/01_CHARACTER_LOCKED_RULES.md`
- `CharacterDesign/MCP/02_MCP_WORK_RULES.md`
- `CharacterDesign/MCP/04_UNITY_MCP_RULES.md`
- `CharacterDesign/MCP/05_CHANGE_CONTROL_RULES.md`
- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `CharacterDesign/MCP/TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md`
- `{{CHARACTER_RUNTIME_READ_PATHS}}`
- `{{CHARACTER_TEST_READ_PATHS}}`
- `{{TASK_SPECIFIC_INTEGRATION_READ_PATHS}}`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`
- `{{TASK_SPECIFIC_RUNTIME_WRITE_PATHS}}`
- `{{TASK_SPECIFIC_TEST_WRITE_PATHS}}`

ALLOWLIST NOTE: LOCKED 템플릿. OPEN 패치에서 토큰을 실제 repo-relative 파일/폴더로 좁혀야 하며 토큰 상태로 실행할 수 없다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘MAP·방 전환 종료 감사’이다.
- 현재 TASK에 필요하지 않은 기존 코드를 리팩터링하거나 공개 API 이름을 변경하지 않는다.
- 새 전역 싱글톤, 새 asmdef, 새 입력 프레임워크를 임의로 추가하지 않는다.
- 게임플레이 로직을 Animator 이벤트 또는 렌더 프레임에 종속시키지 않는다.
- 고정 규칙과 충돌하거나 ALLOWLIST가 부족하면 확장하지 말고 BLOCKED로 보고한다.
- Tilemap 구현을 직접 참조하지 않고 등록된 MAP 계약을 사용한다.
- 입력·속도 KEEP과 미생성 목적지 차단을 동시에 만족한다.

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

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 6개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR03_03: MAP·방 전환 종료 감사`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
