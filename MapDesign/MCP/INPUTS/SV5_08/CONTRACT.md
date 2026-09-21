# SV5_08_INFILL — 작은 방·굴·계단참의 셀과 연결

이 패키지는 SV5_08 하나를 여는 구현 지시이며 구현 결과가 아니다.
정본은 SPACE_V5_RULES/SPACE_FILL과 SV5_07의 실제 완료 plan이다. 기존 45개 순서/등록을 바꾸지 않는다.

## I01. 이번 결과의 모습과 범위

큰 장소 외곽 사이에 작은 빈방·공동·계단참·막다른 굴과 짧은 2칸 연결이 생긴다.
추상 점/선과 방 이름만 더하는 작업이 아니다. 각각 바닥·벽·출입구·되돌아올 길이 있는 실제 1×1 셀 payload를 만든다.
그 셀을 4×4 패턴으로 분할하고 같은 셀에서 전체도/확대도를 생성한다. 사각형 내부를 모두 AIR로 채우는 것으로 끝내지 않는다.
큰 도서관·회랑·동굴 등의 기존 내부는 각 제작 Task의 소유다. 작은 통로로 분쇄하거나 infill을 넣지 않는다.
이번은 일반 공간의 첫 제작/배치 pass다. 전체 월드의 모든 잔여 영역을 채웠다거나 최종 밀도를 승인받았다고 기록하지 않는다.
무목적 빈방·휴식·되돌아오는 말단을 허용하며 자동 상자/NPC/이벤트를 넣지 않는다.
4×4 패턴/12×8 청크/624×416 및 기존 Player 0.4×0.8을 유지한다. 일반 상승+1, Jump+Grab 최대+2다.
SV5_09는 별개 지역 재합류, 10은 긴 불규칙 샛길, 41은 전체 합성, 44는 실제 Player 검증을 담당한다.
이번 새 셀은 생산 데이터다. Scene에 적용하거나 전체 기성 패턴 라이브러리를 수정하지 않는다.

## I02. 기준선 보존과 셀 소유

seed1304의 SV5_07 default ON 및 repeat ON을 두 integration 기준선으로 쓴다. 입력 seed/profile을 실패 후 교체하지 않는다.
기존 Plan 결과는 SOURCE_LOCK의 각 digest를 재현해야 한다. OFF는 infill만 OFF이고 diversity는 계속 ON이다.
기존 모든 place ID/family/formation/bounds, 포트/조건, 원래 연결의 ordered centerline, gate geometry/predicate와 core를 보존한다.
8개 core/2,432개 core 셀·RMAP13 FSM·W01/W02를 유지한다. 재배치·원격 gate 확대·문 삭제로 새 방 자리를 만들지 않는다.
새 방 후보는 기존 place footprint, 정본 보호, Type0 및 guarded corridor/portal 접근 여유 밖의 INFILL_PENDING에 배치한다.
별도로 기존6개 ORDINARY_ROOM_A~F(future_owner=SV5_08_INFILL)는 이 Task의 예약이므로 좌표·크기·포트를 유지하여 내부 셀을 완성한다.
이6개는 신규128개/신규면적에 중복 세지 않는다. 다른 large/core 예약 내부에는 이번 셀을 넣지 않는다.
기존 envelope/clearance는 빈 AIR가 아니다. 새 SOLID로 침범하지 않는다. 기존 보호 AIR 역시 SOLID로 덮을 수 없다.
부모 통로에 연결되는 정확한 개구만 기존 actionless passage와 공유할 수 있다. 공유 좌표/소유/동일 AIR 의미를 명시한다.
허용한 개구 이외의 다른 route cell/face와의 접촉은 실제 좌표로 검출하여 후보를 거절한다. 같은 소유 이름만으로 면제하지 않는다.
새 방끼리도 공유 개구 외의 SOLID/AIR 덮어쓰기를 금지한다. 부분 겹침은 미리 지정한 동일 의미 접합만 허용한다.
미소유 셀은 끝까지 UNKNOWN/INFILL_PENDING이다. 그것을 AIR 또는 SOLID로 채워 수치나 flood 결과를 개선하지 않는다.
기존 큰 공간의 예약 면적·기존 clearance·방의 bounding box를 새 실제 셀 증가량에 중복 집계하지 않는다.

## I03. 네 가지 기본형 — 좌표로 구현

