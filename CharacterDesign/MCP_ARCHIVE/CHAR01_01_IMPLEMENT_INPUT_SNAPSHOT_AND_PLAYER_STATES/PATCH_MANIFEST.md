# PATCH MANIFEST

```yaml
patch_id: CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES
patch_version: 2.0
revision_note: CHAR00 EXIT APPROVED 기준선을 검증하고 CHAR01_01 구현 task 하나만 연다.
requires_status:
  current_task: NONE
  task_states_required:
    CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP: COMPLETE
    CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES: COMPLETE
    CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT: COMPLETE
    CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES: LOCKED
    CHAR01_02_IMPLEMENT_COLLISION_QUERIES_AND_GROUND_MOTOR: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT_RESULT.md
  exact_status: PASS
  required_text:
    - "CHAR00 EXIT: APPROVED"
    - "CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH"
  sha256: c9b1804527c8c381cb8f6e07b0019fe5a5d458340aeb621d6e847d280c75c138
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md
  sha256: 05cb7ccc006511adf854126d0c438cb23bf7a53045044f494c55f74664bea342
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES.md
  sha256: af23f259463041abf62ebc83aeec51e20ab78fbeef5a76f8cfc7ac851e7129e4
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 55cf86be1f8ceb707abf7b9b4980e1459541cd6629f28455d56c90fa2ec5b089
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: c0fd733071ebcca72c5c7112a3e9e791caa82e234e7da78417b8b16efa37cec0
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 3 COMPLETE / 1 CURRENT / 22 LOCKED
  active_character_runtime_before_task: none
  char00_exit: APPROVED
  required_editmode_test_cases: 12
forbids_started_task_prefixes:
  - CHAR02
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES.md
    destination: CharacterDesign/MCP/TASKS/CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES.md
    mode: create
forbidden_operations:
  - delete
  - overwrite_existing_task_file
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_runtime_or_test_code_during_patch_apply
  - modify_inputactions_during_patch_apply
  - modify_scene_prefab_packages_projectsettings_mapdesign
  - modify_contract_schema_fixture_during_patch_apply
  - run_completed_task_package
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR00_03 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- CHAR00_03 REPORT에 `CHAR00 EXIT: APPROVED`와 `CHAR01_01 ENTRY: ELIGIBLE FOR SEPARATE PATCH`가 있다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR01_01만 CURRENT이며 CHAR01_02 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
