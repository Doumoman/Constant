# RUN MAP05_08

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP05_08_BUILD_MANDATORY_ROUTE_GRAPH.md`, MAP05_07 PASS Result를 순서대로 읽어라.

Prior result는 `MapDesign/MCP/REPORTS/MAP05_07_ADD_MANDATORY_ROUTE_LOOPS_RESULT.md`이며 SHA-256은 exact `cbe4f9a136d488df134a6eee676e13950d5dfd15238abf3188a81ce532fbdf65`다. 이 값이 다르면 Phase A에서 `BLOCKED`하고 변경하지 마.

Task의 exact READ/WRITE ALLOWLIST를 지켜 approved Runtime/Test `Generation` 폴더에 production C# 13개, `MandatoryRouteGraphBuilderTests.cs` 1개와 matching meta 14개를 추가하라. `SectorCell.cs`, `GeneratedWorldData.cs`, `GeneratedWorldDataCsvSerializer.cs`는 route stamp/serializer 보존에 필요한 최소 변경만 허용한다. 기존 tests 7개는 MAP05_08 output symbols를 허용하고 MAP05_09+ symbols는 계속 금지하도록만 전환하라. Authoring CSV/meta/asmdef/Scene/Prefab/Package/ProjectSettings는 수정하지 마.

MAP05_07의 loop plan까지 포함해 final `MandatoryRouteGraph`를 만들고, `SectorCell.RouteMaskId`, `MandatoryGraphNode`, `ShortestDistanceFromStart`, `generated_world_edges.csv` byte artifact를 기록하라. generated CSV는 filesystem에 쓰지 않고 deterministic byte[]로만 검증한다.

Type4 규칙을 반드시 지켜라. Type4는 U+D 필수이며 L/R은 actual horizontal graph adjacency에서 복사한다. `ROUTE_T4_UD`, `ROUTE_T4_LUD`, `ROUTE_T4_RUD`, `ROUTE_T4_LRUD` 네 조합 모두 합법이다. L/R canonicalization, forced open/close, Type4 단일 ID 축약, Type0 mandatory graph 편입은 금지한다.

focused `>=252`, MandatoryRouteGraphBuilder `252/252` 이상, MandatoryRouteLoopPlanner `212/212`, UpDownConflictResolver `194/194`, VerticalGatewayPlanner `156/156`, HorizontalBackbone `142/142`, MandatoryConnectorTreeBuilder `129/129`, MandatoryRouteMaskLookupBuilder `127/127`, MandatoryTerminalBuilder `120/120`, GeneratedWorldData `56/56`, SiteReservationValidator `268/268`, BiomePatchValidator `196/196`, Map04Exit `110/110`, actual total `>=1962`, failed/skipped `0/0`을 실행하라. compile/Console/warning `0/0/0`, Authoring CSV/meta `50/50`, final Assets meta `3229`, duplicate GUID `0`, task-marker Assets changes `<=38`, unexpected changes `0`을 확인하라.

전부 PASS일 때 MAP05_08 COMPLETE/Current Task NONE으로만 finalize하고 `MAP05_09_IMPLEMENT_MANDATORY_ROUTE_VALIDATOR`는 LOCKED로 유지하라.
