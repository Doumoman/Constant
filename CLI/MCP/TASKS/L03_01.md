# TASK: L03_01_TOOLS

Full name:

```text
LIVE03_01_IMPLEMENT_CARRY_DROP_THROW_BOMB_ROPE_CONSUMERS
```

## Objective

Implement live runtime consumers for the existing Character request contracts
for carry, drop, throw, bomb, and rope.

This task must consume Character-layer requests in the Live layer only. Do not
edit Character or MAP runtime code. Do not add new player actions. Do not wire
the scene, prefab, HUD, audio, animation, save data, or PlayMode tests in this
package.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L03_01.md
L02_03 RESULT exists
L02_03 RESULT sha256: 35f955f1026035be0300aeb33d623a4f241ccac22fb1b901e53f114fce53f518
L02_03 RESULT contains STATUS: PASS
L02_03 RESULT contains LIVE02_EXIT_DECISION: APPROVED
L02_03 RESULT contains L03_01 ENTRY: ELIGIBLE FOR SEPARATE PACKAGE
L02_03 RESULT contains Current Task after finalize: NONE
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
L03_02 and later tasks are locked
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
9. `CLI/MCP/REPORTS/L01_03_RESULT.md`
10. `CLI/MCP/REPORTS/L02_03_RESULT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_REQUESTS_RESULT.md`
12. `CharacterDesign/MCP/REPORTS/CHAR05_02_IMPLEMENT_ROPE_TRAVERSAL_REQUESTS_RESULT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md`
14. `CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md`
15. `CharacterDesign/MCP/REPORTS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md`
16. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
17. `Assets/_Game/Character/Runtime/Actions/**`
18. `Assets/_Game/Character/Runtime/Equipment/**`
19. `Assets/_Game/Character/Runtime/Traversal/**`
20. `Assets/_Game/Character/Runtime/RunState/**`
21. `Assets/_Game/Character/Runtime/MapIntegration/**`
22. `Assets/_Game/Live/Runtime/Input/**`
23. `Assets/_Game/Live/Runtime/Run/**`
24. `Assets/_Game/Live/Runtime/Movement/**`
25. `Assets/_Game/Live/Runtime/Rooms/**`
26. `Assets/_Game/Live/Runtime/Adapters/Map/**`
27. `Assets/_Game/Live/Runtime/Tools/**` if it already exists
28. `Packages/manifest.json`

Use search before opening broad trees. Local source signatures are authority if
they differ from reports.

## Allowed Writes

```text
Assets/_Game/Live/Runtime/Tools/**
CLI/MCP/REPORTS/L03_01_RESULT.md
```

Forbidden writes:

