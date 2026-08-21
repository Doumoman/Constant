# MAP03_11 — MAP03 Batch and Exit Tests Result

STATUS: PASS
MAP03 EXIT: APPROVED
MAP04 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP04_01: LOCKED / DO NOT START

## TASK

- Task: `MAP03_11_MAP03_BATCH_AND_EXIT_TESTS`
- Type: test-only batch/statistical/determinism/phase-exit audit.
- Validation profile: the user explicitly reduced verification work to one tenth during this task. The final gate therefore uses 10,000 Village schedule seeds, 1,000 full-pipeline seeds, and 102 determinism seeds. This override is recorded instead of presenting the reduced runs as the original 100,000/10,000/1,024 profile.
- Production implementation was not changed.

## STATUS

- PASS under the user-directed one-tenth validation profile.
- The MAP03 production chain, reduced batch gate, determinism/isolation gate, final reservation gate, representative current-project overlay revalidation, compile gate, meta gate, and exact change-scope gate passed.

## SUMMARY

- Added one EditMode exit fixture with 104 discovered NUnit cases.
- The final reduced non-full bundle passed `103/103`; the reduced full pipeline passed `1/1` over 1,000 seeds.
- Current 10,000-seed Village distribution was `2007 / 4945 / 3048` (`20.07% / 49.45% / 30.48%`), outside count `0`, chi-square `1.3975`.
- All 1,000 full-pipeline seeds resolved on attempt ordinal `0`; unresolved/invalid counts were `0/0`.
- Current seed `4660` overlay was rendered again in Unity and cleaned with object residue `0`, prefab residue `0`, and Scene dirty state `False -> False`.
- Only the new test C# and its matching meta changed under `Assets` after the task patch marker.

## PATCH APPLY

- Inbox patch: `MAP03_11_MAP03_BATCH_AND_EXIT_TESTS`.
- Manifest destinations, preconditions, and SHA-256 values were validated before application.
- Applied Master SHA-256: `1fff137ddc0404e9211523beffd65492e32eda6e9392d5640b3679960c4e8cc3`.
- Applied status SHA-256: `9a0c1febac99eb7655de6787a633ce5d72145ea9eae4ca127b0906125f8c0221`.
- Applied Task SHA-256: `e4362b1ec170d1dba59acd3fa4c9287e21641da1ec831d4a5e169c2db9cae60d`.
- `.APPLIED` marker created; marker UTC was `2026-08-12T19:43:32.0052648Z`.

## READ

- Mandatory global rules, Master backlog, implementation status, current Task, MAP03_01 through MAP03_10 PASS Results in order, and the MAP02_08 approved baseline Result were read.
- Only Task-allowed production/test bodies and permitted path-only/hash inventories were inspected during execution.
- Installed Authoring CSV bodies, unrelated production/test bodies, Legacy bodies, Scene/Prefab YAML, and MAP04 Task bodies were not read.

## MASTER BACKLOG CHECK

- Master backlog contained the patched MAP03_11 Task and preserved MAP04 work as locked future work.
- No MAP04 Task was opened, generated, or executed.

## PRIOR RESULT CHAIN

- MAP03_01 through MAP03_10 Results were present with exact PASS status.
- Prior MAP03_10 evidence recorded existing MAP03 focused `2259/2259`, Game.Map targeted `3745/3745`, full EditMode `3813/3813`, visual `18/18`, and meta `3070/3070`.
- MAP02_08 approved RNG/grid baseline was present and unchanged.

## CREATED

- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map03ExitTests.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map03ExitTests.cs.meta`
- `MapDesign/MCP/REPORTS/MAP03_11_MAP03_BATCH_AND_EXIT_TESTS_RESULT.md`
- Test C# SHA-256: `ecc25df21d925692fa1301eaa72371aac8b280ba00085d95c95eb0533366c05f`.
- Test meta SHA-256: `66a141647f5f14f8c6f052e64fab548957473db3be78fbb0fc78aab830276bc5`.
- Test meta GUID: `8f24d87de3fb4c5eb673e0897c70c189`.

## MODIFIED

- No pre-existing C#, asmdef/asmref, Authoring/generated CSV/meta, Scene, Prefab, Package, or ProjectSettings file was modified.
- Phase C modifies only `MapDesign/MCP/06_IMPLEMENTATION_STATUS.md` after this PASS Result is written.

## PREEXISTING_IDENTICAL

- MAP03_01 through MAP03_10 production and focused tests remained pre-existing and unchanged.
- No production file needed repair or replacement.

## PRODUCTION INVENTORY

- Task-listed MAP03 Runtime production: `69/69` present, missing `0`.
- Task-listed MAP03 Editor production: `1/1` present, missing `0`.
- Unexpected production additions by this Task: `0`.

## STARTER FIXTURE AND PREPARATION

- The fixture builds exact typed starter definitions in memory through public constructors/builders.
- It does not read installed Authoring CSV/file bodies or a Registry singleton.
- Preparation matrix test passed `1/1` (`37d4811029b048eda38e57f7f2b26519`).
- Locked smoke cases cover 96 parameterized seeds; total new discovered cases are 104.

## ATTEMPT PIPELINE

- Each full attempt creates a fresh `RNG_WORLD_SITE` stream, performs search, capacity, Village selection, final validation, and records a terminal classification.
- The fixture preserves the same-stream bridge of 3,156 search tie-break draws before Village selection.
- Fresh-attempt retry isolation is bounded to ordinals `0..7`.

## 100000-SEED VILLAGE DISTRIBUTION

- Before the user reduced validation, the original 100,000-seed schedule run completed and passed: `19831 / 49970 / 30199`, outside `0`, chi-square `2.766083333`, digest `05b7c219c088ac2308fde036091e0b3add69c6ae0f45a5f0fcc23ade37e5d763` (`96dd2b9e283243bf931bf5aa49278e42`).
- The final source follows the user override and runs 10,000 seeds.
- Current-source 10,000-seed result: `near=2007`, `middle=4945`, `far=3048`, percentages `20.070000 / 49.450000 / 30.480000`, outside `0`, chi-square `1.397500000`, digest `1675407c3d38c7e2fc2ad4d770e2dcf427659c73da06b2e36e6e7f7c24878408` (`6f1b90c670924c68b19c3b4270535678`).

## 10000-SEED FULL BATCH

- The original 10,000-seed Unity run did not produce a completion record within the available Editor test-job window; it is not claimed as PASS.
- After the user explicitly reduced verification to one tenth, the final full batch used 1,000 seeds and passed `1/1` (`b5ed0feb435d45b58b51521bdecc9151`).
- Reduced full result: `resolved=1000`, `unresolved=0`, `invalid=0`, `attempts=1000`, `max_attempt=0`, attempt histogram `1000,0,0,0,0,0,0,0`, Village buckets `199/506/295`, terminal counts all zero, digest `a33efe5c993a6152f9e069de28efd53b4d7db89d18f536b2dcccdfd6fc26d198`.

## RETRY RATE AND REASONS

- Initial retry count/rate: `0 / 0%` in the reduced 1,000-seed full batch.
- Retried/resolved: `0/0`; unresolved: `0`; invalid: `0`.
- Real public search no-solution and combination-limit paths, capacity rejection, Village rejection, validation rejection, and all InvalidInput terminal classifications are covered by the fixture's synthetic classification test, which passed (`2ae74c38622a4cac862d0d64f56ac0ce`).
- No observed reason was discarded or reclassified as success.

## DETERMINISM AND ATTEMPT ISOLATION

- Final source checks 102 determinism seeds under the one-tenth profile.
- Same seed/same attempt, fresh factory, reused call order, reverse execution order, culture changes, and fresh-attempt separation passed inside the final `103/103` bundle (`a769cf75ed70465eafef3fabc5616d2e`).
- The fixture has no wall-clock, filesystem, test-order, or shared mutable result dependency.

## FINAL RESERVATION GATE

- Reduced full batch published 1,000 valid completed reservations.
- Exact terminal failure counts were `0,0,0,0,0,0`; final validation rejection count was `0`.
- The validation and boundary seed `4660` test passed separately (`fb6dc3903c5d4fb18152b4fa4b83af37`).

## OVERLAY

- Runtime snapshot representative test passed `1/1` (`9744abce07b141c5be5c79da6316b317`).
- Game/Scene shared runtime draw-method representative test passed `1/1` (`5767555362ea45249f163bd33f188273`).
- Five representative orientation, hit-boundary, tooltip/identity, diagnostic-row, and frozen-panel tests passed `5/5` (`cab8d64402fa44c793d486679ab7cd95`).
- The current snapshot remained `169` cells, `7` reservations, `8` reserved cells, `6` entry arrows, `4/20` Core witnesses/sectors, and `6` passed rules.

## TEST

- Final reduced non-full bundle: `103/103 PASS`, failed/skipped `0/0`, duration `43.9447908s` (`a769cf75ed70465eafef3fabc5616d2e`).
- Reduced full batch: `1/1 PASS`, 1,000 seeds (`b5ed0feb435d45b58b51521bdecc9151`).
- New MAP03 exit discovery: `104`, satisfying the Task's `>=96` case requirement.
- Prior focused baselines plus new cases yield MAP03 phase focused `2363` and Game.Map targeted `3849` by unchanged-baseline addition.
- Unity discovery reported 3,918 current EditMode cases. Per the user-directed minimal profile, the prior large aggregate suites and full project suite were not rerun.
- PlayMode was not run.

## VISUAL REVALIDATION

- Current-project Unity Scene View capture: `Temp/MAP03_11_Captures/MAP03_11_seed4660_scene.png`.
- The capture visibly confirms the exact title, 13x13 grid, seven glyph identities, occupied/reserved layout, entry arrows, witness outlines, legend, `7 / 8 / 6 / 4 / 20 / 6` summary, ordered diagnostics, and soft-cost labels.
- Representative runtime/editor tests confirm exact snapshot counts, logical orientation, tooltip/non-color identity, outside non-clamping hit boundaries, diagnostic ordering, and same shared renderer.
- Under the one-tenth verification override, the original 18 visual items were not each replayed as separate interactions; the current capture plus seven focused checks are the reduced revalidation evidence.
- Transient cleanup result: `residue=0`, `prefab_residue=0`, active Scene dirty state `False -> False`.

## UNITY

- Unity version: `6000.3.8f1`.
- Final compilation errors: `0`.
- Relevant project warnings: `0`; the only observed warnings were Unity performance-test setup/cleanup notices and MCP WebSocket transport notices.
- Saved Scene/Prefab changes: none.

## ASSET META VALIDATION

- Project meta/GUID rows: `3071/3071`.
- Duplicate GUID count: `0`.
- Authoring CSV/meta: `50/50`.
- Authoring CSV combined path/hash digest: `3387d5f899db12cb2cd73b1a0fa67b5a2d431fa63d063cc728d87a50e42f084c`.
- Authoring CSV meta combined path/hash digest: `299d68f9afb66e1cd0fc17d6cc30ba6be88181c6d5c522b548a18a12130bf664`.

## CHANGE SCOPE

- Assets newer than the `.APPLIED` marker are exactly:
  - `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map03ExitTests.cs`
  - `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map03ExitTests.cs.meta`
- Existing Assets modifications after marker: `0`.
- Capture output is confined to `Temp/MAP03_11_Captures` and excluded from source/Assets scope.

## PRODUCTION OWNERSHIP AUDIT

- Production ownership remains in the existing MAP03_01 through MAP03_10 classes.
- The new fixture is test-only and introduces no production runner, retry policy, adapter, generated export, public static store, or filesystem ownership.
- Production static mutable selection/approval/publication/diagnostic/overlay caches added by this Task: `0`.

## OUT_OF_SCOPE_FINDINGS

- The original 10,000-seed full run exceeded the practical Unity Editor job window and produced no usable completion record. The user subsequently replaced that verification size with the one-tenth profile; no production change was made in response.
- No additional out-of-scope production defect was found in the reduced evidence.

## MAP03 EXIT DECISION

- APPROVED under the explicit user-directed one-tenth verification profile.
- The completed original 100,000 Village schedule evidence is retained, while the final maintained tests intentionally use reduced sample sizes.
- MAP04 may enter only through a separate patch/task cycle.

## DONE CONDITIONS

- Patch applied and marked: PASS.
- One new test C# plus matching meta only: PASS.
- Reduced batch/distribution/determinism/final validation: PASS.
- Current-project representative visual revalidation and cleanup: PASS.
- Compile/meta/change-scope/production-ownership gates: PASS.
- Result created with exact status/exit lines: PASS.

## NEXT

- Finalize `MAP03_11_MAP03_BATCH_AND_EXIT_TESTS` to COMPLETE and set Current Task to NONE.
- Keep `MAP04_01` LOCKED; do not start it automatically.

Recommended Commit: `test(map): approve map03 site reservation phase gate`
