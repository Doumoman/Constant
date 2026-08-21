# RUN MAP03_03

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_03_IMPLEMENT_FOOTPRINT_PLACEMENT_SOLVER.md`, MAP03_02 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 기존 approved Runtime/Test `Generation` 폴더에 production C# 6개, `FootprintPlacementSolverTests.cs` 1개와 matching meta 7개만 추가하라. existing production/tests/meta/asmdef/CSV/Scene/Prefab를 수정하지 마.

candidate 하나 + transform 하나만 판정하라. `R0/MirrorX/MirrorY/R180`의 coordinate와 side를 footprint cells, required-open-sides, entry sockets에 동일 적용하고, Phase 2 footprint world-bound/overlap/protected-entry-approach 뒤 Phase 3 candidate entry exterior를 검사하라. Start는 synthetic 1×1 R0/entry 0이다. empty blockers starter exact matrix는 `3468 evaluated / 3156 success / 312 rejected`, breakdown `FootprintOutsideWorld 52 / EntryOutsideWorld 260 / other 0`이다.

actual focused cases 최소 96개, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=1959`, full EditMode `>=1999`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3012`, duplicate GUID `0`, exact Assets changes `14`, existing Assets modification `0`을 확인하라.

거리/고도/cost/weight, RNG, option 선택, reservation ID/order/snapshot, backtracking/retry, Core capacity, Village, `PASS_SITE`, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_03 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_04_IMPLEMENT_SITE_DISTANCE_INDEX`는 LOCKED로 유지하라.
