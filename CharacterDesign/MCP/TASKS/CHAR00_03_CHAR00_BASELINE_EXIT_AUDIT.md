# CHAR00_03 — 기준선·하네스 종료 감사

TASK ID: CHAR00_03  
PHASE: CHAR00  
STATE SOURCE: `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`  
DEPENDS ON: CHAR00_02

## 목표

기존 프로젝트를 조사하고 캐릭터 규칙·소유권·경로·테스트 기준을 확정한다.

현재 작업 범위는 **기준선·하네스 종료 감사**에 한정한다.

## READ ALLOWLIST

- `CharacterDesign/**`
- `{{DISCOVERED_READ_PATHS}}`

## WRITE ALLOWLIST

- `CharacterDesign/MCP/RESULTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md`
- `{{HARNESS_WRITE_PATHS}}`

ALLOWLIST NOTE: CURRENT로 열기 전 CHAR00_01 registry를 사용해 모든 토큰을 실제 경로로 치환해야 한다.

## 구현 요구사항

- 현재 TASK의 유일한 구현 목표는 ‘기준선·하네스 종료 감사’이다.
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

TEST COUNT: 6

1. PhaseTaskResultsComplete: 해당 단계의 선행 RESULT가 모두 STATUS: PASS
2. UnityCompileClean: 해당 단계 최종 코드 기준 컴파일 오류 0
3. PhaseRegressionSuite: 단계 로드맵에 지정된 회귀 테스트 전체 PASS
4. AllowlistAudit: 단계 중 WRITE ALLOWLIST 위반 0
5. LockedRuleAudit: 고정 규칙 위반 0
6. NextTaskStillLocked: 다음 단계 또는 작업이 아직 LOCKED

## RESULT 계약

RESULT PATH: `CharacterDesign/MCP/RESULTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md`

RESULT는 다음을 포함한다.

- 독립된 상태 줄 `STATUS: PASS`, `STATUS: FAIL` 또는 `STATUS: BLOCKED`
- 실제 변경 파일 전체
- 구현한 세부 내용
- 컴파일 결과
- 고정 테스트 6개의 개별 결과
- 잔여 문제와 재현 정보

PASS가 아니면 FINALIZE할 수 없다.

## 커밋 계약

권장 제목: `CHAR00_03: 기준선·하네스 종료 감사`

커밋 본문에는 구현 세부 사항, 테스트 결과, 남은 제한을 기록한다. RESULT 작성과 검증 전에는 커밋하지 않는다.