INFILL_PROFILE의 크기는 외벽을 포함한 4의 배수 점유다. 내부 좌표는 좌하단(0,0), 정수 타일, inclusive 구간이다.
아래 기본형의 footprint는 처음 SOLID로 만들고 지정 AIR만 열되, T01/T04의 6×6/이동 검증을 통과해야 한다.
거절된 변형을 이름만 바꾸어 통과시키지 않는다. 사용되지 않은 출입 후보는 실제 SOLID로 닫는다.

| 기본형 | 점유 | 기본 내부/지지면 | 역할 |
|---|---|---|---|
| EMPTY_ROOM |16×12|AIR x=3..12,y=3..8, 바닥 지지면 높이3|10×6 빈방·쉼터|
| SMALL_CAVE |20×12|x=3..16, 바닥3; AIR 상단 y=8(x3..5),9(x6..9),8(x10..13),7(x14..16)|바닥은 안전하고 천장은 구간별 불규칙한 공동|
| LANDING |16×16|x=3..12, 바닥높이 3+min(2,floor((x-3)/2)), AIR는 그 높이부터 y12까지|2칸 폭마다+1씩 두 번 오른 뒤 넓은 고체 계단참|
| DEAD_END |16×12|빈방 기본형에서 x12,y6..8을 SOLID로 복원|뒤쪽 외곽이 눌린 보상 없는 굴|

기본 입구는 왼쪽 x=0..2,y=3..4를 AIR로 연 2칸 높이 개구다. x'=width-1-x의 정확한 좌우 반전을 허용한다.
계단참의 선택적 상부 자식 출입구는 실제 끝 지지면과 같은 높이의 측면 2칸 개구로 만든다. 외부 연결 없으면 닫는다.
범용 90도 회전은 금지다. 중력 방향까지 회전한 벽을 바닥이라고 주장하지 않는다.
기존6개 방은 LEGACY_ORDINARY_PORTED recipe로 현재 bounds에 맞춘다. 좌우 기존 port anchor의 높이 e=height/2를 보존한다.
좌우 개구는 y=e-1..e의2칸, 내부 x=3..width-4에서 floor=max(3,e-1-min(x-3,width-4-x)), ceiling=height-4이다.
floor..ceiling을 AIR로 하여 기존 포트 높이에+1 단위로 오르내리는 내부를 만든다. 모든 기존 passage/aperture AIR를 보존한다.
기존 corridor가 이 기본 셀과 충돌하면 footprint 안에서+1 지지와 개구를 보존하도록 recipe를 국소 조정하고 결과/이유를 기록한다.
기존 통로를 삭제/이동하거나 기존 포트를 다른 높이로 옮기는 방식은 금지다. 적어도6개 모두 셀과 왕복 검증이 필요하다.
최소4 신규 recipe를 모두 실제 배치에 사용한다. 표시 이름/색만 다른 복제는 종류 수에 포함하지 않는다.
기본 두께3을 사용하며 일반 구조 목표3~4를 지킨다. 굴 천장/계단 끝/개구 접합의 국소1~2칸은 좌표와 이유를 기록한다.
단, 예외로 지지면·Type0·core 보호를 지우거나 6×6 검사를 면제하지 않는다.
이 일반 방에는 장치/ONE_WAY/사다리/Grab을 필수로 넣지 않는다. +1 계단으로 안전한 왕복을 우선한다.
+2를 선택적으로 쓰려면 실제 노출 SOLID 모서리·매달림 몸체·당김 공간·지지면을 별도 기록/검사한다. +2 숫자만으로 통과 불가.
내부와 기존 통로 사이의 짧은 연결은 유효 여유2칸이 기본이다. 바닥 높이 변화는 +1 단위 지지면과 머리 여유를 갖는다.
연결 길이는 중심선24칸 이하 운영 상한이다. +3 수직벽·긴 자유낙하·점프 불가능한 천장·고립 발판으로 연결을 대신하지 않는다.

## I04. 실제 4×4 패턴과 데이터

