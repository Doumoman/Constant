# PATCH MANIFEST

```yaml
patch_id: CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT
patch_version: 2.0
revision_note: CHAR01_03 PASS/finalize 기준선을 검증하고 CHAR01 종료 감사 task 하나만 연다.
requires_status:
  current_task: NONE
  task_states_required:
    CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP: COMPLETE
    CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES: COMPLETE
    CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT: COMPLETE
    CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES: COMPLETE
    CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR: COMPLETE
    CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING: COMPLETE
    CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT: LOCKED
    CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING_RESULT.md
  exact_status: PASS
  required_text:
    - "Current Task after finalize: NONE"
    - "CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT"
    - "LOCKED 유지"
  sha256: 373fb206c50790fc99add891783f99bc969a67273da26e6dbd906ea108cad5d2
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR01_03_IMPLEMENT_JUMP_AIR_CONTROL_AND_LANDING.md
  sha256: 4f28c237637c9ace93e87250240cd61d1c8db9cbb384ed5ea5d038e5bdf9b99d
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md
  sha256: ce1f06036b4b75d44af17eb30ede14f69d148b9c097ef6dc691fd8fa1e4f2837
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 9e15f7a344ead6d5840fe507b6a53419009da4a7e9dbfc193692993576f261dc
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 38dff006b1fee0f86a88d131f4e29fcd500246131920f64a909a47e916dd1925
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 6 COMPLETE / 1 CURRENT / 19 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_character_editmode_test_cases: 36
forbids_started_task_prefixes:
  - CHAR02
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md
    destination: CharacterDesign/MCP/TASKS/CHAR01_04_CHAR01_CORE_MOVEMENT_EXIT_AUDIT.md
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
- CHAR01_03 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR01_03 REPORT에 `STATUS: PASS`, `Current Task after finalize: NONE`, CHAR01_04 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR01_04만 CURRENT이며 CHAR02_01 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
