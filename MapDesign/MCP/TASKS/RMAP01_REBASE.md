# RMAP01_REBASE — 새 작업 등록과 현재 구현 연결점 확정

TASK: RMAP01_REBASE
DOCUMENT: v4.2 / 최초 계획 등록 실행 지시서 / 2026-09-08
INPUT: MapDesign/MCP_INBOX/RMAP01_REBASE.md
RESULT: MapDesign/MCP/REPORTS/RMAP01_REBASE_RESULT.md
PREDECESSOR: RUN06_TUNE_GENERATED_RUN_SHAPES_AND_REMOVE_BAD_VARIANTS
NEXT LOCKED: RMAP02_PLAYER

## 1. 사용자 목표와 이번 요청의 권한

사용자는 v4.2의 19개 작업을 새 MapDesign 실행 순서로 등록하고 첫 작업을 수행하도록 요청했다.
이번 작업은 그 최초 등록과 실제 코드의 재사용 판단을 함께 수행하는 문서 계약 변경이다.
게임 C#·Scene·Prefab·Authoring은 읽기만 하며, 완료 시 다음 Player 작업의 실제 수정 경로를 확보한다.
이 문서는 미등록 ID를 일반 single_task_v1 입력으로 가장하지 않는다. 임의의 새 protocol 이름도 선언하지 않는다.
CLI의 명시적인 최초 계획 등록 지시와 함께 실행한다. 기존 적용기가 미등록 ID를 거부하는 것은 정상이다.
이번에 허용한 계획 등록은 아래 별도 적용 단계로 수행한다. 일반 Task의 적용 실패에 대한 우회 경로로 사용하지 않는다.
등록 대상·상태 변경·로컬 기준선 채택은 이 작업에만 한정된다. 후속 Task의 SHA·잠금·선행 PASS 검사는 유지한다.
실제 권한·승인 차단은 우회하지 않는다. 이 문서로 도구 권한을 변경하거나 다른 작업을 자동 승인하지 않는다.

## 2. 읽기 범위와 사전 확인

1. 프로젝트 루트와 적용되는 AGENTS.md, MapDesign/MCP의 Entry·Apply·ChangeControl·Status·Finalize 규칙을 읽는다.
2. git HEAD/branch/status, 관련 staged·dirty 경로, Master/Status의 실제 위치·상태·작업 수를 기록한다.
3. INBOX 미적용 후보는 이 MD 하나여야 한다. 다른 후보를 삭제·이동해서 조건을 맞추지 않는다.
4. 최초 적용은 Current Task=NONE, RUN06=COMPLETE, RUN06 Result의 정확한 TASK와 STATUS: PASS를 현지에서 확인한다.
5. RUN06 실행 Task·Result를 우선 MCP/TASKS·REPORTS에서 찾는다. 이동했으면 Archive/Git 이력에서 실제 실행 경로를 추적한다.
6. RUN06 설치/보관 Task가 둘 다 있으면 바이트를 대조한다. Result가 명시한 선행 SHA·commit 증거도 대조한다.
7. RUN06 Task·Result와 해당 경로의 committed 버전을 대조한다. Git의 정상 EOL 변환은 속성 설정으로 구분하고 실제 파일 SHA·참조 commit을 기록한다.
8. 이번 발행 시 RUN06 Result 원본은 전달되지 않았다. 외부 검수 PASS라고 쓰지 않고 LOCAL_VERIFIED로 구분한다.
9. 로컬 Result/Task/commit의 일관성을 확인한 뒤 사용자 요청에 따라 새 시퀀스의 시작 기준선으로 채택한다.
10. 임의 SHA·REPLACE_ME·자동 SHA 수정은 금지다. 확인된 다른 expected SHA와 충돌하면 원인과 두 값을 보고한다.
11. Current·완료 기록 불일치, 선행 파일의 실제 내용 변경, Task/Archive 충돌은 등록 전에 멈춘다. 이전 작업을 자동 재개하지 않는다.
12. RMAP01~19가 이미 일부 등록됐으면 중복 추가하지 않는다. 동일 계획/상태이면 확인된 지점부터 이어간다.
13. 이미 RMAP01 COMPLETE+유효 Result+Current NONE이면 추가 실행 없이 기존 Result와 다음 잠금을 보고하고 STOP한다.
14. 관련 기존 변경과 이번 변경을 분리할 수 없으면 해당 경로만 보고한다. 무관한 변경은 수정·stage하지 않는다.
15. 실패한 동일 RMAP01의 재개는 설치 Task/Archive/원본 일치와 RMAP01 CURRENT 기록을 확인하고 이어간다. NONE으로 초기화하지 않는다.

