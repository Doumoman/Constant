# PATCH MANIFEST

```yaml
patch_id: MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW
patch_version: 1.0
revision_note: MAP08_12 PASS/finalize 후 MAP08_13 task 하나만 연다. MAP08_14 이후는 locked 상태로 유지한다. MAP08_10은 외부 작업 PASS Result로 source-chain에 반영한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR: COMPLETE
    MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW: LOCKED
    MAP08_14_MAP08_EXIT_TESTS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR_RESULT.md
  exact_status: PASS
  sha256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
requires_external_accounted_result:
  path: MapDesign/MCP/REPORTS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
  sha256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
  sha256: 5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 3c2ed8a8c07f84542f964e7c4048c544a14ed54bada6fbdd380bcd2d607cdb79
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: a544ddf93688a709ca6efbb48f197913c25c03604e4ecdd7d85824c9f1fa456d
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 103 COMPLETE / 0 CURRENT / 102 LOCKED
  status_counts_after_apply: 103 COMPLETE / 1 CURRENT / 101 LOCKED
  map08_phase_before_apply: 12 COMPLETE / 0 CURRENT / 2 LOCKED
  map08_phase_after_apply: 12 COMPLETE / 1 CURRENT / 1 LOCKED
  map08_10_external_result_sha256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
  map08_10_installed_task_sha256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
  map08_12_result_sha256: 26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b
  map08_12_installed_task_sha256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
  map08_12_coverage_digest: f7ff1c49f5bc33a4ad57799269bc3915806fe0cb60f347ed76eb16ea26f7fc68
  map08_12_coverage_counts: 31 candidates / 31 microchunks / 2976 tile rows / 62 socket rows
  map08_12_issues: 0
  map08_12_focused_tests: 720/720
  map08_required_union: 7740/7740
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_12_acceptance_subset_total: 17867/17867
  map08_12_failed_skipped: 0/0
  map08_12_compile_console_warnings: 0/0/0
  map08_12_global_assets_meta_total: 3802
  map08_12_map_assets_meta_total: 596
  map08_12_authoring_csv_meta: 50/50
  map08_12_authoring_manifest_sha256: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
  map08_12_generated_csv_created: 0
  map08_13_scope: editor_boundary_preview_window_only
forbids_started_task_prefixes:
  - MAP08_14
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
    destination: MapDesign/MCP/TASKS/MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW.md
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
  - git_commit_during_patch_apply
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `NONE`이고 MAP08_12는 `COMPLETE`다.
- MAP08_12 Result는 exact `PASS`, SHA-256 `26801f7bc31d354b9639278ec133970e7840f3c7dacb9f0f2a4b6a0e0288896b`다.
- MAP08_10은 외부 작업 Result exact `PASS`, SHA-256 `058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a`로 source-chain에 반영한다.
- 새 MAP08_13 Task payload SHA-256은 `5e932e82ae7ed78e732c26907ac4cd480e43d7bf14ee9978df46d2917310153d`다.
- 205개 상태 행에서 MAP08_12까지 `COMPLETE`, MAP08_13 `CURRENT`, MAP08_14 이후는 `LOCKED`다.
- 새 Master/Status는 `103 COMPLETE / MAP08_13 CURRENT / 101 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
