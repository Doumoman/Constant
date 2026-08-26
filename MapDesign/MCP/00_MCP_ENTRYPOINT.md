# 《별을 물어오는 밤》 MCP 진입 규칙 v1.4

# 1. 세션 모드

MCP 세션은 아래 4개 Phase로 구분한다.

## PHASE A — PATCH APPLY

`MapDesign/MCP_INBOX/`의 immediate children만 스캔한다.

- 정상 입력은 `<TASK_ID>.md` 하나이며 형식은 `single_task_v1`이다.
- legacy 후보는 `.APPLIED`가 없는 patch directory다.
- `*.md` 후보와 legacy 후보를 합쳐 정확히 1개여야 한다.
- 후보가 0개이고 Current Task가 `NONE`이면 clean stop한다.
- 후보가 0개이고 Current Task가 존재하면 PHASE B로 진행한다.
- 후보가 2개 이상이면 `BLOCKED`다.
- MAP09_00R 완료 후, 즉 MAP09_01을 여는 시점부터 Task ID와 무관하게 legacy directory는 폐기된 입력 형식이므로 `BLOCKED`다.
- `MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL`만 마지막 legacy directory patch로 허용한다.

`single_task_v1` 후보는 `07_PATCH_APPLY_RULES.md`에 따라 다음을 모두 검증한다.

1. filename stem, `task_id`, `task_file` stem, `sets_current_task`가 동일하다.
2. Current Task가 `NONE`이고, predecessor는 `COMPLETE`, 새 Task는 Status에 정확히 한 번 `LOCKED`, Master에 이미 존재한다.
3. 이전 Result가 정확히 `STATUS: PASS`이며 Result와 설치된 이전 Task의 SHA-256이 metadata와 일치한다.
4. 모든 SHA 값은 64자리 lowercase hexadecimal이다.
5. inbox MD 전체를 byte-for-byte `MCP/TASKS/<TASK_ID>.md`에 설치하고 SHA equality를 확인한다.
6. 설치와 archive destination collision을 mutation 전에 확인한다. 기존 파일은 byte-identical일 때만 재사용하며 다른 내용은 덮어쓰지 않고 `BLOCKED`다.
7. Status는 `Current Task: NONE -> <TASK_ID>`와 해당 row `LOCKED -> CURRENT` 두 필드만 연다.
8. row 수가 동일하고 status delta가 `COMPLETE 0 / CURRENT +1 / LOCKED -1`인지 검증한다.
9. 성공 후 원본 MD를 `MCP_ARCHIVE/<TASK_ID>.md`로 이동한다. archive collision은 byte-identical일 때만 재사용한다.

single MD에는 `.APPLIED`를 만들지 않으며, 실패 시 inbox body를 직접 실행하거나 상태를 자동 보정하지 않는다.

## PHASE B — TASK EXECUTION

현재 `06_IMPLEMENTATION_STATUS.md`가 지정한 Current Task 하나만 수행한다.

TASK 자체는 `06_IMPLEMENTATION_STATUS.md`를 수정하지 않는다. Task file은 반드시 설치된 `MCP/TASKS/<TASK_ID>.md`에서 읽으며 INBOX 또는 ARCHIVE body를 직접 실행하지 않는다.

## PHASE C — STATUS FINALIZE

TASK Result가 정확히 `STATUS: PASS`일 때만 수행한다.

읽기:
1. `00_MCP_ENTRYPOINT.md`
2. `05_CHANGE_CONTROL_RULES.md`
3. `08_STATUS_FINALIZE_RULES.md`
4. `06_IMPLEMENTATION_STATUS.md`
5. 방금 수행한 Current Task
6. 해당 TASK의 Result 문서

허용 수정:
- `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md`만

상태를:
- 현재 TASK `CURRENT -> COMPLETE`
- `Current Task -> NONE`
으로 닫는다.

다음 TASK는 자동 시작하지 않는다.

## PHASE D — TASK COMMIT

STATUS FINALIZE까지 PASS한 Task는 반드시 하나의 atomic Git commit으로 기록한다.

커밋 범위:
- legacy 입력이면 해당 patch payload와 `.APPLIED`
- `single_task_v1` 입력이면 archived inbox MD와 installed Task MD
- Phase B의 task-owned 구현·테스트·matching meta와 Result
- Phase A의 정확한 Status open과 Phase C의 Status finalize가 반영된 최종 Status

기존에 존재하던 무관한 uncommitted change는 포함하지 않는다.

커밋 메시지는 Task ID를 제목에 포함하고 구현 파일·동작·테스트·정적 gate를 본문에 상세히 기록한다. 자동 push는 수행하지 않는다.

# 2. 기본 실행 파이프라인

사용자가 `MapDesign/MCP/APPLY_PATCH_AND_RUN_CURRENT_TASK.md` 수행을 요청하면:

```text
PHASE A PATCH APPLY
    ↓ PASS
PHASE B CURRENT TASK
    ↓ Result STATUS: PASS
PHASE C STATUS FINALIZE
    ↓ PASS
PHASE D ATOMIC TASK COMMIT
    ↓ PASS
STOP
```

어느 Phase라도 `FAIL` 또는 `BLOCKED`면 즉시 종료한다.

# 3. 규칙 우선순위

1. 사용자의 현재 세션 최신 지시
2. `01_PROJECT_LOCKED_RULES.md`
3. 현재 TASK
4. `02~05`, `07`, `08` 규칙
5. 과거 GDD/참고 문서

# 4. 절대 금지

허용 범위 밖:
- 다음 TASK 선행 구현 또는 자동 시작
- 알려지지 않은 Task ID를 Master/Status에 자동 추가
- 관련 없는 리팩토링
- CSV schema 임의 변경
- Scene/Prefab 임의 변경
- package 설치/삭제
- 다른 내용의 installed Task 또는 archive overwrite
- Phase D 밖의 임의 Git commit
- Git push/branch/reset/rebase/force
- 테스트 규칙 완화
- 실패 결과를 임의 후처리로 숨기기

# 5. 종료 보고

```text
PHASE:
TASK_OR_PATCH:
STATUS: PASS / FAIL / BLOCKED

READ:
CHANGED:
CREATED:
TEST:
UNITY:
COMMIT:
OUT_OF_SCOPE_FINDINGS:
NEXT:
```

TASK COMMIT까지 PASS하면:

```text
NEXT:
- Current Task: NONE
- Awaiting next MCP_INBOX patch: YES
```
