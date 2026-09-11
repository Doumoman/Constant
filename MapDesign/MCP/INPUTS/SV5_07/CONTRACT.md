# SV5_07_DIVERSITY — 가까운 독립 지형의 반복 억제

이 문서는 SV5_07만의 구현 계약이다. 45개 본 작업 순서와 기존 등록을 변경하지 않는다.
기준은 승인된 624×416 구성과 완료된 SV5_06_FIX04다. 이 패키지는 구현 결과가 아니다.

## D01. 이번에 달라질 플레이 공간

같은 종류의 독립 동굴·도서관 등이 가까이 연달아 선택될 가능성을 낮춘다.
이미 요청된 장소를 삭제하거나 크기를 줄여 반복 수치를 개선하지 않는다.
한 긴 동굴을 구성하는 12×8 청크와 4×4 패턴 조각은 동굴 하나로 센다.
일반 작은 방·계단참·샛길이 촘촘히 연결되는 방향은 유지한다. 빈 AIR 띠를 만드는 작업이 아니다.
핵심 구역은 정본 좌표/보호 셀/포트/진행 의미 그대로다. 같은 종류의 출현을 전면 금지하지 않는다.
일반 상승 +1, Jump+Grab 최대 +2, 1×1 타일/4×4 패턴/12×8 청크를 유지한다.
1~2칸 연결 통로는 뒤의 지형 제작에서 구현한다. 동굴·회랑 등 큰 내부 폭을 1~2칸으로 축소하지 않는다.

## D02. 종류와 독립 공간의 식별

- `place_id`: 현재 배치된 장소. `formation_id`: 독립적으로 선택된 연속 지형 한 개의 안정적인 ID.
- `family_key`: 반복 판정용 지형 종류. `variant_id`·표시 이름·패턴 번호와 구분한다.
- 현재 분할되지 않은 Sv5SpacePlace는 하나의 formation이다. 이미 안정적인 ID가 있으면 재사용한다.
- 같은 formation을 청크/패턴으로 분할할 때는 원래 formation ID를 전파한다.
- 같은 동굴의 조각 20개는 1개, 서로 다른 동굴 2개는 2개다. 분할 순서에 따라 결과가 달라지지 않는다.
- 독립 공간 두 개를 사후에 같은 formation ID로 합치거나 Family 문자열만 바꿔 수치를 낮추지 않는다.
- 하나의 formation이 서로 다른 family를 주장하거나, 독립 place들을 근거 없이 묶으면 유효하지 않은 입력이다.
- family 그룹은 DIVERSITY_PROFILE.json의 명시적 매핑을 사용한다. 문자열 접두사 추측으로 묶지 않는다.
- ORDINARY_ROOM_A~F는 모두 ORDINARY_ROOM으로 집계한다. 이들은 연결 공간이므로 기본 감점은 면제한다.
- Core도 분포 표에는 표시하되 위치 선택/감점 대상에서 제외한다.
- 미등록 신규 family는 자신의 exact family를 사용하고 기본 지형 감점을 적용한다. 추후 명시적 매핑 가능.
- 미래 형상 제작용 가짜 청크/패턴 데이터를 만들지 않는다. 분할 소유 전파는 순수 입력 fixture로 검증한다.

## D03. 가까움과 운영 기본값

DIVERSITY_PROFILE.json은 이번 구현을 위한 운영 기본값이다. 사용자가 직접 지정한 수치로 기록하지 않는다.
- 반경: 두 점유 사각형 사이 빈 타일 간격의 Manhattan 합이 36칸 이하이면 가까움에 포함한다.
- half-open bounds A/B에 대해 dx=max(0, B.x-A.maxX, A.x-B.maxX), dy도 동일, gap=dx+dy.
- 변이 맞닿으면 0, 사이 빈 타일 하나면 1이다. 중심 거리나 섹터 번호로 대체하지 않는다.
- formation이 여러 조각이면 조각 간 최소 gap으로 판정하고 formation 쌍은 한 번만 센다.
- 비교 대상은 같은 family의 서로 다른 formation이다. 멀리 떨어진 같은 종류에는 감점이 없다.
- 가까운 같은 family가 0/1/2/3개 이상이면 후보 통과 가중치 100/70/49/34를 사용한다.
- ORDINARY_ROOM과 고정 Core는 100이다. 서로 다른 family 사이에는 감점하지 않는다.
- 가중치는 반복이 불가능하다는 보장이 아니다. 배치 후 남은 반복과 fallback을 그대로 보고한다.
- 반경/가중치/예외/정책 버전은 plan과 export의 semantic digest에 포함한다.

