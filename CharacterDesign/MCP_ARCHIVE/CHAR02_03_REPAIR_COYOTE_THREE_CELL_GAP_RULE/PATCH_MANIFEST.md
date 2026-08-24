# PATCH MANIFEST

```yaml
patch_id: CHAR02_03_REPAIR_COYOTE_THREE_CELL_GAP_RULE
patch_version: 2.0-repair
revision_note: CHAR02_03 FAIL 결과를 근거로 current task body를 change-control repair revision으로 교체한다. CHAR03은 열지 않는다.
requires_status:
  current_task: TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
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
    CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT: CURRENT
    CHAR03_01_CONNECT_MAP_WORLD_QUERY_AND_BOUNDARY_GATE: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT_RESULT.md
  exact_status: FAIL
  required_text:
    - "CHAR02 EXIT: REJECTED"
    - "CHAR03_01 ENTRY: BLOCKED"
    - "코요테 지연 점프"
  sha256: e5fac10bce6791006c2549134834b8d518d0f9aa1d29d276595ce87203208043
requires_current_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
  sha256: e99b725df83b4795a4963709c74335580183a821eca48e1dd51fbf734a10270c
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md
  sha256: 6c4b7f0a9e047db07d3c3c1b667f6b74e619ddddef7d1c1bafa889da52ad2250
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: eee6f0e262e3ca2635d12b3d17ab0928e4cc410e72b2486ce865257f31511819
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 955e7719408b3b649bc2428fd2955c018ee2e50b9d0b0ed3dcc86acf09e1ca73
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 9 COMPLETE / 1 CURRENT / 16 LOCKED
  active_character_runtime: Game.Character.Runtime
  active_character_tests: Game.Character.Tests.EditMode
  required_existing_character_editmode_tests: 52
  required_after_repair_character_editmode_tests: 57
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
    mode: replace
repair_apply_note: 기존 current task file이 존재해야 하며, previous FAIL report와 현재 task hash가 일치할 때만 본 revision 교체를 허용한다.
forbidden_operations:
  - delete
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_runtime_or_test_code_during_patch_apply
  - modify_inputactions_or_asmdef_during_patch_apply
  - modify_scene_prefab_packages_projectsettings_mapdesign
  - start_char03_or_later_task
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `TASKS/CHAR02_03_CHAR02_MOVEMENT_RULE_EXIT_AUDIT.md`이다.
- CHAR02_03 previous REPORT가 `STATUS: FAIL`이며 rejection/block 문구와 hash가 일치한다.
- 현재 설치된 CHAR02_03 task file hash가 manifest와 일치한다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 Current Task는 그대로 CHAR02_03이며 CHAR03_01 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체한다.
- Character runtime/test 수정은 Task 실행 단계에서만 허용된다.
