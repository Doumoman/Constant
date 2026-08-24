# Character Damage Schema v2.0

공용 충격·피해 계약이다. 밟기·투척물·폭탄·도구·환경 위험과 플레이어 피해가 같은 스키마를 사용하되 대상(적/플레이어)에 따라 결과 처리가 분리된다.

| 필드 | 설명 |
|---|---|
| sourceId | 피해 원인 인스턴스 식별자 |
| cause | Stomp, ThrownObject, Explosion, ToolHit, EnemyContact, Spike, Fall, Crush, Environment |
| amount | 피해량 |
| hitPoint | 충돌 위치 |
| impulse | 넉백 요청 벡터 |
| stunDuration | 경직 시간 |
| bypassInvulnerability | 무적 무시 여부, 기본 false |
| eventTick | 중복 피해 판정 틱 |

## 고정 규칙

- cause는 전투 경로 계약(밟기/접촉/투척물/폭탄/도구/환경)과 정합해야 한다. 별도 일반 공격 cause는 존재하지 않는다.
- 일반 적 처리: 첫 유효 밟기 = 기절(stunDuration), 기절 중 두 번째 충격 또는 위험 지형 = 제거.
- 플레이어 반동(stomp bounce)은 피해 스키마가 아니라 이동 튜닝(`stompBounceVelocity`)의 소관이다.
- cause 열거형 확장은 CHANGE CONTROL 또는 해당 Task의 명시적 허용이 필요하다.
- 판정은 물리 틱 기준이며 Animator 이벤트·렌더 프레임에 의존하지 않는다.
