# TASK: L00_02_LOCK

Full name:

```text
LIVE00_02_LOCK_LIVE_CONTRACTS_ALLOWLISTS_TEST_SCENES_AND_RESULT_FORMATS
```

## Objective

Lock the live integration plan before implementation starts.

This is report-only and contract-only. Do not create scenes, prefabs, input assets, MonoBehaviours, tests, asmdefs, UI, build settings, or runtime code in this task.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L00_02.md
L00_01 RESULT exists
L00_01 RESULT sha256: 4e982e431d05a0c01dccac9062327068ea51a7ff713dfe281796a3dd9846d69b
L00_01 RESULT contains STATUS: PASS
L00_01 RESULT contains REGISTRY_STATE: FILLED_BY_L00_01
CLI/MCP/INPUTS/LIVE_SRC.md exists
CLI/MCP/INPUTS/LIVE_SRC.md contains REGISTRY_STATE: FILLED_BY_L00_01
L01_01 and later tasks are locked
```

If false, write `STATUS: BLOCKED`.

## Read

Read in order:

1. `CLI/MCP/ENTRY.md`
2. `CLI/MCP/RULES.md`
3. `CLI/MCP/STATUS.md`
4. `CLI/MCP/MASTER.md`
5. `CLI/MCP/INPUTS/CHAR_EXIT.md`
6. `CLI/MCP/INPUTS/LIVE_SRC.md`
7. `CLI/MCP/REPORTS/L00_01_RESULT.md`
8. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
9. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
10. `Assets/_Game/Character/Runtime/**`
11. `Assets/_Game/Map/Runtime/**`
12. `Assets/_Game/Tests/EditMode/Character/**`
13. `Assets/_Game/Tests/EditMode/Map/**`
14. `Assets/_Game/Scenes/**`
15. `Assets/**.inputactions`
16. `Assets/**.prefab`
17. `Packages/manifest.json`
18. `ProjectSettings/EditorBuildSettings.asset`

Use search before opening broad trees.

## Allowed Writes

```text
CLI/MCP/INPUTS/LIVE_LOCK.md
CLI/MCP/REPORTS/L00_02_RESULT.md
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

## Required Lock File

Create `CLI/MCP/INPUTS/LIVE_LOCK.md` containing:

```text
LOCK_STATE: FILLED_BY_L00_02
OWNER_TASK: L00_02_LOCK
ENTRY_ANCHORS
PATH_TOKENS
TASK_ALLOWLISTS
READONLY_PRECEDENTS
FORBIDDEN_GLOBALS
ACTION_ID_LOCK
INPUT_BINDING_LOCK
LIVE_ASSEMBLY_PLAN
SCENE_PLAN
PREFAB_PLAN
BOOTSTRAP_PLAN
REQUEST_CONSUMER_PLAN
MAP_ADAPTER_PLAN
HUD_PRESENTATION_PLAN
PLAYMODE_TEST_PLAN
BUILD_PLAN
RESULT_FORMAT_LOCK
CHANGE_CONTROL_RULES
KNOWN_RISKS
NEXT_TASK_GATE
```

## Required Decisions

Lock these path tokens exactly unless `LIVE_SRC.md` proves a blocker:

```text
LIVE_RUNTIME: Assets/_Game/Live/Runtime/**
LIVE_INPUT: Assets/_Game/Live/Input/**
LIVE_PREFABS: Assets/_Game/Live/Prefabs/**
LIVE_SCENES: Assets/_Game/Scenes/Live/**
LIVE_PLAYMODE: Assets/_Game/Tests/PlayMode/Character/**
READONLY_PRECEDENT: Assets/_Legacy/**, Assets/2D Fantasy sprite bundle/**
FORBIDDEN_KEEP: Assets/_Game/Character/Runtime/**, Assets/_Game/Map/Runtime/**
```

Lock action IDs exactly:

```text
Move
Down
Jump
Action
Bomb
Rope
```

Lock keyboard bindings exactly:

```text
Move: A/D or Left/Right
Down: S or Down
Jump: Space
Action: X
Bomb: Z
Rope: C
```

Do not introduce:

```text
basic attack
melee
shoot
dash
wall jump
double jump
new ActionId values
```

## Required Per-Task Allowlists

Define read/write allowlists for:

```text
L01_01_INPUT
L01_02_PREFAB
L01_03_SPAWN
L02_01_ROUTE_CAMERA
L02_02_MAP_ADAPTER
L02_03_ROOM_AUDIT
L03_01_TOOLS
L03_02_HUD
L04_01_PLAYMODE
L04_02_FINAL
```

The allowlists must keep pure Character and MAP runtime contracts read-only unless a later task explicitly receives a separate change-control package.

## Required Result Format Lock

Every future result must include:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TESTS
BUILD
SCOPE_VALIDATION
FORBIDDEN_AUDIT
NEXT
```

Implementation task results must additionally include:

```text
LIVE_CONTRACTS_USED
REQUESTS_CONSUMED
ASSETS_WIRED
MANUAL_VERIFICATION
REGRESSION_BASELINE
```

## Required Report

Write:

```text
CLI/MCP/REPORTS/L00_02_RESULT.md
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
LOCKED_PATHS
TASK_ALLOWLISTS
CONTRACT_LOCKS
TEST_AND_BUILD_LOCKS
RESULT_FORMAT_LOCK
CHANGE_CONTROL
RISKS
SCOPE_VALIDATION
NEXT
```

PASS requires `LIVE_LOCK.md`, zero project code/assets changes, and a concrete allowlist for every remaining task.

## Completion

If PASS:

```text
Finalize L00_02 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L01_01_INPUT.
```
