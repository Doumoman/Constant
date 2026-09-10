# SV5 공간 그래프 예약·접촉 보완

## 활성 계약

SV5_06_FIX01은 SV5_06의 장소 수나 전체 공간 구성을 승인하는 작업이 아니다. 이 보완의 정본은
`GENERATED/SV5_06_FIX01/space_graph.json`의 plan digest로 묶인 accepted reservation model이다.
중심선, 필수 clearance, 기존 포트 폭을 따르는 aperture adapter, 조건부 차단 경계를 동일 모델에서
후보 심사·접촉 열거·상태 투영·export에 사용한다.

## 예약과 포트 전이

- 새 통로 후보는 중심선만 먼저 고른 뒤 envelope를 팽창하지 않는다. 중간 구간의 cardinal cross 전체가
  FixedSolid·SOLID route support·다른 장소 예약을 침범하지 않을 때만 채택한다.
- 포트 주변은 기존 aperture에서 전폭 구간까지 이어지는 명시적 adapter를 찾는다. 예를 들어 Start 포트의
  기존 SOLID support `(413,340)`는 그대로 보존하고, 기존 AIR passage를 따라 안전한 전폭 구간에서 분기한다.
- `CoreProtected`, `CoreRoute`, `PlannedFootprint`, `CorridorCenterline`, `CorridorClearance`, `PortAperture`,
  `ConditionalGate`는 별도 의미로 export된다. 채택 계획의 FixedSolid·route support 충돌은 0이다.

## 접촉과 상태

- 모든 active Passage·Clearance·PortAperture 셀이 공용 접촉 열거기에 들어간다. 별도 grid 대조기가 shared와
  cardinal-face pair를 독립 생성해 누락·추가·중복을 거부한다.
- route ID를 정렬해도 `RouteAWorld/RouteBWorld`와 각 kind 집합을 보존한다. contact ordinal은 옛
  `core.RouteCells.Ordinal`이 아니라 실제 connection envelope와 centerline에서 계산한다.
- 같은 물리 접점은 동일 analysis node를 공유한다. 일반 optional/village 연결은 `BIDIRECTIONAL`로 투영하고,
  core edge는 기존 방향·조건·action을 그대로 유지한다.
- 3개 자원의 6순서 모두 goal에 도달하며, 모든 도달 상태가 goal 상태 집합으로 역도달 가능해야 한다.

## 계획 gate

조건이 있는 접촉은 route pair와 실제 predicate가 같은 물리 경계로 병합된다. 각 gate는 boundary ID,
모든 관련 contact ID, blocking cell/face 전체 집합, 양측 anchor, 방향, flow, predicate,
`SEALED_BLOCKS_ALL_BOUNDARY_CELLS_AND_FACES`와
`OPEN_RESTORES_BIDIRECTIONAL_PREDICATE_SEGMENTS` 상태를 가진다. SEALED 검사는 모든 shared cell과 face가
차단 집합에 포함되는지 확인하고, OPEN은 동일 실제 connector의 양방향 predicate segment를 복원한다.
이것은 계획 예약이며 runtime gate나 합성 지형 증거가 아니다.

## 검증과 남은 책임

Unity 6000.3.8f1 focused EditMode에서 SV5 직접 테스트 43개가 모두 통과했다. N01/N02는 기존 5,347쌍에
4,746쌍이 빠졌던 반례와 FixedSolid 72행·route support 113행 충돌을 보존 바이트에서 재현한다. 채택 결과는
접촉 coverage 오류 0, 예약 충돌 0, gate geometry 오류 0, projection 6/6이다.

`geometry_state_ready=false`, `player_verified=false`를 유지한다. SG06-F5의 22장소·대부분 INFILL_PENDING·
단일 순환 구조는 SV5_07의 family 분포, SV5_08의 일반 공간 밀도, SV5_09의 재합류 확장 검토 대상이다.
SV5_41은 합성 geometry, SV5_44는 실제 Player 검증을 담당한다.
