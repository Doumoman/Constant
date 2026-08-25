# TASK: L04_02_FINAL

Full name:

```text
LIVE04_02_RUN_BUILD_AND_FINAL_EXIT_AUDIT
```

## Objective

Run the final Character Live Integration validation: report ledger audit,
compile, Character EditMode baseline, Character Live PlayMode suite, player
build, scope audit, dependency audit, and final exit decision.

This task is validation-only except for the build output and the final report.
Do not implement features or edit production runtime, scenes, prefabs, input
assets, tests, packages, project settings, Character runtime, MAP runtime, save
data, audio, or animation.

## Entry Gate

Verify:

```text
Current Task: CLI/MCP/TASKS/L04_02.md
L04_01 RESULT exists
L04_01 RESULT sha256: 1f005cffa58cd3c2e409a797a151642d19e7750643512986e43a2714a3dac299
L04_01 RESULT contains STATUS: PASS
L04_01 RESULT contains 신규 PlayMode: 9/9 PASS
L04_01 RESULT contains Character EditMode 177/177 PASS
L04_01 RESULT contains Current Task after finalize: NONE
L04_01 RESULT contains Next Task auto-opened: NO
CLI/MCP/INPUTS/LIVE_LOCK.md exists
CLI/MCP/INPUTS/LIVE_LOCK.md contains LOCK_STATE: FILLED_BY_L00_02
No task after L04_02 is open
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
7. `CLI/MCP/REPORTS/L00_01_RESULT.md`
8. `CLI/MCP/REPORTS/L00_02_RESULT.md`
9. `CLI/MCP/REPORTS/L01_01_RESULT.md`
10. `CLI/MCP/REPORTS/L01_02_RESULT.md`
11. `CLI/MCP/REPORTS/L01_03_RESULT.md`
12. `CLI/MCP/REPORTS/L02_01_RESULT.md`
13. `CLI/MCP/REPORTS/L02_02_RESULT.md`
14. `CLI/MCP/REPORTS/L02_03_RESULT.md`
15. `CLI/MCP/REPORTS/L03_01_RESULT.md`
16. `CLI/MCP/REPORTS/L03_02_RESULT.md`
17. `CLI/MCP/REPORTS/L04_01_RESULT.md`
18. `CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md`
19. `Assets/_Game/Live/**`
20. `Assets/_Game/Character/Runtime/**`
21. `Assets/_Game/Map/Runtime/**`
22. `Assets/_Game/Tests/PlayMode/Character/**`
23. `Packages/manifest.json`
24. `ProjectSettings/ProjectSettings.asset`
25. build settings or active build target data available through Unity

Use search before opening broad trees. Local source signatures are authority if
they differ from reports.

## Allowed Writes

```text
Builds/CLI_Live_Final/**
CLI/MCP/REPORTS/L04_02_RESULT.md
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
CLI/MCP/INPUTS/**
Temp/**
```

## Required Audit

Verify prior result ledger:

```text
L00_01_RESULT.md sha256 4e982e431d05a0c01dccac9062327068ea51a7ff713dfe281796a3dd9846d69b PASS
L00_02_RESULT.md sha256 bc4b91f03d9177fc41044430081374d0f9c3f575aefeba9bab7d302f16ac24a4 PASS
L01_01_RESULT.md sha256 4e269dddc9d49ab8a146dd4fe759b2471c516229d7828a44f84541ab3a41c983 PASS
L01_02_RESULT.md sha256 906657a671a710336a64a508f9f213d8172acfd855269efe67797fe25cabb6f3 PASS
L01_03_RESULT.md sha256 6c652ca2728bb3a36a51048371494aac3d917b219627ef7c0ff010ab669da88b PASS
L02_01_RESULT.md sha256 a0e4288ba390cbed70263681efd7ee235ee84a82baa3b434f2a0ad15309e4585 PASS
L02_02_RESULT.md sha256 01bfe28aa2f4bf00245cecae1000ba4ab383cea4a8c297fe962f69ce33ba61d6 PASS
L02_03_RESULT.md sha256 35f955f1026035be0300aeb33d623a4f241ccac22fb1b901e53f114fce53f518 PASS
L03_01_RESULT.md sha256 275fc4ff71a146ee819444f19a9b548ab00de379c5f6cb4a422dc941aa2b9563 PASS
L03_02_RESULT.md sha256 48c5b2c466fb2d70c88afac9770d522c1132640870563402ca76b22015acaa2c PASS
L04_01_RESULT.md sha256 1f005cffa58cd3c2e409a797a151642d19e7750643512986e43a2714a3dac299 PASS
```

Audit completed live surfaces:

```text
Input: Move, Down, Jump, Action, Bomb, Rope only; SafeDrop remains derived from Down+Action.
Spawn: generated or manual start snapshot is consumed exactly once.
Route/camera: generated route and readiness source can drive room transition and camera target without teleporting player.
Generated MAP: snapshot/readiness/route/world query adapter is ready for scene use; ungenerated cells are not empty playable space.
Tools: carry, drop, throw, bomb, rope consumers accept once and reject duplicates without mutation.
HUD/presentation: HUD displays run state; presentation events are ordered and deduped through Character contract.
PlayMode: L04_01 live integration smoke suite executed and passed.
```

Audit dependency direction:

```text
Live may reference Character runtime and MAP runtime public contracts.
Character runtime must not reference Live.
MAP runtime must not reference Live or Character.
Tests may reference Live/Character/MAP for validation only.
No gameplay rule is redefined in Live when a Character policy exists.
```

Audit forbidden features:

```text
No basic attack, melee, shooting, dash, wall jump, or double jump.
No new CharacterActionId or new input binding.
No Tilemap mutation, MAP generator rewrite, MAP facade, save mutation, audio, Animator, timeline, scene loading gameplay flow, or UI package changes.
No future task files opened.
```

## Required Verification

Run or report exact equivalents:

```text
Unity compile: PASS required
Character EditMode baseline: 177/177 PASS required
Character Live PlayMode tests: 9/9 PASS required
MAP EditMode: 13,536 PASS or anchor-valid with no MAP runtime diff; rerun if local policy requires it
Player build: PASS required, output under Builds/CLI_Live_Final/**
Console error audit: 0 unexpected errors
Scope audit: changed files only in allowed paths plus final report
```

Build guidance:

```text
Prefer StandaloneWindows64 if available, matching prior Character final build.
If the active build target differs, use the active target and report the exact target.
Do not edit build settings. Use explicit scene paths if needed.
Recommended output: Builds/CLI_Live_Final/LiveBuild.exe
```

If Unity cannot compile, run tests, or build, write `STATUS: BLOCKED` with the
exact failing command, console errors, and missing environment requirement.

## Required Report

Write:

```text
CLI/MCP/REPORTS/L04_02_RESULT.md
```

Include:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TESTS
BUILD
PRIOR_RESULTS
LIVE_SURFACE_AUDIT
DEPENDENCY_DIRECTION
FORBIDDEN_AUDIT
SCOPE_VALIDATION
CONSOLE_AUDIT
FINAL_EXIT
NEXT
```

For this task:

```text
CHANGED must be None or empty.
CREATED must contain only the build output path and CLI/MCP/REPORTS/L04_02_RESULT.md.
REQUESTS_CONSUMED, if included, must state that this task consumes no production requests.
ASSETS_WIRED, if included, must state None. Final audit and build only.
```

## Completion

PASS requires:

```text
All prior live integration results are verified by status and sha256.
Unity compile passes.
Character EditMode 177/177 passes.
Character Live PlayMode 9/9 passes.
Player build succeeds.
Dependency direction is clean.
Forbidden audit is clean.
No code, asset, test, scene, prefab, input, package, project setting, Character, MAP, save/audio/animation write occurred.
```

If PASS, include exactly:

```text
LIVE_INTEGRATION_FINAL_EXIT_DECISION: APPROVED
Character live integration harness final state: COMPLETE
Current Task after finalize: NONE
Next Task auto-opened: NO
```