Sv5InfillPatterns는 위 셀 recipe를 타입화한다. runtime에서 MCP JSON/CSV를 파일로 읽는 의존성을 만들지 않는다.
각 4×4 블록에 16셀 값을 전부 기록한다. 셀값 SOLID/AIR와 별도로 16-bit write_mask를 두어 미소유 셀을 구분한다.
write_mask=0인 셀은 AIR가 아니다. 블록이 방/통로 경계에 걸쳐도 다른 소유자의 셀을 덮지 않는다.
완전 소유한 방 footprint의 블록은 16셀 모두 소유한다. 연결/접합은 필요한 셀만 소유하고 읽어야 할 이웃 보호를 따로 기록한다.
패턴 ID는 mask+행 순서가 고정된16셀의 semantic hash를 사용하고 같은 mask/셀은 dedup한다. orientation과 world origin을 따로 저장한다.
기존 RMAP07/POOL500 패턴 ID를 지어내지 않는다. 신규 INFILL 전용 패턴임을 명시하고 후속41의 소비 책임을 기록한다.
이것은 기존 풀 데이터를 수정할 권한이 아니다. 이번 runtime 모델과 GENERATED 내 패턴 표로 보유한다.
formation_id/room_id/recipe_id/parent_id/host_connection_id/local→world transform이 각 셀과 인스턴스로 추적돼야 한다.
큰 공간 하나를 4×4 또는12×8로 나누어도 독립 공간 수를 늘리지 않는다. 실제 서로 다른 방은 서로 다른 ID다.
재구성 검사는 pattern instances+catalog만으로 world cells를 복원하여 직접 recipe 결과와 exact 비교한다.
4×4/12×8/48×32 경계를 넘는 방·연결 fixture를 포함한다. 접합 틈/중복/소유 충돌을 검사한다.

## I05. 연결된 채움 선택과 밀도 보고

고정 기존 plan을 만든 뒤 일반 공간을 붙인다. 큰 장소 후보 선택을 다시 돌려 core/기존 장소를 움직이지 않는다.
기존 actionless optional 통로에서 시작해 안전한 방 또는 부모 방의 닫힌 측면을 후보로 확장한다.
부모 방의 같은 root host_connection을 전파하되 geometry 검사를 생략하는 면책 ID로 쓰지 않는다.
배치 순서는 덜 채워진156×104 검토 구역 우선, INFILL_PROFILE의 hash/좌표/recipe tie-break로 결정적이다.
총 목표256개/최소128개/최대384개, 최소12구역에 각6개 이상, 새 고유 소유셀 최소24,576개를 첫 pass의 운영 기준으로 둔다.
모든16구역의 실제 결과를 출력한다. 구역 수는 방 중심의 half-open 좌표로 센다. 한 방을 여러 구역 수로 중복 세지 않는다.
이 수치는 새 구현의 충분한 공간 변화를 확인하는 기준이며 사용자 승인 최종 밀도 또는101개 장소 강제가 아니다.
후보 고갈 시 목표 미달과 거절 원인을 기록한다. 최소 조건에 못 미치면 구현/후보 선택을 고치고, JSON 최소값을 낮추지 않는다.
최소를 넘었으나256 목표 미달이면 안전한 후보를 끝까지 평가한 근거와 부족 수를 기록한다. 성공 후 남은 잔여를 숨기지 않는다.
최소값을 채운 직후 중단하여 미검사 잔여를 후보 없음으로 보고하지 않는다. 상한/정상 고갈/목표 충족 중 종료 이유를 기록한다.
부모 연쇄 깊이 상한12, 외부 연결24칸 상한을 적용하고 필요하면 다른 기존 통로에서 확장한다.
후보 경로 탐색은 작은 지역에 제한한다. 전체 gate solver를 재설계하거나 모든 방마다 전체 FSM product를 재실행하지 않는다.
빠른 셀/접촉/개구 검사를 먼저 하고 완성 후보에 기존 production 검증을 수행한다. 실패 원인과 후보 ID를 기록한다.
repeat/default의 기존 SV5_07 family/formation/선택 trace와 근접 쌍은 보존한다. 새 방은 기존 ORDINARY_ROOM_A~D 계열로 집계한다.
이 family는 원래부터 일반 연결 공간의 감점 면제다. 실제 큰 동굴을 ordinary로 개명하여 반복 억제를 회피하지 않는다.
다양성와 밀도는 별도다. 기존 선택 trace를 지우거나 새 infill 방을 기존14개 요청의 선택 결과로 끼워 넣지 않는다.

## I06. 부모 연결과 실제 통행 공간

