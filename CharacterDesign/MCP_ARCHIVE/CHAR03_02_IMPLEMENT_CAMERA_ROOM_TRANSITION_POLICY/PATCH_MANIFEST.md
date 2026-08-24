# PATCH MANIFEST

```yaml
patch_id: CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY
patch_version: 2.0
revision_note: CHAR03_01 PASS/finalize 기준선을 검증하고 CHAR03_02 camera room transition policy task 하나만 연다.
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
    CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE: COMPLETE
    CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY: LOCKED
    CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
  exact_status: PASS
  required_text:
    - "MAP world query / coordinate conversion    : CONNECTED"
    - "Room boundary detection and readiness gate : IMPLEMENTED"
    - "Current Task after finalize: NONE"
  sha256: 3a3009d76b6b89e5bae44b6d743f866b6209728c3509e22b3ad7332063b9317b
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
  sha256: e6cd5601cdcb25511dc3e61f08353b1b2310ee66c4fd7a63aa0599566194f1fc
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
  sha256: bee0ef965f6aeeb7505eb26e2b9274d27102fc68d879c6394301ce3651860a32
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: cc45c584d9d830933bdc400dda5673f945285a4c99243788ace2d86ff6dcaca6
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 1affc45f56336d3826a8f7af37ca826461bf0205a59158e1445b8737dd31c564
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 11 COMPLETE / 1 CURRENT / 14 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  active_map_runtime: Game.Map.Runtime
  required_existing_character_editmode_tests: 66
  required_char03_02_editmode_tests: 10
  required_after_apply_character_editmode_tests: 76
forbids_started_task_prefixes:
  - CHAR03_03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
    destination: CharacterDesign/MCP/TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
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
  - start_char03_03_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR03_01 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR03_01 REPORT에 `STATUS: PASS`, MAP query connected, readiness gate implemented, `Current Task after finalize: NONE` 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR03_02만 CURRENT이며 CHAR03_03 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
