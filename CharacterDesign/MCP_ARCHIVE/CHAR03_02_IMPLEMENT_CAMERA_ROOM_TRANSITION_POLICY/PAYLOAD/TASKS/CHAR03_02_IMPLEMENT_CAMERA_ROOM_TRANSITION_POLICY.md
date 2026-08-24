# TASK: CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY

```yaml
task_id: CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY
phase: CHAR03_MAP_ROOM_INTEGRATION
task_type: IMPLEMENTATION
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_CAMERA_ROOM_TRANSITION_POLICY_RUNTIME_AND_TESTS
```

## Objective

Implement the camera-room transition policy for character/map integration.

This task owns:

```text
prepared boundary crossing -> transition request to target camera room
unprepared or missing destination -> blocked by existing readiness gate
input KEEP
velocity KEEP
hysteresis against boundary ping-pong
high-speed and airborne boundary entry behavior
```

This task must not implement actual scene camera movement, Cinemachine, camera components, animation, prefab wiring, generated-map integration, terrain mutation, items, enemies, bombs, ropes, health, HUD, or presentation events beyond a pure transition decision/request model.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
CHAR03_01 Result: PASS
CHAR03_01 Result SHA-256: 3a3009d76b6b89e5bae44b6d743f866b6209728c3509e22b3ad7332063b9317b
CHAR03_01 contains: MAP world query / coordinate conversion    : CONNECTED
CHAR03_01 contains: Room boundary detection and readiness gate : IMPLEMENTED
CHAR03_01 contains: Current Task after finalize: NONE
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR03_03 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md`
11. `CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md`
12. `CharacterDesign/01_FIXED_SPEC/06_CHARACTER_MAP_INTEGRATION_RULES.md`
13. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
14. `CharacterDesign/04_TEST_FIXTURES/ROOM_TRANSITION_COURSE_SPEC.md`
15. Current character runtime under `Assets/_Game/Character/Runtime/`
16. Current `Assets/_Game/Character/Runtime/MapIntegration/`
17. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
18. MAP public coordinate/domain runtime under `Assets/_Game/Map/Runtime/WorldGeneration/Domain/`

Do not read or start any `CHAR03_03`, `CHAR04`, `CHAR05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/MapIntegration/**
Assets/_Game/Character/Runtime/RoomTransition/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/MapIntegration/**
Assets/_Game/Tests/EditMode/Character/RoomTransition/**
```

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md
```

Forbidden:

- Scene, prefab, camera component, Cinemachine, render camera, animation, or presentation object changes.
- Input suppression, input rewrite, input lock, or new inputactions.
- Velocity zeroing, velocity rewrite, teleport, snap, or physics body mutation.
- MAP runtime, MapDesign, authoring CSV, generator, validator, overlay, or Tilemap changes.
- Terrain mutation request implementation.
- Generated map route integration.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Pure Camera-Room Transition Policy

Implement a pure runtime policy that evaluates room transition state and emits a transition decision/request.

Required behavior:

```text
same active room -> no transition
prepared target room across boundary -> transition requested
unprepared target room -> blocked by existing readiness gate
missing target room -> blocked by existing readiness gate
transition request contains source room and target room
transition request does not move camera directly
transition request does not mutate player position
```

Use the `CharacterRoomBoundaryGate` and room identifiers from CHAR03_01. Do not duplicate readiness logic.

### 2. Input KEEP

The transition policy must preserve the input snapshot exactly.

Required behavior:

```text
Move intent unchanged
Jump/action/tool button states unchanged
Buffered input state unchanged
No transition-specific input lock reason added
```

If the policy accepts input in a context object, it must return the same input value or prove by API shape that it cannot mutate input.

### 3. Velocity KEEP

The transition policy must preserve velocity exactly.

Required behavior:

```text
horizontal velocity unchanged
vertical velocity unchanged
grounded/airborne transition decision does not alter velocity
blocked transition does not zero or clamp velocity
allowed transition does not zero or clamp velocity
```

### 4. Hysteresis

Implement deterministic hysteresis to prevent camera-room ping-pong near shared room boundaries.

Required baseline:

```text
Hysteresis margin: 0.25 world units
Stable target samples: 2 consecutive policy evaluations
```

The policy may use an equivalent stricter rule only if the report explains it and tests prove:

- crossing into a prepared room eventually requests transition;
- small oscillation around the shared boundary does not repeatedly flip rooms;
- returning across the boundary beyond the margin can request the reverse transition;
- high-speed boundary crossing still resolves to one target transition, not a ping-pong sequence.

### 5. High-Speed and Airborne Boundary Entry

The policy must not depend on grounded state.

Required behavior:

```text
grounded boundary crossing uses the same gate/policy as airborne crossing
high-speed crossing across one boundary emits at most one target-room transition
high-speed crossing into unprepared room is blocked
```

This task does not need full swept collision. If using previous/current sample evaluation, the policy must document the limitation and tests must cover at least one high-speed sample that crosses a shared room boundary in a single evaluation.

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
CameraRoomTransition_PreparedBoundaryCrossingRequestsTargetRoom
CameraRoomTransition_UnpreparedDestinationIsBlocked
CameraRoomTransition_MissingDestinationIsBlocked
CameraRoomTransition_KeepsInputSnapshot
CameraRoomTransition_KeepsVelocityForAllowedAndBlockedDecisions
CameraRoomTransition_HysteresisPreventsBoundaryPingPong
CameraRoomTransition_HysteresisAllowsReturnBeyondMargin
CameraRoomTransition_HighSpeedCrossingProducesSingleTransition
CameraRoomTransition_AirborneCrossingUsesSamePolicyAsGrounded
CameraRoomTransition_DoesNotReferenceSceneCameraOrPresentationTypes
```

Names may vary if they fit existing conventions, but the report must map actual test names to these ten required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 76
Expected result: PASS
```

The expected minimum is previous 66 plus at least 10 CHAR03_02 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md
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
CAMERA_ROOM_TRANSITION_POLICY
INPUT_KEEP
VELOCITY_KEEP
HYSTERESIS
BOUNDARY_GATE_INTEGRATION
HIGH_SPEED_AND_AIRBORNE_ENTRY
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR03_01 PASS and readiness gate implementation verified.
- [ ] Source registry marker/hash verified.
- [ ] Prepared boundary crossing requests target camera room.
- [ ] Unprepared and missing destinations are blocked.
- [ ] Input snapshot is kept exactly.
- [ ] Velocity is kept exactly for allowed and blocked decisions.
- [ ] Hysteresis prevents boundary ping-pong.
- [ ] Reverse transition is allowed only after crossing back beyond hysteresis rule.
- [ ] High-speed crossing produces at most one transition.
- [ ] Airborne crossing uses same policy as grounded crossing.
- [ ] No scene camera, prefab, Cinemachine, animation, or presentation mutation.
- [ ] No MAP runtime, Tilemap, MapDesign, inputactions, Packages, ProjectSettings, or legacy mutation.
- [ ] Character EditMode tests pass with at least 76 tests.
- [ ] Unity compile errors 0.
- [ ] CHAR03_03 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR03_02 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT` locked.
- Do not auto-open CHAR03_03.

If STATUS is FAIL or BLOCKED:

- Keep CHAR03_02 CURRENT.
- Do not open CHAR03_03.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