이번 infill은 각각 하나의 부모로 들어가고 되돌아오는 actionless 구조다. 자식 방 연쇄를 허용한다.
신규 infill 트리가 서로 다른 기존 route로 연결되면 이번에는 거절한다. 새 지역 간 재합류는 SV5_09의 책임이다.
부모 연결은 포트 ID뿐 아니라 정확한 world 개구 셀/공유 face/접근 방향/지지면/왕복 witness를 가진다.
한 셀짜리 필드가 있다는 이유로 접속됐다고 간주하지 않는다. 2칸 개구, 내부 깊은 지점, 부모 네트워크까지 양방향으로 확인한다.
기본 구현은 새 FSM action/node를 만들지 않는 기존 optional corridor의 부속 공간이다.
기존 연결의 centerline/끝점/조건을 보존하고, 그 host의 aperture에 실제 새 AIR를 union한다. envelope도 해당 AIR/보호 여유를 담는다.
같은 host의 union은 전역 좌표 통행에 모두 드러나야 한다. 내부 AIR를 metadata에만 넣고 physical graph에서 제외하면 실패다.
부모-자식 연결 데이터는 infill 모델에 별도 보유한다. 기존 core graph의 edge 개수를 방 개수만큼 가짜로 늘리지 않는다.
예외는 I03의 기존6개 ordinary 내부다. 원래 같은 place의 IN/OUT에 바인딩된 incident optional route들을 명시하여 내부 AIR를 연결한다.
이것은 새 지역 연결이 아니라 기존 방의 입력/출력 구현이며, 해당 contact 전수/기존 guard product를 다시 검증한다. 신규 트리의 두 번째 root 접속 예외로 확대하지 않는다.
이 방식은 상태 없는 방/굴만 적용한다. 후속 action·다른 상태 조건을 가진 공간을 몰래 같은 host로 합치지 않는다.
새 AIR가 다른 연결과 동일 cell/cardinal face로 닿으면 원래 접촉 전수검사에 포함한다. host가 같아도 닫힌 문 우회 검사를 생략하지 않는다.
모든 추가 AIR가 union됐는지 CSV에서 대조하고, 새 SOLID와 기존/추가 통행 셀이 겹치지 않는지 검사한다.
방 하나마다 별도 actionless 연결을 만드는 구현을 선택할 수도 있으나, 실제 endpoint/flow와 모든 새 상태를 현행 production product에 포함해야 한다.
성능 때문에 새 연결/방의 일부를 검증에서 제외하거나 대표 상태만 검증하지 않는다.

## I07. 기존 코드와 생산 진입점

새 public Sv5SpaceGraphPlanner.PlanWithInfill(core,seed,profile=null,diversity=null,infill=null)을 마련한다.
내부에서 기존 Plan으로 기준선을 만들고 이번 모델을 연결한다. 기본 호출에서 infill ON이며 시험/그림 전용 함수로만 두지 않는다.
기존 Plan의 호출/결과와 생성자들은 그대로 유지해 기존82개 회귀 의미와 mutation fixture를 보존한다.
SV5_09+와41이 사용할 생산 진입점/typed 반환을 활성 문서와 protocol에 명시한다. 새 plan 안에 실제 cells/patterns/attachment/validation/digest가 있어야 한다.
Sv5SpaceGraphPlan에 infill evidence를 추가할 때 기준선 plan에는 NONE이다. NONE의 기존 digest token/CSV 의미는 바꾸지 않는다.
infill ON은 정책·실제 셀·패턴·개구·연결·정적 결과를 canonical digest에 포함한다. CLR GetHashCode/UnityEngine.Random을 사용하지 않는다.
기존82개 시험은 새 출력 위치로만 옮긴다. 새 API의 필수 호출은 T06~T12가 검증한다.
생산 코드 소유는 Task allowlist의 기존3개/신규3개 파일이다. 추가 소규모 타입은 해당 신규 파일 안에 둔다.
Sv5SpaceDiversity/GateGeometry/PhysicalMovement/Product/StateProjection/RMAP13은 불변으로 재사용한다.
Planner의 BuildRouteContactCells/BuildReservations/Validate 등 현재 private 접점은 동일 로직을 호출하도록 제한적으로 추출/공개해도 된다.
기존 gate repair 알고리즘·gate 조건·검증 assertion을 바꾸지 않는다. 원격 차단 문제를 다시 만드는 전역 재배치도 금지다.
새 union geometry에 Project/FindContactCoverageErrors/FindConnectionErrors/FindReservationConflicts/FindGateErrors/
FindStateErrors/PhysicalMovement/Product를 호출한다. 원래 PASS 결과를 새 geometry에 복사하지 않는다.
새 gate projection 결과가 기존 gate의 geometry/predicate에서 달라지면 성공으로 덮지 않고 새 infill 후보/attachment를 수정한다.

