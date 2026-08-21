# PATCH MANIFEST

```yaml
patch_id: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS
patch_version: 1.0
revision_note: MAP06_09 PASS/finalize 후 optional region overlay snapshot, editor scene drawer command model, and MAP06 phase exit tests만 연다. MAP07 microchunk work는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR: COMPLETE
    MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: LOCKED
    MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR_RESULT.md
  exact_status: PASS
  sha256: 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR.md
  sha256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
current_task_payload:
  path: PAYLOAD/TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
  sha256: 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 77 COMPLETE / 1 CURRENT / 127 LOCKED
  map06_09_result_sha256: 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
  map06_09_task_sha256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
  map06_09_validation_status_issues: Valid/0
  map06_09_validation_digest: 1180f6a784b29739a2ca640d2c45398066ec7e636a8cb69ee307315cc20cc84e
  map06_09_world_mandatory_regions_type0: 169/47/12/39
  map06_09_access_clue_reward: 12/12/12
  map06_09_return_assignments_returnable_nonreturnable: 12/39/0
  map06_09_inactive_decorative_interior: 78/52/26
  map06_09_protected_union: 91
  map06_09_approved_adapter_overlap_count: 3
  map06_09_open_edge_to_inactive_type0_lr_open: 0/0
  map06_09_optional_region_validator_tests: 321/321
  map06_09_required_total: 4305/4305
  map06_09_failed_skipped: 0/0
  map06_09_compile_console_warnings: 0/0/0
  map06_09_assets_meta_total: 3311
  map06_09_authoring_csv_meta: 50/50
  map06_09_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map06_09_duplicate_guid_groups: 0
  map05_11_graph_nodes_directed_undirected_route_cells: 47/96/48/47
  map05_11_mask_counts_t1_t2_t3_t4ud_t4lud_t4rud_t4lrud: 20/4/4/17/0/0/2
  map05_11_type4_contract: U+D mandatory; L/R independent; UD/LUD/RUD/LRUD legal
forbids_started_task_prefixes:
  - MAP07
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
    destination: MapDesign/MCP/TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
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
- MAP06_09 Result는 exact `PASS`, SHA-256 `51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a`다.
- 이전 MAP06_09 Task file SHA-256은 `e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e`다.
- 새 MAP06_10 Task payload SHA-256은 `205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605`다.
- MAP06_09은 COMPLETE 자격을 갖췄고 MAP06_10은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP06_09까지 `COMPLETE`, MAP06_10 `CURRENT`, MAP07 이후는 `LOCKED`다.
- 새 Master/Status는 `77 COMPLETE / MAP06_10 CURRENT / 127 LOCKED`다.
- MAP06_09 validation digest `1180f6a784b29739a2ca640d2c45398066ec7e636a8cb69ee307315cc20cc84e`, issue count `0`, required task accounting `4305/4305`를 입력 기준으로 보존한다.
- Type4는 U+D mandatory, L/R independent이며 `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 legal이다.
- current Assets meta baseline은 `3311`, Authoring CSV/meta `50/50`, duplicate GUID `0`이다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