소스는 Assets의 Map/Player/Camera 및 참조 assembly, 관련 Tests·Scene·Prefab, 필요한 ProjectSettings/Packages만 읽는다.
rg로 실제 경로를 찾고 긴 파일은 필요한 줄 범위로 나눠 읽는다. Library·Temp·obj·패키지 캐시 전수 조사는 하지 않는다.
원문 PDF나 다른 RMAP 명세가 프로젝트에 없어도 이번 등록·현황 확인은 아래 계약으로 수행한다. 읽지 않은 원문을 설치했다고 쓰지 않는다.

## 3. 쓰기 허용 범위

- MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md, 06_IMPLEMENTATION_STATUS.md의 RMAP 등록 부분.
- 실제 Master 경로가 다르면 참조 관계를 확인한 그 파일만 사용하고 Result에 경로를 남긴다. 두 Master를 새로 만들지 않는다.
- MapDesign/MCP/RMAP/00_BASELINE_V4_2.md, 01_SEQUENCE_V4_2.md, 02_PROTOCOL_V4_2.md.
- MapDesign/MCP/TEMPLATES/RMAP_RESULT_TEMPLATE.md.
- MapDesign/MCP/GENERATED/RMAP01/의 아래 지정 산출물.
- MapDesign/MCP/TASKS/RMAP01_REBASE.md, MapDesign/MCP_ARCHIVE/RMAP01_REBASE.md, 본 INBOX 원본, 지정 Result.
- 필요할 때만 MCP의 00_MCP_ENTRYPOINT.md, 05_CHANGE_CONTROL_RULES.md, 07_PATCH_APPLY_RULES.md, APPLY_PATCH_AND_RUN_CURRENT_TASK.md.
- 마지막 네 문서는 활성 계획 참조·이번 등록 기록·폐기된 고정 행 수/ID 접두사 제한만 정합화한다. 일반 검증 규칙은 보존한다.

이외 게임 파일 변경, 기존 Task·Result 재작성, 기존 승인 파일/GUID 이동·삭제는 금지다.
다른 내용의 RMAP01 Task·Archive가 이미 있으면 덮어쓰지 않는다. 실패 재개는 현재 상태와 기존 Result부터 확인한다.

## 4. 한정된 등록 적용 단계

1. 변경 전 Master/Status의 경로·해시·기존 상태 행과 HEAD를 baseline_evidence.json에 기록한다.
2. 아래 19개 ID·책임·요구 ID를 01_SEQUENCE_V4_2.md에 설치하고 Master의 활성 계획을 해당 문서로 연결한다.
3. 기존 MAP/VIS/RUN 완료 행과 Result는 보존한다. 미시작 RUN07·이전 RMAP 계획은 실행 대상에서 제외한다고 주석으로 명시한다.
4. 이전 LOCKED 행을 COMPLETE로 바꾸거나 미지원 상태값으로 치환하지 않는다. 과거 계획은 보존하고 활성 순서만 v4.2로 지정한다.
5. 누락된 RMAP ID만 Master/Status에 LOCKED로 추가한다. 최초 무등록 상태라면 정확히 19개 추가된다.
6. 본 INBOX 원본의 SHA를 계산하고 TASKS에 바이트 동일하게 설치한다. 설치 원본은 이후 수정하지 않는다.
7. 등록 확인 후 적용 단계에서 RMAP01만 LOCKED→CURRENT, Current NONE→RMAP01로 연다. 나머지 18개는 LOCKED다.
8. 동일 바이트를 확인한 뒤 원본을 MCP_ARCHIVE로 이동한다. 기존 Archive가 동일하면 중복 파일만 정리하고 다른 bytes면 중단한다.
9. 이후 Task 실행은 조사/문서 산출물만 작성한다. 상태 변경은 이 적용 단계와 PASS 이후 Finalize에서만 수행한다.
10. 기존 행 수 상수가 새 계획을 거부하면 승인된 Master의 실제 ID 집합/행 수를 사용하도록 해당 문장만 갱신한다.
11. 미등록 ID 일반 허용, 모든 LOCKED 해제, SHA 검사 생략, protocol 자동 추정 등의 일반 예외는 만들지 않는다.
12. 이 전환은 RMAP01 최초 등록에만 적용됐음을 로컬 원본 SHA·기준 HEAD와 함께 운영 문서에 남긴다.

