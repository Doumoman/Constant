# PATCH MANIFEST

```yaml
patch_id: CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT
patch_version: 2.0
revision_note: CHAR00_02 PASS/finalize 기준선을 검증하고 CHAR00 종료 감사 task 하나만 연다.
requires_status:
  current_task: NONE
  task_states_required:
    CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP: COMPLETE
    CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES: COMPLETE
    CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT: LOCKED
    CHAR01_01_IMPLEMENT_INPUT_SNAPSHOT_AND_PLAYER_STATES: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES_RESULT.md
  exact_status: PASS
  sha256: 87d91f2a9dbede08050a9b34aa05544f40ff8d4bafb48ed59321db00f5471124
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md
  sha256: 29db7d180e8675b17858601f8ba6e9e2aeae03059a24c4252b3efb24dc04b51b
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md
  sha256: 05cb7ccc006511adf854126d0c438cb23bf7a53045044f494c55f74664bea342
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 6255c2a76409d53f265699e13e291a5396db2a2423e3d6edeb55bb2a2e8f6a82
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 1ccb808de1ac5be8b87bc8c3949dff2f0cc0e69a6640721c1b854e66daa7d541
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 26
  status_counts_after_apply: 2 COMPLETE / 1 CURRENT / 23 LOCKED
  active_character_runtime: none
  required_fixture_ids: 16
forbids_started_task_prefixes:
  - CHAR01
  - CHAR02
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md
    destination: CharacterDesign/MCP/TASKS/CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT.md
    mode: create
forbidden_operations:
  - delete
  - overwrite_existing_task_file
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_runtime_or_test_code_during_patch_apply
  - modify_inputactions_or_asmdef
  - modify_scene_prefab_packages_projectsettings_mapdesign
  - modify_contract_schema_fixture_during_patch_apply
  - run_completed_task_package
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR00_02 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR00_03만 CURRENT이며 CHAR01_01 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
