# TASK: L04_01_PLAYMODE

Full name:

```text
LIVE04_01_CREATE_PLAYMODE_KEYBOARD_AND_GENERATED_RUN_SMOKE
```

## Objective

Create PlayMode smoke tests that validate the live integration stack from
keyboard input through generated run, route/camera, tools, HUD, and feedback.

This task creates test code only. Do not edit production runtime, scenes,
prefabs, input assets, Character runtime, MAP runtime, packages, project
settings, save data, audio, animation, or build outputs.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L04_01.md
L03_02 RESULT exists
L03_02 RESULT sha256: 48c5b2c466fb2d70c88afac9770d522c1132640870563402ca76b22015acaa2c
L03_02 RESULT contains STATUS: PASS
L03_02 RESULT contains live scene HUD binding
L03_02 RESULT contains no input asset, audio, animation, save, Character runtime, MAP runtime, or tool consumer wiring
L03_02 RESULT contains Current Task after finalize: NONE
L03_02 RESULT contains Next Task auto-opened: NO
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L04_02 is locked
```

If false, write `STATUS: BLOCKED`.

## Read

Read in order:

1. `CLI/MCP/ENTRY.md`
2. `CLI/MCP/RULES.md`
3. `CLI/MCP/STATUS.md`
4. `CLI/MCP/MASTER.md`
5. `CLI/MCP/INPUTS/LIVE_SRC.md`
6. `CLI/MCP/INPUTS/LIVE_LOCK.md`
7. `CLI/MCP/REPORTS/L00_02_RESULT.md`
8. `CLI/MCP/REPORTS/L01_01_RESULT.md`
9. `CLI/MCP/REPORTS/L01_02_RESULT.md`
10. `CLI/MCP/REPORTS/L01_03_RESULT.md`
11. `CLI/MCP/REPORTS/L02_03_RESULT.md`
12. `CLI/MCP/REPORTS/L03_01_RESULT.md`
13. `CLI/MCP/REPORTS/L03_02_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
15. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
16. `Assets/_Game/Live/Input/**`
17. `Assets/_Game/Live/Runtime/Input/**`
18. `Assets/_Game/Live/Runtime/Run/**`
19. `Assets/_Game/Live/Runtime/Movement/**`
20. `Assets/_Game/Live/Runtime/Rooms/**`
21. `Assets/_Game/Live/Runtime/Adapters/Map/**`
22. `Assets/_Game/Live/Runtime/Tools/**`
23. `Assets/_Game/Live/Runtime/Hud/**`
24. `Assets/_Game/Live/Runtime/Presentation/**`
25. `Assets/_Game/Live/Prefabs/**`
26. `Assets/_Game/Scenes/Live/**`
27. `Assets/_Game/Character/Runtime/**`
28. `Assets/_Game/Map/Runtime/**`
29. `Assets/_Game/Tests/**` if it exists
30. `Packages/manifest.json`
31. `ProjectSettings/ProjectSettings.asset`

Use search before opening broad trees. Local source signatures are authority if
they differ from reports.

## Allowed Writes

```text
Assets/_Game/Tests/PlayMode/Character/**
CLI/MCP/REPORTS/L04_01_RESULT.md
```

Forbidden writes:

```text
Assets/_Game/Character/Runtime/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Live/Runtime/**
Assets/_Game/Live/Input/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/**
Packages/**
ProjectSettings/**
MapDesign/**
CharacterDesign/**
CLI/MCP/STATUS.md
CLI/MCP/MASTER.md
CLI/MCP/TASKS/**
CLI/MCP/INPUTS/**
Builds/**
Temp/**
```

## Required Implementation

Create focused PlayMode tests under:

```text
Assets/_Game/Tests/PlayMode/Character/**
```

Use existing runtime APIs. Test-only doubles, fixtures, or helpers may be
created under the same PlayMode test path. Do not add product hooks to make the
tests pass.

Required PlayMode coverage:

