# CHAR00_02 — 게임플레이 계약·소유권·고정 테스트룸 확정

TASK ID: CHAR00_02  
PHASE: CHAR00  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: CHAR00_01

## 목표

기존 프로젝트를 조사하고 캐릭터 규칙·소유권·경로·테스트 기준을 확정한다.

현재 작업 범위는 **게임플레이 계약·소유권·고정 테스트룸 확정**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/**`
- `{{DISCOVERED_READ_PATHS}}`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md`
- `{{HARNESS_WRITE_PATHS}}`

ALLOWLIST NOTE: CURRENT로 열기 전 CHAR00_01 registry를 사용해 모든 토큰을 실제 경로로 치환해야 한다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘게임플레이 계약·소유권·고정 테스트룸 확정’이다.
- 현재 TASK에 필요하지 않은 기존 코드를 리팩터링하거나 공개 API 이름을 변경하지 않는다.
- 새 전역 싱글톤, 새 asmdef, 새 입력 프레임워크를 임의로 추가하지 않는다.
- 게임플레이 로직을 Animator 이벤트 또는 렌더 프레임에 종속시키지 않는다.
- 고정 규칙과 충돌하거나 ALLOWLIST가 부족하면 확장하지 말고 BLOCKED로 보고한다.
- 프로젝트 사실을 조사 결과와 추측으로 구분한다.
- 기존 사용자 변경을 수정하거나 정리하지 않는다.

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

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 4개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR00_02: 게임플레이 계약·소유권·고정 테스트룸 확정`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
