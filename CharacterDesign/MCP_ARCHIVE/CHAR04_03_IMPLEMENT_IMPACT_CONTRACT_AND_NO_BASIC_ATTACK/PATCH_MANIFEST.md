# PATCH MANIFEST

```yaml
patch_id: CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK
patch_version: 2.0
revision_note: CHAR04_02 PASS/finalize 기준선을 검증하고 CHAR04_03 impact contract task 하나만 연다.
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
    CHAR03_03_CHAR03_MAP_ROOM_EXIT_AUDIT: COMPLETE
    CHAR04_01_IMPLEMENT_CARRY_DROP_AND_DIRECTIONAL_THROW: COMPLETE
    CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE: COMPLETE
    CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK: LOCKED
    CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE_RESULT.md
  exact_status: PASS
  required_text:
    - "Current Task after finalize: NONE"
    - "CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK"
    - "LOCKED 유지"
  sha256: e68259585ed2cfd4ec4baf01cccb732dc073f3a372551725e1c8c185e4d0366f
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR04_02_IMPLEMENT_STOMP_STUN_REMOVAL_AND_CONTACT_DAMAGE.md
  sha256: da237cb82eda9f807656d4cd7efd1226577b9d6ce704dd745649bb43a6e220bf
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md
  sha256: 8f45c9259196844d8d35d7b94bce3c0ba0c9f0904e77943663fac3034fb5fe8a
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 8dff146b9e2f49c42d2e80d566cea94dc87573326f376dd4e61b79753cf200ec
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: bfa20af08fb46c876b09fc77a521476a60f1edcfa158860c2d4784ee3a9776b9
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 15 COMPLETE / 1 CURRENT / 10 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_existing_character_editmode_tests: 100
  required_char04_03_editmode_tests: 10
  required_after_apply_character_editmode_tests: 110
forbids_started_task_prefixes:
  - CHAR04_04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md
    destination: CharacterDesign/MCP/TASKS/CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK.md
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
  - start_char04_04_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR04_02 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR04_02 REPORT에 `STATUS: PASS`, `Current Task after finalize: NONE`, CHAR04_03 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR04_03만 CURRENT이며 CHAR04_04 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
