# RUN MAP02_01

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_01_IMPLEMENT_GENERATED_WORLD_DATA.md`, MAP01_17 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 `GeneratedSectorRole`, immutable `SectorCell`, exact 169-cell `GeneratedWorldData`, UTF-8 BOM/CRLF `generated_world_sectors.csv` 13열 serializer를 구현하라. 신규 focused >=32, targeted >=899, full EditMode >=919, compile error/warning 0/0, input/assembly/meta 보존을 모두 증명해라.

RNG, grid factory, `y*13+x`, neighbor, pass/root, seed manifest/replay, file I/O, overlay는 구현하지 마. 전부 PASS일 때만 MAP02_01 COMPLETE/Current Task NONE으로 finalize하고 MAP02_02는 LOCKED로 유지해라.
