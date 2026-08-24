# PATCH MANIFEST

```yaml
patch_id: CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES
patch_version: 2.0
revision_note: CHAR01 EXIT APPROVED 기준선을 검증하고 CHAR02_01 검증 task 하나만 연다.
requires_status:
  current_task: NONE
  task_states_required:
    CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP: COMPLETE
    CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES: COMPLETE
    CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT: COMPLETE
    CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES: COMPLETE
    CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR: COMPLETE
    CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING: COMPLETE
    CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT: COMPLETE
    CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES: LOCKED
    CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT_RESULT.md
  exact_status: PASS
  required_text:
    - "CHAR01 EXIT: APPROVED"
    - "CHAR02_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH"
    - "Current Task after finalize: NONE"
  sha256: e9abb9a337c7621b74e376f58193850c274a5f2b3937eec9c17495361599d15e
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md
  sha256: ce1f06036b4b75d44af17eb30ede14f69d148b9c097ef6dc691fd8fa1e4f2837
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md
  sha256: 678ed6579dfbd8df99ff00ae841829ea8243c3c477ad62fdc2b865a0dfa0624b
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 723221a568fdd7299f8ffaf843229bb9ec8a2e6f7fbd3ef4ff785189582b336d
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: a7c5eee8955badc15ac375f6b19a144f82f5583dcc778243c24de92f40a4e68d
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 7 COMPLETE / 1 CURRENT / 18 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_existing_character_editmode_tests: 36
  required_char02_01_editmode_tests: 8
forbids_started_task_prefixes:
  - CHAR02_02
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md
    destination: CharacterDesign/MCP/TASKS/CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES.md
    mode: create
forbidden_operations:
  - delete
  - overwrite_existing_task_file
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_runtime_or_test_code_during_patch_apply
  - modify_inputactions_or_asmdef_during_patch_apply
  - modify_scene_prefab_packages_projectsettings_mapdesign
  - modify_contract_schema_fixture_during_patch_apply
  - run_completed_task_package
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR01_04 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR01_04 REPORT에 `STATUS: PASS`, `CHAR01 EXIT: APPROVED`, `CHAR02_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR02_01만 CURRENT이며 CHAR02_02 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
