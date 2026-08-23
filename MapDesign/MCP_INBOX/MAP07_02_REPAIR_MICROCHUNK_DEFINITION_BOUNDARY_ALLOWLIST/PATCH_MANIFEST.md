# PATCH MANIFEST

```yaml
patch_id: MAP07_02_REPAIR_MICROCHUNK_DEFINITION_BOUNDARY_ALLOWLIST
patch_version: 1.1
revision_note: MAP07_02 BLOCKED 후 MicrochunkDefinitionTests obsolete boundary-symbol write allowlist contradiction만 교정한다. MAP07_03은 시작하지 않는다.
requires_status:
  current_task: TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
  status_counts: 79 COMPLETE / 1 CURRENT / 125 LOCKED
  task_states_required:
    MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION: COMPLETE
    MAP07_02_IMPLEMENT_TILE_LAYER_RULES: CURRENT
    MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS: LOCKED
requires_result:
  path: MapDesign/MCP/REPORTS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES_RESULT.md
  exact_status: BLOCKED
  sha256: 8691d0976dd9ab51794c39d076a58625196191ec0195497734883eff9868ef1c
repair_basis:
  current_task_sha256: 0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7
  revised_task_sha256: 18d7d4c330b7a3614f155914aea8247412f65eb9ba04335ad5ea1dfffd5231f4
  blocked_result_sha256: 8691d0976dd9ab51794c39d076a58625196191ec0195497734883eff9868ef1c
  prior_map07_01_result_sha256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
  prior_map07_01_task_sha256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
  original_map07_02_manifest_sha256: e9d840950db8d4f67a1532a04c6b6767fe65d36b529edccf48efaced80ceba68
  blocked_reason: required_MicrochunkTileLayerRules_api_conflicted_with_MicrochunkDefinitionTests_absence_case_and_write_allowlist
  repair_write_allowlist_addition: Assets/_Game/Tests/EditMode/Map/WorldGeneration/Microchunks/MicrochunkDefinitionTests.cs
  allowed_test_edit: replace_obsolete_MicrochunkTileLayerRules_absence_case_with_MAP07_03_plus_forbidden_symbol
  required_test_count_preserved: MicrochunkDefinitionTests 146/146
  map07_01_model_digest: 673f8a5057a28e6b2dbceac1a43f4eee4b30f0ec2a3738939107759e229cb7d5
  authoring_csv_policy: original source artifact only; do not modify unless explicitly requested
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
  - source: PAYLOAD/TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
    destination: MapDesign/MCP/TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md
    mode: replace_if_sha256_matches
    required_existing_sha256: 0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7
forbidden_operations:
  - delete
  - modify_master_implementation_task_list
  - modify_implementation_status_during_patch_apply
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

- 적용 전 Current Task는 `TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md`다.
- 현재 Task SHA-256은 `0b69d8f46654bd2af5e441d603210a1889351cff478b688a23b6b87c697ea9c7`다.
- 현재 MAP07_02 Result는 exact `BLOCKED`, SHA-256 `8691d0976dd9ab51794c39d076a58625196191ec0195497734883eff9868ef1c`다.
- 205개 상태 행에서 MAP07_01까지 `COMPLETE`, MAP07_02 `CURRENT`, MAP07_03 이후는 `LOCKED`다.
- PATCH APPLY 단계에서는 Task 문서만 replace하고 Master/Status/Assets/CSV/C#/test/asmdef는 변경하지 않는다.
- 교정 Task SHA-256은 `18d7d4c330b7a3614f155914aea8247412f65eb9ba04335ad5ea1dfffd5231f4`다.
- repair는 `MicrochunkDefinitionTests.cs` write allowlist 추가와 exact boundary-symbol replacement permission만 포함한다.
- Authoring CSV는 source artifact로만 보존하고 수정하지 않는다.

Task destination이 required_existing_sha256과 다르면 `BLOCKED`다.
