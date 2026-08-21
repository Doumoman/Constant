# RUN MAP05_03

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE.md`, MAP05_02 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 8개, `MandatoryConnectorTreeBuilderTests.cs` 1개와 matching meta 9개만 추가하라. existing Assets/CSV/meta/asmdef/Scene/Prefab를 수정하지 마.

MAP05_01의 exact 7-terminal set과 MAP05_02의 exact 3-mask lookup을 입력으로 받아 terminal pair complete graph를 만들고, deterministic minimum connector tree candidate를 publish하라. 결과는 terminal-to-terminal edge 6개, connected/acyclic, all terminals covered, stable tie-break만 포함한다. sector path, L/R run, Type2/3 gateway, U/D conflict, loop, `MandatoryRouteGraph`, `SectorCell.RouteMaskId`, generated CSV는 만들지 마.

focused `>=118`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=939`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3179`, duplicate GUID `0`, exact Assets changes `18`, existing/unexpected `0/0`을 확인하라.

horizontal router/gateway/U-D conflict/loop/graph/generated CSV/validator/overlay/root를 구현하지 마. 전부 PASS일 때 MAP05_03 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_04_IMPLEMENT_HORIZONTAL_BACKBONE_ROUTER`는 LOCKED로 유지하라.
