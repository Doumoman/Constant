# MAP02_02 Repair v1.2 — Legacy Folder Meta Acceptance

MAP02_02 v1.1 재검증은 전부 PASS했지만 Unity force refresh가 기존 MAP01_15 Editor 경로의 folder `.meta` 6개를 자동 복원해 `Assets drift 0` 조건과 충돌했다.

이 패키지는 exact 6개를 유효하고 GUID-unique한 Unity folder metadata로 감사·수용한다. C#/test/CSV/meta를 고치거나 6개를 삭제하지 않으며, final Assets change set exact 20과 전체 재검증이 PASS할 때만 MAP02_02를 완료한다. `RUN_MAP02_02_REPAIR_V1_2_PROMPT.md`를 실행하고 MAP02_03은 계속 LOCKED로 둔다.
