# PATCH MANIFEST

```yaml
patch_id: MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR
patch_version: 1.0
revision_note: MAP08_11 PASS/finalize 후 MAP08_12 task 하나만 연다. MAP08_13 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES: COMPLETE
    MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR: LOCKED
    MAP08_13_CREATE_BOUNDARY_PREVIEW_WINDOW: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: 9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md
  sha256: MUST_COMPUTE_FROM_INSTALLED_PROJECT_FILE
  reason: uploaded MAP08_11 PASS Result did not report installed MAP08_11 Task SHA-256
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
  sha256: cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: b22cb4aea5d2a2d671718c4c7b2b4d2d6a4e402fbdb1aa9b6b6741a0f619fe07
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 75cf2f7966893ac94711ec07db512775e32060fc4b2760e270495923f0c708d9
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 102 COMPLETE / 0 CURRENT / 103 LOCKED
  status_counts_after_apply: 102 COMPLETE / 1 CURRENT / 102 LOCKED
  map08_phase_before_apply: 11 COMPLETE / 0 CURRENT / 3 LOCKED
  map08_phase_after_apply: 11 COMPLETE / 1 CURRENT / 2 LOCKED
  map08_11_result_sha256: 9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c
  map08_11_installed_task_sha256: MUST_COMPUTE_FROM_INSTALLED_PROJECT_FILE
  map08_11_focused_tests: 720/720
  map08_pair_authoring_categories: 4320/4320
  map08_01_05_baseline_groups: 2700/2700
  map08_required_union: 7020/7020
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_11_acceptance_subset_total: 17147/17147
  map08_11_failed_skipped: 0/0
  map08_11_compile_console_warnings: 0/0/0
  map08_11_global_assets_meta_total: 3794
  map08_11_map_assets_meta_total: 590
  map08_11_authoring_csv_meta: 50/50
  map08_11_authoring_manifest_sha256: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
  map08_11_generated_csv_created: 0
  map08_12_scope: boundary_coverage_validator_only
forbids_started_task_prefixes:
  - MAP08_13
  - MAP08_14
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
    destination: MapDesign/MCP/TASKS/MAP08_12_IMPLEMENT_BOUNDARY_COVERAGE_VALIDATOR.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_11은 `COMPLETE`다.
- MAP08_11 Result는 exact `PASS`, SHA-256 `9c9ce342563858987b2489ae6aa9a50bee2473be4639b07cd7176ee18bcbde4c`다.
- 업로드된 MAP08_11 Result는 installed Task SHA를 직접 보고하지 않았으므로 적용자는 `MapDesign/MCP/TASKS/MAP08_11_AUTHOR_MILL_DOUGH_BOUNDARIES.md` SHA-256을 계산해 MAP08_12 Result에 남긴다.
- 새 MAP08_12 Task payload SHA-256은 `cdaadd203562acdcad7024349499a2df3ac0ccd9860ef580473e984ae9490966`다.
- 205개 상태 행에서 MAP08_11까지 `COMPLETE`, MAP08_12 `CURRENT`, MAP08_13 이후는 `LOCKED`다.
- 새 Master/Status는 `102 COMPLETE / MAP08_12 CURRENT / 102 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
