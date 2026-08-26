# PATCH MANIFEST

```yaml
patch_id: MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL
patch_version: 1.0
revision_note: MAP09_00 v1.0 structure PASS 뒤 누락된 single_task_v1 protocol만 설치한다. 이 패키지가 마지막 legacy folder ZIP이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP09_00_CREATE_V2_MODULE_STRUCTURE: COMPLETE
    MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP09_00_CREATE_V2_MODULE_STRUCTURE_RESULT.md
  exact_status: PASS
  sha256: 4c825c9ac77257bf293b9be86282e0562e3272ec38f1a4f8f9a4ff860983d478
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP09_00_CREATE_V2_MODULE_STRUCTURE.md
  sha256: d3b4d6ffdb149823c1e2686ccded43897127aa0b8ea9bc74a3da0491f457ab63
requires_current_master:
  path: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 2f1fa53df4eb3687507c68d51167f681872622ed818e4835773a9c121e8ef4a7
current_task_payload:
  path: PAYLOAD/TASKS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL.md
  sha256: 35185c5ea8a584cf89e97928e16fcf88c14684e5aaa7e6658a33e12aa741fd2f
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 28476c5171bbdfe5aa8d57eef13772f5a878ab9d6d9841c941e912f5175ff55d
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 4a485efb8c3b370fb8e0eec20192f1c9da517e0c771f5e8a21fc995b585ea8c7
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks_before_apply: 214
  master_tasks_after_apply: 215
  status_counts_before_apply: 106 COMPLETE / 0 CURRENT / 108 LOCKED
  status_counts_after_apply: 106 COMPLETE / 1 CURRENT / 108 LOCKED
  map09_00_structure: APPROVED
  target_directories_and_metas: 24/24
  architecture_fixtures: 10/10
  duplicate_guid_groups: 0
  authoring_manifest_sha256: f63021913802f9ddb1c9b66c7c271b43cd216ba6d4f43e7337e23bd78fd34acb
  global_assets_meta: 3840
  map_assets_meta: 611
  map09_00r_scope: exact_4_mcp_docs_plus_1_inert_template_only
  future_patch_transport: one_markdown_file_in_mcp_inbox
forbids_started_task_keys:
  - MAP09_01_FREEZE_BASELINE_AND_REGISTER_V2_PASSES
forbids_started_task_prefixes:
  - MAP10
  - MAP11
  - MAP12
  - MAP13
  - MAP14
  - MAP15
  - MAP16
  - MAP17
  - MAP18
  - MAP19
  - MAP20
  - MAP21
sets_current_task: TASKS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL.md
    destination: MapDesign/MCP/TASKS/MAP09_00R_INSTALL_SINGLE_MD_INBOX_PROTOCOL.md
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
  - start_map09_01_or_later
  - git_commit_during_patch_apply
  - git_push
```

## Apply Validation

- 적용 전 Current Task는 `NONE`, MAP09_00은 `COMPLETE`, MAP09_01 이후는 `LOCKED`다.
- MAP09_00 Result/Task/Master의 exact SHA-256을 검증한다.
- 적용 후 `215 rows = 106 COMPLETE / MAP09_00R CURRENT / 108 LOCKED`다.
- PATCH APPLY에서는 Master/Status/Task 문서만 설치한다. MCP protocol 문서 4개와 template 1개는 Task Execution에서만 변경한다.
- Assets 및 MAP09_01 기능 구현은 전부 금지한다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
