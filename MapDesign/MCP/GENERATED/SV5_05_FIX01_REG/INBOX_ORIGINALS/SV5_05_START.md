# SV5_05_START — 진행 조건과 지름길 검사

MODE: design_plan_handoff_v1
TASK: SV5_05_ROUTE_STATE
PREVIOUS: SV5_04_CORE_RESERVE
PREVIOUS_RESULT_SHA256: e7f75abeae77693f2190a6cc58dee50fd27cf2ea3bbf589f37140ebc242b073a
PREVIOUS_TASK_ARCHIVE_SHA256: fd1f879e9f3d1ab302a1b2f7eb0b610227d6a7d9d86e7d43d2724978cb05f9a2
SOURCE_SPEC: MCP/INPUTS/SV5_05/SV5_05_ROUTE_STATE.md
SOURCE_SPEC_SHA256: 7f09d1564c90052b2e6e91bfee8a5857bf2c8712ebcac57fe5e891752d21086a

## 시작

실제 AGENTS/MCP, 07_FILE_FLOW, 08_CORE_RESERVE와 SOURCE_SPEC을 읽는다. START는 실행 Task가 아니다.
python -X utf8로 MCP/INPUTS/SV5_05/VERIFY.py와 manifest를 검사한다. 새 보조 파일은 모두 MCP 하위에 있다.
SV5_04 실제 Finalize/소유 commit을 확인한다. 아직 CURRENT면 같은 PASS Result/Task/Archive/focused 증거를 확인한 뒤 기존 승인된 마무리만 한다.
SV5_04 COMPLETE·Current NONE·SV5_05 LOCKED일 때 실제 선행 바이트/코드를 확인하고 별도 실행 Task로 바인딩한다.
이미 SV5_05 COMPLETE면 정상 증거를 보고하고 종료한다. 잘못된 SHA/다른 Current를 덮지 않는다.

## 수행

REVIEW_NOTES/REVIEW_FINDINGS를 읽는다. 기존 core gate는 보호 검사이며 route label만으로 진행 상태를 평가하지 않는다.
실제 RMAP13 graph/상태/action을 읽어 route/port 조건, 자원 6순서·복귀·정식 진행, 지름길 후보집합 검사를 구현한다.
경로 공유/맞닿음 접촉과 리뷰의 AIR witness를 실제 검사 입력에 포함한다. 논리 통과와 geometry/Player 증거 수준을 분리한다.
필요한 focused EditMode만 실행하고 같은 결과에서 trace/판정/해결 의무를 export한다.
Village 미연결 port와 조건 경계의 실제 지형 해결은 SV5_06/09/41 책임으로 명시하며 지금 완료로 바꾸지 않는다.

## 종료

정상 Result·Finalize·작업 소유 commit 후 OUTPUT_CONTRACT의 SV5_05_REVIEW.zip을 만들어 제공한다.
실제 최종 상태·역할별 SHA·commit·ZIP SHA를 콘솔에 보고하고 멈춘다.
기존 입력/CSV/Result를 보존하고 이번 임시파일을 정리한다. 루트 보조 파일/HANDOFF를 새로 만들지 않는다.
SV5_06 이후·RMAP18/19·VIS 후속·전체 회귀·PlayMode·build·Scene Bake·push는 진행하지 않는다.
