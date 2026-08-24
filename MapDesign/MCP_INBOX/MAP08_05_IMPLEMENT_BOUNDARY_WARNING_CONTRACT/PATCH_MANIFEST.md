# PATCH MANIFEST

```yaml
patch_id: MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT
patch_version: 1.0
revision_note: MAP08_04 PASS/finalize 후 MAP08_05 task 하나만 연다. MAP08_06 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_04_FILTER_MANDATORY_BOUNDARIES: COMPLETE
    MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT: LOCKED
    MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_04_FILTER_MANDATORY_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_04_FILTER_MANDATORY_BOUNDARIES.md
  sha256: 9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
  sha256: 7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: b48863034090bbf6e916e616e02809d9611a24e4c5ccdc86c4eb99362facea6d
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: fbcfe5c83b7433a6852aa690f98ea5626291961102529f27bfc6c0f0689def37
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 95 COMPLETE / 0 CURRENT / 110 LOCKED
  status_counts_after_apply: 95 COMPLETE / 1 CURRENT / 109 LOCKED
  map08_phase_before_apply: 4 COMPLETE / 0 CURRENT / 10 LOCKED
  map08_phase_after_apply: 4 COMPLETE / 1 CURRENT / 9 LOCKED
  map08_04_result_sha256: f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd
  map08_04_task_sha256: 9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9
  map08_04_patch_receipt_sha256: 11ef33b9315b643f470229dc23547dda3cd5233d4ada73347024bc255bfea3d9
  map08_04_focused_tests: 520/520
  map08_03_focused_tests: 680/680
  map08_02_focused_tests: 580/580
  map08_01_focused_tests: 400/400
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_04_acceptance_total: 12307/12307
  map08_04_failed_skipped: 0/0
  map08_04_compile_console_warnings: 0/0/0
  map08_04_assets_meta_total: 3447
  map08_04_authoring_csv_meta: 50/50
  map08_04_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map08_04_generated_csv_created: 0
  map08_05_scope: boundary_warning_contract_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
    destination: MapDesign/MCP/TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_04는 `COMPLETE`다.
- MAP08_04 Result는 exact `PASS`, SHA-256 `f189dc539efd54979d376d6bba5c809aadf93e7c63098d81ca0acd0656a7a4fd`다.
- 이전 MAP08_04 Task file SHA-256은 `9e3992c4bcf5ff7f891e236bfddeb00f746c6fee3effd2fb8a18b18da08781b9`다.
- 새 MAP08_05 Task payload SHA-256은 `7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6`다.
- 205개 상태 행에서 MAP08_04까지 `COMPLETE`, MAP08_05 `CURRENT`, MAP08_06 이후는 `LOCKED`다.
- 새 Master/Status는 `95 COMPLETE / MAP08_05 CURRENT / 109 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
