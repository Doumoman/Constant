# PATCH MANIFEST

```yaml
patch_id: MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE
patch_version: 1.0
revision_note: MAP07_06 PASS/finalize 후 microchunk reachability probe task 하나만 연다. MAP07_08 authoring grid 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_06_IMPLEMENT_96_CELL_VALIDATOR: COMPLETE
    MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE: LOCKED
    MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR_RESULT.md
  exact_status: PASS
  sha256: 81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_06_IMPLEMENT_96_CELL_VALIDATOR.md
  sha256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE.md
  sha256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 84 COMPLETE / 1 CURRENT / 120 LOCKED
  map07_06_result_sha256: 81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0
  map07_06_task_sha256: 38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa
  map07_06_96_cell_digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
  map07_05_object_slot_digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
  map07_04_socket_edge_digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
  map07_03_transform_digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
  map07_02_tile_layer_digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
  updated_map07_01_model_digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
  map07_06_acceptance_total: 6705/6705
  map07_06_failed_skipped: 0/0
  map07_06_compile_console_warnings: 0/0/0
  map07_06_assets_meta_total: 3362
  map07_06_authoring_csv_meta: 50/50
  map07_06_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_06_generated_csv_created: 0
  reachability_scope: runtime_probe_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE.md
    destination: MapDesign/MCP/TASKS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE.md
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
- MAP07_06 Result는 exact `PASS`, SHA-256 `81681d92aac6bff244dc7f655014c89cabb43baa178b3355fe701c6046b1a6e0`다.
- 이전 MAP07_06 Task file SHA-256은 `38a601ca63dff23622564cf36b3c02aa2f55849808c69b3a58bf60d2a8d7c6fa`다.
- 새 MAP07_07 Task payload SHA-256은 `0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103`다.
- MAP07_06은 COMPLETE 자격을 갖췄고 MAP07_07은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_06까지 `COMPLETE`, MAP07_07 `CURRENT`, MAP07_08 이후는 `LOCKED`다.
- 새 Master/Status는 `84 COMPLETE / MAP07_07 CURRENT / 120 LOCKED`다.
- MAP07_06 96-cell validator digest `54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c`, acceptance `6705/6705`, Assets meta `3362`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
