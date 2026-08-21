# RUN MAP01_15 REMEDIATION v1.1

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`에서 시작해 patch/change/finalize 규칙, Master, Status, 현재 `TASKS/MAP01_15_CREATE_CSV_IMPORT_WINDOW.md`, 기존 BLOCKED Result, `REPORTS/CsvImportReport.json`을 순서대로 읽어라.

Current Task identity는 `MAP01_15_CREATE_CSV_IMPORT_WINDOW` 그대로다. Task 하단의 `BLOCKED Remediation v1.1 — Authoritative Schema Contracts`를 실행하라. authoritative dictionary와 fixed CSV/meta 50개는 읽기 전용으로 보존하고, exact builder/parser 계약과 허용된 tests만 수정하라. 25개 issue를 숨기거나 우회하지 말고 직접 원인 19개를 수정한 뒤 full pipeline을 실실행하라.

최종 report/test/Unity/visual/evidence가 모두 PASS일 때만 기존 MAP01_15 Result를 갱신하고 MAP01_15 COMPLETE, Current Task NONE으로 finalize하라. 실패하면 MAP01_15 CURRENT/BLOCKED를 유지하라. MAP01_16은 LOCKED이며 시작하지 마.
