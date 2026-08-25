# PATCH MANIFEST

```yaml
patch_id: MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES
patch_version: 1.2-repair-map-meta-scope
revision_note: MAP08_05 PASS/finalize 후 병행 Character 병합이 추가한 Assets meta 226개를 승인된 비-Map 기준 변화로 기록하고, CSV allowlist를 설치된 Data/WorldGeneration/Authoring 경로에 맞췄다. 또한 신규 메타 6개 중 Runtime 4개만 Assets/_Game/Map 아래이고 Test 2개는 Assets/_Game/Tests 아래인 정확한 경로 소유권에 맞춰 Map meta 종료 게이트를 570으로 보정한 뒤 MAP08_06 task 하나만 연다. MAP08_07 이후는 locked 상태로 유지한다. 적용 전 상태는 finalized current NONE 기준이다.
requires_status:
  current_task: NONE
  task_states_required:
    MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT: COMPLETE
    MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES: LOCKED
    MAP08_07_AUTHOR_CRATER_MILL_BOUNDARIES: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT_RESULT.md
  exact_status: PASS
  sha256: ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0
requires_previous_task_file:
  path: MapDesign/MCP/TASKS/MAP08_05_IMPLEMENT_BOUNDARY_WARNING_CONTRACT.md
  sha256: 7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6
current_task_payload:
  path: PAYLOAD/TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
  sha256: 24799620d254846e3a99c0a3dadcb00156ab3f6f4804bbf09d2ce4eacda70293
payload_status:
  path: PAYLOAD/06_IMPLEMENTATION_STATUS.md
  sha256: 9769ad7ed11e84220ad039696af7d57fb69b98771fec40fa00e718f99b942dcc
payload_master:
  path: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
  sha256: 31ac7b4e5c83ab539bbcf3235b76a725468c4d37688e3fe9e4dbe9ac6ddf0735
requires_project_baseline:
  unity: 6000.3.8f1
  master_tasks: 205
  status_counts_before_apply: 96 COMPLETE / 0 CURRENT / 109 LOCKED
  status_counts_after_apply: 96 COMPLETE / 1 CURRENT / 108 LOCKED
  map08_phase_before_apply: 5 COMPLETE / 0 CURRENT / 9 LOCKED
  map08_phase_after_apply: 5 COMPLETE / 1 CURRENT / 8 LOCKED
  map08_05_result_sha256: ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0
  map08_05_task_sha256: 7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6
  map08_05_patch_receipt_sha256: c31e1bde9497cdcfe89e5ea2430ce5415635efce3cb53688515fbbde70d4252e
  map08_05_focused_tests: 520/520
  map08_04_focused_tests: 520/520
  map08_03_focused_tests: 680/680
  map08_02_focused_tests: 580/580
  map08_01_focused_tests: 400/400
  map08_focused_total: 2700/2700
  map07_required_total: 5422/5422
  map06_required_total: 2746/2746
  map05_required_total: 1959/1959
  map08_05_acceptance_total: 12827/12827
  map08_05_failed_skipped: 0/0
  map08_05_compile_console_warnings: 0/0/0
  map08_05_assets_meta_total: 3455
  concurrent_character_merge_assets_meta_delta: 226
  assets_meta_total_after_concurrent_merge: 3681
  map_assets_meta_total_after_concurrent_merge: 566
  map08_05_authoring_csv_meta: 50/50
  map08_05_authoring_manifest_sha256: 4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3
  map08_05_generated_csv_created: 0
  map08_06_scope: crater_root_boundary_authoring_only
forbids_started_task_prefixes:
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
sets_current_task: TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
copy_operations:
  - source: PAYLOAD/MASTER_IMPLEMENTATION_TASK_LIST.md
    destination: MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md
    mode: replace
  - source: PAYLOAD/06_IMPLEMENTATION_STATUS.md
    destination: MapDesign/MCP/06_IMPLEMENTATION_STATUS.md
    mode: replace
  - source: PAYLOAD/TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
    destination: MapDesign/MCP/TASKS/MAP08_06_AUTHOR_CRATER_ROOT_BOUNDARIES.md
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
  - git_commit_during_patch_apply
  - git_push
```

## 적용 검증

- 적용 전 Current Task는 `NONE`이고 MAP08_05는 `COMPLETE`다.
- MAP08_05 Result는 exact `PASS`, SHA-256 `ff8e384a5d450d356017cb91ad52a25b1143931a823c48b67023a7b6be599ac0`다.
- 이전 MAP08_05 Task file SHA-256은 `7336541b62db64f0a4e40c2a892a6a07000fa011c87934a68fb9f785c10842a6`다.
- 새 MAP08_06 Task payload SHA-256은 `16dcc9d0f814043f5b786e4e3b95fed928131a6f09f48c9162a3a0fa0b6431fc`다.
- MAP08_05 이후 병행 Character 병합이 추가한 Assets meta는 정확히 `226`개이며, 전체 Assets meta는 `3681`, `Assets/_Game/Map` meta는 `566`이다.
- 205개 상태 행에서 MAP08_05까지 `COMPLETE`, MAP08_06 `CURRENT`, MAP08_07 이후는 `LOCKED`다.
- 새 Master/Status는 `96 COMPLETE / MAP08_06 CURRENT / 108 LOCKED`다.
- PATCH APPLY 단계에서는 Master/Status/Task 문서만 교체/생성하고 Assets, CSV, C#, test, asmdef는 변경하지 않는다.
- 구현 완료 후에는 Task 본문에 정의된 커밋 요구사항을 따른다. Patch apply 단계에서는 커밋하지 않는다.

Task destination이 이미 존재하면 payload와 바이트 동일할 때만 재사용하며 다르면 `BLOCKED`다.
