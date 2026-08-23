# Character Damage Schema

| 필드 | 설명 |
|---|---|
| sourceId | 피해 원인 인스턴스 식별자 |
| cause | EnemyContact, Explosion, Spike, Fall, Crush 등 |
| amount | 피해량 |
| hitPoint | 충돌 위치 |
| impulse | 넉백 요청 벡터 |
| stunDuration | 경직 시간 |
| bypassInvulnerability | 무적 무시 여부, 기본 false |
| eventTick | 중복 피해 판정 틱 |

cause 열거형 확장은 CHANGE CONTROL 또는 해당 TASK의 명시적 허용이 필요하다.
