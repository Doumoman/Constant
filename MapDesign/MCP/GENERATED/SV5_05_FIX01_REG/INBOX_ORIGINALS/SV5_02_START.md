# SV5_02_START — SV5 규칙 등록

MODE: design_plan_handoff_v1
TASK: SV5_02_RULES
PREVIOUS: SV5_01_APPROVAL_BASELINE
PREVIOUS_RESULT_SHA256: 78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6
PREVIOUS_TASK_ARCHIVE_SHA256: c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed
SOURCE_SPEC: MCP/INPUTS/SV5_02/SV5_02_RULES.md
SOURCE_SPEC_SHA256: 47de1a12a07e7c76ce0156cea451a36129161aa11c17b569fef47389315a52e0

이 INBOX는 인계문이다. 실제 현지 single_task_v1 실행 Task는 별도 바인딩한다.
45개 계획은 이미 등록되었다. 기존 등록을 반복하거나 45행을 다시 추가하지 않는다.

## 시작

1. 프로젝트 루트 SV5_02_README.md의 외부 SHA를 기준으로 새 패키지를 검사한다.
2. Windows에서는 python -X utf8를 사용한다. 기존 SV5_VERIFY.py나 첫 패키지 바이트를 수정하지 않는다.
3. 현지 AGENTS/MCP와 02_PROTOCOL_V5를 읽고 Current·Status·SV5_01 Finalize·실제 commit을 확인한다.
4. 실제 SV5_01 Result/설치/Archive와 이 패치 SOURCE_LOCK의 SHA를 대조한다. reference 보고서는 현지 Result 대체물이 아니다.
5. SV5_02가 이미 완료되었으면 입력·증거를 확인해 재사용하고 종료한다. 새 실행이면 정상 선행과 LOCKED 상태를 확인한다.
6. SOURCE_SPEC을 읽고 실제 Read/Write·선행·정상 등록/Apply/Finalize 접점을 바인딩한다.

## 이번 실행

- MCP/SV5에 활성 규칙 인덱스·문장별 Coverage·Task별 ReadSet을 등록한다.
- 기존 02_PROTOCOL_V5에서 새 인덱스로 읽기 경로를 한 번만 연결한다.
- 승인된 전체 구성·JUMP-01~14·이동 한계·기존 장소 계약을 원문과 같은 의미로 유지한다.
- 제작 기본값·미검증 간격·미지정 고체 비율을 사용자 확정치나 Player PASS로 바꾸지 않는다.
- 원문 INPUTS/SV5, 00_APPROVAL_BASELINE, 01_SEQUENCE와 기존 Task/Archive/Result를 보존한다.

## 종료

SV5_02만 정상 Apply·수행·Finalize하고 이 작업 소유 변경만 현지 commit 절차로 처리한다.
INBOX·원본 명세·bound·installed·archive 각각의 SHA, 실제 전후 상태와 commit을 보고한다.
Result가 Finalize 이전에 쓰이면 그 시점을 정확히 구분하고 최종 콘솔 응답에서 실제 Finalize/commit을 보고한다.
SV5_03 이후·RMAP18/19·Unity 실행·게임 코드 수정·전체 테스트/맵 Bake·push는 진행하지 않는다.
