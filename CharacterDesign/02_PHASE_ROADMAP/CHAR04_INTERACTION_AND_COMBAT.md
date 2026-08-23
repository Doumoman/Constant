# CHAR04 — 상호작용과 접촉 전투

## 목표

들기·내려놓기·던지기와 밟기·기절·제거·피격을 하나의 상호작용 전투 코어로 구현한다.

## 진입 조건

CHAR03 EXIT 승인 및 방 경계 연동 완료

## 종료 조건

Carryable·기절 적·투척물·밟기·접촉 피해가 고정 테스트룸에서 모두 판정됨

## 작업 목록

| 작업 | 내용 | TASK | RESULT |
|---|---|---|---|
| CHAR04_01 | Carryable 검색·휴대·안전 내려놓기·방향 투척 구현 | `CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW.md` | `CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md` |
| CHAR04_02 | 밟기·반동·첫 기절·두 번째 제거·측면 피격 구현 | `CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE.md` | `CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md` |
| CHAR04_03 | 투척·환경 충격 계약과 일반 공격 부재 검증 | `CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md` | `CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md` |
| CHAR04_04 | 상호작용·접촉 전투 종료 감사 | `CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT.md` | `CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md` |

## 단계 규칙

- 위 순서를 변경하지 않는다.
- 동시에 두 작업을 CURRENT로 만들지 않는다.
- 마지막 EXIT AUDIT가 PASS여도 다음 단계는 별도 OPEN 패치 전까지 LOCKED다.
