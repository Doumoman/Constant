# MAP01_15 Repair — Authoritative Schema Contracts

`MAP01_15_CREATE_CSV_IMPORT_WINDOW`의 최초 full reimport가 25 ERROR로 BLOCKED된 exact 상태에만 적용하는 v1.1 same-task 복구 패치다.

1. `PATCH_MANIFEST.md`의 status/result/report precondition과 기존 Task SHA-256을 검증한다.
2. 허용된 단일 copy operation으로 현재 Task 파일을 v1.1 payload로 교체한다.
3. `RUN_MAP01_15_REPAIR_PROMPT.md`를 새 채팅에 붙여 넣어 같은 MAP01_15를 재개한다.

Master, Status, Result, Assets는 patch apply 중 변경하지 않는다. MAP01_16은 열지 않는다.