## I08. 정적 이동·보호·6×6 검사

로컬 STATIC_SCREEN은 AIR flood와 별개다. route상의 발바닥 구간/바닥 SOLID/머리 여유/다음 지지면/+1 step/되돌아오기 evidence를 계산한다.
기존 Player 몸체0.4×0.8의 AABB sweep와 기존 높이 한계를 보수적으로 사용한다. 임의 속도/중력/점프 높이를 도입하지 않는다.
실제 physics 궤적을 이 단계에서 실측하지 않았다면 PLAYER_VERIFIED로 표시하지 않는다.
최소2칸 standing headroom, +1마다 지지면, 연속+1 상승과 역방향 하강/부모 복귀를 검증한다.
높은 계단 끝/문 입구/외부 connector의 얇은 천장·반환 경로 막힘을 검출한다. 단순한 AIR 연결만으로 완주라고 하지 않는다.
사용한 recipe 전체와 실제 배치 전부에 정적 검사를 수행한다. 샘플 하나만 검사하고 나머지를 통과로 복사하지 않는다.
기존 전역 cardinal 이동/product는 새 AIR 전체를 포함한 보수적인 우회 검사다. 이것이 중력 기반 Player 증명을 대신하지 않는다.
정본/확정 SOLID+새 SOLID의 합집합으로 전체254,409개의6×6 창을 확인한다. 모두 알려진 SOLID인36셀이면 실패다.
UNKNOWN을 SOLID/AIR로 간주하지 않는다. fully-known/mixed-pending 창 수와 새 위반 좌표를 함께 기록한다.
국소/합집합 검사0을 아직 미조립인 전체 월드의 최종6×6 PASS로 이름 붙이지 않는다. 최종 모든셀 검사는41 이후 책임이다.
새 고체의 두께/접합 예외 좌표를 내보낸다. 반복1셀 AIR 핀홀로6×6을 피해도 실패다.
contact/local gate/full physical-FSM product는 default/repeat ON 각각6 자원순서와 도달 legal state 전체를 검사한다.
열려야 하는 진행/복귀 차단0, 닫힌 제작/봉인/보스 우회0이어야 한다. source digest/새 plan digest/검증 digest를 연결한다.

## I09. 필수 검증

|ID|실제 입력과 확인|
|---|---|
|T01|네 recipe와 mirror의 모든 셀/2칸 개구/바닥/+1 계단/국소 두께, 6×6 SOLID 음성 fixture 거부|
|T02|16셀+write mask 패턴 재구성 exact 일치, 4×4/12×8 경계, 다른 소유 셀 보존, 중복 충돌 거부|
|T03|같은 seed/profile/입력 열거 역순/global Random 변경에도 같은 placement/cells/digest; 후보 고갈 기록과 최소 미달 FAIL|
|T04|AIR는 이어져도 +3벽/지지면 없음/낮은 천장/닫힌 출입구/되돌아오지 못함이면 STATIC_SCREEN 실패|
|T05|새 SOLID의 기존 passage/clearance/protected AIR 침범, Type0 침범, 서로 다른 host 우회 접촉을 production 검사로 거부|
|T06|실제 PlanWithInfill 기본 호출로07 default ON 위에 최소개수/면적/분포를 만족하는128개 이상 생성; 모든recipe 사용 및 기존6개 ordinary 내부 완성|
|T07|고정07 repeat ON도 동일 완료 조건, 기존24place의 좌표·크기·종류와3동굴 근접쌍2 유지|
|T08|두 ON의 모든 새 AIR가 physical movement에 포함, 새 방/부모/자식의 양방향 개구·로컬 지지·왕복 witness 확인|
|T09|두 ON의 기존 contact/gate/6 resource order×legal FSM state product PASS; 문 제거/우회 AIR 주입 반례는 실패|
|T10|SOURCE_LOCK 기준 모든 기존 place/core/gate와 centerline 의미 보존, 두 OFF가 고정07 digest를 재현|
|T11|새 plan의 CSV/JSON/패턴/그림/영역/지표 exact 일치; 재생성 동일 바이트; 셀/개구/profile 변이는 digest/검증에 반영|
|T12|기존82개+새 focused 실제 발견/실행, failed/skipped0; 이전 GENERATED 불변; old obligations PENDING 오표기 제거; 새 _work 정리|

