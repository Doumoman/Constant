# TASK: CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE

```yaml
task_id: CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE
phase: CHAR05_EQUIPMENT_SURVIVAL_AND_RUN
task_type: IMPLEMENTATION
created: 2026-08-24
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_RUN_STATE_PRESENTATION_CONTRACT_RUNTIME_AND_TESTS
```

## Objective

Implement the pure run-state, HUD snapshot, and presentation bridge contract.

This task owns:

```text
run inventory state for bomb and rope counts
bomb/rope spend request consumption as run-state data updates
run health/status snapshot from survival state and run failure request
HUD snapshot value object for health, bombs, ropes, status, and return token
presentation event requests for damage, death, run failure, bomb, rope, and inventory changes
deterministic event ordering and request-only presentation bridge
no actual HUD, Canvas, TextMeshPro, audio, animation, scene reload, save mutation, prefab, or GameObject mutation
```

This task must not implement real UI widgets, scene transition, retry flow, save data, audio playback, animation playback, camera effects, live physics wiring, prefab spawning, or future integration behavior.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE.md
CHAR05_03 Result: PASS
CHAR05_03 Result SHA-256: d982d596a0efad856db4e8dbaf475538172b9ac8ab11baf4af85bb87b982c03c
CHAR05_03 contains: Current Task after finalize: NONE
CHAR05_03 contains: CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE
CHAR05_03 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR05_05 and later tasks: LOCKED
```

If any entry gate is false, write a BLOCKED report and do not modify project code.

## Mandatory Read Order

Read these files in order:

1. `CharacterDesign/MCP/00_MCP_ENTRYPOINT.md`
2. `CharacterDesign/MCP/01_CHARACTER_LOCKED_RULES.md`
3. `CharacterDesign/MCP/02_MCP_WORK_RULES.md`
4. `CharacterDesign/MCP/03_CHARACTER_DATA_RULES.md`
5. `CharacterDesign/MCP/04_UNITY_MCP_RULES.md`
6. `CharacterDesign/MCP/05_CHANGE_CONTROL_RULES.md`
7. `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md`
8. `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md`
9. `CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md`
10. `CharacterDesign/MCP/TASKS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST.md`
11. `CharacterDesign/MCP/REPORTS/CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md`
12. `CharacterDesign/MCP/TASKS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT.md`
13. `CharacterDesign/MCP/REPORTS/CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT.md`
14. `CharacterDesign/MCP/TASKS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE.md`
15. `CharacterDesign/MCP/REPORTS/CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md`
16. `CharacterDesign/01_FIXED_SPEC/01_CHARACTER_GAMEPLAY_RULES.md`
17. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
18. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
19. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
20. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
21. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_INVENTORY_SCHEMA.md`
22. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_DAMAGE_SCHEMA.md`
23. `CharacterDesign/03_DATA_SCHEMA/CHARACTER_ACTION_SCHEMA.md`
24. Current character runtime under `Assets/_Game/Character/Runtime/`
25. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
26. Legacy run-state/HUD/presentation examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Player/**`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/UI/**`
    - `Assets/_Legacy/_Game/Core/State/**`
    - `Assets/_Legacy/_Game/UI/**`

Do not read or start any `CHAR05_05` or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/RunState/**
Assets/_Game/Character/Runtime/Presentation/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/RunState/**
Assets/_Game/Tests/EditMode/Character/Presentation/**
```

Conditional bridge writes:

```text
Assets/_Game/Character/Runtime/Survival/**
Assets/_Game/Character/Runtime/Equipment/**
Assets/_Game/Tests/EditMode/Character/Survival/**
Assets/_Game/Tests/EditMode/Character/Equipment/**
```

Use conditional bridge writes only if a tiny adapter is required to convert existing Survival or Equipment request types into run-state or presentation bridge input. Do not rewrite health, hazard, bomb, rope, combat, movement, or MAP behavior.

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md
```

Forbidden:

- Runtime or test changes outside allowed RunState/Presentation paths and conditional bridge paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, audio, UI prefab, Canvas, TextMeshPro, save data, or legacy code changes.
- Actual HUD rendering, UI text assignment, audio playback, animation trigger, camera shake, particle effect, scene reload, checkpoint load, PlayerPrefs, save-file mutation, GameObject activation/deactivation, or player transform teleport.
- Health, hazard, death, bomb, rope, combat, movement, room transition, or MAP behavior changes.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Adding ActionId values beyond the existing locked set.
- Animator-event-owned run-state or presentation authority.
- Unity UI, audio, scene, save, or physics callback-owned authority.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Run Inventory State

Implement immutable run inventory state and spend application.

Required behavior:

```text
run inventory records actor id, bomb count, and rope count
default starting bomb count is 4
default starting rope count is 4
bomb spend request decreases bomb count by requested amount
rope spend request decreases rope count by requested amount
spend request cannot reduce count below zero
invalid or mismatched actor request creates no change
application returns a new state and does not mutate input
```

Use existing CHAR05_01/CHAR05_02 spend request types as inputs where possible. If a bridge is needed, prefer RunState-side adapter methods.

### 2. Run Status and Health Snapshot

Implement run-state snapshot values from Survival contracts.

Required behavior:

```text
run state records actor id, health snapshot, inventory snapshot, run status, and optional return token
active player health state produces active run status
player run failure request marks run status as failed
enemy/non-player death or run-neutral events do not fail the player run
return destination token remains opaque data
state update returns a new state and does not reload scenes or mutate save data
```

### 3. HUD Snapshot Bridge

Implement HUD snapshot data only.

Required behavior:

```text
HUD snapshot exposes current health, max health, invulnerability active flag, bomb count, rope count, run status, and optional return token
HUD snapshot is deterministic from run state
HUD snapshot does not reference Unity UI, Canvas, TextMeshPro, GameObject, SceneManager, AudioSource, Animator, or PlayerPrefs
```

This is a data bridge only. Real HUD binding is outside this task.

### 4. Presentation Event Requests

Implement presentation event request values.

Required behavior:

```text
damage application result can create damage presentation event request
death request can create death presentation event request
run failure request can create run failure presentation event request
bomb placement/explosion request can create bomb presentation event request
rope placement request can create rope presentation event request
inventory spend application can create inventory changed presentation event request
events record type, actor/source id, optional amount, optional world/cell coordinate, and deterministic sequence id
events are requests only and do not play audio, animation, particles, camera, UI, or scene effects
```

### 5. Event Ordering and Deduplication

Implement deterministic bridge output rules.

Required behavior:

```text
events from a single bridge call are ordered by explicit priority then sequence id
duplicate equivalent events in the same bridge call are emitted once
ordering is stable across repeated calls with the same inputs
```

### 6. Authority and Forbidden Feature Guard

Keep decision authority pure.

Required behavior:

```text
no Animator event authority
no Unity physics callback authority
no Unity UI authority
no audio authority
no SceneManager authority
no save/PlayerPrefs authority
no direct MAP or Tilemap mutation
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
ActionId locked set remains unchanged
```

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
RunInventory_DefaultBombAndRopeCountsAreCentralized
RunInventory_BombAndRopeSpendRequestsDecreaseCounts
RunInventory_SpendCannotGoBelowZeroOrMutateInput
RunState_HealthSnapshotReflectsSurvivalState
RunState_PlayerRunFailureMarksRunFailedWithReturnToken
RunState_NonPlayerDeathDoesNotFailPlayerRun
HudSnapshot_ContainsHealthInventoryStatusAndReturnToken
HudSnapshot_IsDataOnlyAndDoesNotUseUnityUiSceneAudioOrSave
PresentationBridge_DamageDeathAndRunFailureCreateEventRequests
PresentationBridge_BombRopeAndInventoryEventsAreRequestsOnly
PresentationBridge_EventsAreDeterministicOrderedAndDeduplicated
RunStatePresentationRuntime_DoesNotUseAnimatorPhysicsSceneHudSaveAudioOrForbiddenActions
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 158
Expected result: PASS
```

The expected minimum is previous 146 plus at least 12 CHAR05_04 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md
```

The report must include:

```text
TASK
STATUS
SUMMARY
READ
CHANGED
CREATED
TEST
UNITY
RUN_INVENTORY_STATE
RUN_STATUS_AND_HEALTH_SNAPSHOT
HUD_SNAPSHOT_BRIDGE
PRESENTATION_EVENT_REQUESTS
EVENT_ORDERING_AND_DEDUPLICATION
AUTHORITY_AND_FORBIDDEN_FEATURE_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR05_03 PASS/hash verified.
- [ ] Source registry marker/hash verified.
- [ ] Default bomb and rope counts are centralized.
- [ ] Bomb and rope spend requests decrease counts.
- [ ] Spend cannot reduce counts below zero or mutate input state.
- [ ] Run health snapshot reflects Survival health state.
- [ ] Player run failure marks run failed with return token.
- [ ] Non-player death does not fail player run.
- [ ] HUD snapshot exposes health, bombs, ropes, status, and return token.
- [ ] HUD snapshot is data only.
- [ ] Damage, death, and run failure create presentation event requests.
- [ ] Bomb, rope, and inventory events are presentation requests only.
- [ ] Event ordering is deterministic and deduplicated.
- [ ] No actual UI, scene, save, audio, animation, camera, prefab, GameObject, or presentation side effect exists.
- [ ] Animator events and physics callbacks are not authority.
- [ ] Forbidden basic attack/movement features remain absent.
- [ ] ActionId locked set remains unchanged.
- [ ] Character EditMode tests pass with at least 158 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR05_05 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR05_04 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT` locked.
- Do not auto-open CHAR05_05.

If STATUS is FAIL or BLOCKED:

- Keep CHAR05_04 CURRENT.
- Do not open CHAR05_05.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
