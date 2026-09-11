# SV5_08_FIX01 — 연결 길이 보완 계약

이 문서는 새 보완 Task의 구현 계약이다. 과거08 PASS를 취소하거나 과거 산출물을 고치지 않는다.
08의 기존 지형 생성 구조를 유지하고 사용자가 정한24셀 단위를 실제 생성/검증에 일치시킨다.

## R01. 확인된 문제와 범위

업로드08 Review의 실제 CSV를 다시 세면 default63/213개,repeat59/232개 연결이25~26셀이다.
현행 Sv5InfillLink.ExternalCenterline은 HostAccess.Concat(Path).Distinct()를 소유 cell로 필터링한다.
부모/자식 입구가 다른 소유라는 이유로 빠지면서 실제26셀 경로를24로 보고한다.
ValidatePayload와 후보 수용, 기존 T08도 이 필터링된 길이를 사용해 같은 오류를 놓친다.
LENGTH_EVIDENCE.json에는122건 좌표열과 기존95개 시험 fullname이 있다. CSV 기반 발견이며 Unity 시험 결과를 대신하지 않는다.
대표 SV5_INFILL_ROOM_001fbc81fb72bc0c659e는(571,399)→(588,391),26셀/25회 cardinal 이동이다.
이 고정 반례는 새 후보에서 같은 ID를 찾는 테스트로 만들지 않는다. 이전 좌표를 독립 fixture로 보존한다.

## R02. 하나의 정확한 계수 정의

새 ordinary connector만 대상으로 최대24개의 순서 있는 상하좌우 AIR 중심선 셀을 양 끝점 포함하여 센다.
폭2칸 전체 AIR 면적, 지지면 개수, Manhattan endpoint 거리만으로 대신하지 않는다.

- 자식 연결: immediate parent 경계의 실제 출구 Path[0]부터 child.Entry==Path[-1]까지 Path 전체.
- 루트 연결: 기존 host.Centerline의 실제 anchor HostAccess[0]부터 child.Entry까지.
- 루트는 HostAccess[-1]==Path[0]을 먼저 검사한 뒤 HostAccess + Path.Skip(1)로 정확히 한 접합점만 합친다.
- 자식은 HostAccess가 비어 있어야 한다. 부모 방 내부 Entry→출구의 별도 왕복 witness를 이번 짧은 외부 연결 길이에 합산하지 않는다.
- 모든 연속쌍은 |dx|+|dy|==1이다. 빈 배열/불일치 접합/대각선 미전개/원격 anchor/잘린 endpoint는 거부한다.
- +1 대각 계단 한 걸음은2회 cardinal 이동이다. 최초 셀은 한 번만 세므로 이 한 걸음만 있는 경로는3셀이다.
- 전체 Distinct/정렬/소유자 필터/중복 coordinate 축약으로 길이를 줄이지 않는다. 경로 재방문은 다시 센다.
- 기존6개 legacy ordinary 링크는 방 내부 screen witness이므로 NOT_APPLICABLE_LEGACY_INTERIOR로 명시한다.
  이 예외는 원래6개 ID/기존 bounds에만 적용한다. 새 방을 legacy로 바꿔 길이 검사를 회피하지 않는다.

생산 모델에 ConnectionCenterline/ConnectionCellCount처럼 명백한 authoritative 속성을 둔다.
ExternalCenterline/external_cell_count는 소유 셀 진단으로 보존해도 되지만 상한 판정에 사용하지 않는다.
모델 생성, 후보 수용, ValidatePayload, export, 정책 digest가 이 같은 정의를 사용해야 한다.
테스트/독립 CSV 검사는 같은 helper가 반환한 count만 비교하지 말고 입력 Path/HostAccess에서 직접 재구성한다.

## R03. 후보 길이 예산과 기존 생성 유지

