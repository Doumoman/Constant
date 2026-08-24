# Character Inventory Schema v2.0

| 필드 | 수명 | HUD |
|---|---|---|
| health | 현재 런 | 표시 |
| handSlot | 현재 런 | 표시 |
| currencyWon | 현재 런 | 표시 |
| ropeCount | 현재 런 | 표시 |
| bombCount | 현재 런 | 표시 |
| exitState | 현재 스테이지 | 표시 |
| bellState | 현재 스테이지 또는 런, CHAR05에서 확정 | 표시 |

## 고정 규칙

- `handSlot`은 정확히 하나이며 1×1 이하 휴대물(달떡, 소포, 돌, 상자, 폭탄, 기절 소형 적) 참조를 담는다. 슬롯이 차 있으면 새 들기는 거부된다.
- `ropeCount`/`bombCount`는 소모품 수량이며 휴대 슬롯과 별개다.
- 음수 수량을 허용하지 않는다.
- 영속 저장과 런 내 임시 상태의 경계는 CHAR05에서 기존 저장 구조를 조사한 뒤 잠근다.
