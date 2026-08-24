# CHAR06_02 — 마이크로청크·방 전환·휴대물·폭탄·로프·무작위 런 검증

TASK ID: CHAR06_02  
PHASE: CHAR06  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: CHAR06_01

## 목표

실제 생성 맵에서 필수·선택 경로와 전체 런을 확인하고 최종 컴파일·빌드·문서 게이트를 통과시킨다.

현재 작업 범위는 **마이크로청크·방 전환·휴대물·폭탄·로프·무작위 런 검증**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`
- `CharacterDesign/MCP/01_CHARACTER_LOCKED_RULES.md`
- `CharacterDesign/MCP/02_MCP_WORK_RULES.md`
- `CharacterDesign/MCP/04_UNITY_MCP_RULES.md`
- `CharacterDesign/MCP/05_CHANGE_CONTROL_RULES.md`
- `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
- `CharacterDesign/MCP/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md`
- `{{CHARACTER_RUNTIME_READ_PATHS}}`
- `{{CHARACTER_TEST_READ_PATHS}}`
- `{{TASK_SPECIFIC_INTEGRATION_READ_PATHS}}`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
- `{{TASK_SPECIFIC_RUNTIME_WRITE_PATHS}}`
- `{{TASK_SPECIFIC_TEST_WRITE_PATHS}}`

ALLOWLIST NOTE: LOCKED 템플릿. OPEN 패치에서 토큰을 실제 repo-relative 파일/폴더로 좁혀야 하며 토큰 상태로 실행할 수 없다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘마이크로청크·방 전환·휴대물·폭탄·로프·무작위 런 검증’이다.
- 현재 TASK에 필요하지 않은 기존 코드를 리팩터링하거나 공개 API 이름을 변경하지 않는다.
- 새 전역 싱글톤, 새 asmdef, 새 입력 프레임워크를 임의로 추가하지 않는다.
- 게임플레이 로직을 Animator 이벤트 또는 렌더 프레임에 종속시키지 않는다.
- 고정 규칙과 충돌하거나 ALLOWLIST가 부족하면 확장하지 말고 BLOCKED로 보고한다.
- MAP 결과를 통합 테스트가 임의 수정하지 않는다.
- 실패 seed와 재현 정보를 RESULT에 기록하며 테스트 실패를 숨기지 않는다.

## 금지사항

- WRITE ALLOWLIST 밖 파일 수정
- 다음 TASK 선행 구현
- 기존 파일 삭제·이동·이름 변경
- 테스트 수 감소, Ignore 처리 또는 통과 결과 조작
- 사용자 지시 없는 git push

## 고정 테스트

TEST COUNT: 5

1. UnityCompileClean: 컴파일 오류 0
2. TaskNamedIntegrationTest: TASK 제목과 대응하는 통합 테스트 PASS
3. NoNewConsoleErrors: 테스트 구간 신규 오류 로그 0
4. GeneratedMapContractPreserved: MAP 소유권·좌표·경계 계약 위반 0
5. RegressionScopeRecorded: 실행한 seed/scene/test 목록 RESULT에 기록

## RESULT 계약

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 5개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR06_02: 마이크로청크·방 전환·휴대물·폭탄·로프·무작위 런 검증`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