## D04. 실제 생산 배치 함수에 연결

기존 Sv5SpaceGraphPlanner.Plan → PlaceFamily의 실제 좌표 후보 선택에 이 정책을 적용한다.
별도 CSV의 점수 계산이나 preview 색칠만 구현한 경우 완료가 아니다.

1. 기존 후보 좌표·고체/예약 충돌·섹터 우선순위·크기/포트 조건을 유지한다.
2. 현재 순서대로 검사하는 합법 후보에서 가까운 동일 family formation 수와 weight를 구한다.
3. 기존 좌표 난수와 분리된 고정 salt `SV5_DIVERSITY_V1`을 사용한다.
4. 요청별 roll을 한 번 만든다. token=`SV5_DIVERSITY_V1|<seed>|<ordinal>|<family_key>`를 UTF-8 SHA-256하고 앞 16 hex를 unsigned64로 읽어 `%100`한다.
   정수 문자열은 InvariantCulture다. 같은 요청의 모든 좌표 후보는 같은 roll을 사용한다. 후보마다 새 roll을 뽑지 않는다.
5. roll < weight일 때 선택한다. weight=100이면 기존의 첫 합법 후보를 그대로 고른다.
6. 고체/예약/포트와 크기의 합법 조건은 그대로 유지하고 선택 우선도만 조정한다.
7. 합법 후보가 있었으나 모두 가중치에서 거절되면 가까운 중복 수가 최소인 합법 후보를 고른다.
8. fallback 동률은 기존 섹터 우선순위/안정적 rank/좌표 순서로 결정한다. 결과에 이유를 기록한다.
9. 합법 후보 자체가 없으면 기존 배치 실패다. place 삭제·크기 축소·충돌 무시로 성공 처리하지 않는다.

고정 seed에서 재현되고 입력 열거 순서와 전역 UnityEngine.Random 상태에 의존하지 않아야 한다.
정책 OFF는 동일 profile의 후보 선택 이전 기준선이다. OFF에서 FIX04 1304의 geometry를 재현한다.
기본 Plan 호출도 정책 ON을 소비해야 한다. 비교 전용 API에만 연결하고 기본 경로를 방치하지 않는다.
기존 기본 profile은 큰 family가 대부분 한 번씩만 있으므로 공간 변화가 0일 수 있다.
이 경우 `NO_ELIGIBLE_REPEAT`로 보고하고 D07의 반복 profile 검증으로 실제 작동을 보여준다.
policy seed/판정용 hash를 바꿔 core WorldDefinition seed나 Player 수치를 변경하지 않는다.

## D05. 연결과 상태 안전성

장소 좌표가 달라지면 그 포트와 실제 ordered 통로·예약도 같은 plan에서 다시 만든다.
FIX04의 RepairGateCorridors, exact local gate, 접촉 전수 검사, PhysicalMovement/Product를 그대로 호출한다.
RMAP13 FSM/action/CanTraverse, gate predicate, local barrier validator를 이번 작업에서 수정하지 않는다.
기존 seed 1304, 8개 core, 2,432개 core 셀과 W01/W02 조건을 보존한다.
변경된 두 integration plan 각각에서 6개 자원 순서/도달 상태/정상 접근·복귀/닫힌 문 우회 검사를 수행한다.
실패 후보를 통과시키기 위해 gate를 원격으로 넓히거나 통로 폭을 줄이거나 판정을 생략하지 않는다.
반복 지형 fixture의 profile 선택은 미리 고정한다. 실패 후 다른 seed만 골라 성공률을 포장하지 않는다.
필수 경로 불변을 만족시키지 못하면 이번 허용 배치 코드 안에서 수정·재시험한다.
단순 코드/시험 실패는 즉시 작업 포기 사유가 아니다. hash/권한/상태 문제와 구분한다.