## 5. 설치할 전체 19개 작업

아래 책임·요구 ID는 계획이다. 후속 기능을 이번에 구현하거나 TASKS에 빈 실행 파일을 만들지 않는다.
NEXT는 검수 대기 후보이며 자동 CURRENT 전환을 뜻하지 않는다. 각 후속 MD는 Result 검수 후 별도 발행한다.

| Task ID | 책임과 산출물 | 주 요구 ID |
|---|---|---|
| RMAP01_REBASE | 계획 등록, 현황/재사용/실제 파일 바인딩 | G01~G13 |
| RMAP02_PLAYER | 실제 Tilemap·Collider, 기본 Player 이동·점프·연속 카메라 씬 | P01~P03,C01,C02,E01 |
| RMAP03_GRAB | 모서리 잡기·금지 조건·움직이는 안전 고체 | P05~P08,P10 |
| RMAP04_CLIMB | 사다리·기둥·이탈 점프·일방향 발판 | P11~P13,P15 |
| RMAP05_FALL | 낙하 피해·경직·사망·잡기/등반 초기화 | P09,P14,P16~P18 |
| RMAP06_LOOK | Tab 관찰·입력 우선순위·통합 이동 시험 씬 | C03~C05,E02 |
| RMAP07_PATTERNS | 4×4 셀·역할·태그·변환·초기 40~60개 | A01,A06~A12,A15 |
| RMAP08_PORTS | Type 0~4·출입구 좌표·방향·내부 연결 | A05,A16~A20 |
| RMAP09_COMPOSER | 12×8 조립·보호 공간·Overlay·제한 재선택 | P04,P19,A22~A24 |
| RMAP10_SMALL_RUN | Seed/가변 크기→실제 Player 출구 도달 | E03~E07 |
| RMAP11_POOL500 | 역할별 고유 500개 선별·실제 생성 연결 | A13,A14 |
| RMAP12_WORLD_DATA | Seed/버전·RNG·ID·기본 지형/변경 상태 분리 | W08~W12 |
| RMAP13_WORLD_GRAPH | 자원 자유 순서·제작·봉인·보스·출구·지름길 | W01,W02,W07 |
| RMAP14_BIOMES | 네 바이옴·밀도 프로필·경계 | A25,W05 |
| RMAP15_SPECIALS | 필수 특수지역 8종 예약·고정 지형·접근로 | W04 |
| RMAP16_CLUSTERS | 큰 지형·비밀지역·밀도/활성 비율·전체 조립 | A02,A21,A26,A27,W06 |
| RMAP17_WORLD_BAKE | 624×416 실제 Tilemap/Collider·Player 연결 | A04,E08 |
| RMAP18_WORLD_STATE | 변경 저장/적용·재생성·청크 활성 수명주기 | W13,W14 |
| RMAP19_WORLD_PLAY | 6개 자원 순서·대표 실제 플레이·최종 인계 | A03,W03,W15,E09 |

## 6. 설치할 공통 기준

