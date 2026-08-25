# PATCH MANIFEST

```yaml
patch_id: MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES
patch_version: 1.0
revision_note: MAP08_07 PASS/finalize 후 MAP08_08 task 하나만 연다. MAP08_09 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES: COMPLETE
    MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES: LOCKED
    MAP08_09_AUTHOR_ROOT_MILL_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: 59cc98beaa374a319c656c50f0c5aaf26a4f25a29f591eb017bf21d4a9eb995a
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
  sha256: bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
  sha256: 92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: f4928bc8ef3fa4e171202de8343ab766f6570349820e038e9079e945c5f38474
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: aa843757a108e726471671f0c69db00a166e4363d598444aee379c7e2e5929be
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 98 COMPLETE / 0 CURRENT / 107 LOCKED
  status_counts_after_apply: 98 COMPLETE / 1 CURRENT / 106 LOCKED
  map08_phase_before_apply: 7 COMPLETE / 0 CURRENT / 7 LOCKED
  map08_phase_after_apply: 7 COMPLETE / 1 CURRENT / 6 LOCKED
  map08_07_result_sha256: 59cc98beaa374a319c656c50f0c5aaf26a4f25a29f591eb017bf21d4a9eb995a
  map08_07_installed_repaired_task_sha256: bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577
  map08_07_patch_receipt_sha256: 1493f0a393fbe4744393a7ee7b6c77f3e865442c7d83826f1b37ca4d43f3afc4
  map08_07_focused_tests: 720/720
  map08_required_total: 4140/4140
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_07_acceptance_subset_total: 14267/14267
  map08_07_failed_skipped: 0/0
  map08_07_compile_console_warnings: 0/0/0
  map08_07_global_assets_meta_total: 3693
  map08_07_map_assets_meta_total: 574
  map08_07_authoring_csv_meta: 50/50
  map08_07_authoring_manifest_sha256: d55393e0d60e907462fe6e406b3b8705c98ff82c08b839bd64b54b5cd53808a2
  map08_07_generated_csv_created: 0
  map08_08_scope: crater_dough_boundary_authoring_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_07은 `COMPLETE`다.
- MAP08_07 Result는 exact `PASS`, SHA-256 `59cc98beaa374a319c656c50f0c5aaf26a4f25a29f591eb017bf21d4a9eb995a`다.
- 이전 installed/repaired MAP08_07 Task file SHA-256은 `bf9085abb16be5c0bc736fa78b709fd32972f5903ba332622860d41d13aa4577`다.
- 새 MAP08_08 Task payload SHA-256은 `92106729b49fa13c0fbb95f0338b3b619582a1696f10ac88604401469907d769`다.
- 205개 상태 행에서 MAP08_07까지 `COMPLETE`, MAP08_08 `CURRENT`, MAP08_09 이후는 `LOCKED`다.
- 새 Master/Status는 `98 COMPLETE / MAP08_08 CURRENT / 106 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
