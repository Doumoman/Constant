# TASK: L03_02_HUD

Full name:

```text
LIVE03_02_IMPLEMENT_HUD_PRESENTATION_AND_RUN_FEEDBACK_BINDING
```

## Objective

Implement and bind the live HUD, presentation event consumer, and run feedback
surface for the playable live scene.

This task may create HUD and presentation runtime code and may wire HUD objects
into the live prefab or live scene. It must not edit Character runtime, MAP
runtime, input assets, tool consumers, tests, packages, project settings, save
data, audio, or animation.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L03_02.md
L03_01 RESULT exists
L03_01 RESULT sha256: 275fc4ff71a146ee819444f19a9b548ab00de379c5f6cb4a422dc941aa2b9563
L03_01 RESULT contains STATUS: PASS
L03_01 RESULT contains None. Runtime consumers only; no scene, prefab, HUD, audio, animation, save, or input asset wiring.
L03_01 RESULT contains Current Task after finalize: NONE
L03_01 RESULT contains Next Task auto-opened: NO
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L04_01 and later tasks are locked
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
8. `CLI/MCP/REPORTS/L01_02_RESULT.md`
9. `CLI/MCP/REPORTS/L01_03_RESULT.md`
10. `CLI/MCP/REPORTS/L02_03_RESULT.md`
11. `CLI/MCP/REPORTS/L03_01_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
15. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
16. `Assets/_Game/Character/Runtime/RunState/**`
17. `Assets/_Game/Character/Runtime/Survival/**`
18. `Assets/_Game/Character/Runtime/Equipment/**`
19. `Assets/_Game/Character/Runtime/Presentation/**` if it exists
20. `Assets/_Game/Live/Runtime/Run/**`
21. `Assets/_Game/Live/Runtime/Input/**`
22. `Assets/_Game/Live/Runtime/Movement/**`
23. `Assets/_Game/Live/Runtime/Rooms/**`
24. `Assets/_Game/Live/Runtime/Tools/**`
25. `Assets/_Game/Live/Runtime/Hud/**` if it exists
26. `Assets/_Game/Live/Runtime/Presentation/**` if it exists
27. `Assets/_Game/Live/Prefabs/**`
28. `Assets/_Game/Scenes/Live/**`
29. `Packages/manifest.json`
30. `ProjectSettings/ProjectSettings.asset`

Use search before opening broad trees. Local source signatures are authority if
they differ from reports.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/Hud/**
Assets/_Game/Live/Runtime/Presentation/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/Live/**
CLI/MCP/REPORTS/L03_02_RESULT.md
```

Forbidden writes:

```text
Assets/_Game/Character/Runtime/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Live/Input/**
Assets/_Game/Live/Runtime/Tools/**
Assets/_Game/Tests/**
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

Implement a live HUD and feedback layer that reads existing Character and Live
state rather than redefining gameplay state.

Required HUD data behavior:

```text
Read Character run inventory state for bombs and ropes.
Read Character health/status/run failure data if the local contracts expose it.
Read current room/run/session state from the Live run session.
Project this data into a deterministic Live HUD view model.
Expose stable empty or unavailable values when optional data is absent, and report the exact absence.
Do not store authoritative gameplay state in the HUD.
```

Required HUD view behavior:

```text
Create a runtime binder that can drive existing UI Text, TMP_Text, or image/fill components based on installed packages.
Show health, bombs, ropes, current room or run status, and latest feedback message when source data exists.
Handle missing UI references without throwing during scene load.
Keep layout simple and readable in the existing live scene resolution.
Do not add new packages.
```

Required presentation event behavior:

```text
Consume existing Character presentation event request data from CHAR05_04 if the local contract exists.
Preserve ordering and dedupe semantics from the Character presentation contract.
Convert accepted events into a Live presentation event log or feedback queue.
Do not call audio systems, Animator, timeline, save data, or scene loading APIs.
Expose diagnostics for duplicate event, unknown event, missing target, and missing sink.
```

Required run feedback binding:

```text
Expose a Live feedback sink that later systems can call with tool, room, spawn, damage, death, and run-failure messages.
For this task, bind only data already available from existing Live/Character contracts.
Do not edit L03_01 tool consumers; if tool feedback cannot be hooked without editing Tools, create the receiving surface and report that wiring remains deferred.
```

Required scene/prefab binding:

```text
Add the HUD/presentation components to the existing live scene or live prefab using the shortest stable local pattern.
If a Canvas/EventSystem already exists, reuse it.
If none exists, create the minimal Canvas/EventSystem required for HUD display.
Do not modify input actions.
Do not move or rename the existing player prefab.
Do not wire tool consumers in this task unless the scene already has the required components and no forbidden file must change.
```

Recommended files, unless local patterns indicate better names:

```text
Assets/_Game/Live/Runtime/Hud/CharacterLiveHudSnapshot.cs
Assets/_Game/Live/Runtime/Hud/CharacterLiveHudSnapshotSource.cs
Assets/_Game/Live/Runtime/Hud/CharacterLiveHudBinder.cs
Assets/_Game/Live/Runtime/Presentation/CharacterLiveFeedbackMessage.cs
Assets/_Game/Live/Runtime/Presentation/CharacterLiveFeedbackLog.cs
Assets/_Game/Live/Runtime/Presentation/CharacterLivePresentationEventConsumer.cs
Assets/_Game/Live/Prefabs/CharacterLiveHud.prefab
```

Keep the final file set smaller if the local codebase supports a cleaner
implementation.

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless runner unavailable for an approved environment reason
HUD snapshot smoke: bombs, ropes, health/status, room/run state project deterministically from live run state
HUD binder smoke: missing UI refs do not throw; populated refs receive expected text/fill values
Presentation smoke: ordered events are accepted once; duplicate event is ignored or rejected without duplicate feedback
Scene/prefab audit: live scene or prefab contains exactly one HUD binding path and no broken serialized references
Scope audit: changed files only in allowed paths plus result
Forbidden dependency audit: no Character/MAP/Input/Tools/test/package/project setting edits; no audio, Animator, save, scene load, or future task references
```

Do not create PlayMode test files in this task. Editor or in-memory smoke code
may be used if removed before finalize.

If Unity cannot run, write `STATUS: BLOCKED` unless MCP rules explicitly allow
an environment-only substitute.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L03_02_RESULT.md
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

Include the implementation sections:

```text
LIVE_CONTRACTS_USED
REQUESTS_CONSUMED
ASSETS_WIRED
MANUAL_VERIFICATION
REGRESSION_BASELINE
```

For this task, `REQUESTS_CONSUMED` may include only HUD snapshot or
presentation event request contracts. It must not include input, spawn,
route/camera, tool, terrain, rope, damage, death, run failure, save, audio,
animation, or scene loading requests.

For this task, `ASSETS_WIRED` must list:

```text
HUD runtime components
presentation or feedback runtime components
live HUD prefab if created
live scene or prefab HUD binding if created
no input asset, audio, animation, save, Character runtime, MAP runtime, or tool consumer wiring
```

## Completion

PASS requires:

```text
Live HUD snapshot/binder implemented under allowed paths
Presentation or feedback queue implemented under allowed paths
Live scene or prefab has a valid HUD binding path, or a precise non-blocking absence reason is reported
Ordering and dedupe are preserved for presentation feedback
Compile clean or environment-approved equivalent
Character baseline preserved
No Character runtime, MAP runtime, input asset, tool consumer, test, package, project setting, save/audio/animation changes
```

If PASS:

```text
Finalize L03_02 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L04_01_PLAYMODE.
```
