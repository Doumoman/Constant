# PATCH MANIFEST

```yaml
patch_id: MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES
patch_version: 1.0
revision_note: MAP08_06 PASS/finalize 후 MAP08_07 task 하나만 연다. MAP08_08 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES: COMPLETE
    MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES: LOCKED
    MAP08_08_AUTHOR_CRATER_DOUGH_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES_RESULT.md
  exact_status: PASS
  sha256: 618cec23763ab38d4053a30ae348a4d6c187e2a8d4587d786247a514956a2ece
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
  sha256: 24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
  sha256: b35374f7159c29e6068c3221e458eef16100269dc20e00c1a972d194be6a3c5e
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 658ddbc9ae45849217157629286fd7295a582c071a1ec8b7aefa3ae281896147
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 54ee97a5d26b2e4be0843688aab34675a7108e581d77702f02a492ee848c3b34
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 97 COMPLETE / 0 CURRENT / 108 LOCKED
  status_counts_after_apply: 97 COMPLETE / 1 CURRENT / 107 LOCKED
  map08_phase_before_apply: 6 COMPLETE / 0 CURRENT / 8 LOCKED
  map08_phase_after_apply: 6 COMPLETE / 1 CURRENT / 7 LOCKED
  map08_06_result_sha256: 618cec23763ab38d4053a30ae348a4d6c187e2a8d4587d786247a514956a2ece
  map08_06_installed_repaired_task_sha256: 24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293
  map08_06_patch_receipt_sha256: afbc942b248d870859fa4823b3077754468fb7d99221e59807d11e07b00d2bc4
  map08_06_patch_version: 1.2-repair-map-meta-scope
  map08_06_focused_tests: 720/720
  map08_required_total: 3420/3420
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_06_acceptance_subset_total: 13547/13547
  map08_06_full_editmode_total: 19215/19215
  map08_06_failed_skipped: 0/0
  map08_06_compile_console_warnings: 0/0/0
  map08_06_global_assets_meta_total: 3687
  map08_06_map_assets_meta_total: 570
  map08_06_authoring_csv_meta: 50/50
  map08_06_authoring_manifest_sha256: c10083a3fe89e582cec9249eef6e556471a13b5b849ac2c3b5f0a3b3b940bdfa
  map08_06_generated_csv_created: 0
  map08_07_scope: crater_mill_boundary_authoring_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_06은 `COMPLETE`다.
- MAP08_06 Result는 exact `PASS`, SHA-256 `618cec23763ab38d4053a30ae348a4d6c187e2a8d4587d786247a514956a2ece`다.
- 이전 installed/repaired MAP08_06 Task file SHA-256은 `24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293`다.
- 새 MAP08_07 Task payload SHA-256은 `b35374f7159c29e6068c3221e458eef16100269dc20e00c1a972d194be6a3c5e`다.
- 205개 상태 행에서 MAP08_06까지 `COMPLETE`, MAP08_07 `CURRENT`, MAP08_08 이후는 `LOCKED`다.
- 새 Master/Status는 `97 COMPLETE / MAP08_07 CURRENT / 107 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