```text
Live scene boot smoke:
  Load the live test scene or instantiate the live prefab through the existing project path.
  Verify run bootstrap/session, player rig, movement driver, room/camera driver, HUD binder, and HUD texts are present when expected.
  Verify no broken serialized HUD references and no console errors.

Keyboard input smoke:
  Use Unity Input System test devices or the nearest installed test fixture.
  Verify Move left/right, Down, Jump, Action, Bomb, and Rope inputs flow through CharacterLiveInputAdapter/Source using locked action names and bindings.
  Verify Move/Down remain axes and Jump/Action/Bomb/Rope map to locked CharacterActionId values.

Generated run smoke:
  Build a public MAP-shaped generated sample or reuse a deterministic test fixture from the live adapter.
  Project through CharacterLiveGeneratedMapAdapter.
  Start a live run from CharacterGeneratedMapStartSnapshot.
  Route from room A to room B using projected route/readiness sources.
  Verify room state and camera target update while player position is not teleported.
  Verify ungenerated cells are blocked through ICharacterMapWorldQuery-compatible queries.

Tool consumer smoke:
  Instantiate L03_01 live tool consumers with in-memory carry targets and command sinks.
  Verify carry, drop, throw, bomb, and rope accepted paths consume exactly once.
  Verify duplicate and rejected paths do not mutate live state.
  Verify terrain and rope outputs remain queued command data, not Tilemap or scene mutation.

HUD and feedback smoke:
  Verify HUD snapshot reads run inventory/health/status/room data.
  Verify presentation events become ordered feedback once.
  Verify duplicate presentation events do not duplicate feedback.
  Verify HUD text updates from feedback without audio, Animator, save, or scene load calls.
```

Recommended files, unless local patterns indicate better names:

```text
Assets/_Game/Tests/PlayMode/Character/Game.Character.Live.PlayMode.Tests.asmdef
Assets/_Game/Tests/PlayMode/Character/CharacterLiveInputPlayModeTests.cs
Assets/_Game/Tests/PlayMode/Character/CharacterLiveScenePlayModeTests.cs
Assets/_Game/Tests/PlayMode/Character/CharacterLiveGeneratedRunPlayModeTests.cs
Assets/_Game/Tests/PlayMode/Character/CharacterLiveToolsHudPlayModeTests.cs
```

Keep the suite smaller if a combined test class is clearer and still validates
all required behavior.

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless runner unavailable for an approved environment reason
New Character Live PlayMode tests: PASS required
Scene boot smoke: PASS
Keyboard input smoke: PASS
Generated run route/camera smoke: PASS
Tool consumer smoke: PASS
HUD/presentation feedback smoke: PASS
Console error audit: 0 unexpected errors
Scope audit: changed files only in allowed paths plus result
Forbidden dependency audit: no product runtime, scene, prefab, input asset, Character runtime, MAP runtime, package, project setting, save/audio/animation edits
```

If Unity cannot run, write `STATUS: BLOCKED` unless MCP rules explicitly allow
an environment-only substitute. A test file that compiles but is not executed is
not enough for PASS.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L04_01_RESULT.md
```

Include the common locked sections:

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

Include these validation sections:

```text
PLAYMODE_RESULTS
SCENE_BOOT_SMOKE
KEYBOARD_INPUT_SMOKE
GENERATED_RUN_SMOKE
TOOL_CONSUMER_SMOKE
HUD_FEEDBACK_SMOKE
CONSOLE_AUDIT
REGRESSION_BASELINE
```

For this task:

```text
CHANGED must be None or empty unless an existing PlayMode test asmdef under the allowed path had to be updated.
CREATED must contain only files under Assets/_Game/Tests/PlayMode/Character/** and the report.
REQUESTS_CONSUMED, if included, must state that only tests exercise existing consumers; this task adds no production request consumers.
ASSETS_WIRED, if included, must state None. Test-only fixtures or scene loading only.
```

## Completion

PASS requires:

```text
PlayMode tests implemented under Assets/_Game/Tests/PlayMode/Character/**
New PlayMode tests execute and pass
Scene boot, keyboard input, generated run route/camera, tools, HUD, and feedback smoke coverage is present
Compile clean
Character EditMode 177/177 baseline preserved
No production runtime, scene, prefab, input asset, Character runtime, MAP runtime, package, project setting, build, save/audio/animation changes
No future task auto-opened
```

If PASS:

```text
Finalize L04_01 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L04_02_FINAL.
```
