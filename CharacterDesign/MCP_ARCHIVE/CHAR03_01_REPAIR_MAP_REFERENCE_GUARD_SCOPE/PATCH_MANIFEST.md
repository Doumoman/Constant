# PATCH MANIFEST

```yaml
patch_id: CHAR03_01_REPAIR_MAP_REFERENCE_GUARD_SCOPE
patch_version: 2.0-repair
revision_note: CHAR03_01 BLOCKED 결과를 근거로 current task body를 dependency-guard repair revision으로 교체한다. CHAR03_02는 열지 않는다.
requires_status:
  current_task: TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
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
    CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE: CURRENT
    CHAR03_02_IMPLEMENT_CAMERA_ROOM_TRANSITION_POLICY: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE_RESULT.md
  exact_status: BLOCKED
  required_text:
    - "GroundProbe_DoesNotRequireMapOrTilemapTypes"
    - "write-scope 충돌"
    - "CHAR03_02는 열리지 않는다"
  sha256: b4e37ef7dd56fc1a081969619ace9b25b4edd62f9cef4167cd0bd88ded9e963f
requires_current_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
  sha256: e4127a04a3b75840650bba788cf606c13370c05879674f5e5403eca9a7ef91a5
requires_prior_result:
  path: CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
  exact_status: PASS
  required_text:
    - "CHAR02 EXIT: APPROVED"
    - "CHAR03_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH"
    - "Current Task after finalize: NONE"
  sha256: e118ac9d286252bad58387e2675b32d6eee38abf7f592ecb06b6d591d6370fb5
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md
  sha256: e6cd5601cdcb25511dc3e61f08353b1b2310ee66c4fd7a63aa0599566194f1fc
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 6f474711a60b488def706b077621e183f694250d5752e33eb12ae6dae3d76d8f
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 1ce2aca14f9b48474a2f63b29915295baed7ee751b8a6948112414378e3947f8
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 10 COMPLETE / 1 CURRENT / 15 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  active_map_runtime: Game.Map.Runtime
  required_existing_character_editmode_tests: 57
  required_after_repair_character_editmode_tests: 66
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
    mode: replace
repair_apply_note: 기존 current task file이 존재해야 하며, previous BLOCKED report와 현재 task hash가 일치할 때만 본 revision 교체를 허용한다.
forbidden_operations:
  - delete
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_runtime_or_test_code_during_patch_apply
  - modify_inputactions_or_asmdef_during_patch_apply
  - modify_scene_prefab_packages_projectsettings_mapdesign
  - start_char03_02_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `TASKS/CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE.md`이다.
- CHAR03_01 previous REPORT가 `STATUS: BLOCKED`이며 guard/write-scope 문구와 hash가 일치한다.
- 현재 설치된 CHAR03_01 task file hash가 manifest와 일치한다.
- CHAR02_03 PASS REPORT와 source registry hash가 일치한다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 Current Task는 그대로 CHAR03_01이며 CHAR03_02 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체한다.
- Character runtime/test 수정은 Task 실행 단계에서만 허용된다.
