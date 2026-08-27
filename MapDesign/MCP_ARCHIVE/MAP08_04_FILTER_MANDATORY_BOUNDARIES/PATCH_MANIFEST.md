# PATCH MANIFEST

```yaml
patch_id: MAP08_04_FILTER_MANDATORY_BOUNDARIES
patch_version: 1.0
revision_note: MAP08_03 PASS/finalize 후 MAP08_04 task 하나만 연다. MAP08_05 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER: COMPLETE
    MAP08_04_FILTER_MANDATORY_BOUNDARIES: LOCKED
    MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER_RESULT.md
  exact_status: PASS
  sha256: 43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER.md
  sha256: 1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_04_FILTER_MANDATORY_BOUNDARIES.md
  sha256: 9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 77bae73c2c817ca142c7b29b9231f610174a425c2ad2ce411407a8cbcb414a08
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: fe876cbb7181380279aabe261dce457a203beb0348f9b22c4e847d87b9807ca5
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 94 COMPLETE / 0 CURRENT / 111 LOCKED
  status_counts_after_apply: 94 COMPLETE / 1 CURRENT / 110 LOCKED
  map08_phase_before_apply: 3 COMPLETE / 0 CURRENT / 11 LOCKED
  map08_phase_after_apply: 3 COMPLETE / 1 CURRENT / 10 LOCKED
  map08_03_result_sha256: 43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445
  map08_03_task_sha256: 1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63
  map08_03_patch_receipt_sha256: cf65fbb4444f4a08d67129b185f2c567cc767bc5413500edcf2e5f2f5fd60a26
  map08_03_focused_tests: 680/680
  map08_02_focused_tests: 580/580
  map08_01_focused_tests: 400/400
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_03_acceptance_total: 11787/11787
  map08_03_failed_skipped: 0/0
  map08_03_compile_console_warnings: 0/0/0
  map08_03_assets_meta_total: 3439
  map08_03_authoring_csv_meta: 50/50
  map08_03_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map08_03_generated_csv_created: 0
  map08_04_scope: mandatory_boundary_filter_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_04_FILTER_MANDATORY_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_04_FILTER_MANDATORY_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_04_FILTER_MANDATORY_BOUNDARIES.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_03은 `COMPLETE`다.
- MAP08_03 Result는 exact `PASS`, SHA-256 `43a6d29466996164af4cc8e2d09dd6478a013f95c0b40ad15f132b3bead01445`다.
- 이전 MAP08_03 Task file SHA-256은 `1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63`다.
- 새 MAP08_04 Task payload SHA-256은 `9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9`다.
- 205개 상태 행에서 MAP08_03까지 `COMPLETE`, MAP08_04 `CURRENT`, MAP08_05 이후는 `LOCKED`다.
- 새 Master/Status는 `94 COMPLETE / MAP08_04 CURRENT / 110 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