- 우선순위: 최신 사용자 지시→v4.2 실행 명세→변경되지 않은 v4.0 설계→과거 계획. 실제 완료 여부는 저장소 증거로 판단한다.
- 원문 참조: 마펠렁키_맵재구현_SourceOfTruth_TaskHandoff_v4.0.pdf, 20쪽, §0~43와 Appendix A/B.
- 원문 참조 SHA-256: b36825eaec6af007cf8fe4d6cfc4f0ad16fe042ac7e7a70501251c61e642f877. 프로젝트 원문 보유 여부는 별도 기록한다.
- 계약 묶음은 G01~13/P01~19/C01~05/A01~27/W01~15/E01~09, 총 88개다. 이 수를 구현 완료율로 쓰지 않는다.
- 1×1 Tile→4×4 MicroPattern→12×8 MicroChunk. 4×4 여섯 위치가 한 청크를 구성한다. 12×8은 카메라 시야이기도 하며 방 경계는 아니다.
- 시험 맵은 가로 12배수·세로 8배수다. 시작값 60×40은 조절 가능하며 고정 한도가 아니다. 최종 Full Run은 624×416이다.
- 48×32 Sector 필수 계층·고정 4/16청크 묶음·Camera Room 전환을 새 생성의 필수 조건으로 사용하지 않는다.
- 공간 예약/고정 지형/큰 형상→Port·Route·보호 공간→Pattern·Overlay→기본 연결 확인→실제 Tilemap/Collider 순서다.
- Type 0은 일반 입구 0개인 봉인 형태와 독립 입구 1개인 막다른 형태다. 너비 3셀 출입구도 1개다.
- 열린 Type 0에 비밀/도구 필요를 자동 부여하지 않는다. INACTIVE_SOLID와 구분하고 비밀지역 내부 연결과 외부 진입도 구분한다.
- MirrorY/R180 뒤에도 일방향 발판의 충돌면은 월드 위쪽이다. Base 셀 위치 변환과 충돌 방향을 분리한다.
- 역할형 500개는 최종 고유 Base Geometry 후보 수다. 초기 40~60개부터 실제 조립하며 Overlay/슬롯 조합으로 수량을 부풀리지 않는다.
- Player와 검증기는 같은 이동 설정을 소비하되 생성기는 특정 Player/Camera 인스턴스에 의존하지 않는다.
- 순수 데이터 Bake 계획·Cardinal BFS·Preview Marker를 실제 타일 배치·물리 충돌·Player 통과 증거로 간주하지 않는다.
- 낮은 천장·불리한 배치·까다로운 선택 경로는 허용한다. 확인된 필수 유일 경로 단절을 PASS 처리하지 않는다.
- 자동 굴착·무한 reroll·침묵 수리는 금지다. 실패 이유와 제한된 재선택 범위를 드러낸다.
- 문제 없는 과거 전체 회귀·legacy 19347·full/unfiltered test·불필요한 build는 실행하지 않는다. 새 기능/실제 문제의 필요한 범위만 확인한다.
- Task는 약 1~2시간 목표의 기능 책임 단위다. 클래스별 새 Service나 반복 감사 Task를 늘리지 않는다.
- 첫 실제 Player 씬은 RMAP02, 소형 Seed 맵은 RMAP10, 전체 실제 Bake는 RMAP17, 최종 인계는 RMAP19다.
- 후속 실제 기믹·몬스터·마루·경제·NET 구현은 보류다. 슬롯/ID/결정성/상태/수명주기 접점을 이번 책임에 남긴다.

## 7. 기존 29개 작업의 대응

다음 표를 01_SEQUENCE에 함께 보관한다. 숫자 축약은 원문 ID의 접두사이며 기존 Task 파일명을 바꾸는 명령이 아니다.

| 원문 Task 접두사 | 새 주 책임 번호 |
|---|---|
| RMAP00_01, RMAP00_02, RMAP00_03 | 01 |
| RMAP01_01 | 02,12 |
| RMAP01_02 | 02 |
| RMAP01_03 | 03 |
| RMAP01_04 | 04 |
| RMAP01_05 | 05 |
| RMAP01_06, RMAP01_07 | 02,06 |
| RMAP02_01, RMAP02_02, RMAP02_05 | 07 |
| RMAP02_03 | 08 |
| RMAP02_04 | 07,11 |
| RMAP02_06 | 09 |
| RMAP02_07 | 09,10 |
| RMAP03_01 | 02,10 |
| RMAP03_02, RMAP03_03, RMAP03_05 | 10 |
| RMAP03_04 | 10,11,16 |
| RMAP04_01 | 13 |
| RMAP04_02 | 14 |
| RMAP04_03 | 16 |
| RMAP04_04 | 15 |
| RMAP04_05 | 17 |
| RMAP04_06, RMAP04_07 | 19 |

## 8. 실제 코드 관찰과 재사용 판단

