# MAP05_11 — MAP05 Batch and Exit Tests

MAP05_10 mandatory route overlay PASS 뒤 MAP05의 마지막 Task만 여는 patch package다. Apply는 Master, Status, 새 Task 문서만 설치하고 Assets는 변경하지 않는다.

실행 시 MAP05_01~10의 mandatory route 산출물을 production 수정 없이 batch, determinism, validation, generated CSV, overlay visual, phase-boundary audit로 검증한다.

출력은 `Map05ExitTests.cs`와 matching meta, 그리고 Result다. graph, route mask, `SectorCell`, generated CSV, Authoring CSV, root/pass integration은 수정하지 않는다.

기준선은 MAP05_10 PASS, 실제 결과 SHA `2f8ef4e027c1abd8f93721f840b5a6ab43d812b1bcb9bd6ae71fd8d694823c6f`, Assets meta `3245`, Authoring CSV/meta `50/50`이다. MAP06은 MAP05_11 PASS/finalize 전까지 LOCKED다.
