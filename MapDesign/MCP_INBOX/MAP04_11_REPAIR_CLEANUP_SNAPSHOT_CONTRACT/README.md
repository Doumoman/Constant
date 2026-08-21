# MAP04_11 Repair — Cleanup Snapshot Contract

MAP04_11의 두 번째 실패를 같은 Task에서 교정하는 repair package다. MAP05는 열지 않는다.

## 현재 실패

PASS_SITE handoff 경계는 정상화됐지만 1,000-world batch 결과가 `5 Completed / 951 Handoff / 44 Invalid`다. 44건은 `PatchCleanup / InvalidSourceSnapshot` 비재시도 오류이므로 handoff로 재분류할 수 없다.

가능한 소유자는 둘뿐이다.

- `IntrusionPlacer`가 MAP04_06 계약을 어긴 snapshot을 `Completed`로 발행함
- snapshot은 유효하지만 `PatchCleanup`이 viable fixture 전용 가정으로 과검증함

실행 Task는 44건의 첫 위반 invariant를 먼저 기록하고, producer/consumer 중 증명된 단일 소유자만 최소 수정한다. production 오류를 test assertion 완화로 숨기거나 Invalid를 RetryRequired로 바꾸는 작업은 금지한다.

## 적용 전 필수 상태

- Current Task: `TASKS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS.md`
- 상태: `56 COMPLETE / MAP04_11 CURRENT / 148 LOCKED`
- 현재 Task SHA-256: `d3ba74fd176a07b6d010013806bf229891fd6b132d88861ab09a8b55b41d9733`
- 현재 FAIL Result SHA-256: `8bfd2a9132e9a97e755c5e35c31e544e89a8f679e20b0a515801baa9818719bc`

Patch apply는 현재 Task 한 파일만 SHA 조건부 교체한다. Master, Status, Result, Assets는 적용 단계에서 변경하지 않는다.

`RUN_MAP04_11_PROMPT.md`로 현재 MAP04_11을 다시 실행한다. 모든 gate가 PASS일 때만 MAP04_11을 finalize하며 MAP05_01은 별도 patch 전까지 LOCKED다.
