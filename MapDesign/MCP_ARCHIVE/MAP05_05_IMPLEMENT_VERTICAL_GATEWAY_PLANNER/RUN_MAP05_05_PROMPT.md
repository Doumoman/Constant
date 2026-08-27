# RUN MAP05_05

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER.md`, MAP05_04 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 8개, `VerticalGatewayPlannerTests.cs` 1개와 matching meta 9개를 추가하라. 기존 negative symbol audit 전환을 위해 `MandatoryRouteMaskLookupBuilderTests.cs`, `MandatoryConnectorTreeBuilderTests.cs`, `HorizontalBackboneRouterTests.cs`만 제한적으로 수정할 수 있다. production/CSV/meta/asmdef/Scene/Prefab 기존 파일은 수정하지 마.

MAP05_04 horizontal plan의 4개 pending row transition을 입력으로 받아 각 segment에 same-column upper Type2.D/lower Type3.U gateway pair 하나와 eligible interior Type4(U/D 필수, L/R 선택적·실제 상태 보존) junction cell 목록을 deterministic하게 만들라. pair/span/junction은 checked cost와 source segment/route-mask/site/biome identity를 보존하라.

U/D conflict 해소, loop, `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated CSV, validator, overlay, root는 구현하지 마. Type4는 U/D를 반드시 true로 하고 L/R은 실제 수평 연결 상태를 보존하는 planner-owned semantic output으로만 기록하며 completed MAP05_02 lookup을 수정하지 마. MAP05_05 산출물 심볼을 금지하던 prior negative audit는 MAP05_06+ 심볼 금지로만 전환하라.

focused `>=148`, HorizontalBackbone `142/142`, MandatoryConnectorTreeBuilder `129/129`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=1098`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3197`, duplicate GUID `0`, exact Assets changes `18 new + 3 existing test C#`, production unexpected `0`을 확인하라.

전부 PASS일 때 MAP05_05 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_06_RESOLVE_UP_DOWN_CONFLICTS`는 LOCKED로 유지하라.
