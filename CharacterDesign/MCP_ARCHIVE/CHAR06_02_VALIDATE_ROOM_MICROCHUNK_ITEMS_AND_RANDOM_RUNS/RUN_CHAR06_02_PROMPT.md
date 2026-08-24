# RUN PROMPT: CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS

You are applying and executing one MCP_INBOX task package.

## Apply Package

Package root:

```text
CharacterDesign/MCP_INBOX/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS
```

Apply exactly these copy operations:

```text
PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
-> CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md

PAYLOAD/06_IMPLEMENTATION_STATUS.md
-> CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md

PAYLOAD/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md
-> CharacterDesign/MCP/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md
```

Before copying, verify `PATCH_MANIFEST.md`.

Do not edit Assets, Packages, ProjectSettings, MapDesign, scenes, prefabs, inputactions, asmdefs, runtime code, or test code during package apply.

## Execute Current Task

After apply, execute only:

```text
CharacterDesign/MCP/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md
```

Hard stops:

```text
Do not open CHAR06_03.
Do not read or execute CHAR06_03 task body.
Do not modify MAP runtime or MAP authoring data.
Do not perform PlayMode or build validation.
Do not commit before writing and validating the RESULT.
```

## Required Output

Write the result report to:

```text
CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md
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

If PASS, finalize CHAR06_02 only and stop with:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

