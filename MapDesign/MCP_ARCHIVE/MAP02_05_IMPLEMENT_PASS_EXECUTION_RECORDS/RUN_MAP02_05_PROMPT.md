# RUN MAP02_05

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS.md`, MAP02_04 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 injected UTC/monotonic clock, immutable attempt/pass/root execution records, retry count와 stable failure cause를 구현하라. 기존 `WorldGenerationRoot.cs` exact 1개만 instrumentation을 위해 수정하고 MAP02_04의 plan/artifact/retry/issue semantics, `WorldGenerationRootResult`, 기존 API를 보존하라.

clock/timing은 artifact, RNG, pass input, retry, success 판정에 사용하지 마. seed manifest/CSV/JSON/file I/O, replay, overlay는 구현하지 마. 새 directory/folder meta를 만들지 말고 Assets meta `2967`, accepted legacy folder meta `6/6`을 pre-task baseline으로 사용하라.

focused >=72, 기존 `56/103/90/84`, targeted >=1272, full EditMode >=1292, compile/Console `0/0`, Authoring `50/50`, final Assets meta `2973`, duplicate GUID `0`, exact Assets changes `13`을 모두 PASS하라.

전부 PASS일 때만 MAP02_05 COMPLETE/Current Task NONE으로 finalize하고 MAP02_06은 LOCKED로 유지하라.
