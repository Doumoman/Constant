# CHAR06_04 MCP_INBOX Package

이 패키지는 캐릭터 하네스의 마지막 단일 작업만 연다.

```text
CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT
```

## Extraction Root

이번 ZIP은 `CharacterDesign/`를 최상위에 포함하지 않는다.

ZIP을 다음 위치에 풀면 된다.

```text
CharacterDesign/MCP_INBOX/
```

정상 배치 결과:

```text
CharacterDesign/MCP_INBOX/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT/
```

## Apply

1. `PATCH_MANIFEST.md`의 entry gate와 hash를 확인한다.
2. `PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md`를 `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`에 복사한다.
3. `PAYLOAD/06_IMPLEMENTATION_STATUS.md`를 `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`에 복사한다.
4. `PAYLOAD/TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md`를 `CharacterDesign/MCP/TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md`에 생성한다.
5. `RUN_CHAR06_04_PROMPT.md`를 사용해 MCP 작업을 실행한다.

## Expected Report

```text
CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
```

## Scope

이 작업은 최종 감사 전용이다.

```text
report status ledger
task and report hash ledger
allowlist and scope audit
forbidden feature audit
dependency direction audit
CHAR06_03 validation evidence audit
commit evidence recording
final Character exit decision
```

열지 않는 범위:

```text
runtime or test implementation
MAP edits
ProjectSettings or Packages edits
new commits or pushes without explicit user instruction
```

