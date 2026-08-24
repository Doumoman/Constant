# Character Implementation Status

schema_version: 1
harness_version: 1.3
current_phase: CHAR00
current_task: NONE
last_finalized_task: CHAR00_01
next_task_candidate: CHAR00_02

requires_result:
  path: NONE
  exact_status: NONE

transition_policy:
  finalize_opens_next: false
  open_requires_separate_patch: true
  fail_keeps_current: true
  blocked_keeps_current: true
  commit_after_pass: true
  push_requires_user_instruction: true

task_states:
  CHAR00_01: COMPLETE
  CHAR00_02: LOCKED
  CHAR00_03: LOCKED
  CHAR01_01: LOCKED
  CHAR01_02: LOCKED
  CHAR01_03: LOCKED
  CHAR01_04: LOCKED
  CHAR02_01: LOCKED
  CHAR02_02: LOCKED
  CHAR02_03: LOCKED
  CHAR03_01: LOCKED
  CHAR03_02: LOCKED
  CHAR03_03: LOCKED
  CHAR04_01: LOCKED
  CHAR04_02: LOCKED
  CHAR04_03: LOCKED
  CHAR04_04: LOCKED
  CHAR05_01: LOCKED
  CHAR05_02: LOCKED
  CHAR05_03: LOCKED
  CHAR05_04: LOCKED
  CHAR05_05: LOCKED
  CHAR06_01: LOCKED
  CHAR06_02: LOCKED
  CHAR06_03: LOCKED
  CHAR06_04: LOCKED

## 상태 변경 규칙

이 파일은 별도 FINALIZE 또는 OPEN 패치에서만 수정한다. 구현 패치가 CURRENT를 완료 처리하거나 다음 작업을 열 수 없다.
