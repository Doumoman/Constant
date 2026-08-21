# RUN MAP02_04

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_04_IMPLEMENT_WORLD_GENERATION_ROOT.md`, MAP02_03 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST로 failure policy, immutable artifact store/pass context/result/registry, `GridInitializationPassAdapter`, `WorldGenerationRootResult`, `WorldGenerationRoot`와 focused tests를 구현하라. typed `GenerationPassDefinition`의 order/input/output/policy/retry를 실행하고 output은 success에서만 atomic commit하라.

production에서는 actual `PASS_GRID` prefix를 실행하되 아직 없는 9개 pass를 stub/skip하지 마. full execution은 preflight missing implementation으로 invocation 0이어야 하며, focused test 안의 explicit fakes로만 exact 10-pass order와 네 failure policy를 검증하라.

신규 C# 8 + matching meta 8만 허용한다. 새 directory/folder meta 없이 final Assets meta `2967`, accepted folder meta `6/6`, Authoring `50/50`, unexpected change `0`, duplicate GUID `0`을 확인하라.

focused >=64, MAP02_01 `56/56`, MAP02_02 `103/103`, MAP02_03 `90/90`, targeted >=1180, full EditMode >=1200, compile/Console `0/0`을 모두 PASS하라. timing/records, replay/file I/O, 후속 pass 알고리즘은 구현하지 마.

전부 PASS일 때만 MAP02_04 COMPLETE/Current Task NONE으로 finalize하고 MAP02_05는 LOCKED로 유지하라.
