# TASK: L00_01_SURVEY

Full name:

```text
LIVE00_01_INVENTORY_LIVE_SCENE_INPUT_PREFAB_SURFACES
```

## Objective

Inventory Unity project surfaces required for live character integration.

This is report-only. Do not implement controls yet.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L00_01.md
Character final exit report exists
Character final exit SHA-256: 6efc2ac08d7cb52fd8ba260888310dd403ae64d191767a9338b174a0897fc96c
Character final exit contains STATUS: PASS
Character final exit contains CHARACTER_FINAL_EXIT_DECISION: APPROVED
Character final exit contains Character harness final state: COMPLETE
L00_02 and later tasks are locked
```

If false, write `STATUS: BLOCKED`.

## Read

Read in order:

1. `CLI/MCP/ENTRY.md`
2. `CLI/MCP/RULES.md`
3. `CLI/MCP/STATUS.md`
4. `CLI/MCP/MASTER.md`
5. `CLI/MCP/INPUTS/CHAR_EXIT.md`
6. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
7. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
8. `CharacterDesign/MCP/REPORTS/CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md`
9. `CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md`
10. `CharacterDesign/MCP/REPORTS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md`
11. `Assets/_Game/Character/Runtime/**`
12. `Assets/_Game/Tests/EditMode/Character/**`
13. `Assets/_Game/Map/Runtime/**`
14. `Assets/_Game/Tests/EditMode/Map/**`
15. `Assets/_Game/Scenes/**`
16. `Assets/**.inputactions`
17. `Assets/**.prefab`
18. `Assets/**.unity`
19. `Packages/manifest.json`
20. `ProjectSettings/EditorBuildSettings.asset`
21. `ProjectSettings/ProjectSettings.asset`

Use search before opening broad trees.

## Allowed Writes

```text
CLI/MCP/INPUTS/LIVE_SRC.md
CLI/MCP/REPORTS/L00_01_RESULT.md
```

Forbidden writes:

```text
Assets/**
Packages/**
ProjectSettings/**
MapDesign/**
CharacterDesign/**
CLI/MCP/STATUS.md
CLI/MCP/MASTER.md
CLI/MCP/TASKS/**
Builds/**
Temp/**
```

## Required Inventory

Create `CLI/MCP/INPUTS/LIVE_SRC.md` containing:

```text
REGISTRY_STATE: FILLED_BY_L00_01
OWNER_TASK: L00_01_SURVEY
Unity version
active build target
build scenes
scene candidates for live player integration
prefab candidates for player, camera, HUD, map, run bootstrap
input action assets and keyboard binding candidates
runtime bootstrap candidates
generated MAP output and snapshot adapter candidates
request consumer insertion candidates
HUD and presentation binding candidates
PlayMode test assembly/test scene candidates
recommended read/write path tokens for L00_02
blockers and missing surfaces
pre-existing dirty files
```

## Required Report

Write:

```text
CLI/MCP/REPORTS/L00_01_RESULT.md
```

Include:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
ENTRY_GATE
UNITY_PROJECT_SURFACES
SCENE_PREFAB_SURFACES
INPUT_SURFACES
BOOTSTRAP_AND_REQUEST_CONSUMER_SURFACES
HUD_PRESENTATION_SURFACES
MAP_GENERATED_OUTPUT_SURFACES
TEST_AND_BUILD_SURFACES
RECOMMENDED_L00_02_TOKENS
BLOCKERS
SCOPE_VALIDATION
NEXT
```

PASS requires source registry creation, no project code/assets changes, and clear recommended tokens for L00_02.

## Completion

If PASS:

```text
Finalize L00_01 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L00_02_LOCK.
```

