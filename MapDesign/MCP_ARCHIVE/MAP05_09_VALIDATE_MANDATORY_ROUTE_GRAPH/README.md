# MAP05_09 — Validate Mandatory Route Graph

MAP05_08 route graph PASS 뒤 MAP05의 아홉 번째 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 `MandatoryRouteGraph`, route-stamped `GeneratedWorldData`, generated edge records/CSV bytes를 읽고 Type1/2/3/4 mandatory route 규칙을 검증한다.

출력은 immutable `MandatoryRouteValidationReport`와 diagnostics다. graph, route mask, `SectorCell`, generated CSV, root, overlay는 수정하지 않는다. Type4는 U+D 필수이며 L/R은 actual graph adjacency를 보존한다. `UD/LUD/RUD/LRUD` 네 조합 모두 합법이다.

기준선은 MAP05_08 PASS, 실제 결과 SHA `7c9820290ec5269222b8c145603a9ae53a2ea7f8d1df7b0ca6029e1be3647a99`, Assets meta `3229`, Authoring CSV/meta `50/50`이다. MAP05_10 overlay는 다음 Task로 남긴다.
