# PATCH MANIFEST

```yaml
patch_id: MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES
patch_version: 1.0
revision_note: MAP08_09 PASS/finalize 후 MAP08_10 task 하나만 연다. MAP08_11 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES: COMPLETE
    MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: LOCKED
    MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
  sha256: c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
  sha256: f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 4670edc735736b2003edd3f18b8f4b9c45c9f62b610f045a249de8c614eb3fb5
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 0443c39e7c09208e415b574c6117a811e8112816e641f2836df07e12ed2dbde5
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 100 COMPLETE / 0 CURRENT / 105 LOCKED
  status_counts_after_apply: 100 COMPLETE / 1 CURRENT / 104 LOCKED
  map08_phase_before_apply: 9 COMPLETE / 0 CURRENT / 5 LOCKED
  map08_phase_after_apply: 9 COMPLETE / 1 CURRENT / 4 LOCKED
  map08_09_result_sha256: c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87
  map08_09_installed_task_sha256: c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
  map08_09_focused_tests: 720/720
  map08_required_total: 5580/5580
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_09_acceptance_subset_total: 15707/15707
  map08_09_failed_skipped: 0/0
  map08_09_compile_console_warnings: 0/0/0
  map08_09_global_assets_meta_total: 3705
  map08_09_map_assets_meta_total: 582
  map08_09_authoring_csv_meta: 50/50
  map08_09_authoring_manifest_sha256: b67b1235806a1acb4d5163917aa97ac93863e3cfba29c7842f656afc0d57096a
  map08_09_generated_csv_created: 0
  map08_10_scope: root_dough_boundary_authoring_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_09는 `COMPLETE`다.
- MAP08_09 Result는 exact `PASS`, SHA-256 `c923b445d9dc7b1d057ec368cb154a5745f2e473e67a736fcf8ee20f66a9ef87`다.
- 이전 installed MAP08_09 Task file SHA-256은 `c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f`다.
- 새 MAP08_10 Task payload SHA-256은 `f8050e209aa342602616afadc7cf0c3258731c8034ab9aff94f2bd94918d04f8`다.
- 205개 상태 행에서 MAP08_09까지 `COMPLETE`, MAP08_10 `CURRENT`, MAP08_11 이후는 `LOCKED`다.
- 새 Master/Status는 `100 COMPLETE / MAP08_10 CURRENT / 104 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
