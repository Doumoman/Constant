# MAP05_08 — Build Mandatory Route Graph

MAP05_07 loop plan PASS 뒤 MAP05의 여덟 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 `MandatoryRouteTerminalSet`, `MandatoryRouteMaskLookup`, `MandatoryConnectorTree`, `HorizontalBackbonePlan`, `VerticalGatewayPlan`, `UpDownConflictResolutionPlan`, `MandatoryRouteLoopPlan`을 읽고 최종 `MandatoryRouteGraph`를 deterministic하게 만든다.

출력은 immutable mandatory graph, route-stamped `GeneratedWorldData`, `SectorCell.RouteMaskId`, `mandatory_graph_node`, `generated_world_edges.csv` byte artifact다. Type4는 U+D 필수이며 L/R은 실제 horizontal adjacency를 보존한다. `UD/LUD/RUD/LRUD` 네 조합 모두 합법이고 canonicalization은 금지한다.

기준선은 MAP05_07 PASS, 실제 결과 SHA `cbe4f9a136d488df134a6eee676e13950d5dfd15238abf3188a81ce532fbdf65`, Assets meta `3215`, Authoring CSV/meta `50/50`이다. Authoring CSV schema/body는 수정하지 않고, validator/overlay/root는 다음 Task로 남긴다.
