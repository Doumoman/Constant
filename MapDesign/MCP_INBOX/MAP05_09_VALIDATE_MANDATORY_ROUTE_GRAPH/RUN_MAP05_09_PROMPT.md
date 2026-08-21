# RUN MAP05_09

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_09_VALIDATE_MANDATORY_ROUTE_GRAPH.md`, MAP05_08 PASS Result를 순서대로 읽어라.

Prior result는 `MapDesign/MCP/REPORTS/MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH_RESULT.md`이며 SHA-256은 exact `7c9820290ec5269222b8c145603a9ae53a2ea7f8d1df7b0ca6029e1be3647a99`다. 이 값이 다르면 Phase A에서 `BLOCKED`하고 변경하지 마.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 8개, `MandatoryRouteGraphValidatorTests.cs` 1개와 matching meta 9개를 추가하라. 기존 tests 8개는 MAP05_09 output symbols를 허용하고 MAP05_10+ symbols는 계속 금지하도록만 전환하라. 기존 production/CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings는 수정하지 마.

MAP05_08의 final graph를 검증하라. graph nodes `47`, directed edges `96`, route cells `47`, terminals reachable `7/7`, accepted loops represented `2/2`, generated sectors CSV bytes `16838`, generated edges CSV bytes/rows `7094/96`을 starter vector로 사용한다.

Type4 규칙을 반드시 지켜라. Type4는 U+D 필수이며 L/R은 actual graph adjacency에서 복사된 상태로 검증한다. `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 합법이다. L/R canonicalization, forced open/close, graph repair, CSV rewrite, `SectorCell` mutation은 금지한다.

focused `>=240`, MandatoryRouteGraphValidator `240/240` 이상, MandatoryRouteGraphBuilder `281/281`, MandatoryRouteLoopPlanner `212/212`, UpDownConflictResolver `194/194`, VerticalGatewayPlanner `156/156`, HorizontalBackbone `142/142`, MandatoryConnectorTreeBuilder `129/129`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, GeneratedWorldData `56/56`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=2231`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3238`, duplicate GUID `0`, exact Assets changes `26`, unexpected changes `0`을 확인하라.

전부 PASS일 때 MAP05_09 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_10_CREATE_MANDATORY_ROUTE_OVERLAY`는 LOCKED로 유지하라.
