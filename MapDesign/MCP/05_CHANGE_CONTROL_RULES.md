# 변경 통제·Git 규칙 v1.3

# 1. Git과 atomic commit

PASS Result와 STATUS FINALIZE가 완료된 Task는 종료 전에 정확히 하나의 atomic commit으로 기록한다.

커밋은 다음 task-owned 파일만 포함한다.

- legacy patch로 연 Task: 해당 patch payload 전체와 `.APPLIED`
- `single_task_v1`로 연 Task: `MCP_ARCHIVE/<TASK_ID>.md`와 byte-identical installed `MCP/TASKS/<TASK_ID>.md`
- 해당 Task WRITE ALLOWLIST의 구현·테스트·matching meta
- 해당 Task Result
- Phase A Status open과 Phase C Status finalize가 반영된 최종 `06_IMPLEMENTATION_STATUS.md`

기존에 존재하던 무관한 uncommitted change는 stage하거나 commit하지 않는다. staging 후에는 staged path inventory가 위 범위와 정확히 일치하는지 검증한다.

커밋 메시지 규칙:
- 제목에 Task ID와 핵심 구현을 명시
- 본문에 구현 내용, 주요 파일, 테스트 실행 수, compile/Console/static gate를 상세히 기록
- 커밋 후 commit SHA와 제목을 종료 보고에 기록

자동 push/branch/reset/rebase/force는 금지한다. push는 사용자의 별도 명시 지시가 있을 때만 수행한다.

FAIL/BLOCKED Task의 부분 작업은 사용자의 별도 지시 없이 자동 commit하지 않는다.

# 2. 기존 변경 보호

기존 uncommitted change를 임의로 읽거나 수정하거나 되돌리지 않는다. Task와 겹치는 기존 변경을 보존할 수 없으면 `BLOCKED`다.

# 3. `06_IMPLEMENTATION_STATUS.md` 수정 권한

## 일반 TASK EXECUTION

```text
수정 금지
```

## PATCH APPLY — legacy

```text
마지막 허용 legacy patch인 MAP09_00R까지 PATCH_MANIFEST가 replace를 명시한 경우에만 허용
MAP09_00R 완료 후에는 Task ID와 무관하게 legacy patch directory가 BLOCKED
```

## PATCH APPLY — `single_task_v1`

`07_PATCH_APPLY_RULES.md` 검증이 모두 PASS한 경우에만 아래 두 필드를 한 transaction으로 연다.

```text
Current Task: NONE -> <TASK_ID>
| <TASK_ID> | LOCKED | -> | <TASK_ID> | CURRENT |
```

다른 상태, baseline, Last Completed Task, Last Result 또는 설명을 변경하지 않는다. row 수는 동일해야 하며 status delta는 정확히 `COMPLETE 0 / CURRENT +1 / LOCKED -1`이어야 한다.

## STATUS FINALIZE

정확히 `STATUS: PASS`인 matching Result를 검증한 후 `08_STATUS_FINALIZE_RULES.md`가 허용한 필드만 닫는다.

```text
Current Task: <TASK_ID> -> NONE
| <TASK_ID> | CURRENT | -> | <TASK_ID> | COMPLETE |
```

다음 Task의 `LOCKED` row는 열지 않는다.

# 4. `single_task_v1` 파일 통제

- inbox MD 전체를 byte-for-byte 설치하고 SHA-256 equality를 확인한다.
- installed Task 또는 archive destination이 존재하면 byte-identical일 때만 재사용한다.
- 다른 내용의 파일을 overwrite, merge, auto-correct하지 않는다.
- 성공한 원본 MD는 archive로 이동하며 `.APPLIED`를 사용하지 않는다.
- 알려지지 않은 Task ID를 Master/Status에 자동 추가하지 않는다.
- Task Execution은 INBOX/ARCHIVE body를 직접 실행하지 않고 installed Task만 실행한다.

# 5. Source of Truth 변경

고정 월드 크기, Sector/MicroChunk 크기, Route Type, 필수 경로 규칙,
패치 규칙, CSV schema, 생성 순서 핵심 원칙은 Current Task의 명시적 권한 없이 변경하지 않는다.
