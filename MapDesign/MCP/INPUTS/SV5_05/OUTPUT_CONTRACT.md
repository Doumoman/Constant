# SV5_05 출력과 다음 소비자 계약

모든 module 경로는 MapDesign 기준이다. runtime source 경로는 PROJECT 기준으로 구분한다.
CSV는 UTF-8 RFC4180, JSON은 UTF-8, MD는 각 300줄 이하. 아래 파일명은 generated 보고 형식이며 runtime 타입명을 강제하지 않는다.

## 활성 진입점

MCP/SV5/09_ROUTE_STATE_V5.md에 실제 API/상태 소유/입출력/논리·물리 검증 수준·거부 이유·SV5_06/09/41 호출 의무를 적는다.
기존 protocol/08_CORE_RESERVE/SV5_03 조사 CSV는 수정하지 않는다. 다음 Task의 명시적 Read로 새 결과를 소비한다.

## MCP/GENERATED/SV5_05/

| 파일 | 내용 |
|---|---|
| condition_bindings.csv | route/edge/logical node/physical port와 실제 predicate/action/flow의 대응; label은 별도 열 |
| order_proofs.csv | 6개 정확한 순서별 성공/실패·정식 action 및 복귀 요약·trace 참조 |
| state_traces.csv | proof/candidate/state ID, step ordinal, node/edge/action, state before/after, predicate 결과 |
| shortcut_decisions.csv | 후보와 전체 후보집합의 논리 허용/거부·진단·필수 action 보존·geometry 검증 수준 |
| contact_checks.csv | overlap/face contact·관련 route/좌표/조건/state·분류·실제 guard 근거·counterexample 참조 |
| obligations.csv | 미연결 port/미해결 contact/physical gate·geometry 대응을 정확한 owner Task·issue ID·readiness로 기록 |
| route_state_manifest.json | source/core/graph digest, 실제 API와 condition schema, 6순서·후보/접촉 집계, LOGICAL/GEOMETRY/PLAYER 수준 분리 |
| BINDING.json | 실제 Read/Write/API/테스트/선행 finalize commit·현재 HEAD와 각 역할 SHA |
| validation.json | 실제 focused XML/정적 참조·원본 보존·diff·cleanup·negative witness 확인·미실행 범위 |
| focused_results.xml | 실제 focused 결과; native 파일명이 다르면 그 이름/경로를 manifest에 명시 |

상태 flag·resource cursor의 CSV 인코딩은 기존 상태 타입에 맞춰 명시한다. 되돌릴 수 없는 요약 문자열만 남기지 않는다.
raw AIR witness는 진단용 별도 trace로 표기하고 논리 completion trace나 Player trajectory와 혼합하지 않는다.
각 출력은 같은 검사 결과 객체에서 만든다. 수작업 PASS 행으로 검증을 대신하지 않는다.

## 완료 수준과 후속 의무

이번 Result의 PASS는 논리 상태/후보·접촉 검사 구현 및 요구된 focused 검증 PASS다.
baseline의 physical readiness가 미확인/미해결이면 manifest에 false/PENDING과 근거를 기록해도 된다. 이를 숨기거나 실제 geometry PASS라고 하지 않는다.
SV5_06/09/41은 obligations가 가리키는 상태 경계/미연결 port를 해결하고 자기 최종 geometry에서 다시 검사해야 한다.
Village 두 port는 실제 선택적 접근/복귀를 SV5_06에 인계한다. Start 미선택 EXIT는 사용/미사용 설계를 명시한다.
후속 완료 gate에 필요한 issue마다 owner·해결 판정·검증 방법을 적어 단순 메모로 사라지지 않게 한다.

## 정리·Finalize·검토 ZIP

MCP/SV5/07_FILE_FLOW_V5.md를 따른다. 보조 파일은 MCP 하위, 임시 자료는 이번 _work 아래 추적된 소유 파일만 사용하고 Result 전에 정리한다.
immutable 입력/manifest·기존 Task/Archive/Result/CSV는 보존한다. 루트 CHECK/VERIFY/FILES/HANDOFF를 새로 만들지 않는다.
Result에는 실제 task-owned source/test/generated의 SHA와 변경 전/후 역할을 적는다. Finalize 전이면 그 상태를 정확히 표시한다.
Finalize 후 실제 상태/commit은 콘솔로 보고한다. 그 SHA를 넣으려고 Result를 amend하지 않는다.
마지막에 MCP/GENERATED/SV5_05/SV5_05_REVIEW.zip을 만들고 최종 ZIP SHA를 콘솔에 적는다. ZIP 자체를 위해 추가 commit/amend하지 않는다.
ZIP에는 같은 Result, 09_ROUTE_STATE 요약, 위 CSV/JSON/XML, 실제 task-owned source/test와 관련 .meta를 담는다.
이번 API 해석에 필요한 RmapWorldGraphPlanner.cs와 GeneratedCompletionSearch.cs도 실제 읽은 버전의 동일 바이트 사본으로 포함한다.
SV5_04 adapter는 그 source/SHA를 검토자가 대응할 수 있게 포함한다. 전체 repo/환경/무관한 소스를 묶지 않는다.
각 복사 source의 project 상대 경로와 SHA를 목록에 기록한다. ZIP 자기 자신/자기 hash는 내용에 넣지 않는다.