테스트 개수를 목표로 만들지 않는다. 위 책임을 중복 없는 fixture로 검증한다. 작은 반례를 먼저, 큰 integration은 두 고정 사례로 제한한다.
안전한 후보 선택/정적 검사는 생산 함수를 사용한다. 별도 Python 예시의 PASS로 Unity 구현 시험을 대신하지 않는다.
Plan이나 product를 캐시한다면 전체 immutable 입력 digest가 같을 때만 재사용한다. 다른 infill AIR/SOLID를 같은 캐시로 통과시키지 않는다.
실패 원인이 일반 코드/시험이면 허용 범위에서 수정·재시험한다. hash/상태/권한 mismatch와 구분한다.

## I10. 실제 검토 산출물

모든 최종 출력은 GENERATED/SV5_08 한 곳에 둔다. default/repeat 각각의 ON plan을 기존 WriteAll로 한 벌씩 출력한다.

|파일|내용|
|---|---|
|BINDING.json|패키지/선행commit/actual READ·WRITE/API/비소유 dirty/실행명령/XML·시험소스 SHA/정리 기록|
|default/,repeat/|각 ON plan의 기존 표준 export+아래 infill 전용 파일. OFF 거대 export를 복제하지 않음|
|각 case/infill.json|정책/기준선digest/plan digest, 방·부모·host·개구·실제 cells 및 readiness/검증/목표 미달 근거|
|각 case/infill_cells.csv|world x,y/base/owner/recipe/역할, 기존셀과 같은 의미 공유 구분, pattern origin/index|
|각 case/infill_patterns.csv|stable pattern ID,16셀 고정순서,write mask,semantic hash|
|각 case/infill_instances.csv|pattern ID/world origin/room ID/formation ID/mirror; 겹침 합성 순서에 독립|
|각 case/infill_rooms.csv|room/recipe/bounds/부모/root host/깊이/개구/실제 AIR·SOLID 수/보상없음/검증|
|각 case/infill_links.csv|부모/자식/host,개구cells/faces,ordered connector/지지면/접근·복귀 witness/검증|
|각 case/infill_windows.csv|48×32의13×13=169창, before/after 실제소유·예약·pending counts,새 방 수/말단/접근 실패|
|각 case/infill_validation.json|별개 항목 CELLS/PATTERNS/STATIC_SCREEN/CONTACT/PHYSICAL_PRODUCT/COMPOSED/PLAYER와 근거|
|comparison.json|두 기준선/두ON의 개수·실제셀·16구역·미계획잔여·기존보존 비교, goal/shortfall 및 종료 이유|
|preview/before_after.svg|default 전체624×416 OFF/ON 좌우. 실제 infill SOLID/AIR와 미조립 배경을 구분|
|preview/repeat_before_after.svg|repeat 같은 전체 비교|
|preview/detail.svg|실제 방·계단·개구·연결이 함께 보이는 변경 구역;1칸격자/4×4경계/12×8경계/지지면|
|preview/A1~D4.svg|default를156×104씩 나눈16개 실제 타일 확대. 방 ID/개구/미계획 영역 표시|
|preview/index.html|전체·확대·지표 연결. 셀 격자와 역할 표시를 읽을 수 있게 구성|
|focused_results.xml|최종 실제 SV5 focused 실행 한 개|

