# Character Movement Tuning Schema v2.0

## 저장 형식(고정)

- authoritative source는 정확히 하나다. 같은 의미의 값을 코드, Prefab, ScriptableObject, CSV에 중복 소유하지 않는다.
- 형식 기준선은 CHAR00_01 조사 선례인 ScriptableObject(레거시 `P1MovementTuning`)를 따른다. 실제 자산 생성과 최종 형식 확정은 CHAR01 구현 Task에서 한다.
- 좌표 기준: `1 logical cell = 1 world unit`.

## 논리 필드

| 필드 | 의미 | 검증 |
|---|---|---|
| colliderSize | 캡슐 콜라이더 크기 | 기준선 `0.72 × 0.90`, 1×1 셀 미만 |
| walkSpeed | 걷기 목표 속도 | 0보다 큼 |
| runSpeed | 달리기 목표 속도 | walkSpeed보다 큼 |
| groundAcceleration | 지상 가속 | 0보다 큼 |
| groundDeceleration | 지상 감속 | 0보다 큼 |
| airAcceleration | 공중 제어 가속 | 0 이상 |
| jumpVelocity | 점프 초기 속도 | 0보다 큼 |
| riseGravity | 상승 중 중력 계수 | 0보다 큼 |
| fallGravity | 하강 중 중력 계수 | riseGravity 이상 권장, 실제 값은 테스트로 결정 |
| maxFallSpeed | 최대 낙하 속도 | 0보다 큼 |
| coyoteTime | 코요테 시간 | 0 이상 |
| jumpBufferTime | 점프 버퍼 시간 | 0 이상 |
| groundProbeDistance | 접지 프로브 거리 | 기준선 `0.08` |
| groundedVyThreshold | 접지 수직 속도 게이트 | 기준선 `≤ 0.05` |
| stompBounceVelocity | 밟기 반동 속도 | 0보다 큼 |

## 판정 규칙

- 수치 PASS 기준은 값 자체가 아니라 2셀 높이 도달 / 2셀 틈 통과 / 3셀 틈 기본 통과 실패의 이동 문법 결과로 판정한다.
- 필드 추가·삭제·이름·단위·저장 의미 변경은 CHANGE CONTROL 대상이다.