현재 Enqueue의 endpoint 할인과 BuildAttempt의 endpointAllowance를 없애고 실제 전체 길이 예산을 준다.
Path의 지원점 수+sum(abs(deltaY))가 cardinal 셀 수다. HostAccess prefix 추가는 Count-1이다.
즉 최종 길이 = 1 + 가로 이동 수 + 모든 수직 이동 수 + (루트 HostAccess.Count-1).
루트 수직 neck prefix는1, side portal prefix는2, 자식 prefix는0이다. 최종 좌표열 검사가 항상 우선이다.
후보 빠른 하한 검사도 1+distance+abs(totalRise)+prefix<=24로 일치시킨다.
하한은 필요조건이다. 실제 지형 회피에서 오르내린 모든 높이 변화는 최종 길이에 다시 센다.
24 초과 연결은 실제 경로를 더 짧게 찾거나 후보 방을 더 가까이 선정한다. 끝점/막힌 셀을 생략하지 않는다.
기존 결정적 candidate 순서/보호/재시도 구조 안에서 구현한다. 새 전역 solver/portal/gate 설계를 추가하지 않는다.
정책 버전을 올리고 새 규칙을 ON profile/plan digest에 포함한다. 원래 INPUTS/SV5_08/PROFILE과 과거 export는 불변이다.
기존 API PlanWithInfill,4×4write mask,12×8청크,624×416월드,바닥/+1/2칸 여유를 유지한다.

## R04. 고정 integration과 기존 요구

seed1304,기존 default/repeat diversity profile 두 개를 그대로 사용한다. 실패 seed/프로필을 바꾸지 않는다.
07 baseline digest는 default10080e53c3d4c47f6e93f49118c7c648f07c1f3162742ce866b6ecd8a0be3e46,
repeat94c9f3a353e39984633a755af7842e87989e86dc97c3545ce87a4ac4da4397b7 그대로다.
08 새 방/새 연결은 교정된 후보 선택으로 위치/ID/개수가 달라질 수 있다.213/232 고정을 요구하지 않는다.
기존 기본 목표256/최소128/최대384,최소 신규소유24,576타일,16구역 중12구역에6방 이상은 완화하지 않는다.
네 recipe를 모두 사용하고 기존6개 ordinary를 같은 bounds/port로 완성한다. 후보 고갈/목표 미달은 정직하게 보고한다.
기존07 large/core/place/port/ordered centerline/gate predicate/geometry와8core2,432셀을 보존한다.
모든 새 AIR가 실제 coordinate movement에 포함되며 새 SOLID는 기존/새 통행과 겹치지 않는다.
모든 방/연결의 양방향 지원면/머리 여유/정적 왕복,4×4재구성,알려진SOLID6×6,contact/local gate와
두 사례 각각6자원순서×도달 legal FSM state 전체 physical product를 현재 셀로 다시 검증한다.
기존 PASS/기존 matrix를 새 plan에 복사하지 않는다. product 행 수는 현재 plan/state에 맞게 새로 산출한다.
ComposedGeometryReady=false,PlayerVerified=false. 실제 Unity Player/Scene bake는 후속 Task다.

## R05. 필요한 회귀 검증

|검증|완료 기준|
|---|---|
|F01 경계|독립 좌표 fixture에서23/24셀 허용,25/26셀 거부; production 최종 validator에서도 초과 거부|
|F02 루트/자식|수직 neck,side portal,자식 모두 양 끝점 포함; parent 내부 witness는 별개|
|F03 이동/접합|+1계단 전개,접합1회만 합침,원격/불일치/빈 배열/대각 미전개 거부; 재방문을 Distinct로 숨기지 않음|
|F04 이전 반례|LENGTH_EVIDENCE의26셀 fixture가 교정 전 결함을 보여주며 교정 후 반드시 길이 오류|
|F05 실제 생성|두 고정 ON의 모든 새 연결을 Path+HostAccess에서 독립 재구성하여24이하; 끝점/AIR/부모경계 일치|
|F06 exports/digest|새 길이 열과 순서/정책이 실제 모델과 일치; 변이는digest/validation에 반영; 같은 입력 재생성 동일|
|F07 회귀|기존95개 fullname 발견/실행/성공 보존 + 새 책임 검증,failed/skipped0; 원본시험을 생략하거나 통과로 치환하지 않음|

