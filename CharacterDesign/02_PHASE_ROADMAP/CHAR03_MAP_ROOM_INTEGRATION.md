# CHAR03 — MAP·방 전환 연동

## 목표

MAP 좌표·월드 질의·방 준비 게이트와 카메라룸 전환을 캐릭터에 연결한다.

## 진입 조건

CHAR02 EXIT 승인 및 이동 문법 확정

## 종료 조건

준비되지 않은 방은 차단되고 준비된 방 전환에서 입력·속도 KEEP과 Hysteresis가 동작함

## 작업 목록

| 작업 | 내용 | TASK | RESULT |
|---|---|---|---|
| CHAR03_01 | MAP 좌표·월드 질의와 방 경계 준비 게이트 연결 | `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md` | `CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md` |
| CHAR03_02 | 카메라룸 전환·입력 KEEP·속도 KEEP·Hysteresis 구현 | `CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md` | `CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md` |
| CHAR03_03 | MAP·방 전환 종료 감사 | `CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md` | `CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md` |

## 단계 규칙

- 위 순서를 변경하지 않는다.
- 동시에 두 작업을 CURRENT로 만들지 않는다.
- 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
