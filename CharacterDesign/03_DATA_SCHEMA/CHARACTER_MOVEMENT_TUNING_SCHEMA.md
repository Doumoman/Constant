# Character Movement Tuning Schema

저장 형식은 CHAR00 조사 후 기존 프로젝트 방식에 맞춰 확정한다. 다음 논리 필드는 반드시 하나의 등록된 설정 소스에서 제공한다.

| 필드 | 의미 | 검증 |
|---|---|---|
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
| stompBounceVelocity | 밟기 반동 속도 | 0보다 큼 |

수치 PASS 기준은 값 자체가 아니라 2셀/3셀 이동 문법 결과로 판정한다.