## D06. 코드 접점과 제한

기존 생산 코드 최소 수정:
- SectorPlanning/Sv5SpaceGraphPlan.cs: 정책 profile, family/formation 식별과 실제 결과/digest 연결.
- SectorPlanning/Sv5SpaceGraphPlanner.cs: PlaceFamily 후보 선택과 동일 plan의 재연결.
- SectorPlanning/Sv5SpaceGraphExport.cs: 같은 plan에서 분포·좌표·비교 그림을 출력하는 접점.
신규 생산 코드:
- SectorPlanning/Sv5SpaceDiversity.cs (+ .meta): 순수 profile/거리/집계/선택 정책. 거대한 새 solver를 만들지 않는다.
신규 시험:
- Tests/EditMode/Map/SV5/Sv5SpaceDiversityTests.cs (+ .meta).
- 필요한 경우 같은 폴더 Sv5TestOutputPaths.cs (+ .meta): 이번 출력 위치를 한 곳에서 정하는 작은 helper.
기존 8개 SV5 시험 파일은 WRITE 목록의 정확한 파일만 사용하며 출력 위치 격리만 수정한다.
기존 70개 시험의 조건·assertion·test name·카테고리·fixture 의미를 완화하거나 제거하지 않는다.
Runtime은 MapDesign/MCP 경로를 읽지 않는다. 설정은 타입화된 profile로 전달하고 package JSON과 값 일치를 시험한다.
새 asmdef/패키지/씬/프리팹/Player/Camera/WorldDefinition/원본 core 변경은 이번 범위가 아니다.

## D07. 필수 검증 — 실제 효과와 반례

아래 책임을 검증한다. 테스트 개수를 인위적으로 맞추지 않는다.

| ID | 입력과 확인할 결과 |
|---|---|
| V01 | 거리 35/36/37 경계, 접한 bounds, 한 칸 간격, 반경 밖 동일 family의 정확한 판정 |
| V02 | 연속 동굴의 4×4/12×8 분할은 1개; 독립 동굴 2개는 2개; 같은 formation의 충돌 family는 거부 |
| V03 | variant/표시 이름 변경으로 같은 family 감점을 피할 수 없음; ORDINARY_ROOM은 집계하되 감점 면제 |
| V04 | 0/1/2/3+ 이웃의 weight와 결정성; OFF/weight100은 기존 후보; 후보 열거 역순에도 선택 동일 |
| V05 | 실제 후보 선택 함수에 가까운/먼 합법 후보를 함께 공급; 고정 seed 0~63에서 OFF와 ON 비교 |
| V06 | V05의 ON 가까운 동일 family 선택 총수가 OFF보다 작음; 강제로 모든 soft roll이 실패하는 fixture에서 합법 fallback을 선택하고 이유 기록 |
| V07 | 실제 624×416 Plan(core1304)의 OFF/ON; core 동일/기본 profile 좌표 변화 여부와 이유를 정직하게 기록 |
| V08 | 같은 family가 최소 3개 있는 고정 repeat profile을 같은 실제 Plan(core1304)으로 OFF/ON 각각 생성 |
| V09 | V08은 OFF에 가까운 동일 family 쌍이 최소 1개 있고 ON에서 그 수가 실제 감소; 장소 수·종류별 수·개별 크기·core 동일 |
| V10 | V07 ON과 V08 ON 모두 FIX04 생산 validators/product PASS; 의도적으로 보호 영역 침범/닫힌 문 우회를 만든 negative 입력은 기존 검증이 거부 |
| V11 | 프로필/seed/실제 선택/plan digest가 JSON·CSV·SVG에 연결되고 export 재실행이 동일 바이트; 정책 값 변경은 digest 변경 |
| V12 | 기존 SV5 focused 70개 + 새 시험 failed/skipped 0; 시험 전후 이전 GENERATED와 immutable source 바이트 동일 |

