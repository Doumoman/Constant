# SV5_06_FIX01 - 예약 셀·접촉·차단 경계의 일치

검토 원본: SV5_06_REVIEW.zip, 137개 manifest 파일 SHA 일치, 제출된 focused31/31 Passed.
이 환경에서는 Unity를 실행하지 않았다. REVIEW_FINDINGS.json의 수치는 첨부 좌표를 독립 분석한 결과다.
이 보완은 SG06-F1~F4의 현재 검사 누락을 해결한다. F5의 공간 구성은 계속 미완으로 기록한다.
624×416, Player의 일반+1/Jump+Grab최대+2, 정본core/필수진행/기존Task순서는 유지한다.

## C01. 실제 실패 사례부터 고정

기존06의contact_checks5347쌍은 기존호출입력을독립재현하면같다. 열거기 자체의예전 공통route 오류는고쳐졌다.
하지만 새통로의 실제exported Envelope를 더하면10093쌍, 기존과의차이는4746쌍이다.
대표 누락: (175,77)-(176,77), SV5_OPTIONAL_07/08. contact 쌍의수이지4746개 exploit을뜻하지않는다.
새CorridorClearance와FixedSolid가겹친예약72행/47좌표, 새통로예약과기존route SOLID support겹침113행도고정한다.
대표충돌: (492,280), 기존Village S/FixedSolid와SV5_OPTIONAL_TO_VILLAGE CLEARANCE_RESERVED.
CLEARANCE_CONFLICTS.json에모든72행을제공한다. 기존06 JSON/CSV는읽기전용반례입력으로사용한다.
위수치는수정후계획의정답개수가아니다. 실패원본을삭제하거나검사목록에서빼서통과시키지않는다.

## C02. 예약을 단일 정본으로 만들고 전체 폭으로 배치

centerline, footprint, clearance, support, port aperture, conditional barrier를명확한의미로구분한다.
생성/후보심사/접촉/상태투영/export가같은accepted reservation model과digest를사용한다.
BFS중심선만장애물검사하고주변한칸을나중에늘리는현재방식을수정한다.
후보각step의전체필수clearance와port개구/안팎접점을검사해유효한경로만채택한다.
2칸core개구에3칸원형Envelope를강제한뒤고체를잘라내는방식은안된다. 실제port폭과통로단면전이를명시적으로계획한다.
전체폭에필요한공간이없으면reroute/단면전이재설계/후보거부를한다. required clearance를무조건삭제해PASS시키지않는다.
정본core의S/A/O, slot, state barrier, identity는변경하지않는다. 기존core bytes불변과새예약의semantic호환을둘다검사한다.
현재06소유의새route geometry/adapter는수정가능하다. 기존04/RMAP16 routeCSV와core.Source객체를변경하지않는다.
옛route support를교체해야하면old route ID -> new active geometry와support/clearance를명시해일관되게교체한다.
새geometry에옛예약/옛contactordinal을섞거나출력과다른보호mask로검사하지않는다.
합친예약집합을별도검증해FixedSolid와RequiredClearance/Passage의동시소유등의충돌을좌표/소유/의미로거부한다.
정상공유AIR,동일support공유,실제상태가다른OPEN/SEALED는허용조건을구별한다.

## C03. 전체 접촉과 실제 상태 투영

accepted Passage와Clearance를모두공용EnumerateContactPairs에전달한다. kind/route별coverage도기록한다.
별도독립coverage검사로shared pair와cardinal-face pair가누락되지않는지대조한다.
routeA/B정렬후에도각route가어느좌표/방향을점유했는지잃지않는다. 공통route/같은조건때문에생략하지않는다.
접촉위치는active geometry로찾는다. 옛core.RouteCells.Ordinal을새geometry의ordinal로간주하지않는다.
공유셀/인접face/실제port의연결을반영한다. 같은좌표의여러pair를문자열ID순의가상일방향사슬로만연결하지않는다.
일반양방향통로는양방향으로,일방향은실제설계근거가있을때만투영한다. Flow를무시하거나임의ONE_WAY로안전성을만들지않는다.
현재정본RMAP13 action/CanTraverse/FSM을재사용한다. 일반node에새resource/Forge/Seal/Boss action을만들지않는다.
실제accepted connector와접촉으로투영한전체집합에서6순서goal과모든관련reachable state의복귀/goal역도달을검사한다.
기존baseline11edge를무조건덧붙여실제삭제된경로를숨기지않는다. unknownnode/port/방향/참조는검사전명시거부한다.
실패는실제시작prefix와before/after/action/좌표를내보낸다. 성공목표하나만찾았다고검사를중단하지않는다.
미평가접촉을checked=true로표시하지않는다. 상태안전성,접촉coverage,계획차단,composed geometry,Player증거를분리한다.

## C04. 점과 조건 문자열을 실제 계획 차단 경계로 바꾸기

기존103gate기록은26개좌표에있다. 많은record가같은차단영역을가리킬수있으므로숫자만으로결함/성공을판정하지않는다.
현재Sv5SpaceGate는한World점과predicate/문자열state만있다. 이것을실제통로폭차단검사로소비하지않는다.
gate는영역/경계ID, 전체blocking cell또는차단face집합, 양쪽anchor, 허용방향, 실제predicate필드, OPEN/SEALED상태예약을가져야한다.
인접gate의같은물리경계를일관되게합치거나공유한다. 서로다른조건을근거없이OR/AND하지않는다.
상태별허용공간에서gate를닫으면해당전이가정말차단되는지,열면해당정상전이와복귀가남는지검사한다.
core/통로의전체폭과주변clearance를포함한다. centerline의한셀만막고옆으로돌수있으면계획gate검사실패다.
segment에원래guard를반복하는것만으로물리적분리를가정하지않는다. 해당전이guard는실제계획barrier/port근거를가져야한다.
근거가없는접촉은재배치/분리/차단설계로해결하거나후보를거부한다. PENDING을accepted-ready로올리지않는다.
reserved gate는runtime gate구현이아니다. ComposedGeometryReady=false,PlayerVerified=false를유지한다.
이검토는확정Player exploit을주장하지않는다. 현재선언된계획검사가미충족이라는보완이다.

