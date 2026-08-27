# PATCH MANIFEST

```yaml
patch_id: MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER
patch_version: 1.0
revision_note: MAP08_02 PASS/finalize 후 MAP08_03 task 하나만 연다. MAP08_04 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX: COMPLETE
    MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER: LOCKED
    MAP08_04_FILTER_MANDATORY_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX_RESULT.md
  exact_status: PASS
  sha256: 2a160c7bc32cf7177208bbb0d06c0e449ef7dd3e7904bb23060484509d893c54
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX.md
  sha256: 767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER.md
  sha256: 1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: f44d5e12b7baa0df44eae42c74d7013f70f46d4bf9288e63b8a0f02799c4b1cd
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: fa7606c00bd790496eaf0c17eb8a21db80fabda00a12651e3f5d3e383f87aa11
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 93 COMPLETE / 0 CURRENT / 112 LOCKED
  status_counts_after_apply: 93 COMPLETE / 1 CURRENT / 111 LOCKED
  map08_phase_before_apply: 2 COMPLETE / 0 CURRENT / 12 LOCKED
  map08_phase_after_apply: 2 COMPLETE / 1 CURRENT / 11 LOCKED
  map08_02_result_sha256: 2a160c7bc32cf7177208bbb0d06c0e449ef7dd3e7904bb23060484509d893c54
  map08_02_task_sha256: 767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50
  map08_02_patch_receipt_sha256: 7b39a6ad3c7690e86e4313fd801173083317c95d73d7192fb59c17f6cc40d693
  map08_02_focused_tests: 580/580
  map08_01_focused_tests: 400/400
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_02_acceptance_total: 11107/11107
  map08_02_failed_skipped: 0/0
  map08_02_compile_console_warnings: 0/0/0
  map08_02_assets_meta_total: 3429
  map08_02_authoring_csv_meta: 50/50
  map08_02_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map08_02_generated_csv_created: 0
  map08_03_scope: boundary_chunk_resolver_only
forbids_started_task_prefixes:
  - MAP08_04
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
sets_current_task: TASKS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER.md
    destination: MapDesign/MCP/TASKS/MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER.md
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

- 적용 전 Current Task는 `NONE`이고 MAP08_02는 `COMPLETE`다.
- MAP08_02 Result는 exact `PASS`, SHA-256 `2a160c7bc32cf7177208bbb0d06c0e449ef7dd3e7904bb23060484509d893c54`다.
- 이전 MAP08_02 Task file SHA-256은 `767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50`다.
- 새 MAP08_03 Task payload SHA-256은 `1b5efb56faec6a0ea0e5fa6d751efc241f5474bf4403338fbb365b15f69b4b63`다.
- 205개 상태 행에서 MAP08_02까지 `COMPLETE`, MAP08_03 `CURRENT`, MAP08_04 이후는 `LOCKED`다.
- 새 Master/Status는 `93 COMPLETE / MAP08_03 CURRENT / 111 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
