# Character Inventory Schema

| 필드 | 수명 | HUD |
|---|---|---|
| health | 현재 런 | 표시 |
| currencyWon | 현재 런 | 표시 |
| ropeCount | 현재 런 | 표시 |
| bombCount | 현재 런 | 표시 |
| exitState | 현재 스테이지 | 표시 |
| bellState | 현재 스테이지 또는 런, CHAR05에서 확정 | 표시 |

음수 수량을 허용하지 않는다. 영속 저장과 런 내 임시 상태의 경계는 CHAR05에서 기존 저장 구조를 조사한 뒤 잠근다.
