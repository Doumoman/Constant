# PATCH MANIFEST

```yaml
patch_id: MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID
patch_version: 1.0
revision_note: MAP07_07 PASS/finalize 후 microchunk authoring grid task 하나만 연다. MAP07_09 socket/slot editor 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE: COMPLETE
    MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID: LOCKED
    MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE_RESULT.md
  exact_status: PASS
  sha256: afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_07_IMPLEMENT_MICROCHUNK_REACHABILITY_PROBE.md
  sha256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID.md
  sha256: 6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: fc8fb141a1e37b824b6722f5657e15229bdf0ead06ed1b12bc369f71e1a62154
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 200e8d94143070c268538596d27420b66d1e389091fcd7c84b15f41e0dd8d0cf
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 85 COMPLETE / 1 CURRENT / 119 LOCKED
  map07_07_result_sha256: afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19
  map07_07_task_sha256: 0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103
  map07_07_reachability_digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
  map07_06_96_cell_digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
  map07_05_object_slot_digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
  map07_04_socket_edge_digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
  map07_03_transform_digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
  map07_02_tile_layer_digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
  updated_map07_01_model_digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
  map07_07_acceptance_total: 7227/7227
  map07_07_failed_skipped: 0/0
  map07_07_compile_console_warnings: 0/0/0
  map07_07_assets_meta_total: 3369
  map07_07_authoring_csv_meta: 50/50
  map07_07_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_07_generated_csv_created: 0
  map07_08_scope: editor_authoring_grid_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID.md
    destination: MapDesign/MCP/TASKS/MAP07_08_CREATE_MICROCHUNK_AUTHORING_GRID.md
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
- MAP07_07 Result는 exact `PASS`, SHA-256 `afaf3f058c34457d26491b15c06858ba1c1c7355cf14d5902d65f66a43a1fa19`다.
- 이전 MAP07_07 Task file SHA-256은 `0d9ec87691cf31db249b2fed7b411ea6b69a1d8c456469672c96999145add103`다.
- 새 MAP07_08 Task payload SHA-256은 `6d3b211b593743d9aebf6ba4f0c4fc9ef720d85139e9fe1e687231014ee00f29`다.
- MAP07_07은 COMPLETE 자격을 갖췄고 MAP07_08은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_07까지 `COMPLETE`, MAP07_08 `CURRENT`, MAP07_09 이후는 `LOCKED`다.
- 새 Master/Status는 `85 COMPLETE / MAP07_08 CURRENT / 119 LOCKED`다.
- MAP07_07 reachability digest `f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3`, acceptance `7227/7227`, Assets meta `3369`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
