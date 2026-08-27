# PATCH MANIFEST

```yaml
patch_id: MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX
patch_version: 1.0
revision_note: MAP08_01 PASS/finalize 후 MAP08_02 task 하나만 연다. MAP08_03 이후는 locked 상태로 유지한다.
requires_status:
  current_task: MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS
  task_states_required:
    MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS: CURRENT
    MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX: LOCKED
    MAP08_03_IMPLEMENT_BOUNDARY_CHUNK_RESOLVER: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS_RESULT.md
  exact_status: PASS
  sha256: bc9298f3e51615b4d9724bcd2d7c8809b1ba8d3455aa30e8436f6a25ab6d5970
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS.md
  sha256: 19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX.md
  sha256: 767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: dbf837cbb8271b5efd575dcddd5bc0ae692ba32040889f132a4acd34fbd46902
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 4cb0f63f153e270c6c6f19cb394f8411f30a513b75520374d5038ee643161ed5
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 91 COMPLETE / 1 CURRENT / 113 LOCKED
  status_counts_after_apply: 92 COMPLETE / 1 CURRENT / 112 LOCKED
  map08_phase_before_apply: 0 COMPLETE / 1 CURRENT / 13 LOCKED
  map08_phase_after_apply: 1 COMPLETE / 1 CURRENT / 12 LOCKED
  map08_01_result_sha256: bc9298f3e51615b4d9724bcd2d7c8809b1ba8d3455aa30e8436f6a25ab6d5970
  map08_01_task_sha256: 19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d
  map08_01_patch_receipt_sha256: b82282016e5d352cb6adbd0605ed474698bf4c569ce32d40473984c1ead56858
  map08_01_focused_tests: 400/400
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_01_acceptance_total: 10527/10527
  map08_01_failed_skipped: 0/0
  map08_01_compile_console_warnings: 0/0/0
  map08_01_assets_meta_total: 3419
  map08_01_authoring_csv_meta: 50/50
  map08_01_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map08_01_generated_csv_created: 0
  map08_02_scope: boundary_candidate_index_only
forbids_started_task_prefixes:
  - MAP08_03
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
sets_current_task: TASKS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX.md
    destination: MapDesign/MCP/TASKS/MAP08_02_IMPLEMENT_BOUNDARY_CANDIDATE_INDEX.md
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

- 적용 전 Current Task는 `MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS`다.
- MAP08_01 Result는 exact `PASS`, SHA-256 `bc9298f3e51615b4d9724bcd2d7c8809b1ba8d3455aa30e8436f6a25ab6d5970`다.
- 이전 MAP08_01 Task file SHA-256은 `19b9c50827238251e0851e7bfee6e6a216141696ed434509a47ff08b0e39848d`다.
- 새 MAP08_02 Task payload SHA-256은 `767fa235852c8b892fdb4dffe6cfdbda4283aa994f0dce64b295bdbbd4857e50`다.
- 205개 상태 행에서 MAP08_01까지 `COMPLETE`, MAP08_02 `CURRENT`, MAP08_03 이후는 `LOCKED`다.
- 새 Master/Status는 `92 COMPLETE / MAP08_02 CURRENT / 112 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
