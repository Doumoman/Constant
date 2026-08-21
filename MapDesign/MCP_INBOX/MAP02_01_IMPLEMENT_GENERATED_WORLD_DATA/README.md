# MAP02_01 — Implement Generated World Data

MAP01 phase gate PASS 상태에서 MAP02의 첫 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_01_PROMPT.md`로 실행한다. 이 Task의 범위는 169-cell immutable world data와 exact `generated_world_sectors.csv` v1 byte serializer까지이며, MAP02_02 이후는 계속 LOCKED다.