기존 Sv5SpaceInfillTests.T08의 잘못된 ExternalCenterline 길이 assertion은 이번 명시 소유 변경으로 강화한다.
동시에 기존 external 소유 일치 확인은 보존하고 별도 metric으로 분리한다. 다른 assertions/fixtures를 약화하지 않는다.
기존95개 중 코드/geometry/density 책임을 삭제하지 않는다. 테스트 수를 늘리는 것 자체는 목표가 아니다.
같은 고정 integration을 필요한 focused 한 벌에 재사용할 수 있으나 입력/geometry가 달라지면 cache를 공유하지 않는다.
새 검사 실패는 일반 구현/시험 단계이며 허용 범위에서 수정·재시험한다. 정확한 lock/상태 불일치와 구분한다.

## R06. 레거시 사용처 조사만 수행

동봉 SV5_LEGACY_RETIREMENT_DIRECTIVE.md는 원문 그대로이며 파일 이동·삭제 승인 문서가 아니다.
이번 Task는 아래 실사용 관계를 읽기 전용으로 조사하고 GENERATED/SV5_08_FIX01/legacy_inventory.json에 기록한다.

- PlanWithInfill→현재 Sv5SpaceGraphPlanner/Plan→Sv5SpaceInfill Capture/Build→현재 exporter/validator.
- 현재 PhysicalMovement/Product/StateProjection와 RMAP13 FSM,core/Type0/RMAP15/16 입력의 실제 consumer.
- 현재 SV5 테스트/export helper가 읽는 과거 GENERATED/INPUTS/Task/Result 경로.
- 위 체인에서 발견한 구형 wrapper/export 복제의 소비자와 대체 구현. 프로젝트 전체 정리로 확장하지 않는다.

각 대상에 path/symbol/role/classification/실제consumer path:line/검색범위/참조수/남은불확실성/rawSHA를 기록한다.
ACTIVE_SV5_CANON,COMPAT_ADAPTER,HISTORICAL_EVIDENCE,RETIRE_CANDIDATE,UNKNOWN_BLOCKED를 구분한다.
RETIRE_CANDIDATE를 제시하려면 코드/Editor/Test뿐 아니라 asmdef/asmref/GUID/scene/prefab/serialized asset/
reflection/export 경로까지 확인한다. 검색0건만으로 동적 사용0을 증명했다고 쓰지 않는다.
불명확한 항목은 UNKNOWN_BLOCKED로 보존하고 이유를 보고한다. 이것만으로 길이 보완을 BLOCKED로 종료하지 않는다.
이전 directive의 불명확성 중단 조건은 실제 격리/폐기 판단에 적용한다. 여기서는 모르는 항목을 이동하지 않는다.
기존 GraphPlanner를 이름이 legacy라고 지우거나, 실사용하는 FSM을 새것으로 다시 만들지 않는다.
완료된 MCP 증거는 역사 자료로 원형 보존한다. 현재 구현의 검증근거와 역사 보관 역할을 구분한다.
어떠한 실제 이동/삭제/참조 변경도 이번 조사에 포함되지 않는다. 후속 별도 정확한 등록 Task가 담당한다.

## R07. 출력과 보존

동봉 input들은 immutable, 추가 helper는 INPUTS/SV5_08_FIX01/tools만, 모든 결과/중간파일은 GENERATED/SV5_08_FIX01만 사용한다.
11개 기존 SV5 테스트의 모든 쓰기를 _work/legacy_exports/<fixture>/로 격리한 뒤 실행한다. 과거 입력 읽기는 유지한다.
08 historical XML/CSV/JSON/SVG는 다시 생성하지 않는다. SourceLock794항목 중 ALWAYS780개를 전후 바이트 검사한다.
각 실행 소유를 BINDING에 남기고 성공 준비 후 이번 _work 생성물만 정리한다. root CHECK/FILES/VERIFY/명령MD는 만들지 않는다.
필요한 최종파일은 아래와 같다. 예전4중 export 복제나 전체 과거 증거를 새로 복제하지 않는다.

