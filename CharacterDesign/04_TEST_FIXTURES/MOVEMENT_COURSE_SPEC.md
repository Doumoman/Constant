# Movement Course Spec v2.0

## 공통 규약

- 지형은 MAP 공용 논리 셀 좌표로 기술한다(`1 cell = 1 world unit`, x 오른쪽 양수, y 위쪽 양수).
- 바닥·벽 셀은 GroundSolid 의미이며 플레이어는 셀 상단에 접지 상태로 시작한다.
- "기본 이동"은 Move/Down/Jump 입력만 사용한다(도구·폭탄·로프 금지).
- 판정은 고정 물리 틱 기준이다. 프레임레이트별 위치 허용 오차는 `±0.05 world unit`이다.
- 코스 구성 필수 구간: 평지 가속/감속, 방향 전환, 높이 1셀, 높이 2셀, 동일 높이 2셀 틈, 3셀 틈, 낮은 천장, 관통(one-way) 발판, 이동 발판.

## fixture: two_cell_height_jump_course

- setup: 평지 y=0. 전방에 상단 높이 y=+2, 폭 2셀 이상 발판.
- action: 발판 인접 접지 상태에서 Jump 1회(필요 시 전방 Move 병행).
- expected: 단일 점프로 y=+2 발판 상단에 착지하고 접지 판정이 true가 된다.
- failure: 점프 정점이 발판 상단에 미달하거나, 도달에 2회 이상의 점프 입력이 필요하다.

## fixture: two_cell_same_level_gap_run_course

- setup: y=0 동일 높이 바닥. 폭 정확히 2셀인 틈(아래는 낙하 감지 구역). 건너편 바닥 동일 높이, 조주 구간 6셀 이상.
- action: 달리기 최고 속도로 진입해 기본 이동(달리기 점프 허용)만으로 통과한다.
- expected: 낙하 감지 구역에 진입하지 않고 건너편에 접지한다.
- failure: 낙하 감지 구역 진입, 또는 도구·환경 도움 없이는 통과 불가.

## fixture: three_cell_same_level_gap_basic_movement_failure_course

- setup: y=0 동일 높이 바닥. 폭 정확히 3셀인 틈(아래는 낙하 감지 구역). 건너편 바닥 동일 높이, 조주 구간 6셀 이상.
- action: 기본 이동의 모든 조합(최고 달리기 속도 + 최대 지속 점프 + 코요테/버퍼 활용 포함)으로 통과를 시도한다.
- expected: 기본 이동만으로는 통과 불가 — 시도는 낙하 감지 구역 진입 또는 출발측 잔류로 끝난다.
- failure: 기본 이동만으로 건너편 접지에 성공한다(이동 문법 위반).

## fixture: forbidden_wall_jump_course

- setup: 바닥 y=0, 전방에 높이 4셀 이상 수직 벽. 플레이어가 벽에 밀착 가능한 구간.
- action: 점프로 공중에 뜬 뒤 벽 접촉 상태에서 Jump를 반복 입력한다.
- expected: 벽 접촉이 새 점프를 부여하지 않는다 — 총 상승량이 단일 점프 정점을 초과하지 않고 벽을 이용한 재상승이 없다.
- failure: 벽 접촉 상태의 Jump 입력으로 추가 상승(벽 점프)이 발생한다.

## fixture: forbidden_dash_course

- setup: 평지 y=0, 직선 주로 12셀 이상. 수평 속도 로거.
- action: 고정 입력 전체(Move/Down/Jump/Action/Bomb/Rope)의 조합·연타를 입력하며 주행한다.
- expected: 수평 속도가 어느 순간에도 튜닝 최고 달리기 속도(runSpeed)를 초과하는 순간 가속(대시)이 존재하지 않는다.
- failure: 어떤 입력 조합이 runSpeed 초과의 수평 속도 버스트를 발생시킨다.

## fixture: forbidden_double_jump_course

- setup: 평지 y=0. 점프 궤적 로거.
- action: 점프 후 공중에서(코요테·버퍼 유예 창 밖에서) Jump를 재입력한다.
- expected: 공중 재점프가 발생하지 않는다 — 상승 곡선이 단일 점프 곡선과 동일하다. 코요테/버퍼는 지상 이탈 직후·착지 직전 유예로만 동작한다.
- failure: 공중에서 두 번째 상승 임펄스가 발생한다.
