# SPACE v5 활성 규칙 인덱스

문서 역할: 계속 읽는 SV5 규칙 진입점. 게임 기능의 구현 완료를 선언하는 문서가 아니다.
경로 표기는 MapDesign 모듈 루트 기준이다. 실제 작업자는 02_PROTOCOL_V5에서 이 문서로 진입한다.

## 우선순위와 원본

1. 현재 사용자의 명시적 결정과 SV5 승인 범위를 따른다.
2. 아래 SV5 규칙 원본이 구체 규칙의 정본이다. 이 파일은 원본을 찾아 읽는 인덱스다.
3. 기존 SV5 상세 계약은 새 결정과 충돌하지 않는 부분을 유지한다.
4. 과거 예시의 수치·정적 검사·작성 기본값을 사용자 지정값이나 Player 통과 증거로 승격하지 않는다.

| 자료 | MapDesign 기준 경로 | SHA-256 |
|---|---|---|
| 승인된 규칙 | MCP/INPUTS/SV5/SPACE_V5_RULES.md | 2a89e036aafb94f012eb4b4d7e0379a878ed4d4794f7653e5bc1ccaaeee07b7e |
| 최신 결정 메모 | MCP/INPUTS/SV5/SPACE_V5_MEMORY.md | 8759d6bc0ded6cdfb306bcb0ef13404b9bfef612092021cc868215bb91e26f85 |
| 45개 계획 | MCP/INPUTS/SV5/SPACE_V5_TASKS.md | 6ca7ba9fc236e8c1a0625145221bf226613203dbff2870fc3e7157cb0c30efdc |

## 반드시 유지할 해석

- 624×416 전체 구성은 승인된 시각 기준이다. 모든 시드에 101개 장소·166개 연결을 강제하지 않는다.
- 큰 장소와 보통 공간·샛길·재합류 통로를 함께 설계한다. 외부 AIR로 둘러싼 점 배치로 돌아가지 않는다.
- 점프맵의 실제 루트에 SOLID와 안전 Grab을 포함한다. ONE_WAY의 측면은 Grab으로 취급하지 않는다.
- 외곽 일부를 1~2칸 변형하되 출입구·머리 여유·매달림·착지·주변 보호를 보존한다.
- 일반 +1칸, Jump+Grab 최대 +2칸. 실제 Player 수치를 올려 불가능한 지형을 통과시키지 않는다.
- Grab 최소 1회·외곽 구간 2곳은 제작 기본값이다. 고체 비율은 미지정이다.
- 가로 3~4칸 점프와 (가로 2, 세로 2+Grab)는 실물 검증 전 후보이며 자동 PASS가 아니다.
- SV5 핵심 ID·보호 셀·접근·복귀와 진행 조건을 실제 정본에 연결한다.
- Type0 봉쇄·절구 작업장 예약·핵심 경로의 진행 조건을 랜덤 샛길로 우회하지 않는다.
- 도서관 계단·열차·엘리베이터 등은 후속 구현 책임이다. 규칙 등록만으로 READY 처리하지 않는다.

## 단계별 읽기

모든 작업은 승인 범위와 SPACE_V5_RULES를 읽고, 해당 책임의 상세 파일만 이어 읽는다.
05_RULE_READSET_V5.json에 source 경로·SHA와 소비 Task 범위를 기록한다.
전체 JSON·CSV를 문서 한 번으로 밀어 넣지 않고 스크립트로 필요한 행을 선택한다.

| 작업 | 추가 상세 계약 |
|---|---|
| 04~10, 41~45 | SPECIALS·NETWORK·FILL·SPAWN |
| 11~20 | PLACES·MOVEMENT |
| 21~23 | REGIONS·STAIRS·MOVEMENT |
| 24~31 | REGIONS·SPAWN·MOVEMENT |
| 32~34 | RAIL·REGIONS |
| 35~40 | SPECIALS·PLACES·REGIONS·MOVEMENT |

파일들은 MCP/INPUTS/SV5/SPACE_V5_RULES.md 아래 SPACE_*.md이다.
이 표는 실제 실행 Task의 Read 목록을 대신하지 않는다. 해당 Task 발행 시 정확한 Read/Write/API를 바인딩한다.

## 문장별 추적과 상태

04_RULE_COVERAGE_V5.csv는 원본의 각 의미 있는 문장을 원본 행·SHA·구분·후속 Task 범위와 연결한다.
JUMP-01~14를 빠짐없이 포함하며 NORMATIVE / AUTHORING_DEFAULT / UNVERIFIED_MOVEMENT_CANDIDATE / REFERENCE 등을 구분한다.
CSV의 구분은 읽기/검토용이다. Unity 런타임에 새 설정 로더나 규칙 엔진을 추가하는 요구가 아니다.
규칙을 바꿀 때에는 원본의 새 버전과 이 인덱스·Coverage·ReadSet을 함께 정상 갱신하고 과거 Task/Archive 증거는 보존한다.
SV5_02 완료는 규칙 경로 등록 완료다. 지형·물리·전체 진행 검증은 각 담당 Task의 증거로 판단한다.
