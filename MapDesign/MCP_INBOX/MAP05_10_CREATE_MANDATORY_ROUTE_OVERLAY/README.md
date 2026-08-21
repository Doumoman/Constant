# MAP05_10 — Create Mandatory Route Overlay

MAP05_09 route graph validation PASS 뒤 MAP05의 열 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 검증 완료된 `MandatoryRouteGraph`, `GeneratedWorldData`, generated edge records, `MandatoryRouteValidationReport`를 읽어 Game View와 Scene View에서 볼 수 있는 mandatory route overlay를 만든다.

출력은 overlay runtime snapshot/GUI와 editor Scene drawer, 그리고 focused tests다. graph, route mask, `SectorCell`, generated CSV, Authoring CSV, root/pass pipeline은 수정하지 않는다.

기준선은 MAP05_09 PASS, 실제 결과 SHA `72df536b5d51c7db7ff364e74e7bd7141f0399465e38b3a75d366640a1d3b33a`, Assets meta `3238`, Authoring CSV/meta `50/50`이다. Type4는 계속 U+D 필수이며 L/R은 actual graph adjacency를 보존한다.
