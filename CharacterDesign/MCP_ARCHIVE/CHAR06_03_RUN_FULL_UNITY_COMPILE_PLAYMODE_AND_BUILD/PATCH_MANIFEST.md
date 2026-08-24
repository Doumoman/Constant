# PATCH MANIFEST

```yaml
patch_id: CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD
patch_version: 2.1
revision_note: CHAR06_02 PASS 기준선을 검증하고 CHAR06_03 full Unity validation task 하나만 연다. ZIP root is task folder to avoid CharacterDesign/MCP_INBOX nesting.
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
    CHAR05_05_CHAR05_EQUIPMENT_SURVIVAL_EXIT_AUDIT: COMPLETE
    CHAR06_01_INTEGRATE_PLAYER_WITH_GENERATED_MAP_AND_ROUTES: COMPLETE
    CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS: COMPLETE
    CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD: LOCKED
    CHAR06_04_AUDIT_REPORTS_ALLOWLIST_COMMITS_AND_FINAL_EXIT: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS_RESULT.md
  exact_status: PASS
  required_text:
    - "177/177 PASS"
    - "Current Task after finalize: NONE"
    - "CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD"
    - "LOCKED 유지"
  sha256: 9ae578c70b7062ce7285ac75c2ec35689ee4c4246ae50d89ce42496fd60e37ab
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR06_02_VALIDATE_ROOM_MICROCHUNK_ITEMS_AND_RANDOM_RUNS.md
  sha256: 541e602b0847f5491e94aee484ec733ae285a668f49edf033c62470a8874736d
requires_locked_task_template:
  path: CharacterDesign/MCP/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
  source_template_sha256: d63491cb10010b4605f6428760d5853ed31052d1cf83ff7b7598df2b37391dda
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
  sha256: ece4775c35fc0c136bb250d54cef38c1c02ea6c87635035b7271ad3e115a4822
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 5a405765c2fec9763c57d90e26e130bf7dc4cbe306434c11debb87a5e4375536
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 3e6066ddad67f4066f5baecc299e2e5658738304cec8b464a29d9f202e39080e
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 24 COMPLETE / 1 CURRENT / 1 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_existing_character_editmode_tests: 177
  required_after_apply_character_editmode_tests: 177
  build_validation: active build target
forbids_started_task_prefixes:
  - CHAR06_04
sets_current_task: TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
zip_layout:
  top_level_directory: CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD
  extraction_target: CharacterDesign/MCP_INBOX
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
    destination: CharacterDesign/MCP/TASKS/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD.md
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
  - modify_map_runtime_or_map_authoring_data
  - run_completed_task_package
  - start_char06_04_task
  - git_commit
  - git_push
```

## 적용 검증

- ZIP을 `CharacterDesign/MCP_INBOX` 안에 풀면 `CharacterDesign/MCP_INBOX/CHAR06_03_RUN_FULL_UNITY_COMPILE_PLAYMODE_AND_BUILD/`가 된다.
- 적용 전 Current Task는 NONE이다.
- CHAR06_02 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR06_02 REPORT에 `STATUS: PASS`, `177/177 PASS`, `Current Task after finalize: NONE`, CHAR06_03 LOCKED 유지 문구가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR06_03만 CURRENT이며 CHAR06_04는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
