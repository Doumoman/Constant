# RUN MAP03_08

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_08_IMPLEMENT_VILLAGE_RESERVATION.md`, MAP03_07 PASS Result를 순서대로 읽어라.

Task의 READ/WRITE ALLOWLIST와 frozen contract를 그대로 지켜 `VillageDistanceBucket`, candidate/diagnostics/rejection/error/selection/result/selector production C# exact 8개, `VillageReservationSelectorTests.cs` exact 1개와 matching meta만 만든다. 기존 Assets/CSV/asmdef/Scene/Prefab은 수정하지 마.

MAP03_07 `CoreCapacityApproval`, exact active `VIL_MOON_PRIMARY`, `SITE_PRIMARY_VILLAGE`, profile allowed active layouts와 continued `RNG_WORLD_SITE`를 입력으로 사용한다. bucket은 `NextInt(100)`으로 먼저 한 번 선택하고, 그 bucket에서 viable layout만 exact selection weight로 고른 뒤 canonical candidate pool에서 unbiased `NextInt(count)`로 하나를 고른다. candidate는 row-major origin, layout ID ordinal, entry side `L/R/U/D` order이며 occupied overlap, existing/prospective entry approach 충돌, exact Start distance, other-site minimum distance, four Core witness sector 침범을 거부한다. 선택 bucket이 비면 다른 bucket으로 fallback/redraw하지 말고 whole `PASS_SITE` retry-required를 반환한다.

actual focused cases 최소 220개, MAP03_07 `215/215`, MAP03_06 `248/248`, MAP03_05 `270/270`, MAP03_04 `239/239`, MAP03_03 `170/170`, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=3225`, full EditMode `>=3265`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3054`, duplicate GUID `0`, exact Assets changes `18`, existing Assets modification `0`을 확인하라.

성공 결과는 original `CoreCapacityApproval`과 one immutable Village selection을 가진 `VillageReservationApproval`까지만 만든다. 내부 layout cell/facility, CoreBiomeSeed, reservation ID/final snapshot, biome painting/growth, local repair, pass/root retry 실행, serializer/file I/O를 구현하지 마. 전부 PASS일 때 MAP03_08 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR`는 LOCKED로 유지하라.