조사 결과와 새 설계를 구분한다. 코드에 없는 기능을 만들어 이번 조사 항목을 통과시키지 않는다.
G01/G06: 선행 상태·해시·실제 실행 증거를 baseline_evidence에 기록한다. RUN06 완료를 추측으로 채우지 않는다.
G02: RUN01~06의 후보 선택/분기/재생성/선별 증거와 실제 Player·물리·전체 월드의 증거를 구분한다.
G03: Seed 결정성·Authoring/Generated 분리·4×4 원본·선택/실패 이유·Start/Exit·분기/재합류·Seed Window를 보존 후보로 둔다.
MAP08의 승인 경계 Authoring/증거와 기존 Preview Scene/Result를 보존한다. 보존을 위해 과거 테스트를 다시 돌리지 않는다.
G04: 500 풀·BFS·Camera Room·좌표·TerrainCluster/Activity/SpecialRegion의 API와 실제 호출자를 확인한다.
G05: 새 설계에서 제외한 강제 계층은 DEBUG_ONLY/ADAPT로 분류한다. DELETE_CANDIDATE는 후속 검토 표시이며 삭제하지 않는다.
G07: 관련 소스 경로/타입은 자동 목록화하고 생성 버튼→생성 데이터→Bake→Scene→Player/Camera 흐름을 우선 상세히 읽는다.
전체 파일의 모든 메서드에 수동 리뷰를 강제하지 않는다. 정적 참조 추적은 실제 런타임 호출 증명과 구분한다.
G08: 이미 있는 기능은 KEEP/ADAPT로 연결하고 없을 때만 후속 신규 책임을 제안한다. 기존 코드를 본 뒤 파일 수를 정한다.
G09/G10: 현지 적용기의 정확한 메타데이터 필드·경로·해시 규칙을 02_PROTOCOL에 기록한다. 정상 Task 처리 규격은 바꾸지 않는다.
G11/G12: Result는 사용자 기능·파일별 책임부터 보고하고 실행하지 않은 테스트/Scene 확인을 PASS로 표시하지 않는다.
G13: 19개 실행 순서와 원문 29개 대응을 설치한다. 6개였던 이전 계획을 전체 범위로 계속 사용하지 않는다.

반드시 확인할 연결점:

- 실제 Player Prefab/Scene, Collider 크기/Pivot, 입력 모듈, 이동/상태/체력, Camera 참조와 프로젝트 Physics2D 설정.
- RUN05 생성 Window와 RUN06 선별 도구의 호출 대상, 4×4 후보 원본/결정성 API, 이미 있는 CSV/지형 좌표 변환.
- GeneratedTilemapLayerBaker의 출력이 계획 데이터인지 실제 Tilemap.SetTile/SetTiles 호출까지 포함하는지.
- GeneratedTraversalProfile/RuleRegistry/TileMovementGraphBuilder/CompletionSearch의 소비자와 실제 Player 공유 가능성.
- Player/Tilemap/Camera가 누락됐으면 신규 구현 필요로 기록한다. 예상되는 기능 부재 자체는 이번 감사 FAIL이 아니다.

## 9. 조사 산출물과 다음 작업 바인딩

GENERATED/RMAP01 아래에 다음 5개만 만든다. CSV는 UTF-8, 헤더 포함, 상대 프로젝트 경로, 안정된 정렬을 쓴다.

| 파일 | 필수 내용 |
|---|---|
| baseline_evidence.json | HEAD, Current/행 수 전후, RUN06 Task/Result 경로·SHA·commit, 로컬 확인 범위, 설치된 RMAP01 SHA, 등록/정책 변경 |
| current_map_file_inventory.csv | path,type,assembly,Runtime/Editor/Test,MB/SO/Pure,책임,조사 깊이 |
| current_map_dependency_summary.csv | from_path,to_path,symbol,참조/호출/Scene 참조,근거 위치,미확인 이유 |
| current_map_reuse_decisions.csv | path,KEEP/ADAPT/DEBUG_ONLY/DELETE_CANDIDATE/UNKNOWN,근거,후속 소유 Task |
| file_bindings.csv | task_id,role,actual_or_proposed_path,EXISTING/PROPOSED/UNRESOLVED,변경 이유,읽기/쓰기 의도 |

file_bindings는 후속 18개 작업마다 최소 한 책임 행을 가진다. 모든 미래 소스 경로를 미리 확정할 필요는 없다.
RMAP02는 실제 Player/입력/이동 설정/Tilemap Bake/Camera/Scene/테스트 assembly 경로를 구체화한다.
신규 파일은 기존 폴더·namespace·asmdef를 근거로 PROPOSED 경로를 제시한다. 별칭 이름마다 새 클래스를 만들지 않는다.
RMAP02의 필요 파일과 공개 API/연결 지점은 Result에도 요약해 다음 실행 MD 발행에 CSV 전체를 다시 첨부하지 않게 한다.
00_BASELINE은 §6과 보존/교체 판단을, 01_SEQUENCE는 §5/7을, 02_PROTOCOL은 아래 발행/보고 규칙과 실제 적용기 스키마를 담는다.
문서마다 300줄 이내로 쓰고 긴 소스·CSV는 부분 읽기를 안내한다. 원문 전체 재독·24개 파일 일괄 설치를 요구하지 않는다.

