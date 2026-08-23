# PATCH MANIFEST

```yaml
patch_id: MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION
patch_version: 1.0
revision_note: MAP07_03 PASS/finalize 후 socket-edge validation task 하나만 연다. MAP07_05 object-slot validation 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: COMPLETE
    MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION: LOCKED
    MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
  exact_status: PASS
  sha256: 062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS.md
  sha256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION.md
  sha256: a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 81 COMPLETE / 1 CURRENT / 123 LOCKED
  map07_03_result_sha256: 062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505
  map07_03_task_sha256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
  map07_03_transform_digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
  updated_map07_01_model_digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
  map07_02_tile_layer_digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
  map07_03_acceptance_total: 5484/5484
  map07_03_failed_skipped: 0/0
  map07_03_compile_console_warnings: 0/0/0
  map07_03_assets_meta_total: 3344
  map07_03_authoring_csv_meta: 50/50
  map07_03_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_03_generated_csv_created: 0
  socket_edge_scope: runtime_validator_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION.md
    destination: MapDesign/MCP/TASKS/MAP07_04_IMPLEMENT_SOCKET_EDGE_VALIDATION.md
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
- MAP07_03 Result는 exact `PASS`, SHA-256 `062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505`다.
- 이전 MAP07_03 Task file SHA-256은 `f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170`다.
- 새 MAP07_04 Task payload SHA-256은 `a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6`다.
- MAP07_03은 COMPLETE 자격을 갖췄고 MAP07_04는 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_03까지 `COMPLETE`, MAP07_04 `CURRENT`, MAP07_05 이후는 `LOCKED`다.
- 새 Master/Status는 `81 COMPLETE / MAP07_04 CURRENT / 123 LOCKED`다.
- MAP07_03 transform digest `7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031`, acceptance `5484/5484`, Assets meta `3344`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
