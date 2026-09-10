# SV5_04_START — 핵심 구역·접근·복귀 선예약

MODE: design_plan_handoff_v1
TASK: SV5_04_CORE_RESERVE
PREVIOUS: SV5_03_BINDINGS
PREVIOUS_RESULT_SHA256: 0b6f9c0a8ba658de8e911781f3b5a3e614c27333ff25faf3c63720e7486ae422
PREVIOUS_TASK_ARCHIVE_SHA256: b8f6f5865dae26d512a50a3f62061c1370daec8bc12e9c56578ebda3565677b3
SOURCE_SPEC: MCP/INPUTS/SV5_04/SV5_04_CORE_RESERVE.md
SOURCE_SPEC_SHA256: 658aa2ccf67d1f4c3a4507cd4b1521d0fc56dc414fd946636410f682e4b54d3c

## 시작

실제 AGENTS/MCP, 06_BINDINGS_V5, 07_FILE_FLOW_V5와 원본 명세를 읽는다.
보조 파일은 MCP/INPUTS/SV5_04 아래에 있다. python -X utf8 VERIFY.py --manifest-sha로 새 패키지를 검사한다.
SV5_03 실제 Finalize/소유 commit을 확인한다. 아직 CURRENT면 동일 PASS Result/Task/Archive를 검증하고 기존 승인된 마무리만 한다.
SV5_03 COMPLETE·Current NONE·SV5_04 LOCKED에서 실제 선행 바이트와 현지 연결표를 읽고 별도 실행 Task를 바인딩한다.
이미 SV5_04 COMPLETE면 정상 증거를 보고하고 종료한다. SHA 불일치나 다른 Current를 임의 변경하지 않는다.
이 START는 인계문이며 source_spec 자체를 이름만 바꿔 Apply하지 않는다.

## 수행

SV5_04 한 개만 정상 Apply한다. 기존 핵심 site·보호 셀과 접근/복귀 예약의 실제 소비 API를 구현한다.
대표 정본 8개 site/2,432셀의 ID/좌표/소유/port/slot/state를 보존하고 같은 plan에서 파생 자료를 출력한다.
기존 route 자료를 읽어 연속 경로·clearance/support·방향/조건을 예약한다. 종점 목록만으로 복귀 완료를 표시하지 않는다.
SV5_04에 필요한 focused EditMode로 허용/침범/실패/결정성/출력 대응을 검사한다.
실제 Player 통과나 전체 월드 Scene 완료는 주장하지 않는다.

## 종료

임시 자료를 정리하고 정상 Result·Finalize·작업 소유 commit 후 실제 상태/역할별 SHA/commit을 콘솔로 보고한다.
OUTPUT_CONTRACT에 따라 Result·자료·실제 source/test·관련 SV5_03 연결표를 SV5_04_REVIEW.zip으로 묶어 제공한다.
SV5_05 이후·RMAP18/19·VIS 후속·전체 회귀·PlayMode·build·Scene Bake·push는 진행하지 않는다.