## C05. 의미를 묶는 digest와 증거

schema version을갱신하고place/port boundary set/anchor/flow/source-node,status,
connection kind/from-to place/port/flow/direction/전체centerline/envelope/selection,
gate의전체OPEN/SEALED형상과predicate, reservation소유/의미,contact방향/coverage/판정,proof identity를묶는다.
구분자escaping또는lengthprefix를사용한다. 집합순서는무관하고경로순서는보존한다.
유효한의미변경은digest를바꾸며reject후원본/채택집합은불변이다. export가검증한객체와동일한지독립대조한다.

## C06. 실제 집중 검증

| 검사 | 필수 사례 |
|---|---|
| N01 coverage | 기존5347/추가4746의반례재현; 새모델모든clearance에대해독립pair검사와완전일치 |
| N02 reservation | 기존72행/47셀충돌탐지; core고체/route support의위반을거부하고수정된positive를허용 |
| N03 port/width | 잘못된endpoint/미지port/잘못된flow/좁은통로/가짜단면축소를거부; 유효한port전이는통과 |
| N04 projection | 실제connector하나를삭제하면실패; 중간무조건접촉/반대방향우회는실제검사에서실패 |
| N05 gates | 옆clearance로돌수있는barrier fixture 실패; 전체폭차단과정상OPEN연결/복귀 positive 통과 |
| N06 state | 6순서/정상action/normal return유지, 도달가능복귀불능가지거부, 정상왕복말단허용 |
| N07 semantics | port경계/Flow/Envelope/gate OPEN-SEALED변경시digest변경; 집합순서변경은같음 |
| N08 evidence | 기존04/05/FIX01/06inputs/generated/Result불변; 새JSON/CSV/그림/검사결과일치 |

위는책임이며함수이름N01~08만만드는조건이아니다. negative fixture를실제production validator/public API에넣는다.
가능하면수정전N01/N02실패를별도EXPECTED_PRE_FIX_FAILURE로보존하고최종pass수에섞지않는다.
SV5_06직접test와05/FIX01핵심회귀를포함해필터있는EditMode만실행한다. RMAP13코드를바꾸면직접test도포함한다.
기존SV5_06/05/FIX01test export경로를이번GENERATED/SV5_06_FIX01/legacy_exports의각각다른폴더로먼저격리한다.
기존문제를드러내는assertion을삭제/완화/skip하지않는다. 잘못된readiness assertion만이유를기록해정정한다.
실제Unity버전·필터·전체명령·발견/실행/통과/실패/스킵수·XML의작업트리SHA를기록한다.
Git blob SHA가다르면별도역할로기록한다. 파일의줄바꿈/expected SHA를현지에서자동변환하지않는다.
무필터전체회귀/PlayMode/build/Scene Bake/Player는이번범위가아니다.

## C07. 출력과 후속 작업

이번출력은MCP/GENERATED/SV5_06_FIX01 아래만쓴다.
BINDING.json에별도REG등록diff/SHA/전후상태/선행livecommit/실제Read-Write/소스before-after/원본보존을기록한다.
space_graph.json,reservation_cells.csv,contact_checks.csv,gate_geometry.json,state_proofs.json,
validation.json,focused_results.xml,obligations.csv를같은검증plan에서생성한다.
coverage에는입력kind별셀수/route수/독립pair총수/누락/중복을,충돌검사에는위반0또는정확한실패목록을남긴다.
04/06반례는원본/계획차이를명시하고개선전후대표좌표확대도를preview에출력한다. 전체/16확대도도현재plan에서재생성한다.
활성설명SV5/11_SPACE_GRAPH_FIX01.md를추가하고02_PROTOCOL_V5.md에는동봉suffix만정확히한번append한다.
SG06-F5(22장소,미설계91.4%,단일순환)는구조미완으로남긴다. 101/166을고정숫자로강요하지않는다.
07은Family분포,08은실제일반공간밀도,09는재합류확장을담당한다. 보완완료를전체공간의시각승인으로표시하지않는다.
기존예시의핵심좌표를복사하거나Player능력을바꾸지않는다. 이번수정에필요한통로재배치만현재plan에서수행한다.

## C08. 종료

N01~N08의현재소유검사가실제PASS이고accepted plan의미해결접촉/보호충돌/계획gate결함이없어야PASS다.
미실행된실제geometry/Player는false로남기되지금해야할검사를후속owner에밀어넣어PASS하지않는다.
작업중Status쓰기금지. PASS Result후정상Finalize, 이번등록/입력/구현/증거만atomiccommit, push없음.
후속SV5_07은자동시작하지않는다. 기존06COMPLETE/Task/Archive/Result는보존한다.
SV5_06_FIX01_REVIEW.zip에새Result/Task/Archive/Status/Master,실제소스/tests/meta/JSON/CSV/XML/preview,
읽기전용core adapter/RMAP13 및consumer interface를포함한다. selfzip/임시_work/무관한파일은포함하지않는다.
review manifest에실제commit/parent,모든파일path/rawSHA/bytes,focused XML의worktree/blob각SHA를기록한다.