## 10. 후속 발행 규칙과 Result 템플릿

RMAP02부터는 확인된 선행 Result/설치 Task SHA를 담은 기존 단일 MD 형식으로 발행한다.
한 Task만 CURRENT, PASS 후 Finalize, 다음은 LOCKED, 관련 파일만 commit, push 없이 STOP한다.
Result 첫머리는 사용자 관점 기능과 실제 추가/수정 파일의 책임이다. 아래 항목을 템플릿에도 넣는다.

```text
TASK: RMAP01_REBASE
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT 중 하나
USER-FACING IMPLEMENTATION REPORT: 새 계획/확정된 재사용 경로, 게임 기능 변화 여부
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 이번에 맡지 않는 책임
PRECONDITIONS: HEAD, 실제 RUN06 경로/Task·Result SHA/commit, LOCAL_VERIFIED, Current
REGISTRATION: 실제 Master/Status 경로, 전후 행 수, 19개 상태, 운영 규칙 수정 구간
REUSE SUMMARY: 핵심 스크립트·책임·판단·호출/참조 근거
RMAP02 BINDINGS: 실제/제안 경로, 공개 API/설정/Scene 참조, 변경 사유, 미확인
FOLLOWUP ISSUING CONTRACT: 현지 단일 MD 메타데이터 필드/경로 규칙, 후속 발행에 필요한 실제 값
REQUIREMENT EVIDENCE: G01~G13 각각의 산출물/확인 결과
VALIDATION: 문서/상태/바이트 확인, 게임 변경 0, 수행하지 않은 테스트와 이유
UNITY VISIBLE OUTPUT: 이번엔 게임/씬 변경 없음, 기존 실동작 확인 여부를 구분
OUT-OF-SCOPE FINDINGS: 해결하지 않은 문제와 후속 책임
FINAL EVIDENCE: 판정, 설치된 RMAP01 Task의 실제 SHA-256, 남은 미확인
NEXT: RMAP02_PLAYER LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 작업 commit 조회 방법; 실제 생성 SHA는 CLI 최종 보고
```

## 11. 필요한 확인과 완료 조건

- 기존 상태 행/승인 Result 보존, RMAP 19개 ID 중복 0, 주 요구 ID 88개 누락/중복 0, 이전 29개 대응 누락 0.
- 설치 Task와 Archive SHA 동일, 원본 내용 임의 수정 0, 실제 적용/Finalize 단계의 상태 변경이 기록된다.
- 지정 CSV/JSON/운영 문서가 존재하고 RMAP02 바인딩의 EXISTING 경로가 실제 존재한다. PROPOSED는 구분한다.
- 게임 C#/Scene/Prefab/Authoring/ProjectSettings/Packages 변경 0. 관련 기존 dirty 내용은 그대로 보존한다.
- Unity refresh/compile/test/build를 새로 돌리지 않는다. 기존 Console 정보를 얻을 수 있으면 관찰만 기록한다.
- 이 문서 작업은 Unity MCP 연결 부재만으로 막지 않는다. 실제 게임 PASS나 전체 구현 완료를 주장하지 않는다.
- G01~G13 완료와 재사용 판단/다음 수정 경로가 있으면 작업 PASS다. 정상적인 미구현 기능은 후속 책임으로 남긴다.

## 12. 실패, Finalize, Commit, STOP

사전 실제 상태 충돌은 STATUS_CONFLICT, 필수 선행 증거 부재는 BLOCKED, 적용/산출물 불충족은 FAIL로 보고한다.
실패 시 확인된 현재 상태·바뀐 파일·최소 해결 항목을 기록한다. SHA/Status를 맞춰 쓰거나 다른 작업을 시작하지 않는다.
PASS Result 작성 후 기존 Finalize 규칙으로 RMAP01 CURRENT→COMPLETE, Current→NONE만 변경한다. RMAP02~19는 LOCKED다.
Task/Archive/Result·허용된 운영/조사 산출물만 commit한다. 무관한 staged 변경을 포함하거나 자동 unstage/stash/reset하지 않는다.
commit 메시지: RMAP01 register v4.2 plan and map reuse bindings
commit 자체 SHA를 Result 안에 넣으려고 amend/추가 commit을 반복하지 않는다. Result는 HEAD와 git log 조회 경로로 작업 commit에 연결한다.
최종 CLI 보고: Result 경로, commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP02 LOCKED.
Push와 RMAP02 실행 없이 STOP한다.
