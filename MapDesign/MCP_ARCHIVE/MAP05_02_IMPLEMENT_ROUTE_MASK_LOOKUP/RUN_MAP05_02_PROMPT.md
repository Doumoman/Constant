# RUN MAP05_02

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP.md`, MAP05_01 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 8개, `MandatoryRouteMaskLookupBuilderTests.cs` 1개와 matching meta 9개만 추가하라. existing Assets/CSV/meta/asmdef/Scene/Prefab를 수정하지 마.

MAP01 typed `SectorRouteMaskDefinition`/`WorldRouteDefinitionSet.RouteMasks`에서 mandatory-allowed active Type1/2/3 rows만 읽어 exact lookup을 만든다. 승인 조합은 `ROUTE_T1_LR = L/R`, `ROUTE_T2_LRD = L/R/D`, `ROUTE_T3_LRU = L/R/U` 세 개뿐이다. Type0, inactive, mandatory_allowed false, duplicate route type/open mask, U+D 동시 개방, L/R 누락은 deterministic error로 거부하라.

focused `>=112`, MandatoryTerminalBuilder `120/120`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=806`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3170`, duplicate GUID `0`, exact Assets changes `18`, existing/unexpected `0/0`을 확인하라.

connector tree/router/gateway/U-D conflict/loop/graph/generated CSV/validator/overlay/root를 구현하지 마. 전부 PASS일 때 MAP05_02 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE`는 LOCKED로 유지하라.
