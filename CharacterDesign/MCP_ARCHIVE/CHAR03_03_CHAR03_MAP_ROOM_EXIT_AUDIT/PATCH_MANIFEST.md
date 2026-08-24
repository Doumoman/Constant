# PATCH MANIFEST

```yaml
patch_id: CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT
patch_version: 2.0
revision_note: CHAR03_02 PASS/finalize 기준선을 검증하고 CHAR03_03 exit audit task 하나만 연다.
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
    CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY: COMPLETE
    CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT: LOCKED
    CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY_RESULT.md
  exact_status: PASS
  required_text:
    - "Camera room transition policy              : IMPLEMENTED"
    - "Current Task after finalize: NONE"
    - "CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT"
  sha256: a99a1ed377aed266632ee1da2245610cbcc97015a67af23bc31ac3fc81092082
requires_prior_result:
  path: CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
  exact_status: PASS
  required_text:
    - "MAP world query / coordinate conversion    : CONNECTED"
    - "Room boundary detection and readiness gate : IMPLEMENTED"
  sha256: 3a3009d76b6b89e5bae44b6d743f866b6209728c3509e22b3ad7332063b9317b
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY.md
  sha256: bee0ef965f6aeeb7505eb26e2b9274d27102fc68d879c6394301ce3651860a32
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md
  sha256: 644919d9843a92333a4ba7fb069ffd07f3e26ac1c8ffca32fdfc05620c3a690e
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 3a2ef6be38926a8b2535c3e350c1cae69ca83fc01d45cc93c48fd727cb985b41
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 7811fd3e5ead8e0f0f4af51b620b47969327375c2d3a10024d0797c5f34bf2b8
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 12 COMPLETE / 1 CURRENT / 13 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  active_map_runtime: Game.Map.Runtime
  required_existing_character_editmode_tests: 76
  required_char03_03_editmode_tests: 0
forbids_started_task_prefixes:
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md
    destination: CharacterDesign/MCP/TASKS/CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT.md
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
  - start_char04_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR03_01/CHAR03_02 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR03_02 REPORT에 `STATUS: PASS`, camera transition implemented, `Current Task after finalize: NONE`, CHAR03_03 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR03_03만 CURRENT이며 CHAR04_01 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
