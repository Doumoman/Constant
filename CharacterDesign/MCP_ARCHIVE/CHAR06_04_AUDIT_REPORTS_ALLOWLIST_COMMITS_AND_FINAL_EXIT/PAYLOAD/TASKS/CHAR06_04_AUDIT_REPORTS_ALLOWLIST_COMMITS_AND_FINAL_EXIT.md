# TASK: CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT

```yaml
task_id: CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT
phase: CHAR06_GENERATED_MAP_AND_FINAL_VALIDATION
task_type: FINAL_EXIT_AUDIT
created: 2026-08-25
workflow: MCP_INBOX_PATCH_ONLY
write_scope: REPORT_AND_AUDIT_ARTIFACTS_ONLY
```

## Objective

Perform the final Character harness EXIT audit.

This task owns:

```text
all Character RESULT report status audit
task and report hash ledger
allowlist and scope audit
git status and change evidence audit
commit evidence recording when available
forbidden feature and dependency direction audit
CHAR06_03 validation evidence audit
final Character exit decision
no gameplay implementation, no runtime rewrite, no test rewrite, no MAP rewrite, no build rerun unless evidence is inconsistent
```

This is the last task in the current Character harness sequence. It must not open any later Character task.

## Entry Gate

Before auditing, verify:

```text
Current Task: TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md
CHAR06_03 Result: PASS
CHAR06_03 Result SHA-256: ff92b0e6854a237937ce90236fb714b6f82cc85b4c33653271bb62c4d484ee00
CHAR06_03 contains: Character EditMode 177/177 PASS
CHAR06_03 contains: MAP EditMode 13,536/13,536 PASS
CHAR06_03 contains: Build Finished, Result: Success
CHAR06_03 contains: Current Task after finalize: NONE
CHAR06_03 contains: CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT
CHAR06_03 contains: LOCKED 유지
Source Registry marker: REGISTRY_STATE: FILLED_BY_CHAR00_01
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
No later Character task is open
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
10. Current task body: `CharacterDesign/MCP/TASKS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT.md`
11. All installed task files under `CharacterDesign/MCP/TASKS/`
12. All reports under `CharacterDesign/MCP/REPORTS/`
13. `CharacterDesign/01_FIXED_SPEC/**`
14. `CharacterDesign/03_DATA_SCHEMA/**`
15. `CharacterDesign/04_TEST_FIXTURES/**`
16. `CharacterDesign/05_GENERATED_OUTPUT_SCHEMA/**`
17. Current character runtime under `Assets/_Game/Character/Runtime/`
18. Current character tests under `Assets/_Game/Tests/EditMode/Character/`
19. MAP public contracts referenced by `CHAR00_SOURCE_REGISTRY.md`
20. `Packages/manifest.json`
21. git status, git diff summary, and latest commit identity if available

Do not read or start any task outside the Character harness sequence.

## Allowed Writes

Allowed writes:

```text
CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_ARTIFACTS/**
```

Forbidden writes:

```text
Assets/**
Packages/**
ProjectSettings/**
MapDesign/**
CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
CharacterDesign/MCP/TASKS/**
Builds/**
Temp/**
```

Forbidden:

- Runtime code changes.
- Test code changes.
- asmdef changes.
- Scene, prefab, physics layer asset, inputactions, Packages, ProjectSettings, MAP runtime, MAP authoring data, Tilemap, camera, animation, audio, UI, save data, build output, or legacy code changes.
- Running destructive cleanup commands.
- Reverting user changes.
- Creating commits or pushing unless the user explicitly instructs it.
- Test count reduction, Ignore insertion, test-result editing, console-log filtering that hides errors, or build-result manipulation.
- Opening any later task.

If final approval requires a forbidden write or missing evidence, write `STATUS: BLOCKED` and record the exact missing evidence.

## Required Audit

### 1. Report Status and Hash Ledger

Required behavior:

```text
read all Character reports from CHAR00_01 through CHAR06_03
verify every required report has independent STATUS: PASS
verify every phase exit report records APPROVED where applicable
record sha256 for every required task file and report file
record missing, duplicate, or mismatched reports as BLOCKED
```

Required report set:

```text
CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES_RESULT.md
CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES_RESULT.md
CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md
CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md
CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT_RESULT.md
CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW_RESULT.md
CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md
CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK_RESULT.md
CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT_RESULT.md
CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST_RESULT.md
CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT_RESULT.md
CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE_RESULT.md
CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md
CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT_RESULT.md
CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES_RESULT.md
CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md
CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD_RESULT.md
```

### 2. Allowlist and Scope Audit

Required behavior:

```text
audit current git status
audit changed paths against all task allowlists and reports
confirm no unauthorized changes in Assets, Packages, ProjectSettings, MapDesign, scenes, prefabs, inputactions, UI, audio, save, legacy, MAP runtime, MAP authoring data, or build outputs
record pre-existing dirty files separately
record generated reports and audit artifacts separately
```

### 3. Forbidden Feature and Dependency Audit

Required behavior:

```text
confirm no added basic attack, melee, shoot, dash, wall jump, or double jump
confirm ActionId locked set remains unchanged
confirm Character depends only on approved MAP public coordinate/query/mutation contracts
confirm MAP runtime does not depend on Character runtime
confirm no Animator, Unity physics callback, UI, audio, scene, save, Tilemap, GameObject, or prefab authority was introduced into pure policies
```

### 4. Validation Evidence Audit

Required behavior:

```text
verify CHAR06_03 compile error count is 0
verify CHAR06_03 Character EditMode count is at least 177 and PASS
verify CHAR06_03 MAP EditMode rerun is 13,536/13,536 PASS
verify CHAR06_03 PlayMode discovery is successful with 0 tests and 0 errors
verify CHAR06_03 active build target build succeeds
verify CHAR06_03 new project console error count is 0
verify CHAR06_03 source changes are 0
```

Do not rerun full tests or build unless report evidence is internally inconsistent.

### 5. Commit Evidence Audit

Required behavior:

```text
record latest commit hash and branch if available
record whether Character changes are committed or uncommitted
do not create a commit or push in this task
if project policy requires a commit before final exit and no commit evidence exists, report STATUS: BLOCKED
if no commit is required by the active workflow, record commit status as informational evidence
```

## Required Report

Write:

```text
CharacterDesign/MCP/REPORTS/CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT_RESULT.md
```

The report must include:

- `TASK`
- independent line `STATUS: PASS`, `STATUS: FAIL`, or `STATUS: BLOCKED`
- `SUMMARY`
- `READ`
- `CHANGED`
- `CREATED`
- `REPORT_STATUS_LEDGER`
- `TASK_AND_REPORT_HASH_LEDGER`
- `ALLOWLIST_SCOPE_AUDIT`
- `FORBIDDEN_FEATURE_AUDIT`
- `DEPENDENCY_DIRECTION_AUDIT`
- `VALIDATION_EVIDENCE_AUDIT`
- `COMMIT_EVIDENCE_AUDIT`
- `FINAL_EXIT_DECISION`
- `OUT_OF_SCOPE_FINDINGS`
- `DONE CONDITIONS`
- `NEXT`

Required final decision line when PASS:

```text
CHARACTER_FINAL_EXIT_DECISION: APPROVED
```

Required report facts:

```text
Entry gate verification result
CHAR06_03 report hash used
source registry hash used
all report statuses
all report hashes
all task hashes
git status summary
latest commit hash and branch if available
authorized changed path summary
pre-existing dirty file summary
CHAR06_03 compile, EditMode, PlayMode, build, console, and scope evidence
confirmation that no later task was opened
```

PASS requires all required reports PASS, all exit decisions approved where applicable, no unauthorized scope changes, validation evidence accepted, no forbidden feature introduction, dependency direction preserved, and final decision approved.

## Completion and Finalization Rule

If PASS:

```text
Finalize CHAR06_04 as COMPLETE.
Set Current Task after finalize: NONE.
Set Character harness final state: COMPLETE.
Do not auto-open any later task.
```

If FAIL or BLOCKED:

```text
Keep CHAR06_04 as CURRENT.
Do not open any later task.
Report exact missing report, hash mismatch, scope violation, validation evidence gap, commit evidence blocker, or forbidden dependency.
```

