# Combat Course Spec v2.0

## 공통 규약

- 지형은 MAP 공용 논리 셀 좌표로 기술한다(`1 cell = 1 world unit`).
- 구간 분리: 상단 접촉, 측면 접촉, 하단 접촉, 투척 충격, 폭발, 가시, 낙사를 각각 분리된 구간에서 검증한다.
- 일반 공격 입력용 구간은 만들지 않는다.
- 판정은 공용 충격·피해 계약(`CHARACTER_DAMAGE_SCHEMA`)과 물리 틱 기준이다.

## fixture: stomp_contact_course

- setup: 평지의 일반 적 1기와 상단 접근용 단차(높이 2셀). 측면 접촉용 별도 구간에 적 1기.
- action: 1) 하강 중 적 상단 접촉(밟기) → 2) 기절한 적에 두 번째 충격(재밟기 또는 투척물). 별도 구간: 지상에서 측면 접촉.
- expected: 첫 유효 밟기 = 적 기절 + 플레이어 반동(stompBounceVelocity). 기절 중 두 번째 충격 = 적 제거. 측면·하단 접촉 = 밟기가 아니며 플레이어 피해 후보다.
- failure: 상승 중 접촉이 밟기로 인정되거나, 측면 접촉이 밟기로 인정되거나, 첫 밟기에서 적이 즉시 제거된다.

## fixture: no_basic_attack_button_course

- setup: 전방에 일반 적 1기. 플레이어는 휴대물 없이 시작.
- action: 고정 입력 전체(Move/Down/Jump/Action(X)/Bomb(Z)/Rope(C))와 조합을 입력한다.
- expected: 어떤 입력도 "일반 공격" 판정·상태를 만들지 않는다 — X는 상호작용/들기, Z는 폭탄, C는 로프로만 동작한다. 적 피해는 밟기/접촉/투척물/폭탄/도구/환경 경로로만 발생한다.
- failure: 입력만으로 별도 공격 상태 또는 공격 판정이 발생한다.

## fixture: thrown_object_impact_course

- setup: 돌 1개와 전방 3셀 거리의 일반 적 1기. 기절 상태 적 변형 구간 포함.
- action: 돌을 들고 Left/Right+Action으로 적에게 투척한다.
- expected: 투척물 충돌이 공용 충격·피해 계약(cause=ThrownObject)으로 전달된다 — 일반 적은 기절, 이미 기절한 적은 제거된다. 판정은 물리 틱 기준이며 렌더·애니메이션과 무관하다.
- failure: 충격 미전달, 투척 직후 소유자 피격, 또는 Animator/렌더 프레임 의존 판정.
