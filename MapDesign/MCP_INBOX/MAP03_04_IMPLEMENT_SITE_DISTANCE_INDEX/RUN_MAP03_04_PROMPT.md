# RUN MAP03_04

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX.md`, MAP03_03 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 7개, `SiteDistanceIndexTests.cs` 1개와 matching meta 8개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

distance는 두 placement occupied cells 사이 P00 L/R/U/D graph 최단 간선 수이며 exact Manhattan minimum이다. canonical closest-cell tie-break와 pair O(1) lookup을 구현하라. typed definitions로 Start 1 + Boss/Forge/Core 3의 exact six keys와 15 constraints를 만들고 distribution `minimum 2×5 / 3×9 / 4×1`을 검증하라. complete set evaluation과 partial pair lookup을 분리하라.

actual focused cases 최소 128개, exhaustive sector pairs `28561`, MAP03_03 `170/170`, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=2161`, full EditMode `>=2201`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3020`, duplicate GUID `0`, exact Assets changes `16`, existing Assets modification `0`을 확인하라.

cost/altitude/edge/quadrant penalty, RNG, option selection, reservation publication, backtracking/retry, Core capacity, Village bucket/layout, route-aware/tile movement distance, `PASS_SITE`, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_04 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST`는 LOCKED로 유지하라.
