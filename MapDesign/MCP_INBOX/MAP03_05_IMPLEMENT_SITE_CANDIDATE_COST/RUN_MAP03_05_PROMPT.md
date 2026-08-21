# RUN MAP03_05

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_05_IMPLEMENT_SITE_CANDIDATE_COST.md`, MAP03_04 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 6개, `SiteCandidateCostTests.cs` 1개와 matching meta 7개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

placement option 하나마다 exact default weights `10/25/1000/100/10000`로 altitude/edge/distance/capacity forecast/three-Core cluster cost를 계산하라. default aggregate reference는 `2/1/1/1/1 units -> 11145`다. distance deficit과 4×4 three-Core cluster만 hard signals이며 altitude/edge/capacity forecast는 soft다. capacity `-1`은 unavailable이며 실제 flood hard gate를 대신하지 않는다.

actual focused cases 최소 128개, MAP03_04 `239/239`, MAP03_03 `170/170`, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=2400`, full EditMode `>=2440`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3027`, duplicate GUID `0`, exact Assets changes `14`, existing Assets modification `0`을 확인하라.

후보 목록 정렬·RNG·선택, reservation publication, backtracking/retry, actual Core flood, Village, route/tile distance, `PASS_SITE`, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_05 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING`은 LOCKED로 유지하라.
