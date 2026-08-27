# MAP05_05 — Implement Vertical Gateway Planner

MAP05_04 horizontal backbone PASS 뒤 MAP05의 다섯 번째 Task만 여는 patch package다. 아직 실행 전인 패키지의 규칙 보정판이며 Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 MAP05_04의 네 개 row-transition segment마다 Type2.D 상단 → Type4(U/D 필수, L/R 선택적·실제 상태 보존) 중간 junction → Type3.U 하단 후보를 찾는다.

출력은 같은 column의 upper/lower gateway pair, Type4 중간 junction cell과 diagnostics다. Type4는 U/D를 항상 열고 L/R은 강제하지 않는다. U/D conflict 해소, loops, final graph, generated CSV, validator, overlay는 시작하지 않는다.

기준선은 MAP05_04 PASS, Assets meta `3188`, Authoring CSV/meta `50/50`이다. 기존 MAP05_02 Type1/2/3 lookup은 수정하지 않고, Type4 semantic은 MAP05_05 planner output으로 먼저 보존한다.
