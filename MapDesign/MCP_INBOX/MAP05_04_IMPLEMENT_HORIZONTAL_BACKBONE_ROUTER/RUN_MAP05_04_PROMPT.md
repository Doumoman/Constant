# RUN MAP05_04

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER.md`, MAP05_03 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 8개, `HorizontalBackboneRouterTests.cs` 1개와 matching meta 9개를 추가하라. 기존 negative symbol audit 전환을 위해 `MandatoryRouteMaskLookupBuilderTests.cs`와 `MandatoryConnectorTreeBuilderTests.cs`만 제한적으로 수정할 수 있다. production/CSV/meta/asmdef/Scene/Prefab 기존 파일은 수정하지 마.

MAP05_03 connector tree의 6개 abstract edge를 입력으로 받아 each edge의 horizontal-only backbone segment 후보를 만들라. 각 segment는 source/target approach sector 사이에서 L/R run을 보존하는 deterministic sector sequence를 가진다. 같은 row는 straight run, 다른 row는 horizontal leg candidates만 만들고 vertical gateway는 placeholder endpoint로 남겨라.

수직 gateway, U/D conflict, loop, `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated CSV, validator, overlay, root는 구현하지 마. MAP05_04 산출물 심볼을 금지하던 prior negative audit는 MAP05_05+ 심볼 금지로만 전환하라.

focused `>=132`, MandatoryConnectorTreeBuilder `129/129`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=1082`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3188`, duplicate GUID `0`, exact Assets changes `18 new + 2 existing test C#`, production unexpected `0`을 확인하라.

전부 PASS일 때 MAP05_04 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_05_IMPLEMENT_VERTICAL_GATEWAY_PLANNER`는 LOCKED로 유지하라.
