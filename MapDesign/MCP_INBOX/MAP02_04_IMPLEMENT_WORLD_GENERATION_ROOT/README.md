# MAP02_04 — Implement World Generation Root

MAP02_03 PASS 상태에서 MAP02의 네 번째 Task만 여는 patch package다. Patch apply는 Master, Status, 새 Task 문서만 설치하고 Assets를 변경하지 않는다.

내부 `RUN_MAP02_04_PROMPT.md`로 실행한다. 범위는 typed CSV pass plan, immutable artifact store, explicit pass registry, grid adapter, failure policy/retry와 transactional execution root다. 현재 구현된 `PASS_GRID` prefix는 실제 실행하고, 아직 없는 후속 pass는 skip/stub하지 않고 full execution preflight에서 명확히 보고한다. MAP02_05 이후는 계속 LOCKED다.
