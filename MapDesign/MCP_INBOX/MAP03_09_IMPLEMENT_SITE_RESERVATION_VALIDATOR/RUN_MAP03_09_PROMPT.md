# RUN MAP03_09

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP03_09_IMPLEMENT_SITE_RESERVATION_VALIDATOR.md`, MAP03_08 PASS Result를 순서대로 읽어라.

Task의 READ/WRITE ALLOWLIST와 frozen contract를 그대로 지켜 validation rule/violation/diagnostics/publication/error/result, internal snapshot publisher, validator production C# exact 8개, `SiteReservationValidatorTests.cs` exact 1개와 matching meta만 만든다. 기존 Assets/CSV/asmdef/Scene/Prefab은 수정하지 마.

MAP03_08 `VillageReservationApproval`과 exact six active special-map definitions, seven footprint cells, six entry sockets를 입력으로 사용한다. 구조 preflight 뒤 exact six validation rules `RequiredSiteCounts / WorldBounds / FootprintOverlap / DistanceConstraints / EntryAnchors / CoreCapacity`를 모두 평가한다. PASS일 때만 order `Start/Boss/Forge/Cassia/Yeast/Meteor/Village`, `RSV_{order:D2}_{sourceId}` ID, 169-sector table, exact six entry anchors, exact four Core seeds를 가진 `SiteReservationSnapshot`을 한 번에 publish한다. 실패 시 plan/Village/witness를 고치거나 RNG를 소비하지 말고 whole `PASS_SITE` retry-required를 반환한다.

actual focused cases 최소 260개, MAP03_08 `339/339`, MAP03_07 `215/215`, MAP03_06 `248/248`, MAP03_05 `270/270`, MAP03_04 `239/239`, MAP03_03 `170/170`, MAP03_02 `268/268`, MAP03_01 `81/81`, MAP02 phase `667/667`, SpecialVillage `57/57`, BiomeBoundary `38/38`, StaticRegistry `53/53`, ContentVersionHash `54/54`, Game.Map targeted `>=3604`, full EditMode `>=3644`, failed/skipped `0/0`을 실행하라. compile/Console `0/0`, Authoring CSV/meta `50/50`, final Assets meta `3063`, duplicate GUID `0`, exact Assets changes `18`, existing Assets modification `0`을 확인하라.

성공 결과는 source approval identity + final snapshot을 가진 `SiteReservationPublication`까지만 만든다. serializer/file I/O, generated_special_sites rows, patch instance ID, biome painting/growth, overlay, batch runner, pass/root retry 실행을 구현하지 마. 전부 PASS일 때 MAP03_09 COMPLETE/Current Task NONE으로만 finalize하고 `MAP03_10_CREATE_SITE_RESERVATION_OVERLAY`는 LOCKED로 유지하라.
