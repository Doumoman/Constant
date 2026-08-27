# PATCH MANIFEST

```yaml
patch_id: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS
patch_version: 1.0
revision_note: MAP07_13 PASS/finalize 후 MAP08 첫 task 하나만 연다. MAP08_02 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_13_MAP07_STARTER_AND_EXIT_TESTS: COMPLETE
    MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS: LOCKED
    MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md
  exact_status: PASS
  sha256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS.md
  sha256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS.md
  sha256: 19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 2bcf5e0f4ce2ecbcf92e45c6b258dcd5bc03f73c13b7973f13e2d6caa303cc67
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: c8aaf530ac58a49a2affd4f61dbe01770155caaa8650126201e410c4f7ff8687
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 91 COMPLETE / 0 CURRENT / 114 LOCKED
  status_counts_after_apply: 91 COMPLETE / 1 CURRENT / 113 LOCKED
  map07_phase_before_apply: COMPLETE / EXIT APPROVED
  map08_phase_after_apply: 0 COMPLETE / 1 CURRENT / 13 LOCKED
  map07_13_result_sha256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
  map07_13_task_sha256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
  map07_13_patch_receipt_sha256: 5964a1611a3c57bd8134ea4d9e78d8a7d45e655cb2e082514045b1a2eb70fa77
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map07_13_acceptance_total: 10127/10127
  map07_13_failed_skipped: 0/0
  map07_13_compile_console_warnings: 0/0/0
  map07_13_assets_meta_total: 3409
  map07_13_authoring_csv_meta: 50/50
  map07_13_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_13_generated_csv_created: 0
  map08_01_scope: biome_pair_contract_only
forbids_started_task_prefixes:
  - MAP08_02
  - MAP08_03
  - MAP08_04
  - MAP08_05
  - MAP08_06
  - MAP08_07
  - MAP08_08
  - MAP08_09
  - MAP08_10
  - MAP08_11
  - MAP08_12
  - MAP08_13
  - MAP08_14
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS.md
    destination: MapDesign/MCP/TASKS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS.md
    mode: create
forbidden_operations:
  - delete
  - overwrite_existing_task_file
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_csv_during_patch_apply
  - modify_runtime_or_editor_code_during_patch_apply
  - modify_tests_during_patch_apply
  - modify_asmdef
  - run_completed_task_package
  - git_commit
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `NONE`이다.
- MAP07_13 Result는 exact `PASS`, SHA-256 `263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e`다.
- 이전 MAP07_13 Task file SHA-256은 `698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb`다.
- 새 MAP08_01 Task payload SHA-256은 `19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d`다.
- 205개 상태 행에서 MAP07_13까지 `COMPLETE`, MAP08_01 `CURRENT`, MAP08_02 이후는 `LOCKED`다.
- 새 Master/Status는 `91 COMPLETE / MAP08_01 CURRENT / 113 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