```text
Assets/_Game/Character/Runtime/**
Assets/_Game/Map/Runtime/**
Assets/_Game/Live/Input/**
Assets/_Game/Live/Prefabs/**
Assets/_Game/Scenes/**
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

Create a live tool consumer layer under:

```text
Assets/_Game/Live/Runtime/Tools/**
```

The implementation must consume existing Character contract outputs only. It
must not create new CharacterActionId values, new input bindings, or alternate
gameplay rules.

Required carry/drop/throw behavior:

```text
Consume Character carry request data and attach exactly one eligible live carry target.
Reject missing, inactive, already-carried, or out-of-range targets deterministically.
Consume Character drop request data and release the carried target at the requested drop point or validated fallback point.
Consume Character throw request data and release the carried target with the Character contract's requested throw vector or impulse.
Preserve ownership: one carrier may carry one target; one target may be carried by one carrier.
Expose diagnostics for no target, invalid target, already carrying, target already carried, blocked drop, and no carried target.
Do not implement basic attack, melee, shooting, dash, wall jump, or double jump.
```

Required bomb behavior:

```text
Consume existing Character bomb placement or terrain-damage request data.
Spend inventory only through the existing Character run state/inventory result contract.
Emit a live terrain command or sink call for the bomb blast request.
If no terrain sink exists yet, create a narrow Live-side interface and in-memory command queue under Tools.
Do not mutate Tilemap, MAP runtime data, generated MAP data, or scene assets in this task.
Expose diagnostics for no bomb stock, invalid placement, missing terrain sink, and duplicate request consumption.
```

Required rope behavior:

```text
Consume existing Character rope traversal or rope placement request data.
Spend inventory only through the existing Character run state/inventory result contract.
Emit a live rope command or sink call for anchor/rope creation data.
If no rope sink exists yet, create a narrow Live-side interface and in-memory command queue under Tools.
Do not instantiate rope prefabs or edit scenes in this task.
Expose diagnostics for no rope stock, invalid anchor, blocked anchor, missing rope sink, and duplicate request consumption.
```

Required request handling:

```text
Every request must have an idempotency key, sequence id, frame id, or equivalent deterministic dedupe path if the Character contract exposes one.
Each accepted request is consumed exactly once.
Rejected requests must not mutate live state.
Consumers must be callable from future scene components without requiring static global state.
Consumers must be testable with in-memory doubles.
```

Allowed absence handling:

```text
If a Character request contract has a different exact name than expected, adapt to the local source signature and report it.
If a required Character request contract is absent, write STATUS: BLOCKED with the exact missing type or member.
If inventory spend contracts are insufficient for bomb/rope live consumption, write STATUS: BLOCKED unless a no-mutation diagnostic-only consumer can satisfy the current Character contract.
```

Recommended files, unless local patterns indicate better names:

```text
Assets/_Game/Live/Runtime/Tools/CharacterLiveToolUseResult.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveToolDiagnosticKind.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveToolRequestLedger.cs
Assets/_Game/Live/Runtime/Tools/ICharacterLiveCarryTarget.cs
Assets/_Game/Live/Runtime/Tools/ICharacterLiveTerrainCommandSink.cs
Assets/_Game/Live/Runtime/Tools/ICharacterLiveRopeCommandSink.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveCarryConsumer.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveDropConsumer.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveThrowConsumer.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveBombConsumer.cs
Assets/_Game/Live/Runtime/Tools/CharacterLiveRopeConsumer.cs
```

Keep the final shape smaller if the local contracts allow one cohesive
implementation without losing clarity.

## Required Verification

Run or report the nearest available equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required unless runner unavailable for an approved environment reason
Carry smoke: eligible target accepted once; duplicate request rejected or ignored; invalid target rejected without mutation
Drop smoke: carried target released once; no-carried-target path rejected without mutation
Throw smoke: carried target released with requested vector/impulse; duplicate request does not throw twice
Bomb smoke: valid request spends/queues exactly once; no-stock and duplicate paths do not queue terrain commands
Rope smoke: valid request spends/queues exactly once; blocked/no-stock and duplicate paths do not queue rope commands
Scope audit: changed files only in allowed paths plus result
Forbidden dependency audit: Live code references Character/MAP public contracts only and does not reference tests, editor-only APIs, Tilemap mutation, scene lookup, UI, audio, save, animation, or future tasks
```

Do not create PlayMode test files in this task. In-memory EditMode or short-lived
manual smoke code is allowed if it is removed before finalize.

If Unity cannot run, write `STATUS: BLOCKED` unless MCP rules explicitly allow
an environment-only substitute.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L03_01_RESULT.md
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

For this task, `REQUESTS_CONSUMED` must list only the implemented tool request
types from existing Character contracts. It must not include route/camera,
spawn, damage, death, run failure, HUD, presentation, save, audio, animation,
or scene requests.

For this task, `ASSETS_WIRED` must include:

```text
None. Runtime consumers only; no scene, prefab, HUD, audio, animation, save, or input asset wiring.
```

## Completion

PASS requires:

```text
Live tool consumers implemented under Assets/_Game/Live/Runtime/Tools/**
Carry/drop/throw/bomb/rope request consumption works against existing Character contracts
Accepted requests are consumed exactly once
Rejected or duplicate requests do not mutate live state
Bomb and rope do not directly mutate Tilemap/MAP data/scene assets
Compile clean or environment-approved equivalent
Character baseline preserved
No Character runtime, MAP runtime, scene, prefab, input asset, test, package, project setting, HUD, save/audio/animation changes
```

If PASS:

```text
Finalize L03_01 as COMPLETE.
Current Task after finalize: NONE
Do not auto-open L03_02_HUD.
```
