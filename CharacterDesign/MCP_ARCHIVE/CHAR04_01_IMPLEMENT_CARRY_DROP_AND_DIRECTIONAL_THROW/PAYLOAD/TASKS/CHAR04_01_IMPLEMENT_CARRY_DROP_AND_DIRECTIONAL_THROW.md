# TASK: CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW

```yaml
task_id: CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW
phase: CHAR04_INTERACTION_AND_COMBAT
task_type: IMPLEMENTATION
created: 2026-08-23
workflow: MCP_INBOX_PATCH_ONLY
write_scope: LIMITED_INTERACTION_RUNTIME_AND_TESTS
```

## Objective

Implement the first character interaction core: carryable search, pickup, safe drop, and directional throw.

This task owns:

```text
single carry slot
explicit carry candidate query and priority
1x1-or-smaller carryable pickup
stunned small enemy pickup contract shape
Down+Action safe drop
Up/Left/Right+Action directional throw
drop refusal when destination is blocked
owner collision grace policy for carried/thrown object requests
no basic attack addition
```

This task must not implement stomp damage, enemy stun/removal, thrown-object impact damage, environmental impact, bomb, rope, health, HUD, presentation, actual physics layer asset changes, prefab wiring, or scene objects.

## Entry Gate

Before changing anything, verify:

```text
Current Task: TASKS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW.md
CHAR03_03 Result: PASS
CHAR03 EXIT: APPROVED
CHAR04_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH
CHAR03_03 Result SHA-256: 28e83c356e53683370bc15a787a8f80700ea3fa3052523df6c4b09f9d4812f52
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
CHAR04_02 and later tasks: LOCKED
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
10. `CharacterDesign/MCP/TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md`
11. `CharacterDesign/MCP/REPORTS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md`
12. `CharacterDesign/01_FIXED_SPEC/02_CHARACTER_INPUT_RULES.md`
13. `CharacterDesign/01_FIXED_SPEC/04_CHARACTER_INTERACTION_RULES.md`
14. `CharacterDesign/01_FIXED_SPEC/05_CHARACTER_COMBAT_RULES.md`
15. `CharacterDesign/01_FIXED_SPEC/07_CHARACTER_TEST_RULES.md`
16. `CharacterDesign/04_TEST_FIXTURES/INTERACTION_COURSE_SPEC.md`
17. `CharacterDesign/04_TEST_FIXTURES/COMBAT_COURSE_SPEC.md`
18. Current character runtime under `Assets/_Game/Character/Runtime/`
19. Current character EditMode tests under `Assets/_Game/Tests/EditMode/Character/`
20. Legacy carry examples for read-only reference only:
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/CarrySystem.cs`
    - `Assets/_Legacy/StarNight/Scripts/Runtime/Objects/CarryableObject2D.cs`
    - `Assets/_Legacy/_Game/Interaction/Runtime/Carry/**`

Do not read or start any `CHAR04_02`, `CHAR04_03`, `CHAR04_04`, `CHAR05`, or `CHAR06` task body.

## Allowed Writes

Allowed runtime writes:

```text
Assets/_Game/Character/Runtime/Interaction/**
```

Allowed test writes:

```text
Assets/_Game/Tests/EditMode/Character/Interaction/**
```

Required report:

```text
CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md
```

Forbidden:

- Runtime or test changes outside the allowed Interaction paths.
- asmdef changes unless compile proves the new files are not included by existing `Game.Character.Runtime` or `Game.Character.Tests.EditMode`; if asmdef change is unavoidable, BLOCK and report instead.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MapDesign, MAP runtime, Tilemap, camera, animation, presentation, or legacy code changes.
- Stomp, enemy damage, enemy removal, thrown impact damage, environmental impact, bomb, rope, health, HUD, or run-state implementation.
- Adding a basic attack, melee, shoot, dash, wall jump, or double jump.
- Directly mutating carryable internal state without using request/contract value objects.
- Opening or installing any future task.
- Editing `CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md` or `CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md` during task execution.

## Required Implementation

### 1. Carryable Contract and Candidate Query

Implement pure C# interaction contract types for carry candidates and carryable operations.

Required behavior:

```text
Candidate has stable id or handle.
Candidate has bounds or size measured in logical cells/world units.
Candidate declares whether it is carryable.
Candidate can represent ordinary carryables and stunned small enemies.
Only 1x1-or-smaller candidates are eligible.
Query returns exactly one selected candidate using explicit priority.
No candidate -> no pickup.
Already carrying -> no pickup.
```

The candidate selection priority must be deterministic. Recommended priority:

```text
1. reachable/interactable true
2. lower explicit priority number or higher priority score, whichever convention is chosen and documented
3. shorter distance to player
4. stable id tie-break
```

The report must document the actual priority rule.

### 2. Single Carry Slot

Implement a single-slot carry state/controller.

Required behavior:

```text
pickup fills the empty slot
slot exposes held object id/handle
second pickup while occupied is rejected
drop or throw clears the slot only on accepted request
rejected drop/throw keeps the slot unchanged
```

