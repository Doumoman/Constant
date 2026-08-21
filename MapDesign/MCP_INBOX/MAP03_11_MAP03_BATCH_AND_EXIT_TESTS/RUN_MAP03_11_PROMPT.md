# RUN MAP03_11

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_11_MAP03_BATCH_AND_EXIT_TESTS.md`, MAP03_01~10 PASS Results, MAP02_08 approved baseline을 순서대로 읽어라.

Task의 READ/WRITE ALLOWLIST와 frozen contract를 그대로 지켜 `Map03ExitTests.cs` exact 1개와 matching meta 1개만 만든다. existing MAP03 production/test, Assets/CSV/asmdef/Scene/Prefab은 수정하지 마.

existing public APIs로 seed-independent `933 / 3468 / 3156 / 312 / 6 / 15` fixture를 준비하고, full attempt를 `fresh RNG_WORLD_SITE -> backtracker -> capacity -> Village using continued stream -> validator/publication` 순서로 실행한다. 100,000 seeds `0..99,999`의 attempt-0 bucket schedule을 exact 3156 draws 뒤 집계해 `20/50/30`, ±0.75 percentage point, chi-square `<=13.815511`을 검사한다. seeds `0..9,999` full pipeline은 initial retry `<=5%`, maximum 8 test-observation attempts, invalid/unresolved `0`, resolved `10000/10000`, final bucket `20/50/30` ±2 points, terminal/reason conservation을 검사한다.

actual focused 최소 `96`, existing MAP03 `2259/2259`, MAP02 `667/667`, SpecialVillage/BiomeBoundary/StaticRegistry/ContentHash `57/57 / 38/38 / 53/53 / 54/54`, Game.Map targeted `>=3841`, full EditMode `>=3909`, failed/skipped `0/0`을 실행하라. current-project visual `18/18`, compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3071`, duplicate GUID `0`, exact Assets changes `2`, existing Assets modification `0`을 확인하라.

전부 PASS일 때 Result에 exact `MAP03 EXIT: APPROVED`, `MAP04 ENTRY: ELIGIBLE FOR SEPARATE PATCH`, `MAP04_01: LOCKED / DO NOT START`를 기록한다. MAP03_11 COMPLETE/Current Task NONE으로만 finalize하고 MAP04_01은 LOCKED로 유지하라.
