# PATCH MANIFEST

```yaml
patch_id: MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION
patch_version: 1.0
revision_note: MAP06_10 PASS/finalize 및 MAP06 PHASE EXIT APPROVED 후 microchunk immutable definition model task 하나만 연다. MAP07_02 tile-layer rules 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS: COMPLETE
    MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: LOCKED
    MAP07_02_IMPLEMENT_TILE_LAYER_RULES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
  exact_status: PASS
  exact_phase_exit: "MAP06 PHASE EXIT: APPROVED"
  sha256: 690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS.md
  sha256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION.md
  sha256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 78 COMPLETE / 1 CURRENT / 126 LOCKED
  map06_10_result_sha256: 690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb
  map06_10_repaired_task_sha256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
  map06_phase_exit: APPROVED
  map06_overlay_digest: 9cbd3833bf5e8b771f46cc3442c1c60d63493d8ffb7e8aa3c9a701f6a097fefd
  map06_10_acceptance_total: 4705/4705
  map06_10_failed_skipped: 0/0
  map06_10_compile_console_warnings: 0/0/0
  map06_10_assets_meta_total: 3323
  map06_10_authoring_csv_meta: 50/50
  map06_10_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  starter_microchunk_catalog_tile_cells_sockets_slots: 14/1344/25/9
  microchunk_dimensions_cells_layers: 12x8/96/8
  map05_11_graph_nodes_directed_undirected_route_cells: 47/96/48/47
  map05_11_mask_counts_t1_t2_t3_t4ud_t4lud_t4rud_t4lrud: 20/4/4/17/0/0/2
  map05_11_type4_contract: U+D mandatory; L/R independent; UD/LUD/RUD/LRUD legal
forbids_started_task_prefixes:
  - MAP07_02
  - MAP07_03
  - MAP07_04
  - MAP07_05
  - MAP07_06
  - MAP07_07
  - MAP07_08
  - MAP07_09
  - MAP07_10
  - MAP07_11
  - MAP07_12
  - MAP07_13
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION.md
    destination: MapDesign/MCP/TASKS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION.md
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
- MAP06_10 Result는 exact `PASS`, `MAP06 PHASE EXIT: APPROVED`, SHA-256 `690a7cef9dbf1d22416e38b3675d76b0ef758062de2425e8e4841381f0d9bdeb`다.
- 이전 MAP06_10 Task file SHA-256은 repaired task 기준 `623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb`다.
- 새 MAP07_01 Task payload SHA-256은 `912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c`다.
- MAP06_10은 COMPLETE 자격을 갖췄고 MAP07_01은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP06_10까지 `COMPLETE`, MAP07_01 `CURRENT`, MAP07_02 이후는 `LOCKED`다.
- 새 Master/Status는 `78 COMPLETE / MAP07_01 CURRENT / 126 LOCKED`다.
- MAP06 overlay digest `9cbd3833bf5e8b771f46cc3442c1c60d63493d8ffb7e8aa3c9a701f6a097fefd`, acceptance `4705/4705`, Assets meta `3323`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- Type4는 U+D mandatory, L/R independent이며 `UD`, `LUD`, `RUD`, `LRUD` 네 조합 모두 legal이다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
