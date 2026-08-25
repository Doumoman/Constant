# PATCH MANIFEST

```yaml
patch_id: MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES
patch_version: 1.0
revision_note: MAP08_08 PASS/finalize 후 MAP08_09 task 하나만 연다. MAP08_10 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES: COMPLETE
    MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES: LOCKED
    MAP08_10_AUTHOR_ROOT_DOUGH_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
  sha256: 92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
  sha256: c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: a0dd3b5668b4e4eb7f66c83bff6f5f4daf55bbc0bfbd114216d88d87476becd1
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: b0b53b1156296892c7d7ec54bde3cfa662994b63a65c5006b8e548b7496591b8
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 99 COMPLETE / 0 CURRENT / 106 LOCKED
  status_counts_after_apply: 99 COMPLETE / 1 CURRENT / 105 LOCKED
  map08_phase_before_apply: 8 COMPLETE / 0 CURRENT / 6 LOCKED
  map08_phase_after_apply: 8 COMPLETE / 1 CURRENT / 5 LOCKED
  map08_08_result_sha256: df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9
  map08_08_installed_task_sha256: 92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
  map08_08_focused_tests: 720/720
  map08_required_total: 4860/4860
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_08_acceptance_subset_total: 14987/14987
  map08_08_failed_skipped: 0/0
  map08_08_compile_console_warnings: 0/0/0
  map08_08_global_assets_meta_total: 3699
  map08_08_map_assets_meta_total: 578
  map08_08_authoring_csv_meta: 50/50
  map08_08_authoring_manifest_sha256: 61d5462d00b7d4f435297523be15d0bef636dfc84a87b05004b209928bacce1b
  map08_08_generated_csv_created: 0
  map08_09_scope: root_mill_boundary_authoring_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_08은 `COMPLETE`다.
- MAP08_08 Result는 exact `PASS`, SHA-256 `df4137defbb8e44cba12ef3b74cd8635044b886657525ec128e05dd5b1bd67c9`다.
- 이전 installed MAP08_08 Task file SHA-256은 `92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769`다.
- 새 MAP08_09 Task payload SHA-256은 `c559168ad86e866124cc87843926d66b98f59ad1243a33755fce7f78621fd01f`다.
- 205개 상태 행에서 MAP08_08까지 `COMPLETE`, MAP08_09 `CURRENT`, MAP08_10 이후는 `LOCKED`다.
- 새 Master/Status는 `99 COMPLETE / MAP08_09 CURRENT / 105 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