V05의 작은 후보 fixture는 생산 함수를 직접 사용한다. 축소 그림만으로 전체 plan 통과를 대신하지 않는다.
V08은 RepresentativeV1의 기존 14개 요청에 ordinal=16,32인 CAVE_BAND(각 60×24, Large, SV5_24_CAVE)를 추가한 고정 profile이다.
원래 ordinal=0의 동굴까지 독립 동굴 총 3개다. 세계 seed=1304, 요청 순서는 ordinal 오름차순이다.
이 요청 목록/seed/크기는 DIVERSITY_PROFILE.json에 고정한다. 실패 후 다른 profile/seed로 교체해 성공 사례를 고르지 않는다.
추가 반복 요청은 같은 동굴의 크기/미래 제작 담당을 보존한다. 단순 표시 이름 교체는 불가하다.
비교 양쪽에서 요청 ID·개수·크기·seed·core·후보 생성 규칙은 동일하고 policy enabled만 다르게 한다.
앞서 선택된 장소가 이동해 이후의 합법 후보 집합이 달라지는 것은 정상적인 배치 결과이며 이력에 남긴다.
fixture의 요청 목록과 후보 생성 규칙은 시험 전에 코드/JSON으로 고정하고 export에 남긴다.
패키지 제작 시 좌표 선택 계산에서는 동굴 근접 쌍이 OFF 3→ON 2로 감소했다. 이는 실제 Unity의 통로/product PASS 증거가 아니다.
64개 seed 평가는 순수 선택 fixture만 실행한다. 거대한 월드 64개 전체 product를 돌리는 요구가 아니다.
현재 생산 기본 profile의 중복 쌍 0을 감소 성과로 표시하지 않는다.

## D08. 검토 화면과 최종 산출물

새 결과는 `MapDesign/MCP/GENERATED/SV5_07/` 한 폴더에 모은다.
- BINDING.json: 패키지/Task/선행 커밋, read/write/API, 시험 전후 SHA, 반복 fixture 정의와 정책 값.
- `default/`: 현재 기본 profile의 ON plan, 기존 WriteAll 출력 한 벌.
- `repeat/`: 반복 검증 profile의 ON plan, 기존 WriteAll 출력 한 벌.
- `diversity.json`: OFF/ON 집계, 제외·fallback·미완 사유, 실제 사용 profile, 두 plan digest.
- `decisions.csv`: 후보 선택 이력. case/formation/family/x/y/size/neighbor IDs·count/weight/roll/decision/fallback/digest.
- `pairs.csv`: OFF/ON의 동일 family 독립 쌍, 두 실제 bounds/gap/반복 판정/제외 사유.
- `preview/before_after.svg`: 반복 profile OFF/ON을 각각 전체 624×416 축척으로 좌우 비교한다.
- `preview/detail.svg`: 실제 변경 구역 확대. 1칸 격자와 4×4 경계, 기존/이동된 장소 외곽, 실제 통로를 표시한다.
- `preview/index.html`: 전체 비교·확대·실측 수치를 연결한다. 지형별 색과 번호는 전후 동일하다.
- `focused_results.xml`: 최종 실제 focused 실행 결과 한 개.

현재 단계의 그림은 장소 점유와 예정 통로를 보여준다. 미조립 내부를 완성된 플레이 지형처럼 채색하지 않는다.
별도의 미술 concept 이미지나 추상 노드 그림으로 실제 좌표 비교를 대체하지 않는다.
OFF 비교는 places/ports/connections/gates와 profile/digest를 diversity.json에 정확히 포함하면 된다.
OFF의 거대한 full export나 legacy export를 최종본으로 여러 벌 복제하지 않는다.
validation에는 PLANNED_LAYOUT/DIVERSITY/CONTACT/PHYSICAL_PRODUCT와 COMPOSED_GEOMETRY/PLAYER를 분리한다.
`ComposedGeometryReady=false`, `PlayerVerified=false`다. 08의 밀도·09의 loop·41 조립·44 Player는 미완이다.