- BINDING.json: 선행commit/등록전후/소스raw·blob/testedSHA/실행명령/소유/dirty/전후 잠금/cleanup.
- default/,repeat/: 현재 생산 export 및 infill 셀/패턴/room/link/validation 한 벌씩.
- infill_links.csv는 기존 열을 보존하고 connection_centerline,connection_cell_count,length_policy,length_status를 추가한다.
  connection_centerline은 기존 infill Points형식의 JSON [[x,y],...] 배열이다. 정책 SV5_INFILL_LENGTH_RULE_V2,새 연결 status PASS,
  legacy6개만 NOT_APPLICABLE_LEGACY_INTERIOR. legacy도 실제 path/count는 투명하게 출력한다.
- infill.json/infill_validation.json에 새 정책/전체대상수/최대길이/초과수/endpoint 오류수와 plan digest를 기록한다.
- length_audit.json: 동봉 tools/check_lengths.py로 두 CSV에서 독립 재구성한 전수 결과.
- comparison.json: 기존08 대비 새 방수/실제소유/분포/최대길이/초과0/후보거절/기존보존/미계획잔여.
- preview/index.html,전체624×416 old08/newFIX default·repeat 비교,실제1×1지형과4×4경계가 보이는 대표26셀 수정 구간 확대.
  이미 사라진 old room ID를 억지로 새 plan에서 찾지 말고 같은 좌표 창을 전후 비교한다.
  기존 SV5_08 전체/확대 파일은 읽기 입력이며 덮어쓰지 않는다. 표준 case exporter 그림은 한 벌만 유지한다.
- legacy_inventory.json 및 활성 SV5/16_INFILL_FIX01.md의 짧은 해설/불확실성/향후격리범위.
- focused_results.xml 최종1개,교정전 대표 실패 근거는 간단한 fixture 결과와 정확한testedsource SHA로 BINDING에 기록.

채팅 작성자가 만든 패키지 검사와 현지 실제 Unity 시험을 분리 보고한다.
XML worktree CRLF SHA와Git LF blob SHA는 별도다. 새 SOURCE_LOCK에는 최신 manifest raw를 사용하며 실행 중 EOL 보정은 금지다.
08 Review XML은 ZIP에LF blob이 들어있지만 raw pin은CRLF다. 이것이 정상 두 역할이며 SHA를 서로 바꿔 쓰지 않는다.

## R08. Finalize와 다음 전달

실제95개+회귀 focused,두 integration/모든새연결24이하/원본보존이 통과해야 PASS Result를 쓴다.
등록과 native Task 소유 변경만 최종 atomic commit.09는LOCKED,Current NONE,291=252/0/39로 끝낸다.
Review ZIP은 정확한 최종commit blob을 repository-relative path로 담고 manifest에rawSHA/bytes와blobSHA/bytes/OID를 각각 기록한다.
이번 INPUTS 모든파일,최종Master/Status,Task/Archive/Result,코드/tests/meta,최종증거를 포함한다.
선행08 INPUTS는 재복제하지 않고 SOURCE_LOCK으로 참조한다. ZIP 자신/_work/무관dirty/다른ZIP은 제외한다.
Result 자기 해시/최종commit을 자기참조로 강제하지 않는다. 실제 commit/parent/ZIP SHA는 manifest/콘솔에 남긴다.
다음 보고서 수신 시 검토와 다음 정상/보완 패키지·인라인 실행문을 한 응답에서 제공한다. 추가 “ㄱㄱ”를 기다리지 않는다.
새 증거가 부족하면 정확히 부족한 파일/조건을 설명하고 허위 패키지를 만들지 않는다. 현지 자동09시작/push 권한은 아니다.
