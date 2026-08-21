# MAP02_06 — Implement Seed Manifest and Replay Recorder

MAP02_05 PASS 상태에서 MAP02의 여섯 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_06_PROMPT.md`로 실행한다. 범위는 successful exact `PASS_GRID` record를 Map Package v1.0의 `seed_manifest.csv`와 `generated_world_sectors.csv` 두 파일로 기록하고, generated-output root에 원자적으로 publish/load한 뒤 content/build identity와 replayed static sector bytes를 검증하는 것까지다. timing-bearing manifest는 정적 결정론 비교에서 제외하며 후속 generated CSV placeholder와 overlay는 만들지 않는다. 현재 Assets meta `2973`과 accepted legacy folder meta `6/6`을 baseline으로 고정했고 새 directory는 만들지 않는다.
