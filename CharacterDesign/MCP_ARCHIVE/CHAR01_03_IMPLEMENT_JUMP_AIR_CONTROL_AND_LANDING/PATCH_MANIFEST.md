# PATCH MANIFEST

```yaml
patch_id: CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING
patch_version: 2.0
revision_note: CHAR01_02 PASS/finalize 기준선을 검증하고 CHAR01_03 구현 task 하나만 연다.
requires_status:
  current_task: NONE
  task_states_required:
    CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP: COMPLETE
    CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES: COMPLETE
    CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT: COMPLETE
    CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES: COMPLETE
    CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR: COMPLETE
    CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING: LOCKED
    CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR_RESULT.md
  exact_status: PASS
  required_text:
    - "Current Task after finalize: NONE"
    - "CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING"
    - "LOCKED 유지"
  sha256: bc637e315cd123ea689977ce173fd70f848048bf7a7dcb527e8de2dd53553186
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR.md
  sha256: 448516103d18a2fea2716e08d60929a735e462aa0e9f7774a30d4fb8695127b4
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md
  sha256: 4f28c237637c9ace93e87250240cd61d1c8db9cbb384ed5ea5d038e5bdf9b99d
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 5cfed347b7554e0b83103602f2f686c66f5e3ac505cb3b92a0286693c3313136
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 59fc4f08176fc6654b5facd57087c7e105e96028ca9d57d7bf6688bf0f04e501
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 5 COMPLETE / 1 CURRENT / 20 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_char01_03_editmode_test_cases: 12
  required_char01_01_02_regression_test_cases: 24
forbids_started_task_prefixes:
  - CHAR01_04
  - CHAR02
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md
    destination: CharacterDesign/MCP/TASKS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md
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
- CHAR01_02 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR01_02 REPORT에 `STATUS: PASS`, `Current Task after finalize: NONE`, CHAR01_03 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR01_03만 CURRENT이며 CHAR01_04 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
