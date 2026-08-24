# Interaction Course Spec v2.0

## 공통 규약

- 지형은 MAP 공용 논리 셀 좌표로 기술한다(`1 cell = 1 world unit`).
- 필수 더미 휴대물: 달떡, 소포, 돌, 상자, 폭탄, 기절 소형 적(전부 1×1 이하).
- 검증 위치 변형: 빈 공간, 벽 근처, 천장 아래, 방 경계 부근.
- 판정은 물리 틱 기준이며 Animator 이벤트에 의존하지 않는다.

## fixture: carry_one_slot_course

- setup: 평지에 휴대물 A와 B를 1셀 간격으로 배치. 플레이어는 A 인접에서 시작.
- action: A에 Action(X)으로 들기 → B로 이동해 Action(X)으로 들기 시도.
- expected: A만 휴대 상태다. B 들기는 거부되고 A는 유지된다(자동 스왑·중복 휴대 없음). handSlot은 항상 1개 이하다.
- failure: 동시에 2개를 휴대하거나, B 시도 중 A가 소실·교체된다.

## fixture: safe_down_x_place_reject_overlap_course

- setup: 구간1 = 발밑이 빈 안전 바닥. 구간2 = 발밑 배치 지점이 다른 오브젝트 또는 벽으로 막힌 위치(천장 아래·벽 근처 변형 포함).
- action: 휴대 상태로 각 구간에서 Down+Action(아래+X).
- expected: 구간1에서는 발밑 안전 위치에 내려놓기 성공 — 투척이 아니며 수평 임펄스가 없다. 구간2에서는 행동이 거부되고 휴대가 유지되며 겹침 배치가 발생하지 않는다.
- failure: 구간2에서 오브젝트가 기존 오브젝트·지형과 겹쳐 배치되거나 소실된다.

## fixture: directional_throw_course

- setup: 상·좌·우가 비어 있는 개활지. 휴대 상태로 시작. 궤적 로거.
- action: Up+Action, Left+Action, Right+Action을 각각 수행한다.
- expected: 위+행동은 상향 궤적, 좌/우+행동은 해당 수평 방향 궤적으로 투척된다. 투척 직후 소유자 충돌 유예가 적용되고 휴대 슬롯이 비워진다.
- failure: 궤적 방향 불일치, 투척 직후 소유자 피격, 또는 휴대 해제 실패.
