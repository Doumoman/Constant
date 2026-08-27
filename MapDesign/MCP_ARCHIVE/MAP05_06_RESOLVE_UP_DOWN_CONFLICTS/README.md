# MAP05_06 — Resolve Up/Down Conflicts

MAP05_05 vertical gateway planner PASS 뒤 MAP05의 여섯 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 MAP05_05의 immutable `VerticalGatewayPlan`을 읽고 Type4로 표현할 수 없는 U/D 충돌만 분리·해결한다.

출력은 immutable `UpDownConflictResolutionPlan`과 충돌별 분리 gateway pair/diagnostics다. Type4는 U/D를 항상 보장하고 L/R은 실제 상태를 그대로 보존한다. Type4로 표현 가능한 셀은 충돌로 세지 않으며, loop, final graph, generated CSV, validator, overlay는 시작하지 않는다.

기준선은 MAP05_05 PASS, Assets meta `3197`, Authoring CSV/meta `50/50`이다. 기존 MAP05_02 Type1/2/3 lookup은 수정하지 않고, Type4 semantic은 U+D 필수·L/R 독립 보존 규칙으로 계속 유지한다.
