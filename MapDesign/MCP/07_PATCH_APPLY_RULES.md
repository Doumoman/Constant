# MCP Patch Apply Rules v2.0

# 1. 구조와 정상 입력

```text
MapDesign/
├─ MCP/
│  ├─ TASKS/
│  └─ 06_IMPLEMENTATION_STATUS.md
├─ MCP_INBOX/
└─ MCP_ARCHIVE/
```

정상 입력은 `MapDesign/MCP_INBOX/<TASK_ID>.md` 하나다. 파일의 첫 Task body 이전 metadata는 다음 exact schema를 사용한다.

```yaml
mcp_patch:
  format: single_task_v1
  task_id: <TASK_ID>
  task_file: TASKS/<TASK_ID>.md
  requires_current_task: NONE
  requires_completed_task: <PREVIOUS_TASK_ID>
  requires_result:
    path: REPORTS/<PREVIOUS_TASK_ID>_RESULT.md
    status: PASS
    sha256: <64-lowercase-hex>
  requires_installed_task:
    path: TASKS/<PREVIOUS_TASK_ID>.md
    sha256: <64-lowercase-hex>
  sets_current_task: <TASK_ID>
```

같은 MD의 나머지가 완전한 Task body다. 별도 manifest, payload directory, README, run prompt 또는 ZIP은 사용하지 않는다.

# 2. candidate scan

`MCP_INBOX` immediate children만 스캔한다.

- `*.md` file은 single-task candidate다.
- `.APPLIED`가 없는 directory는 legacy candidate다.
- 하위 directory를 재귀적으로 후보로 세지 않는다.
- MD와 legacy 후보의 합계가 정확히 1개여야 한다.
- 0개이며 Current Task가 `NONE`이면 clean stop한다.
- 0개이며 Current Task가 존재하면 Patch Apply 없이 Task Execution으로 진행한다.
- 2개 이상이면 `BLOCKED`다. MD 여러 개와 mixed legacy+MD 모두 포함한다.
- MAP09_00R 완료 후, 즉 MAP09_01을 여는 시점부터 Task ID와 무관하게 legacy directory candidate는 폐기된 형식이므로 `BLOCKED`다.
- `MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL`은 마지막으로 허용된 legacy directory patch다.

# 3. `single_task_v1` metadata 검증

모든 조건을 mutation 전에 검증한다.

1. `format`은 정확히 `single_task_v1`이다.
2. inbox filename stem, `task_id`, `task_file` filename stem, `sets_current_task`가 완전히 동일하다.
3. `task_file`은 정확히 `TASKS/<TASK_ID>.md` 형식이며 상대 경로 탈출이 없다.
4. `requires_current_task`는 정확히 `NONE`이고 실제 Current Task도 `NONE`이다.
5. `requires_completed_task` row가 Status에 정확히 한 번 존재하고 `COMPLETE`다.
6. 새 Task row가 Status에 정확히 한 번 존재하고 `LOCKED`다.
7. 새 Task ID가 Master에 정확히 한 번 이미 존재한다.
8. 새 Task ID가 설치된 215-row Master/Status 밖이면 `BLOCKED`이며 별도 contract-change patch가 필요하다.
9. `requires_result.path`는 정확히 `REPORTS/<PREVIOUS_TASK_ID>_RESULT.md` 형식이다.
10. `requires_result.status`는 정확히 `PASS`다.
11. 이전 Result file이 존재하고 Task ID가 predecessor와 일치하며 정확한 독립 line `STATUS: PASS`를 포함한다.
12. 이전 Result file의 SHA-256이 metadata와 일치한다.
13. `requires_installed_task.path`는 정확히 `TASKS/<PREVIOUS_TASK_ID>.md` 형식이다.
14. 설치된 이전 Task file이 존재하고 SHA-256이 metadata와 일치한다.
15. metadata의 모든 SHA 값은 정확히 64자리 lowercase hexadecimal(`[0-9a-f]{64}`)이다.
16. installed Task와 archive destination collision을 mutation 전에 검사한다. 기존 destination은 inbox와 byte-identical일 때만 재사용할 수 있다.

누락, 중복, path mismatch, hash mismatch 또는 state mismatch는 모두 `BLOCKED`다. Status, metadata 또는 path를 자동 보정하지 않는다.

