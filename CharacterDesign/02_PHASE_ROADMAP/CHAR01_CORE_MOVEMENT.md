# CHAR01 — 핵심 이동

## 목표

입력·상태·충돌·지상 모터·점프를 하나의 플레이어 이동 코어로 구현한다.

## 진입 조건

CHAR00 EXIT 승인 및 기존 프로젝트 경로 등록 완료

## 종료 조건

고정 테스트룸에서 이동·점프·착지·상태 전환이 재현 가능함

## 작업 목록

| 작업 | 내용 | TASK | RESULT |
|---|---|---|---|
| CHAR01_01 | 논리 입력 스냅샷·버퍼와 플레이어 상태 머신 구현 | `CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES.md` | `CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md` |
| CHAR01_02 | 충돌 질의·지지체 추적·걷기·달리기·가감속 구현 | `CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR.md` | `CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md` |
| CHAR01_03 | 점프·가변 높이·코요테·공중 제어·낙하·착지 구현 | `CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md` | `CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md` |
| CHAR01_04 | 핵심 이동 종료 감사 | `CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md` | `CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md` |

## 단계 규칙

- 위 순서를 변경하지 않는다.
- 동시에 두 작업을 CURRENT로 만들지 않는다.
- 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
