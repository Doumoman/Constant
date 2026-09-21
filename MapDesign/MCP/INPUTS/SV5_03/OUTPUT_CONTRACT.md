# SV5_03 산출물 계약

이 문서의 CSV/JSON은 이번 조사 보고 형식이다. 게임 런타임의 새 schema/API를 만드는 요구가 아니다.
경로는 별도 표기가 없으면 MapDesign 기준이다. 코드 경로는 project 기준으로 기록하고 path_base로 구분한다.
모든 새 MD는 300줄 이하, CSV는 RFC4180·UTF-8, JSON은 UTF-8로 쓴다. 긴 표는 CSV/JSON으로 나눈다.

## 활성 문서

- MCP/SV5/06_BINDINGS_V5.md: 실제 코드/데이터의 진입점과 재사용 결정 요약. 아래 조사 파일로 연결한다.
- MCP/SV5/07_FILE_FLOW_V5.md: FILE_FLOW.md를 동일 바이트로 등록한다. 후속 패키지 배치/정리 방식을 지속 참조한다.
- MCP/SV5/02_PROTOCOL_V5.md: PROTOCOL_APPEND.md의 표식 블록을 한 번만 추가한다. 기존 바이트는 그대로 prefix로 보존한다.

## MCP/GENERATED/SV5_03/BINDINGS.csv

열: binding_id,audit_id,consumer_tasks,classification,path_base,source_path,source_sha256,symbol_or_key,signature_or_header,owner,reason,planned_change,evidence_status
REUSE는 실제 파일·타입/메서드 시그니처 또는 데이터 키와 소비 위치를 기록한다.
ADAPT는 실제 원소와 변경이 필요한 이유/후속 Task를 기록한다. 이 작업에서는 수정하지 않는다.
NEW는 실제 검색 근거와 기존 원소가 맡을 수 없는 이유를 기록한다. 존재하지 않는 API의 source_path/symbol 칸은 비운다.
NEW의 기존 확장 접점은 별도 REUSE/ADAPT 행으로 연결한다. 제안한 이름은 planned_change에만 PROPOSED로 쓴다.
consumer_tasks가 여러 개면 세미콜론으로 구분한다. evidence_status는 SOURCE_READ / DATA_READ / ABSENCE_SEARCH 등 관측 범위를 나타낸다.
기존 테스트 보고의 PASS는 현재 소스 실행 PASS를 뜻하지 않는다.

## MCP/GENERATED/SV5_03/DATA_SCHEMAS.csv

열: schema_id,path_base,source_path,source_sha256,format,header_or_type,key_fields,coordinate_frame,producer_binding,consumer_bindings,authority,notes
RMAP12~17의 실제 입출력, pattern/port/cell/protection/slot/graph/state 자료를 필요한 파일 단위로 기록한다.
CSV는 실제 header, JSON은 실제 필수 key/type, C# contract는 실제 선언과 immutable/version 의미를 적는다.
대용량 전체 셀 데이터를 이 보고서에 복제하지 않는다. path/SHA/키/범위와 필요한 집계로 참조한다.

## MCP/GENERATED/SV5_03/CORE_BINDINGS.csv

열: site_id,roles,graph_binding_ids,source_path,source_sha256,footprint_source,protected_cells_source,access_source,return_requirement,slots_source,state_geometry_source,consumer_gate_binding,notes
실제 RMAP15 정본의 8개 물리 site를 행으로 기록한다. Start·세 자원·Village·Forge·공유 Seal/Boss·Exit 역할을 누락하지 않는다.
공유 Seal/Boss의 하나인 물리 점유와 별도 진행 상태/graph 연결을 혼동하지 않는다. ID·graph 개수는 실제 정본에서 읽는다.
보호 셀은 실제 source/키/filter와 count를 notes에 연결한다. 기존 export가 없다면 실제 API/자료형의 읽기 근거를 명시한다.
예시 그림의 번호·좌표를 canonical site_id나 보호 셀로 대입하지 않는다.
정본이 현재 파일과 충돌하면 임의 선택하지 말고 원인과 후속 준비 불가를 기록한다.