### 3. Safe Drop

Implement Down+Action safe drop decision.

Required behavior:

```text
drop target is near the player's feet or configured safe drop offset
drop requires destination occupancy/space check
blocked destination -> reject, no overlap placement
accepted destination -> returns a placement request
accepted request clears carry slot
drop does not modify scene or physics object directly
```

### 4. Directional Throw

Implement Up/Left/Right+Action directional throw decision.

Required behavior:

```text
Up+Action throws upward
Left+Action throws left
Right+Action throws right
No directional throw without a held object
Throw returns a throw request with direction, impulse/speed, owner id, and held object id
Accepted throw clears carry slot
Rejected throw keeps carry slot
No thrown impact damage is applied in this task
```

If both up and horizontal directions are pressed, the task must define and test a deterministic priority. Recommended: Up has priority over horizontal for throw direction.

### 5. Owner Collision Grace Policy

Implement a central request/policy model for owner collision grace after pickup/drop/throw.

Required behavior:

```text
carried object records owner id
drop/throw request includes owner collision grace duration or frame count
grace value is centralized, validated, and deterministic
grace policy does not modify Unity physics layers directly
```

### 6. Forbidden Feature Guard

Keep forbidden movement/combat features absent:

```text
no basic attack
no melee
no shoot
no dash
no wall jump
no double jump
```

Do not add ActionId values beyond the existing locked set.

## Required Test Coverage

Add deterministic EditMode tests covering at least these behaviors:

```text
CarryCandidateQuery_SelectsSingleCandidateByDeterministicPriority
CarryCandidateQuery_RejectsOversizedOrNonCarryableCandidates
CarrySlot_PickupFillsSingleSlotAndRejectsSecondPickup
CarryDrop_DownActionCreatesSafeDropPlacementRequest
CarryDrop_BlockedDestinationRejectsAndKeepsHeldObject
CarryThrow_RightActionCreatesRightThrowRequest
CarryThrow_LeftActionCreatesLeftThrowRequest
CarryThrow_UpActionCreatesUpThrowRequestAndHasPriority
CarryThrow_RejectedThrowKeepsHeldObject
CarryOwnerCollisionGrace_IsCentralizedAndIncludedInDropAndThrowRequests
CarryContract_UsesRequestsAndDoesNotMutateCarryableInternals
InteractionRuntime_DoesNotIntroduceBasicAttackDashWallJumpDoubleJumpOrShoot
```

Names may vary if they fit existing conventions, but the report must map actual test names to these twelve required behaviors.

Run:

```text
Unity Version: 6000.3.8f1
EditMode Assembly: Game.Character.Tests.EditMode
Expected minimum tests: 88
Expected result: PASS
```

The expected minimum is previous 76 plus at least 12 CHAR04_01 tests.

PlayMode is not required for this task.

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md
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
CARRY_CANDIDATE_QUERY
SINGLE_CARRY_SLOT
SAFE_DROP
DIRECTIONAL_THROW
OWNER_COLLISION_GRACE
FORBIDDEN_FEATURE_GUARD
DEPENDENCY_DIRECTION
SCOPE_VALIDATION
DEPENDENCY_LEDGER
OUT_OF_SCOPE_FINDINGS
DONE CONDITIONS
NEXT
```

## Done Conditions

All done conditions must be checked in the report:

- [ ] CHAR03_03 PASS and CHAR03 EXIT approved verified.
- [ ] Source registry marker/hash verified.
- [ ] Candidate query selects exactly one candidate by deterministic priority.
- [ ] Oversized and non-carryable candidates are rejected.
- [ ] Single carry slot accepts first pickup and rejects second pickup.
- [ ] Down+Action safe drop returns placement request.
- [ ] Blocked safe drop rejects without overlap and keeps held object.
- [ ] Up/Left/Right directional throws return deterministic throw requests.
- [ ] Rejected throw keeps held object.
- [ ] Owner collision grace is centralized and included in drop/throw requests.
- [ ] Carryable internals are not mutated directly.
- [ ] Forbidden basic attack/dash/wall jump/double jump/shoot remain absent.
- [ ] Character EditMode tests pass with at least 88 tests.
- [ ] Unity compile errors 0.
- [ ] Scope validation completed.
- [ ] CHAR04_02 remains locked.

## Completion Rule

If STATUS is PASS:

- Finalize CHAR04_01 to COMPLETE.
- Set Current Task after finalize to NONE.
- Keep `CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE` locked.
- Do not auto-open CHAR04_02.

If STATUS is FAIL or BLOCKED:

- Keep CHAR04_01 CURRENT.
- Do not open CHAR04_02.

The NEXT section must include:

```text
Current Task after finalize: NONE
Next Task auto-opened: NO (`CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE`는 LOCKED 유지, 새 MCP_INBOX 패키지로만 개방)
```

only when PASS/finalized. If not PASS, state why the task remains CURRENT.
