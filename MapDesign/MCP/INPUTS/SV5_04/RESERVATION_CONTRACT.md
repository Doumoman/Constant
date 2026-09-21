# SV5_04 핵심 구역·접근·복귀 예약 계약

범위: 공간 배치보다 먼저 사용할 core reservation 기능과 소비자 검사를 구현한다.
현재 Task는 상태 진행 규칙 재설계(SV5_05), 랜덤 공간 그래프(SV5_06), 전체 합성/Scene Bake(SV5_41/43)를 실행하지 않는다.
아래 자료/판정 이름은 요구 의미다. 실제 C# 타입·필드·함수는 현지 SV5_03 연결표를 읽어 기존 표현에 바인딩한다.

## C01. 같은 정본과 좌표

1×1 타일, 4×4 패턴, 12×8 마이크로청크, 624×416 월드와 기존 origin/axis/local↔world 규칙을 따른다.
실제 RMAP12 definition/seed/content/generator version, RMAP13 graph, RMAP14 ownership, RMAP15 special plan의 동일 입력 계보를 사용한다.
대표 입력은 SV5_03 CORE_BINDINGS/SOURCE_SNAPSHOT이 가리킨 실제 정본이다. 과거 보고의 seed 1304를 임의 새 정본으로 덮어쓰지 않는다.
기존 API 결과와 generated CSV는 같은 digest/버전인지 확인한다. 서로 다른 시드/버전의 자료를 섞지 않는다.
PDF/PNG의 장소 번호나 좌표를 정본 ID/보호 셀에 대신 사용하지 않는다.

## C02. 8개 물리 site와 별도 논리 역할

Start, MooncoreOre, CondensedCoefficientSap, DeepStarYeast, Village, Forge, 공유 SealBoss, Exit를 현재 정본 그대로 연결한다.
나열한 문자열은 역할명이다. 실제 site_id는 CSV/API에서 읽고 새 문자열로 만들어 바꾸지 않는다.
Village는 별도 물리 site이며 RMAP15 보고상 독립 graph reservation을 소비하지 않는다. 이 차이를 강제로 맞추지 않는다.
Seal/Boss의 하나인 물리 footprint와 별개 논리 노드/상태/슬롯을 보존한다.
site·slot stable ID, footprint/local/world 좌표, patch owner, port 방향과 requirement는 이번 작업에서 이동/변경하지 않는다.
SV5_03 대표 정본의 물리 site 8개·고정/보호 셀 2,432개를 정확히 재현한다. 향후 다른 입력의 모든 좌표나 셀 수를 고정하는 규칙은 아니다.

## C03. 기본 셀과 보호 의미

RMAP15의 실제 S/A/O 기본 셀과 FixedSolid/ProtectedAir/port/slot 의미를 그대로 소비한다.
기존 보호 소비자 RmapSpecialReservationPlan.EvaluateTerrainCells는 현지 시그니처를 확인해 재사용한다. 같은 역할의 별도 진실 원본을 만들지 않는다.
core reservation 결과에는 실제 원본의 cell/owner/role을 연결하고, 일반 지형이 바꿀 수 없는 셀을 조회할 수 있어야 한다.
같은 셀의 여러 예약 이유는 원본 ID와 함께 보존한다. 호환되는 같은 보호를 중복으로 세거나 잘못된 충돌로 판정하지 않는다.
core S/A/O를 다른 값으로 바꾸거나 protected air를 채우거나 slot 지지/머리 공간을 침범하는 후보는 거부한다.
검사는 후보를 검토하는 단계에서 수행한다. 원본을 먼저 수정한 뒤 복구하는 흐름을 쓰지 않는다.
거부 결과에는 실패 이유와 좌표, 기존 owner/site/route ID를 담는다. 대체 core 위치 선택이나 silent carve를 수행하지 않는다.

## C04. 출입구와 복귀 통로를 실제 셀로 예약