## D09. 실행·보존·정리

STAGE.py는 package/preflight/post-readonly는 읽기 전용이며 stage만 INBOX MD 하나를 생성한다.
파일이 많다는 이유로 과거 원본 해시 검사를 제거하지 않는다. SOURCE_LOCK은 최신 FIX04 바이트로 고정한다.
WORKTREE SHA와 COMMIT_BLOB SHA는 별개다. CRLF/LF 자동 정규화·기대 SHA 갱신·restore는 하지 않는다.
이미 SV5_07 CURRENT이면 설치 Task/Archive와 post-readonly만 검사하여 현재 구현을 계속한다. 재Apply하지 않는다.
이미 COMPLETE이면 Result/Finalize/소유 commit/Review ZIP만 확인하고 재실행하지 않는다.
INBOX가 다른 후보로 차 있으면 자동 삭제·이동하지 않고 정확한 경로를 보고한다.

실제 Unity 시험 전에 기존 8개 SV5 test의 출력 경로를 모두 조사한다.
기존 회귀의 새 출력은 `GENERATED/SV5_07/_work/legacy_exports/<fixture>/`로만 보낸다.
Historical FIX03 입력은 원래 읽기 경로를 유지한다. 이전 FIX04/03 GENERATED로 새 출력하지 않는다.
이 출력 격리를 먼저 끝내고 시험 전후 SOURCE_LOCK의 ALWAYS와 BINDING의 비소유 변경 목록을 대조한다.
중간 시험 XML/진단은 `_work/`를 사용하고 마지막 시험만 최종 focused_results.xml로 기록한다.
통상 시험 실패는 허용된 소스 안에서 수정·재시험한다. 원본/상태/권한 mismatch 때는 쓰기를 중단한다.
PASS 준비가 끝나면 이번 실행이 만든 `_work` 항목만 정리한다. 다른 dirty 파일이나 이전 작업 증거는 건드리지 않는다.
프로젝트 root에 CHECK/FILES/VERIFY/명령 MD를 새로 만들지 않는다. 추가 helper가 필요하면 INPUTS/SV5_07/tools/만 사용한다.
STAGE.py나 SOURCE_LOCK/DIVERSITY_PROFILE/동봉 Task를 실행 중 고쳐 검사를 통과시키지 않는다.
기존 8개 시험의 코드 수정도 최종 tested source SHA에 포함한다.

## D10. 정상 종료와 Review ZIP

실제 focused 명령과 버전/발견 수/실행 수/pass/fail/skip/XML raw SHA·commit blob SHA를 단일 Result에 기록한다.
기존70개 이름이 그대로 발견됐는지 이전 XML과 비교하고 새로운 시험 목록/실제 소스 SHA를 함께 기록한다.
TASK가 PASS일 때만 native Finalize 후 이번 소유 파일만 atomic commit한다. push하지 않는다.
최종 상태는 290 = 250 COMPLETE / 0 CURRENT / 40 LOCKED, Current NONE, SV5_08_INFILL LOCKED다.
`SV5_07_REVIEW.zip`에는 이번 실제 입력/Task/Archive/Result/활성 문서/변경 소스·meta/시험/새 최종 출력/Status/Master를 넣는다.
`_REVIEW_MANIFEST.json`에 path/role/raw bytes·SHA/git blob bytes·SHA/OID/실제 commit와 parent를 기록한다.
ZIP과 manifest 자체를 자신의 해시 목록에 포함하지 않는다. ZIP은 commit 후 만들어 같은 commit의 manifest를 가진다.
기존 FIX04의 Result/Task/Archive/BINDING/validation/해당 소비 데이터는 필요한 읽기 참조로 넣어도 된다.
과거 legacy export 전체, 이전 ZIP, Unity Library/Temp, `_work`, 무관한 dirty는 넣지 않는다.
Result와 manifest는 패키지 검증·Unity 기록·실제 좌표 검증·Player 완료를 혼동하지 않는다.
SV5_08 이후를 시작하거나 RMAP18/19/VIS/PlayMode/build/Scene Bake를 실행하지 않는다.
