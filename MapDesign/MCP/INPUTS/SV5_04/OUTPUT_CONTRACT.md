# SV5_04 출력·인계 계약

경로는 MapDesign 기준이다. runtime/test/editor 소스의 위치는 project 기준으로 별도 바인딩한다.
아래 CSV/JSON 이름은 이번 generated 출력 파일이다. C# runtime 모델의 이름/필드를 강제하지 않는다.
CSV는 UTF-8 RFC4180, JSON은 UTF-8, 각 MD는 300줄 이하로 작성한다.

## 활성 문서

MCP/SV5/08_CORE_RESERVE_V5.md에 실제 입력/생산/소비 API·자료형·코드 경로·실패 결과·출력 진입점을 요약한다.
SV5_05/06/41이 어떤 core plan과 보호 query/gate를 재사용해야 하는지 정확한 Read와 접점을 적는다.
기존 02_PROTOCOL/06_BINDINGS/07_FILE_FLOW와 이전 조사 CSV는 변경하지 않는다. 다음 Task의 명시적 Read에서 새 요약을 소비한다.

## MCP/GENERATED/SV5_04/

| 파일 | 담을 내용 |
|---|---|
| core_sites.csv | 실제 site/role/graph binding/slot/footprint/patch identity와 원본 경로·digest |
| core_cells.csv | 원본 고정/보호 셀의 site·world/local 좌표·S/A/O·protection·owner; 대표 원본 2,432셀과 동일 |
| access_bindings.csv | 실제 port/flow/requirement, 접근 및 복귀 요구, 그 요구를 충족하는 route ID·방향 |
| route_cells.csv | route별 순서·좌표·from/to port·요구조건, clearance/support 종류·S/O 지지·원본 spine 참조 |
| state_geometry.csv | 원본 sealed/open gate 셀·조건·owner 연결; 일반 통행 예약과의 양립 여부 |
| reservation_manifest.json | plan/definition/source SHA·counts/digest, 원본과 예약의 역할, 실제 API, 필수 접근/복귀 충족 집계 |
| BINDING.json | 정확한 실행 Task/Read/Write/선행·현재 HEAD, 입력/명세/실행 SHA 역할과 정상 절차 |
| validation.json | 실제 focused 시험·정적 증거·write scope·cleanup·미실행 범위 |
| focused_results.xml | 실제 Unity focused 시험 결과. native 출력 이름이 다르면 실제 파일명/경로를 manifest에 연결 |

통행 여유·지지·state의 다중 의미가 같은 셀에 있으면 의미별 행 또는 명시적 연결을 허용한다. 이를 원본 셀 수와 혼동하지 않는다.
안정 정렬 기준과 digest 대상/encoding을 manifest에 명시한다. export 헤더는 위 의미를 모두 담도록 실제 모델에서 정하고 기록한다.
누락/충돌 경로는 해당 ID/좌표/원인으로 보고한다. 거부한 후보를 조용히 성공 출력에 포함하지 않는다.
필요한 그림은 예약 확인용만 허용하며 전체 지형이나 Scene을 새로 만들지 않는다.

## 다음 인계와 정리

정상 Result는 실제 MCP REPORTS 계약 위치에 쓰고, source/code/test/generated 출력의 SHA를 기록한다.
참조 검토용 원본 연결표는 MCP/GENERATED/SV5_03에 그대로 둔다. 이전 CSV를 수정하거나 손으로 재현하지 않는다.
이번 임시 자료는 MCP/GENERATED/SV5_04/_work/의 추적된 Task 소유 파일만 사용하고 Result 작성 전에 정리한다.
07_FILE_FLOW_V5에 따라 원본 입력/manifest·Task/Archive/Result·지속 증거는 보존한다. 프로젝트 루트에 보조 파일을 만들지 않는다.
Finalize 뒤 실제 상태와 commit은 콘솔로 보고한다. immutable Result를 commit SHA 때문에 amend하지 않는다.
다음 검토 자료는 MCP/GENERATED/SV5_04/SV5_04_REVIEW.zip 하나로 묶는다.
이 검토 ZIP에는 immutable Result의 동일 바이트 사본, 08_CORE_RESERVE 요약, 위 CSV/JSON/XML 및 실제 task-owned 새/수정 source/test를 상대 경로로 포함한다.
새 패치에 필요한 실제 API를 검토할 수 있도록 관련 SV5_03 BINDINGS/CORE_BINDINGS/DATA_SCHEMAS/TASK_COVERAGE의 동일 바이트 사본도 포함한다.
SOURCE_SNAPSHOT에 목록만 있는 무관한 소스·전체 repo·민감한 환경 파일은 포함하지 않는다.
검토 ZIP은 Finalize 후 생성 가능한 인계용 파생물이다. 과거 Result를 바꾸거나 이를 위해 추가 commit/amend하지 않는다.
ZIP 자기 자신/자기 hash를 내용에 포함하지 않고, 최종 ZIP SHA는 콘솔로 보고한다.
