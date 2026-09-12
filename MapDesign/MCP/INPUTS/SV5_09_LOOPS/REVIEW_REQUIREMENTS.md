# SV5_09 Review requirements

Review ZIP은 최종 commit에서 다시 추출하며 `_REVIEW_MANIFEST.json`에 commit/parent와 각 entry의 Git blob OID,
blob SHA/bytes를 기록한다. 다음 항목은 반드시 포함한다.

- Task, Archive, Result, Status, active rule doc, protocol, 이번 INPUTS 전부.
- 변경한 runtime/test 소스와 meta.
- BINDING, focused XML, independent audit, default/repeat의 필수 export, 비교 지표와 SVG/HTML preview.
- `git diff-tree` task-owned 목록과 source-lock/post-readonly 요약.

ZIP 자신, `_work`, Library/Temp/Logs, 이전 Generated 복제, unrelated dirty는 제외한다.
Result에는 `TASK: SV5_09_LOOPS`, `TASK_ID: SV5_09_LOOPS`, 실제 `STATUS:`를 독립 행으로 쓴다.
PASS 시 accepted/sector/max length/loop/shortcut/reject counts, cycle delta, bypass0, XML 수치/SHA, commit/parent/ZIP SHA를 기록한다.
