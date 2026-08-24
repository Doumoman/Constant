# PATCH MANIFEST

```yaml
patch_id: CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE
patch_version: 2.0
revision_note: CHAR02_03 PASS/finalize 기준선을 검증하고 CHAR03_01 MAP query/boundary gate task 하나만 연다.
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
    CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT: COMPLETE
    CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE: LOCKED
    CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
  exact_status: PASS
  required_text:
    - "CHAR02 EXIT: APPROVED"
    - "CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH"
    - "Current Task after finalize: NONE"
  sha256: e118ac9d286252bad58387e2675b32d6eee38abf7f592ecb06b6d591d6370fb5
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
  sha256: 6c4b7f0a9e047db07d3c3c1b667f6b74e619ddddef7d1c1bafa889da52ad2250
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
  sha256: e4127a04a3b75840650bba788cf606c13370c05879674f5e5403eca9a7ef91a5
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 9ff8309d5a8e257207a7f850b05b6bd5fba35b0abbc5c9d450b6b39eb1ce9a87
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 985d003919e6e3cae107c6625fde10b70249a73f59b3ef44cd99a1e812a84670
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 10 COMPLETE / 1 CURRENT / 15 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  active_map_runtime: Game.Map.Runtime
  required_existing_character_editmode_tests: 57
  required_char03_01_editmode_tests: 8
  required_after_apply_character_editmode_tests: 65
forbids_started_task_prefixes:
  - CHAR03_02
  - CHAR03_03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
    destination: CharacterDesign/MCP/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
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
  - run_completed_task_package
  - start_char03_02_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR02_03 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR02_03 REPORT에 `STATUS: PASS`, `CHAR02 EXIT: APPROVED`, `CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`, `Current Task after finalize: NONE` 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR03_01만 CURRENT이며 CHAR03_02 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
