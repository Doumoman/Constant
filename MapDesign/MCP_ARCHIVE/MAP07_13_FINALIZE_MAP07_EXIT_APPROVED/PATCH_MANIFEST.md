# PATCH MANIFEST

```yaml
patch_id: MAP07_13_FINALIZE_MAP07_EXIT_APPROVED
patch_version: 1.0
revision_note: MAP07_13 PASS 후 MAP07 phase exit만 확정한다. MAP08_01 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_13_MAP07_STARTER_AND_EXIT_TESTS: COMPLETE
    MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS_RESULT.md
  exact_status: PASS
  sha256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_13_MAP07_STARTER_AND_EXIT_TESTS.md
  sha256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 1c5ed4a33ec619db6b28fe0e535e3ad0efc71e983ca72e80cc18099b23328829
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: c76c5af69c7b1d0ba63c17b82ef4021b62925b41e2a5c316971e6c5e916c53cc
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 91 COMPLETE / 0 CURRENT / 114 LOCKED
  map07_phase_after_apply: COMPLETE / EXIT APPROVED
  map07_13_result_sha256: 263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e
  map07_13_task_sha256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
  map07_13_required_total: 10127/10127
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map07_13_compile_console_warnings: 0/0/0
  map07_13_assets_meta_total: 3409
  map07_13_authoring_csv_meta: 50/50
  map07_13_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_13_generated_csv_created: 0
  map07_13_scope: finalize_map07_exit_only
forbids_started_task_prefixes:
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: NONE
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
forbidden_operations:
  - create_task_file
  - delete
  - overwrite_existing_task_file
  - move_existing_project_files
  - rename_existing_project_files
  - modify_assets_during_patch_apply
  - modify_csv_during_patch_apply
  - modify_runtime_or_editor_code_during_patch_apply
  - modify_tests_during_patch_apply
  - modify_asmdef
  - start_next_phase
  - git_commit
  - git_push
```

## 적용 검증

- MAP07_13 Result는 exact `PASS`, SHA-256 `263a2bbf291e4df25dbe6bc101986e11ebf39bc0fc3d0074759fb7450b6df77e`다.
- 이전 MAP07_13 Task file SHA-256은 `698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb`다.
- 205개 상태 행에서 MAP07_13까지 `COMPLETE`, Current Task `NONE`, MAP08_01 이후는 `LOCKED`다.
- 새 Master/Status는 `91 COMPLETE / 0 CURRENT / 114 LOCKED`다.
- MAP07 Phase는 `COMPLETE / EXIT APPROVED`다.
- PATCH APPLY 단계에서는 Master/Status만 교체하고 Assets, CSV, C#, test, asmdef, Task file은 변경하지 않는다.
