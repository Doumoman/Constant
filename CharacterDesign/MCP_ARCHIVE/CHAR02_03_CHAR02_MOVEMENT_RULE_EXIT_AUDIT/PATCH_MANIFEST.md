# PATCH MANIFEST

```yaml
patch_id: CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT
patch_version: 2.0
revision_note: CHAR02_02 PASS/finalize 기준선을 검증하고 CHAR02_03 exit audit task 하나만 연다.
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
    CHAR02_01_VALIDATE_TWO_CELL_HEIGHT_AND_GAP_RULES: COMPLETE
    CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT: COMPLETE
    CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT: LOCKED
    CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT_RESULT.md
  exact_status: PASS
  required_text:
    - "Current Task after finalize: NONE"
    - "CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT"
    - "LOCKED 유지"
  sha256: 09e033b8d559afbefa7f761f4367c5294e06ab9f44cdd0f153966bf0af5cb192
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR02_02_VALIDATE_THREE_CELL_FAILURE_AND_FORBIDDEN_MOVEMENT.md
  sha256: e290545cb0ff8a64f2de1e30c1426522a2d9757a18b29c65e703b30c9a115458
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
  sha256: e99b725df83b4795a4963709c74335580183a821eca48e1dd51fbf734a10270c
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 99fd8db79e06dc0bebc46a5ba39eef5b5e36fa4c4547fdafc61dfb2252d77edd
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: d4fe3a80b300202135fab4358a7f2419eb40a0d9da654bec6cfc34175942505e
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 9 COMPLETE / 1 CURRENT / 16 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_existing_character_editmode_tests: 52
  required_char02_03_editmode_tests: 0
forbids_started_task_prefixes:
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
    destination: CharacterDesign/MCP/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
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
  - start_char03_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR02_02 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR02_02 REPORT에 `STATUS: PASS`, `Current Task after finalize: NONE`, CHAR02_03 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR02_03만 CURRENT이며 CHAR03_01 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
