# Character Action Schema v2.0

## 고정 논리 행동 ID

```text
Move, Down, Jump, Action, Bomb, Rope
```

조합 의미: `Down+Action` = 안전 내려놓기, `Up+Action` / `Left+Action` / `Right+Action` = 방향 투척.
기준 바인딩은 `01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`를 따른다(Jump=Space 기준선, Action=X, Bomb=Z, Rope=C).

## 스냅샷 필드

| 필드 | 설명 |
|---|---|
| actionId | 위 고정 논리 행동 ID |
| pressedThisFrame | 이번 렌더 프레임 눌림 |
| held | 현재 유지 여부 |
| releasedThisFrame | 이번 렌더 프레임 해제 |
| consumed | 현재 물리 틱에서 소비 여부 |
| timestamp | 입력 수집 시각 또는 틱 |
| direction | 조합 입력 방향 |
| lockReasons | 행동을 차단한 사유 집합 |

## 소비 규칙

- Update 수집과 FixedUpdate 소비 사이에 눌림 이벤트가 소실되지 않아야 한다.
- 동일 입력을 두 상태가 중복 소비하지 않도록 소비 주체와 틱을 기록한다.
- 조합 입력이 성립하면 단일 의미로 중복 소비하지 않는다(Down+Action 성립 시 단독 Action 소비 금지).
