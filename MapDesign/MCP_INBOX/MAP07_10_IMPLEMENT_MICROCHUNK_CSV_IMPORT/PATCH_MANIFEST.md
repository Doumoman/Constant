# PATCH MANIFEST

```yaml
patch_id: MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT
patch_version: 1.0
revision_note: MAP07_09 PASS/finalize 후 microchunk CSV import task 하나만 연다. MAP07_11 CSV export 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR: COMPLETE
    MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT: LOCKED
    MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR_RESULT.md
  exact_status: PASS
  sha256: 7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_09_CREATE_SOCKET_AND_SLOT_EDITOR.md
  sha256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT.md
  sha256: a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 3c908bc3ae125be1f1af847cbe61d70f40c097fe0afcb8bb72187dec2ca43fa9
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 9d26ba12e24b820f9745ec1092aaab30254c498a66d8d794f5eb17891a4f63fb
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 87 COMPLETE / 1 CURRENT / 117 LOCKED
  map07_09_result_sha256: 7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340
  map07_09_task_sha256: 5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87
  map07_09_socket_slot_editor_digest: fee0842a7066866ec9f665fbd924c2fead24300c190d9be8e6e42ff0b435dffa
  map07_08_authoring_grid_digest: fe55586945da9aaa3b4bcebb3dd38ac82d2f5287e9f99bc31dc50fd30163abe9
  map07_07_reachability_digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
  map07_06_96_cell_digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
  map07_05_object_slot_digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
  map07_04_socket_edge_digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
  map07_03_transform_digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
  map07_02_tile_layer_digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
  updated_map07_01_model_digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
  map07_09_acceptance_total: 7927/7927
  map07_09_failed_skipped: 0/0
  map07_09_compile_console_warnings: 0/0/0
  map07_09_assets_meta_total: 3386
  map07_09_authoring_csv_meta: 50/50
  map07_09_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_09_generated_csv_created: 0
  map07_10_scope: editor_csv_import_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT.md
    destination: MapDesign/MCP/TASKS/MAP07_10_IMPLEMENT_MICROCHUNK_CSV_IMPORT.md
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
- MAP07_09 Result는 exact `PASS`, SHA-256 `7bc550e92359f4f24c642b24000be1e1a8198fdeb014ce1685555bf5f83a0340`다.
- 이전 MAP07_09 Task file SHA-256은 `5e870b792acdaff3ffb12058919f8973cd0fa50dcfd505b662c323f47a6f1a87`다.
- 새 MAP07_10 Task payload SHA-256은 `a21f95a87c1f962fed4672376d55eb740af6fa5d8b0aa8ec286ba782b2f54735`다.
- MAP07_09는 COMPLETE 자격을 갖췄고 MAP07_10은 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_09까지 `COMPLETE`, MAP07_10 `CURRENT`, MAP07_11 이후는 `LOCKED`다.
- 새 Master/Status는 `87 COMPLETE / MAP07_10 CURRENT / 117 LOCKED`다.
- MAP07_09 socket/slot editor digest `fee0842a7066866ec9f665fbd924c2fead24300c190d9be8e6e42ff0b435dffa`, acceptance `7927/7927`, Assets meta `3386`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