NONE 기준선과 ON의 readiness는 분리한다. 전체 ComposedGeometryReady/PlayerVerified는 false다.
실제 infill 타일은 검은 SOLID/밝은 AIR, 큰 미제작 내부/미소유 잔여는 다른 색으로 표시해 확정 지형으로 오해하지 않게 한다.
큰 사각형 색칠이나 추상 노드 그림으로 위 타일 확대를 대체하지 않는다. 모든 그림은 같은 instance/cells를 읽는다.
일반 통로 연결/방 내부를 모두 가느다란 중심선으로만 그리지 않는다. 1~2칸 통로의 실제 폭과 바닥/벽이 보여야 한다.
기존 exporter의 SV5_07_DISTRIBUTION PENDING은 실제 diversity 연결 상태에 따라 IMPLEMENTED/APPLIED로 고친다.
SV5_08 행은 infill ON 검증 통과일 때 LOCAL_CELLS_STATIC_SCREEN, NONE이면 PENDING으로 출력한다.
SV5_09나 전체 COMPOSED/PLAYER 완료를 표시하지 않는다. 역사적 GENERATED 파일을 고치는 작업이 아니다.
기존 SG06_F5 같은 희소성 obligation은 실제 현재 방/연결 수와 08 밀도·09 loop 잔여 책임을 구분한다. 과거22라는 숫자만 재사용하지 않는다.

## I11. 원본·시험·파일 정리

동봉7파일은 immutable이다. SOURCE_LOCK raw와 commit blob은 각각의 실제 바이트를 검사하며 실행 중 EOL 정규화하지 않는다.
이전07 패키지가 pinned했던 파일 중 이번까지 불변인 항목을 상속하고, 이후 실제 변경된 것은 최신07 Review SHA로 갱신한 새08 계약이다.
STAGE는 read-only검사와 INBOX 생성만 담당한다. Apply/Finalize는 현행 native 절차로 한 Task만 수행한다.
기존9개 시험의 모든 쓰기 목적지를 조사하여 GENERATED/SV5_08/_work/legacy_exports/<fixture>/로 먼저 옮긴다.
07의 V11 WriteDiversityComparison도 옛GENERATED/SV5_07에 쓰지 않게 격리한다. V04/V12의 과거 입력 읽기는 그대로 둔다.
9개 파일의 assertion/이름/분기/fixture 의미를 건드리지 않는다. 테스트의 새 파일 출력만 이번 소유 _work로 보낸다.
중간 XML/진단도 _work, 최종 XML 하나만 focused_results.xml이다. PASS 준비 후 이번 실행 생성물만 정리한다.
정리 전 경로가 GENERATED/SV5_08/_work 아래인지, 실행 전 없었거나 이번 생성으로 기록됐는지 확인한다.
무관한 파일 삭제나 git restore/checkout/reset/clean으로 원본을 맞추지 않는다. 과거4중 legacy export를 다시 복제하지 않는다.
프로젝트 root에 CHECK/FILES/VERIFY/명령 MD를 만들지 않는다. helper는 INPUTS/SV5_08/tools에 모으고 BINDING에 기록한다.
최종 _work가 남으면 그 사유/경로를 보고한다. 보존/정리 조건이 충족되기 전 Finalize하지 않는다.

## I12. 종료와 다음 작업 인계

Result에 실제 시험 명령/Unity 버전/기존82개 이름보존/추가시험/발견·실행·성공·실패·skip와 XML raw·blob SHA를 기록한다.
구현된 API/인수/호출 예시/새 데이터 소비 위치/일반 공간 수·유효 면적·분포/보존 값/미계획잔여를 기록한다.
두 기준선 중 하나만 통과한 상태를 PASS로 합치지 않는다. 정책 수치 하향/실패seed교체로 성공 처리하지 않는다.
정상 PASS 뒤 native Finalize하고 이번 소유 파일만 atomic commit한다. 최종290=251 COMPLETE/0 CURRENT/39 LOCKED, Current NONE이다.
SV5_09_LOOPS는 LOCKED다. User가 다음 task를 요청할 때 이번 실제 Result/Finalize/Review에서 새 INBOX를 바인딩한다.
commit 후 SV5_08_REVIEW.zip을 만든다. manifest에는 각파일 path/role/raw bytes·SHA/git blob bytes·SHA/OID와 실제commit/parent를 분리 기록한다.
manifest/ZIP 자신은 자신의 해시 목록에서 제외한다. 기존 legacy exports/다른ZIP/Library/Temp/_work/무관한 dirty는 넣지 않는다.
Result에 자신의 최종commit을 강제로 자기참조시키지 않는다. commit/parent/ZIP SHA/경로는 최종 콘솔 보고와 manifest에 남긴다.
09+, RMAP18/19, VIS, 전체무필터시험, PlayMode/build/Scene Bake/Player/push를 시작하지 않는다.
