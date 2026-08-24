# RUN PROMPT: CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT

You are applying and executing one MCP_INBOX task package.

## Package Location

This package is expected at:

```text
CharacterDesign/MCP_INBOX/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT
```

If the ZIP was extracted into `CharacterDesign/MCP_INBOX`, this path is already correct.

## Apply Package

Apply exactly these copy operations:

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
-> CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md

PAYLOAD/06_IMPLEMENTATION_STATUS.md
-> CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md

PAYLOAD/TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md
-> CharacterDesign/MCP/TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md
```

Before copying, verify `PATCH_MANIFEST.md`.

Do not edit Assets, Packages, ProjectSettings, MapDesign, scenes, prefabs, inputactions, asmdefs, runtime code, test code, build output, or previous reports during package apply.

## Execute Current Task

After apply, execute only:

```text
CharacterDesign/MCP/TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md
```

Hard stops:

```text
Do not open any later task.
Do not modify runtime code or test code.
Do not modify MAP runtime or MAP authoring data.
Do not change ProjectSettings or Packages.
Do not create commits or push unless the user explicitly instructs it.
Do not rerun full tests or build unless the existing evidence is inconsistent.
```

## Required Output

Write the result report to:

```text
CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
```

The report must contain an independent status line:

```text
STATUS: PASS
```

or:

```text
STATUS: FAIL
```

or:

```text
STATUS: BLOCKED
```

If PASS, finalize CHAR06_04 only and stop with:

```text
CHARACTER_FINAL_EXIT_DECISION: APPROVED
Current Task after finalize: NONE
Next Task auto-opened: NO
Character harness final state: COMPLETE
```