## MCP/GENERATED/SV5_03/TASK_COVERAGE.csv

열: task_id,audit_ids,binding_ids,readiness,blocking_reason,next_reads
SV5_04~45의 실제 계획 ID 42개를 각 한 행씩 기록한다. READ_READY / NEEDS_IMPLEMENTATION / BLOCKED를 구분한다.
AUDIT_SCOPE의 담당 그룹마다 조사 결과가 있어야 한다. NEW는 해당 후속 구현의 필요성이며 조사 실패와 구별한다.
SV5_04~06은 실제 핵심 ID/소유/진행 조건과 데이터 접점이 확인되어야 READ_READY 또는 NEEDS_IMPLEMENTATION으로 판단할 수 있다.
이 표는 후속 실행 허가나 완료 표시가 아니다. 각 Task가 자기 시작 시점의 실제 파일을 다시 확인한다.

## MCP/GENERATED/SV5_03/SOURCE_SNAPSHOT.json

읽은 각 실제 파일의 path_base/path/raw SHA·역할·Git tracked/dirty 상태와 관측 시점을 기록한다.
현재 HEAD, 실제 SV5_02 Finalize/소유 commit, 로컬 Task/Archive/Result 경로와 SHA를 기록한다.
RMAP12~17·SPACE/SV4·VIS 이력의 존재/없음과 실제 증거 경로를 기록한다. 없던 Task를 등록하지 않는다.
NEW 판정에 쓴 rg 검색 root/pattern/결과 요약을 기록한다. 검색을 실행하기 전부터 없음으로 기록하지 않는다.
기존 소스의 역사적 SHA와 현재 SHA가 다르면 정상 변경 이력과 책임을 조사한다. 역사적 SHA로 현재 코드를 되돌리지 않는다.

## MCP/GENERATED/SV5_03/BINDING.json 및 validation.json

BINDING은 실제 실행 Task의 Read/Write/native Apply·Finalize 접점과 입력/명세/실행 Task SHA 역할을 구분한다.
validation은 참조 존재/SHA/헤더·symbol 실재, 13그룹/42Task 연결, 8site 정본 대응, 활성 문서 연결, 쓰기 범위를 기록한다.
source read와 runtime run을 구분하고 Unity/compile/test/build/bake/physics는 NOT_RUN으로 기록한다.
작업 전후 git diff와 read-only source snapshot을 대조한다. 기존 무관한 dirty 변경을 revert/commit하지 않는다.
cleanup 항목에는 이번 생성 임시파일의 정확한 경로·소유·삭제/보존 이유와 프로젝트 루트 새 보조 파일 0개를 기록한다.
과거 루트 파일의 보존/정리 후보 판정은 참조 근거와 함께 기록한다. 과거 증거의 경로/SHA를 조용히 바꾸지 않는다.
CSV/JSON 상호 참조의 유일 ID·존재를 스크립트로 검사한다. 보고 내용을 그대로 재현하는 Unity 테스트를 새로 만들지 않는다.

## Result와 최종 콘솔

정상 MCP Result 경로에 독립 TASK/STATUS와 모든 task-owned 지속 산출물의 실제 SHA를 적는다.
Result가 Finalize 전이면 그 시점의 Current/Status를 정확히 적는다. Finalize 이후 상태/commit은 콘솔로 보고한다.
Result를 amend하지 않는다. 임시파일은 Result 작성 전에 정리하고, 기록 파일 자체를 사후 삭제하지 않는다.
다음 인계에는 실제 코드 접점이 담긴 BINDINGS.csv/CORE_BINDINGS.csv/DATA_SCHEMAS.csv/TASK_COVERAGE.csv도 읽을 수 있게 제공한다.
