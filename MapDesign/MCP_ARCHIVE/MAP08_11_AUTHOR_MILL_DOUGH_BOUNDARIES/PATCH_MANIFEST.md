# PATCH MANIFEST

```yaml
patch_id: MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES
patch_version: 1.0
revision_note: MAP08_10 PASS/finalize 후 MAP08_11 task 하나만 연다. MAP08_12 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: COMPLETE
    MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: LOCKED
    MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
  sha256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
  sha256: 67f2852a01e19d61a78160e6cae79c77b4103ccf2d378e98c7e08becfcb3fda5
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: ba471afe8164dddd7f51035bb96f2c43282408f6a309073b6a67bd845b5cd16f
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: b6659ffe63c42a50eddff44708d67b468e60086eccb596e9396f4f381c922251
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 101 COMPLETE / 0 CURRENT / 104 LOCKED
  status_counts_after_apply: 101 COMPLETE / 1 CURRENT / 103 LOCKED
  map08_phase_before_apply: 10 COMPLETE / 0 CURRENT / 4 LOCKED
  map08_phase_after_apply: 10 COMPLETE / 1 CURRENT / 3 LOCKED
  map08_10_result_sha256: 058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a
  map08_10_installed_task_sha256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
  map08_10_focused_tests: 720/720
  map08_required_total: 6300/6300
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_10_acceptance_subset_total: 16427/16427
  map08_10_failed_skipped: 0/0
  map08_10_compile_console_warnings: 0/0/0
  map08_10_global_assets_meta_total: 3788
  map08_10_map_assets_meta_total: 586
  map08_10_authoring_csv_meta: 50/50
  map08_10_authoring_manifest_sha256: 0842d140f399da076cf41218b360e784cee776c62266bd251f4debb18657a950
  map08_10_generated_csv_created: 0
  map08_11_scope: mill_dough_boundary_authoring_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_10은 `COMPLETE`다.
- MAP08_10 Result는 exact `PASS`, SHA-256 `058b5ed32dbca7ca06adf12595ac693f5218dec5f8ed0bfe14d5c12c03563f5a`다.
- 이전 installed MAP08_10 Task file SHA-256은 `f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8`다.
- 새 MAP08_11 Task payload SHA-256은 `67f2852a01e19d61a78160e6cae79c77b4103ccf2d378e98c7e08becfcb3fda5`다.
- 205개 상태 행에서 MAP08_10까지 `COMPLETE`, MAP08_11 `CURRENT`, MAP08_12 이후는 `LOCKED`다.
- 새 Master/Status는 `101 COMPLETE / MAP08_11 CURRENT / 103 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 수정하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.

