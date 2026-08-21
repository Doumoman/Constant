# RUN MAP03_06

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_06_IMPLEMENT_RESERVATION_BACKTRACKING.md`, MAP03_05 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 8개, `SiteReservationBacktrackerTests.cs` 1개와 matching meta 9개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

exact six groups `Start / Boss / Forge / Cassia / Deep Star Yeast / Moon Core`에서 각각 하나를 선택하라. input preflight 완료 뒤 canonical 3156 options마다 fresh `RNG_WORLD_SITE` draw를 정확히 한 번 배정하고, `(TotalCost, RandomTieBreak, OriginIndex, TransformOrdinal, CandidateOrdinal)` 순으로 평가하라. RNG는 equal-cost tie-break에만 사용한다. footprint/entry collision, MAP03_04 distance, MAP03_05 three-Core cluster hard signal을 거부하고, 후보 고갈 시 depth-first로 직전 선택까지 backtrack하라. failed combination 최대값은 exact `200`이다.

actual focused cases 최소 160개, MAP03_05 `270/270`, MAP03_04 `239/239`, MAP03_03 `170/170`, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=2702`, full EditMode `>=2742`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3036`, duplicate GUID `0`, exact Assets changes `18`, existing Assets modification `0`을 확인하라.

성공 결과는 exact six-step 잠정 `SiteReservationSelectionPlan`까지만 만든다. 실제 Core capacity flood, Village, final reservation/snapshot/ID publication, pass/root retry 실행, route/tile distance, `PASS_SITE`, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_06 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_07_IMPLEMENT_CORE_CAPACITY_FLOOD_CHECK`는 LOCKED로 유지하라.
