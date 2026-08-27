# PATCH MANIFEST

```yaml
patch_id: MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR
patch_version: 1.0
revision_note: MAP06_08 PASS/finalize 후 optional region source-chain validation report만 연다. overlay/exit/generated CSV는 MAP06_10으로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP06_08_ASSIGN_INACTIVE_BUFFERS: COMPLETE
    MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: LOCKED
    MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
  exact_status: PASS
  sha256: 43dd272802bfe6094ac5f1dff91ddb30229acf0c5a0885742509945a496bf58b
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP06_08_ASSIGN_INACTIVE_BUFFERS.md
  sha256: 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
current_task_payload:
  path: PAYLOAD/TASKS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR.md
  sha256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 76 COMPLETE / 1 CURRENT / 128 LOCKED
  map06_08_result_sha256: 43dd272802bfe6094ac5f1dff91ddb30229acf0c5a0885742509945a496bf58b
  map06_08_task_sha256: 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
  map06_08_inactive_assignments: 78
  map06_08_decorative_interior: 52/26
  map06_08_protected_union: 91
  map06_08_approved_site_mandatory_overlap: [0, 28, 106]
  map06_08_full_accounting: 169 = 8 + 44 + 39 + 78
  map06_08_canonical_digest: 426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578
  map06_08_inactive_buffer_tests: 281/281
  map06_08_prior_required_total: 3984/3984
  map06_08_failed_skipped: 0/0
  map06_08_compile_console_warnings: 0/0/0
  map06_08_assets_meta_total: 3304
  map06_08_authoring_csv_meta: 50/50
  map06_08_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map06_08_duplicate_guid_groups: 0
  map05_11_graph_nodes_directed_undirected_route_cells: 47/96/48/47
  map05_11_mask_counts_t1_t2_t3_t4ud_t4lud_t4rud_t4lrud: 20/4/4/17/0/0/2
  map05_11_type4_contract: U+D mandatory; L/R independent; UD/LUD/RUD/LRUD legal
forbids_started_task_prefixes:
  - MAP06_10
  - MAP07
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR.md
    destination: MapDesign/MCP/TASKS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR.md
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

- 적용 전 Current Task는 `NONE`이다.
- MAP06_08 Result는 exact `PASS`, SHA-256 `43dd272802bfe6094ac5f1dff91ddb30229acf0c5a0885742509945a496bf58b`다.
- 이전 MAP06_08 Task file SHA-256은 `0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340`다.
- 새 MAP06_09 Task payload SHA-256은 `e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e`다.
- MAP06_08은 COMPLETE 자격을 갖췄고 MAP06_09은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP06_08까지 `COMPLETE`, MAP06_09 `CURRENT`, MAP06_10 이후는 `LOCKED`다.
- 새 Master/Status는 `76 COMPLETE / MAP06_09 CURRENT / 128 LOCKED`다.
- MAP06_08 accounting `169 = 8 + 44 + 39 + 78`, protected union `91`, inactive `78`, digest `426f269e39d8a2d75a93020a00c7bb617612c00dd60a663fdbeffc60f8ea9578`를 입력 기준으로 보존한다.
- Type4는 U+D mandatory, L/R independent이며 `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 legal이다.
- current Assets meta baseline은 `3304`, Authoring CSV/meta `50/50`, duplicate GUID `0`이다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
