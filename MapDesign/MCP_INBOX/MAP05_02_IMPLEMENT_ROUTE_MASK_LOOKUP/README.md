# MAP05_02 — Implement Route Mask Lookup

MAP05_01 terminal build PASS 뒤 MAP05의 두 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 MAP01 typed route mask definitions에서 mandatory Type1/2/3 mask 세 개만 canonical lookup으로 만든다.

정확한 승인 조합은 `ROUTE_T1_LR = L/R`, `ROUTE_T2_LRD = L/R/D`, `ROUTE_T3_LRU = L/R/U`다. connector tree, routing path, gateway placement, graph, validator, overlay는 시작하지 않는다.

기준선은 MAP05_01 PASS, Assets meta `3161`, Authoring CSV/meta `50/50`이다.
