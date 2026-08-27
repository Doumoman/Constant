# RUN MAP05_06

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_06_RESOLVE_UP_DOWN_CONFLICTS.md`, MAP05_05 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 8개, `UpDownConflictResolverTests.cs` 1개와 matching meta 9개를 추가하라. 기존 negative symbol audit 전환을 위해 `MandatoryRouteMaskLookupBuilderTests.cs`, `MandatoryConnectorTreeBuilderTests.cs`, `HorizontalBackboneRouterTests.cs`, `VerticalGatewayPlannerTests.cs`만 제한적으로 수정할 수 있다. production/CSV/meta/asmdef/Scene/Prefab 기존 파일은 수정하지 마.

MAP05_05 `VerticalGatewayPlan`을 입력으로 받아 Type4로 표현되지 않는 U/D 충돌만 deterministic하게 resolution pair로 분리하라. Type4 셀은 U+D가 보장되면 충돌이 아니며 L/R은 실제 horizontal adjacency를 그대로 보존하라. resolution과 diagnostics는 source gateway/route-mask/site/biome identity를 보존하라.

loop, `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated CSV, validator, overlay, root는 구현하지 마. Type4는 U/D를 반드시 true로 하고 L/R은 실제 수평 연결 상태를 보존하는 mask-family semantic으로 취급하라. MAP05_06 산출물 심볼을 허용하되 MAP05_07+ 심볼은 계속 금지하라.

focused `>=150`, UpDownConflict `150/150` 이상, VerticalGatewayPlanner `156/156`, HorizontalBackbone `142/142`, MandatoryConnectorTreeBuilder `129/129`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=1398`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3206`, duplicate GUID `0`, exact Assets changes `18 new + 4 existing test C#`, production unexpected `0`을 확인하라.

전부 PASS일 때 MAP05_06 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_07_ADD_MANDATORY_ROUTE_LOOPS`는 LOCKED로 유지하라.
