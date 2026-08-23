# Character Action Schema

| 필드 | 설명 |
|---|---|
| actionId | 안정적인 논리 행동 ID |
| pressedThisFrame | 이번 렌더 프레임 눌림 |
| held | 현재 유지 여부 |
| releasedThisFrame | 이번 렌더 프레임 해제 |
| consumed | 현재 물리 틱에서 소비 여부 |
| timestamp | 입력 수집 시각 또는 틱 |
| direction | 조합 입력 방향 |
| lockReasons | 행동을 차단한 사유 집합 |

동일 입력을 두 상태가 중복 소비하지 않도록 소비 주체와 틱을 기록한다.
