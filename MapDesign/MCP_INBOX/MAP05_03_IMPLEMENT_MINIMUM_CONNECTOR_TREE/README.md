# MAP05_03 — Implement Minimum Connector Tree

MAP05_02 route mask lookup PASS 뒤 MAP05의 세 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 MAP05_01 mandatory terminal set을 받아 7개 terminal을 잇는 immutable minimum connector tree 후보를 만든다.

출력은 terminal-to-terminal abstract edge 6개와 deterministic cost/diagnostics뿐이다. sector path, horizontal router, Type2/3 gateway, route graph, generated CSV, validator, overlay는 시작하지 않는다.

기준선은 MAP05_02 PASS, Assets meta `3170`, Authoring CSV/meta `50/50`이다.
