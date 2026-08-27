# PATCH MANIFEST

```yaml
patch_id: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
patch_version: 1.0
revision_note: MAP07_01 PASS/finalize 후 microchunk tile-layer compatibility rule matrix task 하나만 연다. MAP07_03 transform 이후는 locked 상태로 유지한다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: COMPLETE
    MAP07_02_IMPLEMENT_TILE_LAYER_RULES: LOCKED
    MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION_RESULT.md
  exact_status: PASS
  sha256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION.md
  sha256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
current_task_payload:
  path: PAYLOAD/TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
  sha256: 0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_after_apply: 79 COMPLETE / 1 CURRENT / 125 LOCKED
  map07_01_result_sha256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
  map07_01_task_sha256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
  map07_01_model_digest: 673f8a5057a28e6b2dbceac1a43f4eee4b30f0ec2a3738939107759e229cb7d5
  map07_01_acceptance_total: 4851/4851
  map07_01_failed_skipped: 0/0
  map07_01_compile_console_warnings: 0/0/0
  map07_01_assets_meta_total: 3334
  map07_01_authoring_csv_meta: 50/50
  map07_01_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map07_01_forbidden_map07_02_plus_hits: 0
  tile_layer_rule_scope: compatibility_matrix_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
    destination: MapDesign/MCP/TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
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
- MAP07_01 Result는 exact `PASS`, SHA-256 `b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474`다.
- 이전 MAP07_01 Task file SHA-256은 `912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c`다.
- 새 MAP07_02 Task payload SHA-256은 `0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7`다.
- MAP07_01은 COMPLETE 자격을 갖췄고 MAP07_02는 아직 `LOCKED / DO NOT START`였다.
- 205개 상태 행에서 MAP07_01까지 `COMPLETE`, MAP07_02 `CURRENT`, MAP07_03 이후는 `LOCKED`다.
- 새 Master/Status는 `79 COMPLETE / MAP07_02 CURRENT / 125 LOCKED`다.
- MAP07_01 model digest `673f8a5057a28e6b2dbceac1a43f4eee4b30f0ec2a3738939107759e229cb7d5`, acceptance `4851/4851`, Assets meta `3334`, Authoring CSV/meta `50/50`를 입력 기준으로 보존한다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
