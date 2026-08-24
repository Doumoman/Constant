# RUN PROMPT: CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD

You are applying and executing one MCP_INBOX task package.

## Package Location

This package is expected at:

```text
CharacterDesign/MCP_INBOX/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD
```

If the ZIP was extracted into `CharacterDesign/MCP_INBOX`, this path is already correct.

## Apply Package

Apply exactly these copy operations:

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
-> CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md

PAYLOAD/06_IMPLEMENTATION_STATUS.md
-> CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md

PAYLOAD/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
-> CharacterDesign/MCP/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
```

Before copying, verify `PATCH_MANIFEST.md`.

Do not edit Assets, Packages, ProjectSettings, MapDesign, scenes, prefabs, inputactions, asmdefs, runtime code, or test code during package apply.

## Execute Current Task

After apply, execute only:

```text
CharacterDesign/MCP/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
```

Hard stops:

```text
Do not open CHAR06_04.
Do not read or execute CHAR06_04 task body.
Do not modify runtime code or test code.
Do not modify MAP runtime or MAP authoring data.
Do not change ProjectSettings or Packages.
Do not commit before writing and validating the RESULT.
```

## Required Output

Write the result report to:

```text
CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md
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

If PASS, finalize CHAR06_03 only and stop with:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

