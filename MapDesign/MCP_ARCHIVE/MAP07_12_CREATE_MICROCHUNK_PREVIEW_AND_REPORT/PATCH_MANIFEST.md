# PATCH MANIFEST

```yaml
patch_id: MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT
patch_version: 1.0
revision_note: MAP07_11 PASS/finalize 후 microchunk preview/report task 하나만 연다. MAP07_13 starter/exit 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT: COMPLETE
    MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT: LOCKED
    MAP07_13_MAP07_STARTER_AND_EXIT_TESTS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT_RESULT.md
  exact_status: PASS
  sha256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_11_IMPLEMENT_MICROCHUNK_CSV_EXPORT.md
  sha256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT.md
  sha256: 73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 2cc6df1bb48e3f3ee9c9d28699790d67285eee7d1c8d2e74b86bb492da4cae04
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 1f636e4fe962df21f28be2122cee71221018ce3d03999990c4041175fb849cae
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 89 COMPLETE / 1 CURRENT / 115 LOCKED
  map07_phase_after_apply: 11 COMPLETE / 1 CURRENT / 1 LOCKED
  map07_11_result_sha256: 340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6
  map07_11_task_sha256: 1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca
  map07_11_csv_exporter_digest: abd090a627f295cc91593e49b78e2c7871ff3210c5ace87af43677027898f976
  map07_10_csv_importer_digest: 14bf29aa6edab12ed11caffbd38770690a16ac0a13c82e1ec3fc2c25739b26c6
  map07_09_socket_slot_editor_digest: fee0842a7066866ec9f665fbd924c2fead24300c190d9be8e6e42ff0b435dffa
  map07_08_authoring_grid_digest: fe55586945da9aaa3b4bcebb3dd38ac82d2f5287e9f99bc31dc50fd30163abe9
  map07_07_reachability_digest: f488c8a65dacb8f7bdd2c107478074c131e3011110058375c06e165bfb1ddaf3
  map07_06_96_cell_digest: 54a09f7327c37405a826e4fbc3bea9443e1472ec3f92f15b9495edfba422710c
  map07_05_object_slot_digest: 9a3b86991302add138c36acccd18789ad79195cd27cab7fb5fe2c5bb8a520e6a
  map07_04_socket_edge_digest: fdfbcb7bf651eb963d899f7e9800e0a8f23826d43ec4ffed25e3c32ee7a0c048
  map07_03_transform_digest: 7031695ec2c4bb333be69ba490c03aef003124d50953d577b4943c634336b031
  map07_02_tile_layer_digest: ace56d38399aa6ccea6e0e0e7361f3802b7eb7418b1ef1d081f6a40b04e08160
  updated_map07_01_model_digest: 5ed21ca7b86cfebf0095eba6d14bf4bb27be75ce39ffaf46f3c32a516d77613b
  map07_11_acceptance_total: 8807/8807
  map07_11_failed_skipped: 0/0
  map07_11_compile_console_warnings: 0/0/0
  map07_11_assets_meta_total: 3400
  map07_11_authoring_csv_meta: 50/50
  map07_11_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_11_generated_csv_created: 0
  map07_12_expected_assets_meta_after: 3407
  map07_12_scope: editor_preview_report_only
forbids_started_task_prefixes:
  - MAP07_13
  - MAP08
  - MAP09
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
sets_current_task: TASKS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT.md
    destination: MapDesign/MCP/TASKS/MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT.md
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
- MAP07_11 Result는 exact `PASS`, SHA-256 `340cbed5424208ebeef144028c1806ea6a9039e8a6c14a5f39a824b042b062c6`다.
- 이전 MAP07_11 Task file SHA-256은 `1359b31bd70bd8288f86fb2d994267d480b7130a96a45e25541de1c05ba7e6ca`다.
- 새 MAP07_12 Task payload SHA-256은 `73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0`다.
- MAP07_11은 COMPLETE 자격을 갖췄고 MAP07_12는 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_11까지 `COMPLETE`, MAP07_12 `CURRENT`, MAP07_13 이후는 `LOCKED`다.
- 새 Master/Status는 `89 COMPLETE / MAP07_12 CURRENT / 115 LOCKED`다.
- MAP07_11 CSV exporter digest `abd090a627f295cc91593e49b78e2c7871ff3210c5ace87af43677027898f976`, acceptance `8807/8807`, Assets meta `3400`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
