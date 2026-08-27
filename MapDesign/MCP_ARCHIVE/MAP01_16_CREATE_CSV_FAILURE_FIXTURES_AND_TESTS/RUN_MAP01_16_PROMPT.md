# RUN MAP01_16

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`에서 시작해 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP01_16_CREATE_CSV_FAILURE_FIXTURES_AND_TESTS.md`, MAP01_15 PASS Result를 순서대로 읽어라.

실제 Authoring CSV/meta와 production pipeline은 read-only로 보존하고, test-owned temporary copy에서 exact failure fixtures를 만들어 duplicate PK, invalid enum/number, missing single/list FK, BOM, header/row ordering, compound errors, previous Registry 보존, recovery를 검증하라.

신규 focused >=20, targeted >=784, full EditMode >=804, compile/warning 0/0 및 CSV/meta 50/50 unchanged를 증명하라. 모든 조건 PASS일 때만 MAP01_16 COMPLETE/Current Task NONE으로 finalize하고 MAP01_17은 LOCKED로 유지하며 시작하지 마.
