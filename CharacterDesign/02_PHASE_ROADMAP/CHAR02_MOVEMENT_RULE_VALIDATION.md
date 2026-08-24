# CHAR02 — 이동 문법 검증

## 목표

기획된 셀 이동 문법과 금지된 이동 능력을 실제 수치와 테스트로 확정한다.

## 진입 조건

CHAR01 EXIT 승인 및 이동 코어 완료

## 종료 조건

2셀 높이·2셀 달리기 틈·3셀 기본 통과 불가가 모두 검증됨

## 작업 목록

| 작업 | 내용 | TASK | RESULT |
|---|---|---|---|
| CHAR02_01 | 2셀 높이 점프와 동일 높이 2셀 틈 달리기 검증 | `CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md` | `CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md` |
| CHAR02_02 | 3셀 기본 통과 실패와 벽 점프·대시·이중 점프 부재 검증 | `CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT.md` | `CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md` |
| CHAR02_03 | 이동 문법 종료 감사 | `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md` | `CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md` |

## 단계 규칙

- 위 순서를 변경하지 않는다.
- 동시에 두 작업을 CURRENT로 만들지 않는다.
- 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
