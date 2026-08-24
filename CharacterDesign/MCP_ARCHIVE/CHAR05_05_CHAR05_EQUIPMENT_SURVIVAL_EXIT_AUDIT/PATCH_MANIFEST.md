# PATCH MANIFEST

```yaml
patch_id: CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT
patch_version: 2.0
revision_note: CHAR05_04 PASS/finalize 기준선을 검증하고 CHAR05_05 exit audit task 하나만 연다.
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
    CHAR04_03_IMPLEMENT_IMPACT_CONTRACT_AND_NO_BASIC_ATTACK: COMPLETE
    CHAR04_04_CHAR04_INTERACTION_COMBAT_EXIT_AUDIT: COMPLETE
    CHAR05_01_IMPLEMENT_BOMB_AND_TERRAIN_MUTATION_REQUEST: COMPLETE
    CHAR05_02_IMPLEMENT_ROPE_CLIMBING_AND_TRAVERSAL_SUPPORT: COMPLETE
    CHAR05_03_IMPLEMENT_HEALTH_HAZARDS_DEATH_AND_RUN_FAILURE: COMPLETE
    CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE: COMPLETE
    CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT: LOCKED
    CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE_RESULT.md
  exact_status: PASS
  required_text:
    - "Current Task after finalize: NONE"
    - "CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT"
    - "LOCKED 유지"
  sha256: 321877eda8f80333bb285abd9d850cd7d9a44577ac85dfc53515d7a47331572c
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR05_04_IMPLEMENT_RUN_STATE_HUD_AND_PRESENTATION_BRIDGE.md
  sha256: 053ecb3e0f0ae02d3c729dc4bf8dcd5ee3247f1e3b2ff95da641fb76898e888b
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md
  sha256: b740d76985ef294defc53c04d885d57ef64cc833674cbb1313a259d6850531f6
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 19fedbbec2a223c8b39abb5ea185d194e6b6020ba4f403912a815721841117f9
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 8ecdfd978056ac7053e7561e6d6abb77e9e0ad29b83b5df04b3714ab45cecd66
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 21 COMPLETE / 1 CURRENT / 4 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_existing_character_editmode_tests: 158
  required_char05_05_editmode_tests: 0
  required_after_apply_character_editmode_tests: 158
forbids_started_task_prefixes:
  - CHAR06
sets_current_task: TASKS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md
    destination: CharacterDesign/MCP/TASKS/CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT.md
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
  - start_char06_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR05_04 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR05_04 REPORT에 `STATUS: PASS`, `Current Task after finalize: NONE`, CHAR05_05 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR05_05만 CURRENT이며 CHAR06 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
