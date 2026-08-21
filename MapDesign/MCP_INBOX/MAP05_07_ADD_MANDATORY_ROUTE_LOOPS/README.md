# MAP05_07 — Add Mandatory Route Loops

MAP05_06 conflict resolution PASS 뒤 MAP05의 일곱 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 `MandatoryConnectorTree`, `HorizontalBackbonePlan`, `VerticalGatewayPlan`, `UpDownConflictResolutionPlan`을 읽고 필수망 내부의 독립적인 loop 후보를 deterministic하게 계획한다.

출력은 immutable `MandatoryRouteLoopPlan`과 최소 2개의 loop/diagnostics다. Type4는 U/D를 항상 보장하고 L/R은 실제 상태를 그대로 보존한다. graph edge, generated CSV, `SectorCell.RouteMaskId`, validator, overlay는 시작하지 않는다.

기준선은 MAP05_06 PASS, Assets meta `3206`, Authoring CSV/meta `50/50`이다. 기존 MAP05_02 Type1/2/3 lookup은 수정하지 않고, Type4 semantic은 U+D 필수·L/R 독립 보존 규칙으로 계속 유지한다.