port의 IN/OUT/BOTH·graph endpoint·접근 방향·진입 높이·기존 조건을 정본에서 가져온다.
핵심 역할별로 현재 논리 graph가 요구하는 접근과 복귀를 연결한다. 상태상 일방향인 간선을 무조건 양방향으로 바꾸지 않는다.
RMAP16에 있는 실제 정적 route spine/clearance/support 자료 또는 해당 생산 API를 SV5_03 schema/binding으로 찾는다.
자료가 같은 정본과 맞고 이번 예약 의미를 충족하면 재사용한다. 구형 최종 지형 전체를 SV5 월드 결과로 복사하지 않는다.
기존 spine이 몸체 여유/지지/복귀 정보를 주지 않으면 실제 기존 경로 자료에서 필요한 예약을 계산하는 최소 adapter를 구현한다.
임의의 start/end 쌍과 직선 한 줄만 적어 복귀 경로 완료로 처리하지 않는다.
경로는 출발 port부터 목표 port까지 연속된 실제 좌표와 순서, 필요한 통행 여유·지지 셀을 가져야 한다.
각 접근/복귀 요구를 어느 route ID/방향/조건이 충족하는지 기록한다. 같은 경로를 왕복할 수 있으면 별도 두 통로를 강제하지 않는다.
외부 경로가 새 랜덤 장소 전체 배치에 의존해 아직 완성될 수 없다면 미해결 요구를 기록하고 충족으로 표시하지 않는다.
필수 접근/복귀 예약을 만들 수 없으면 원인/최초 실패 지점을 보고한다. 이 단계에서 SV5_05/06 전체를 선행 구현하지 않는다.

## C05. 여유·지지·조건의 구분

통행 여유는 실제 Player 0.4×0.8 몸체와 기존 foot pivot, port/route contract를 기준으로 확보한다.
무아이템 일반 +1칸, Jump+Grab 최대 +2칸의 기존 한계를 따른다. +2칸에는 실제 SOLID Grab 접점/당겨 오르기 여유의 정적 근거가 필요하다.
ONE_WAY의 옆면이나 배경을 Grab 지지로 취급하지 않는다. 가로 3~4칸을 실물 검증 없이 확정 성공으로 사용하지 않는다.
경로의 머리/몸체 통행 여유와 S/O 지지물은 별도 의미로 예약한다. 통로를 보호한다며 바닥까지 AIR로 지우지 않는다.
지지와 통행 여유가 같은 좌표에서 모순되면 양쪽 이유/owner를 기록해 실패 처리한다.
비고정 외부 예약끼리의 호환되는 중복은 합칠 수 있지만 core의 보호/소유/상태 조건을 우선 보존한다.
예약 경로가 Type0 봉쇄 공간이나 다른 고정 site 내부를 무조건 관통하지 않게 한다.
이 검사는 정적 예약 적합성이다. 실제 Player 이동 성공은 SV5_20/44의 별도 증거를 요구한다.

## C06. Seal/Boss 상태 geometry 보존

기존 sealed/open 셀 쌍과 상태 조건/소유를 그대로 기록한다. 닫힌 gate를 일반 통행 AIR로 평탄화하지 않는다.
조건부 통로의 닫힘 상태를 일반 지형이 덮지 못하게 하고 열린 상태의 통행 여유도 막지 못하게 한다.
일반 후보는 기존 state geometry의 owner가 아니다. 동적 변경 권한이나 새로운 gate 전환 API를 임의로 부여하지 않는다.
각 상태에서 해당 조건을 유지하는지만 검사하고 자원 획득/제작/보스 runtime을 구현하지 않는다.

## C07. 소비자 연결과 결정성

입력 plan이 같으면 같은 예약과 stable digest를 생성한다. 입력 열거 순서의 차이는 의미 없는 결과 변경을 만들지 않는다.
새 전역 RNG를 만들거나 기존 SpecialReservation stream을 추가로 소모해 원본 site 배치를 바꾸지 않는다.
입력 좌표의 범위/정수성, 중복 ID, 다른 plan digest/seed 혼합, 보호 충돌을 명시적으로 진단한다.
실제 후속 지형 후보가 호출할 수 있는 조회/검사 접점을 제공하고 focused 시험에서 허용/거부 후보로 호출한다.
설정에 사용되지 않는 bool이나 문서만 추가해 예약 소비 검사를 구현했다고 보고하지 않는다.
SV5_06/41에서 이 접점을 호출할 책임과 반환 결과를 active 요약 문서에 기록한다. 전체 생성기의 교체는 이번 범위가 아니다.

## C08. 같은 결과에서 출력

검사한 동일 plan 객체에서 site/cell/access/route/state geometry/manifest를 export한다. 별도 손 작성 예약 CSV를 정본으로 사용하지 않는다.
출력은 기존 code/data 정본을 가리키는 파생 자료다. RMAP15/16의 원본 export 파일과 이전 Result를 덮지 않는다.
원본 fixed/protected 셀 집합의 동일성과 추가 접근/복귀 예약 셀 수를 별도로 기록한다.
일반 지형을 모든 미예약 셀에 자동 채우거나 이번 결과를 전체 월드 Bake 완료로 발표하지 않는다.
