# RUN MAP02_03

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS.md`, MAP02_02 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved `Generation` 폴더 안에 `WorldGridIndex`, immutable `SectorNeighborIndices`, immutable `GridInitializationResult`, stateless `GridInitializationPass`와 focused test를 구현하라. `(y * SectorColumns + x)`, 왼쪽 아래 원점, exact L/R/U/D, 월드 밖 `-1`, 169 unassigned cell을 exhaustive 검증하라.

새 directory/folder meta를 만들지 마. Assets meta `2954`와 legacy Editor folder meta `6/6`은 pre-task baseline이며 삭제하거나 drift로 오인하지 마. 신규 C# 5 + matching meta 5만 허용하고 final Assets meta `2959`, unexpected change `0`, duplicate GUID `0`을 확인하라.

focused >=48, MAP02_01 `56/56`, MAP02_02 `103/103`, targeted >=1074, full EditMode >=1094, compile/Console `0/0`, Authoring `50/50`을 모두 PASS하라. RNG, WorldGenerationRoot, pass record/retry, replay/file I/O, overlay는 구현하지 마.

전부 PASS일 때만 MAP02_03 COMPLETE/Current Task NONE으로 finalize하고 MAP02_04는 LOCKED로 유지하라.
