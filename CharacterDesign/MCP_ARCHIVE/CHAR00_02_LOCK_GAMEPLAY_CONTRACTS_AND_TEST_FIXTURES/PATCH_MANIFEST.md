# PATCH MANIFEST

```yaml
patch_id: CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES
patch_version: 2.0
revision_note: MapDesign 방식으로 CHAR00_01 PASS 기준선을 검증하고 CHAR00_02 문서 계약 task 하나만 연다.
requires_status:
  current_task: NONE
  task_states_required:
    CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP: COMPLETE
    CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES: LOCKED
    CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT: LOCKED
requires_result:
  path: CharacterDesign/MCP/REPORTS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
  exact_status: PASS
  sha256: 1bc1a931d43030561014c8cdf49c4609ac635bfd57e27d568ec975abefcef6c0
requires_previous_task_file:
  path: CharacterDesign/MCP/TASKS/CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP.md
  sha256: 08b8141effaf9c66b0cec28d3e8bfba21023fee3f46800062d3ff70ff640f0f8
requires_input:
  path: CharacterDesign/MCP/INPUTS/CHAR00_SOURCE_REGISTRY.md
  exact_marker: "REGISTRY_STATE: FILLED_BY_CHAR00_01"
  sha256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
current_task_payload:
  path: PAYLOAD/TASKS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md
  sha256: 29db7d180e8675b17858601f8ba6e9e2aeae03059a24c4252b3efb24dc04b51b
status_payload:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: d7999088c4dcf514433ed25496e992d83f5b0e03b2cf8bc4ba9d41dcf72f624d
master_payload:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 7747d7551410e5a1714a364a22ff32835ef68f082d560717f09fcc1a32d0d032
requires_project_baseline:
  unity: 6000.3.8f1
  branch_commit: main@24cb1b9
  master_tasks: 26
  status_counts_after_apply: 1 COMPLETE / 1 CURRENT / 24 LOCKED
  active_character_runtime: none
  input_system: 1.18.0
forbids_started_task_prefixes:
  - CHAR00_03
  - CHAR01
  - CHAR02
  - CHAR03
  - CHAR04
  - CHAR05
  - CHAR06
sets_current_task: TASKS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: CharacterDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: CharacterDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md
    destination: CharacterDesign/MCP/TASKS/CHAR00_02_LOCK_GAMEPLAY_CONTRACTS_AND_TEST_FIXTURES.md
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
  - run_completed_task_package
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 NONE이다.
- CHAR00_01 Task와 REPORT, source registry의 hash와 marker가 일치한다.
- 새 payload 3개의 hash가 manifest와 일치한다.
- 적용 후 CHAR00_02만 CURRENT이며 CHAR00_03 이후는 LOCKED다.
- PATCH APPLY에서는 Master/Status/Task 문서만 교체·생성한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 BLOCKED한다.
