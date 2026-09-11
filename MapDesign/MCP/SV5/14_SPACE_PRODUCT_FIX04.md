# SV5_06_FIX04 — 국소 통로 경계와 물리/FSM product

최종 실행 수치와 승인 상태의 단일 기록은 `REPORTS/SV5_06_FIX04_RESULT.md`다.
이 문서는 구현 의미를 설명하며, 좌표 탐색을 지형 완성이나 Player 완주로 승격하지 않는다.

## 원인과 수정

입구 후보의 원래 조건은 선택 포트의 `OpenCells`에서 `first`를 꺼낸 뒤 모든
포트의 `OpenCells` 집합에 속하지 않아야 한다고 요구했다. 두 조건은 동시에
성립할 수 없다. 단순히 보호 검사를 제거하지 않고 `PortBoundaryCandidates`가
ordered centerline을 따라 입구 바깥으로 이동하여 두 셀 모두 보호 AIR와 aperture
밖인 cardinal edge만 반환하도록 바꿨다. 포트로부터 최대 7칸이며 Clearance는
통행 셀로 사용하지 않는다. C02는 전체 plan의 geometry 개수가 아니라 이 생산
후보 함수의 정확한 좌표와 후보 부재를 deterministic fixture로 검사한다.

기존 route에 원격 cut을 누적하는 `ExpandGlobalCuts`는 제거했다. Seal/Boss
통로를 우선 확보하고 정상 통로가 그 통행 셀 및 옆면에 접촉하지 않도록
cardinal router로 재배치한다. Forge의 문 앞 구간은 정상 Forge 귀환길과 합류할
수 있다. 다른 게이트 통로에는 선언된 공유 입구 근처 외에는 접촉하지 않는다.
optional 통로 재배치는 중심선뿐 아니라 기존 폭/clearance 계약과 고체·바닥
예약을 검사한다. 고정 place 크기와 과거 core reservation 데이터는 수정하지 않는다.

각 gate는 실제 ordered path에 있는 단일 국소 neck face에서 새로 생성한다.
다른 connection의 path edge, 보호 AIR, 다른 portal과 공유하는 face를 거부한다.
해당 gate 단독 폐쇄 시 전역 좌표 graph가 끊기는지 확인하고, 후보의 trial gate
집합을 전체 canonical reachable-state product에 적용하여 expected-open 연결이
모두 보존될 때만 선택한다. 원격 face 또는 이전 FIX03 blocking geometry를 합치지 않는다.

segment는 path 길이의 3등분이 아니다. 실제 차단 edge의 ordinal에서 source
corridor / 두 셀 portal / target corridor로 나눈다. predicate는 portal segment에만
있다. `CORRIDOR`/`EXACT_NECK`은 설명용 구간 종류이며 새로운 전역 영역 체계가 아니다.

## 접촉과 이동

현재 connection의 Centerline/ApertureCells 합집합만 전역 통행 graph다.
Clearance, INFILL_PENDING, 폐기한 centerline은 이동 노드가 아니다. 과거 reservation은
보존 데이터로 남지만 현재 접촉 목록에 과거 통행선을 다시 합치지 않는다.

route predicate가 다르다는 이유로 접촉 전체를 gated/separated로 분류하지 않는다.
정확한 SHARED blocking cell 또는 FACE blocking edge가 있을 때만 ConditionalGate다.
나머지는 좌표 Join이며 그 상태 안전성은 닫힌 guarded bypass까지 검사하는
물리 product에서 검증한다. 문자열 boundary, route owner, ContactIds membership은
geometry 증거를 대체하지 않는다. FindGateErrors와 PhysicalMovement.Analyze는
동일한 production local-barrier 판정을 소비한다.

합법적인 reroute 후 ConditionalGate contact가 0개여도 세 core gate는 존재하고
각각 닫힘/열림을 실제 전역 graph로 증명해야 한다. 접촉 225개는 FIX03 반례의
수치이지 FIX04의 필수 출력 개수가 아니다.

## 실제 product 증거

RMAP13 `ExploreWithAnalysisNodes`가 여섯 자원 순서 각각의 합법적 상태·행동·전이를
제공한다. canonical 코드는 변경하지 않는다. 모든 canonical reachable state에서
세 gate를 동시에 적용하고 모든 core/optional 방향의 기대 도달 여부를 검사한다.
닫혀야 하는 guarded 연결도 제외하지 않는다.

별도의 INITIAL BFS가 물리적으로 통과한 MOVE와 canonical action만 따라간다.
이 실제 product에서 goal까지의 행동열, 도달 상태 수, 전이 수, goal 역도달 상태,
dead-end를 계산한다. 이전 논리 proof에 1/0/1을 채워 넣는 방식은 제거했다.
캐시는 connection/direction/실제 closed gate 집합이 같은 이동 결과만 재사용한다.

F01은 exact FIX03 CSV/JSON을 읽어 같은 production validator로 218/225 오류를
재현한다. F03은 exact FIX03 connection/gate와 FIX04 connection/gate에 같은
DeepStar ID·초기 상태를 전달한다. F04는 gate 부분집합 여덟 개에서 정상 경로를,
F06은 optional 세 경로의 양방향 및 전체 product를 검사한다. F07은 입력 연결 ID와
집합 순서를 실제 변경하고 gate 제거가 digest와 bypass 결과를 바꾸는지 확인한다.

기존 Fix01/Fix02/Fix03/Plan 시험의 accepted-plan contact lookup이나 distributed
face 개수 가정은 국소 neck 모델에 맞지 않는다. 접촉을 다시 만들거나 null을
허용하지 않고 deterministic geometry fixture 또는 실제 세 gate의 closed/open
검사로 목적을 보존한다. G07의 digest mutation은 반드시 실제 존재하는 decision을
바꾸며, geometry mutation은 다른 위치의 face로 교체하여 실제 bypass를 일으킨다.

## Export와 보존

connections/segments/gate_geometry/local_barrier_checks/physical_transition_matrix/
physical_contact_checks/physical_gate_state_checks/state_proofs/validation/preview는
같은 plan을 소비한다. matrix는 order/state/connection/direction/predicate/모든 gate
상태/도달 결과/실제 witness 참조/차단 이유를 기록한다. witness 좌표열은 ID별로
중복 제거한다. validation PASS는 plan 계산값이며 수동 후처리하지 않는다.

legacy export는 FIX04 `legacy_exports/<시험별 고유 경로>`에만 기록한다.
과거 GENERATED/Task/Archive/Result와 무관한 dirty 변경은 보존한다. 실행 전후의
전체 299 ALWAYS 및 35 commit blob 검사는 `PRECHECK --mode post-readonly`로 수행한다.
FIX03 GENERATED 161개는 그 검사에 포함된다. `_work`는 임시 진단이며 최종 ZIP에서 제외한다.

`repair_before_after.svg`는 1칸 격자, 4×4 경계, 확인된 고체, 예정 통로,
미배치 셀, 닫힌 문 face, 좌표 탐색 witness를 구분한다. viewport 밖의 gate도 실제
탐색에는 모두 적용한다. 그림은 실제 바닥·착지·머리 여유의 완성 증거가 아니다.

`ComposedGeometryReady=false`, `PlayerVerified=false`. 624×416, 일반 상승 +1,
Jump/Grab 최대 +2를 변경하지 않는다. SV5_07 이후 공간/조립/Player 작업은 시작하지 않는다.