# 4. byte-for-byte Task 설치

1. inbox MD 전체 bytes의 SHA-256을 계산한다.
2. 전체 파일을 byte-for-byte `MapDesign/MCP/TASKS/<TASK_ID>.md`로 copy한다.
3. installed file SHA-256이 inbox SHA-256과 같은지 검증한다.
4. destination이 없으면 create한다.
5. destination이 이미 있으면 byte-identical일 때만 재사용한다.
6. destination 내용이 다르면 overwrite, merge 또는 rename하지 않고 `BLOCKED`다.

검증되지 않은 INBOX body를 직접 Task Execution에 사용하지 않는다.

# 5. Status open transaction

Task 설치 검증 후 Patch Apply가 변경할 수 있는 Status field는 정확히 두 개다.

```text
Current Task: NONE -> <TASK_ID>
| <TASK_ID> | LOCKED | -> | <TASK_ID> | CURRENT |
```

적용 전후를 비교해 다음을 모두 검증한다.

- 전체 row count unchanged
- 해당 Task row exactly once
- `COMPLETE` delta `0`
- `CURRENT` delta `+1`
- `LOCKED` delta `-1`
- 다른 Task row와 Status 문서의 다른 field 변경 `0`

두 변경은 한 transaction으로 취급한다. 부분 적용 또는 검증 실패는 `BLOCKED`이며 다음 Task를 열지 않는다.

# 6. archive

Task 설치와 Status open validation이 성공한 후 원본 inbox MD를 `MapDesign/MCP_ARCHIVE/<TASK_ID>.md`로 이동한다.

- archive collision preflight는 Status를 열기 전에 완료한다.
- archive destination이 없으면 원본 bytes 그대로 move한다.
- archive destination이 존재하면 byte-identical일 때만 재사용하고, 원본과 archive SHA equality를 확인한 뒤 inbox 원본을 제거한다.
- archive destination 내용이 다르면 overwrite하지 않고 `BLOCKED`다.
- archive SHA-256은 설치된 Task와 inbox에서 계산한 SHA-256과 동일해야 한다.
- single MD에는 `.APPLIED`를 만들지 않는다.

# 7. 적용 후 검증

1. installed Task가 존재하고 비어 있지 않다.
2. installed Task와 archive가 byte-identical이다.
3. Current Task가 metadata `sets_current_task`와 일치한다.
4. Current Task row가 정확히 `CURRENT`다.
5. Master/Status row count가 적용 전과 동일하다.
6. status delta가 정확히 `COMPLETE 0 / CURRENT +1 / LOCKED -1`이다.
7. inbox 원본이 남아 있지 않고 archive가 존재한다.
8. 다음 Task는 여전히 `LOCKED`이며 자동 시작되지 않았다.

# 8. Task Execution, Finalize, Commit

Patch Apply가 PASS하면 installed Current Task 하나만 실행한다. Task Execution은 Status를 수정하지 않는다.

matching Result가 정확히 `STATUS: PASS`일 때만 Status Finalize가 다음 두 필드를 닫는다.

```text
Current Task: <TASK_ID> -> NONE
| <TASK_ID> | CURRENT | -> | <TASK_ID> | COMPLETE |
```

Finalize는 다음 Task를 열지 않는다. 그 후 atomic commit에는 archived single MD, installed Task MD, task-owned implementation/test/meta, Result, finalized Status만 포함한다. 무관한 dirty 변경은 제외하고 push하지 않는다.

# 9. collision과 failure policy

precondition, path, SHA, state, byte equality, Status delta 또는 archive 검증 중 하나라도 실패하면 `BLOCKED`다.

금지:
- 다른 installed Task/archive overwrite
- Status 자동 수정 또는 알려지지 않은 Task row 추가
- 후보 삭제로 candidate count 맞추기
- validation 완화
- INBOX body 직접 실행 fallback
- 다음 Task 자동 시작
- 자동 push

# 10. legacy compatibility 종료

`MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL`까지는 manifest의 명시적 copy와 `.APPLIED` receipt를 사용하는 legacy 적용을 허용한다. 그 patch가 완료된 이후에는 Task ID와 무관하게 legacy directory가 모두 `BLOCKED`이고 이 문서의 `single_task_v1` 절차만 유효하다.
