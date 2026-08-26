# APPLY PATCH + RUN CURRENT TASK + FINALIZE + COMMIT

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`를 먼저 읽는다.

## Phase A — Patch Apply

`MapDesign/MCP_INBOX/` immediate children에서 다음 candidate를 센다.

- `*.md` single-task file
- `.APPLIED`가 없는 legacy patch directory

합계가:

- 정확히 1개면 `07_PATCH_APPLY_RULES.md`에 따라 검증하고 적용한다.
- 0개이고 Current Task가 존재하면 Phase B로 진행한다.
- 0개이고 Current Task도 `NONE`이면 clean stop한다.
- 2개 이상이면 `BLOCKED`다.

MAP09_00R 완료 후, 즉 MAP09_01을 여는 시점부터 Task ID와 무관하게 legacy directory는 `BLOCKED`이며 `MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL`만 마지막 legacy patch다.

### `single_task_v1` 적용

1. filename stem, `task_id`, `task_file` stem, `sets_current_task` identity를 검증한다.
2. Current Task `NONE`, predecessor `COMPLETE`, 새 Task row exactly once `LOCKED`, Master membership을 검증한다.
3. 이전 Result의 exact `STATUS: PASS`, Result SHA-256, installed predecessor Task SHA-256을 검증한다.
4. 모든 SHA가 정확히 64 lowercase hex인지 검증한다.
5. inbox MD 전체 SHA-256을 계산하고 `MCP/TASKS/<TASK_ID>.md`로 byte-for-byte 설치한다.
6. installed/archive collision을 mutation 전에 확인한다. 기존 destination은 byte-identical일 때만 재사용하며 다른 내용이면 `BLOCKED`다.
7. Status의 정확한 두 field만 연다: Current Task `NONE -> <TASK_ID>`, Task row `LOCKED -> CURRENT`.
8. row count unchanged와 `COMPLETE 0 / CURRENT +1 / LOCKED -1` delta를 검증한다.
9. 원본 MD를 `MCP_ARCHIVE/<TASK_ID>.md`로 이동한다. archive collision은 byte-identical일 때만 재사용한다.
10. installed/archive/inbox SHA equality를 검증한다. single MD에는 `.APPLIED`를 만들지 않는다.

어떤 precondition, path, hash, state, byte 또는 collision 검증도 실패하면 자동 보정·overwrite·fallback 없이 `BLOCKED`다.

## Phase B — Current Task

1. 전역 규칙을 읽는다.
2. `06_IMPLEMENTATION_STATUS.md`가 지정한 installed Current Task 하나만 읽고 실행한다.
3. READ/WRITE ALLOWLIST를 준수한다.
4. INBOX 또는 ARCHIVE body를 직접 실행하지 않는다.
5. Task Execution 중 Status를 수정하지 않는다.
6. 지정된 Result를 생성한다.
7. Result에 정확한 `STATUS: PASS`와 matching Task ID가 있는지 확인한다.

Result가 `FAIL` 또는 `BLOCKED`면 즉시 종료하며 Status Finalize와 commit을 수행하지 않는다.

## Phase C — Status Finalize

Task Result가 정확히 PASS일 때만:

1. `08_STATUS_FINALIZE_RULES.md`를 읽는다.
2. Result와 Current Task가 일치하고 DONE CONDITIONS가 충족됐는지 확인한다.
3. `06_IMPLEMENTATION_STATUS.md`에서 정확히:
   - Current Task `<TASK_ID> -> NONE`
   - 해당 row `CURRENT -> COMPLETE`
   로 닫는다.
4. 다른 Task row가 변하지 않았고 다음 Task가 `LOCKED`인지 검증한다.

다음 Task를 자동 시작하지 않는다.

## Phase D — Atomic Task Commit

Task 수행과 Status Finalize가 모두 PASS라면:

1. `05_CHANGE_CONTROL_RULES.md`의 commit 범위를 확인한다.
2. 기존 무관한 dirty 파일을 제외한다.
3. legacy patch이면 patch payload와 `.APPLIED`를 포함한다.
4. `single_task_v1`이면 archived inbox MD와 installed Task MD를 포함한다.
5. task-owned 구현·테스트·matching meta, Result, finalized Status만 stage한다.
6. staged path inventory와 diff를 검증한다.
7. Task ID가 포함된 제목과 구현·테스트·gate 상세 본문으로 정확히 하나의 atomic commit을 만든다.
8. commit SHA와 제목을 검증한다.

commit 실패 시 `BLOCKED`로 보고한다. 자동 push는 수행하지 않는다.

## 종료

commit 후 STOP한다. 다음 Task를 자동 시작하거나 다음 inbox file을 생성하지 않는다.
