TASK: MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER
STATUS: PASS

## SUMMARY

- Implemented the immutable P00 seed manifest, canonical strict CSV serialization, exact two-file replay bundle, recorder, atomic directory publisher, replay player, and stable verification result.
- The recorder consumes one existing successful `ExecuteThroughRecorded(..., "PASS_GRID")` result without re-executing the root/pass and records only the existing `GRID` artifact.
- The player validates bundle/manifest/content/build boundaries before one exact grid replay and compares generated sector bytes without comparing timing diagnostics.
- No later-map output, placeholder sidecar, Authoring mutation, scene/prefab change, or next-task work was introduced.

## READ

- Read the MCP entrypoint, patch workflow, global rules, master backlog, implementation status, current Task, and MAP02_05 PASS Result within the workflow allowlists.
- Read only the current Task's exact existing runtime/test/asmdef allowlist for implementation API confirmation.
- Optional Map Package v1.0 exact paths were absent; the current Task frozen contract was used without substituting Legacy paths.
- Unity resource-first checks confirmed one active `Constant@ced6e0dfc4a31d45` instance on Unity `6000.3.8f1`.

## MASTER BACKLOG CHECK

- Master unique task IDs: `205`
- After Phase A patch: `32 COMPLETE / 1 CURRENT / 172 LOCKED`
- Current Task: `MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER`
- `MAP02_07` remains `LOCKED`.

## MAP02_05 GATE CHECK

- Previous Result: `MAP02_05_IMPLEMENT_PASS_EXECUTION_RECORDS_RESULT.md`
- Exact previous status: `STATUS: PASS`
- Previous focused/targeted/full evidence: `77/77`, `1277/1277`, `1297/1297`
- Gate: PASS

## CREATED

- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedManifest.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedManifestCsvSerializer.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayBundle.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayRecorder.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayVerificationResult.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayPlayer.cs`
- `Assets/_Game/Map/Runtime/WorldGeneration/Generation/SeedReplayPublisher.cs`
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/SeedReplayRecorderTests.cs`
- Matching Unity `.cs.meta` files: `8`
- This Result file.

## MODIFIED

- Existing C#/test/asmdef/asmref assets modified: `0`
- Existing Authoring CSV/meta modified: `0`
- Existing scene/prefab/package/project-setting files modified: `0`

## PREEXISTING_IDENTICAL

- None. All eight Task C# destinations and matching metas were absent before implementation.

## SEED MANIFEST

- `SeedManifest` is sealed and immutable with the exact 11 frozen properties.
- Strings are non-null and preserved exactly; profile/build IDs are non-empty; content hash is exact lowercase 64-hex.
- UTC offset, non-negative Int32 diagnostics, failure-rule IDs, delimiter exclusion, ordered duplicates, and copied read-only failure lists are enforced.
- P00 values produced by the recorder are exact: `Approved=false`, empty failure IDs, `Notes=MAP02_GRID_CHECKPOINT_V1`.

## CSV BYTES

- Filename/header/order: exact `seed_manifest.csv` 11-column contract.
- Header-only template: `184` bytes.
- Header-only SHA-256: `fb45bfbb905f165b4702515484b97c83232fca9aa7bf775dd46cc52421761b0c`.
- Encoding/records: one UTF-8 BOM, strict UTF-8, CRLF only, one header + one data row, one final CRLF.
- Canonical grammar: invariant unsigned/signed numbers, `0/1` bool, seven-digit UTC fraction, ordered `|` list, RFC4180 escaping.
- Strict deserialize rejects malformed BOM/header/UTF-8/line ending/quote/record/field/type/hash/UTC/range/canonical forms and accepted bytes serialize back identically.

## REPLAY BUNDLE

- Exact exposed file order: `seed_manifest.csv`, `generated_world_sectors.csv`.
- Frozen relative directory: `GeneratedWorlds/{world_profile_id}/{seed D16 minimum}` without truncating longer ulong values.
- Unsafe, traversal, rooted, control, invalid-filename, trailing-dot/space, and reserved-device world profile segments are rejected.
- Manifest object/bytes, relative identity, exact file set, defensive byte copies, and read-only filenames are enforced.
- Sector bytes must be the exact canonical 169-row P00 neutral grid for the manifest seed; malformed or silently repaired bytes are rejected.

## RECORDER

- Requires successful Result and ExecutionRecord, inclusive/last `PASS_GRID`, one successful pass/attempt, zero retries, exact seed propagation, and exact single `GRID` artifact.
- Validates the 13x13 P00 neutral cells and topology before serialization.
- Maps all 11 manifest fields from the existing execution record, content hash, and caller build ID exactly.
- Calls neither `WorldGenerationRoot` nor the pass and performs no filesystem access.

## PUBLISHER

- `SeedReplayPublisher` is the only new production type using `System.IO`.
- Requires a full normalized absolute root and resolves only the frozen safe relative directory.
- Uses deterministic `.staging` and `.backup` siblings, rejects stale state, writes/flushes/closes exact files, and reload-verifies before swap.
- New publish and replacement use directory moves; replacement restores the original directory where possible while preserving the original exception.
- Load rejects missing/extra/case-variant files, subdirectories, non-regular/reparse entries, ancestor reparse traversal, and manifest/path identity mismatch.
- Successful publish leaves exact two files and no staging/backup residue.

## PLAYER

- Holds only the injected non-null existing `WorldGenerationRoot`; no mutable/static replay state is used.
- Verification order is exact: bundle, P00 manifest, content hash, build ID, one `ExecuteThroughRecorded(..., PASS_GRID)`, execution identity, then one sector serialization/byte comparison.
- Invalid bundle/manifest/hash/build boundaries invoke the root zero times.
- Replay timing diagnostics are intentionally ignored and the recorded manifest is not regenerated for comparison.

## VERIFICATION RESULT

- Sealed immutable result with exact `Succeeded`, `Code`, and `Message` properties.
- Success requires empty code/message; failure requires a deterministic message and one exact stable code.
- Stable code set: `INVALID_BUNDLE`, `INVALID_MANIFEST`, `CONTENT_HASH_MISMATCH`, `GENERATOR_BUILD_MISMATCH`, `REPLAY_EXECUTION_FAILED`, `ARTIFACT_MISMATCH`.

## DETERMINISM BOUNDARY

- Same profile/seed/content/build across 100 fresh/reused recordings produces byte-identical sector output and SHA-256.
- Different valid clocks change only timing-bearing manifest diagnostics; sector bytes remain exact.
- Same bundle across 100 fresh/reused players verifies successfully.
- Static sample seed `4660` generated sector bytes: `5865` bytes.
- Static sample seed `4660` sector SHA-256: `94ea893d55e80e4ec0a5a4758b7d84bd8e999942064d3205600e0f5a8a1bd13b`.
- No whole-bundle or timing-bearing manifest hash is treated as the static generation identity.

## TEST

- New seed manifest/replay focused: `97/97` PASS
- MAP02_01 GeneratedWorldData: `56/56` PASS
- MAP02_02 DeterministicRngStream: `103/103` PASS
- MAP02_03 GridInitializationPass: `90/90` PASS
- MAP02_04 WorldGenerationRoot: `84/84` PASS
- MAP02_05 execution records: `77/77` PASS
- ContentVersionHash focused confirmation: `54/54` PASS
- Targeted `Game.Map.Tests.EditMode`: `1374/1374` PASS
- Full project EditMode: `1394/1394` PASS
- MAP00 coordinate/architecture and MAP01 registry/content/import regressions are included in the passing targeted/full runs.
- PlayMode: NOT RUN per Task scope
- Visual: NOT APPLICABLE

## UNITY

- Active instance: `Constant@ced6e0dfc4a31d45`
- Unity: `6000.3.8f1`
- Final forced all-asset refresh and requested script compilation: PASS
- Final follow-up refresh state: `idle`; editor ready hint returned.
- Final Console errors: `0`
- Final Console warnings: `0`
- Relevant code warnings: `0`
- Scene/Prefab changes: none

## ASSET META VALIDATION

- Authoring CSV/meta: `50/50`
- Accepted legacy Editor folder meta: `6/6`
- New matching `.cs.meta`: `8/8`
- Final Assets meta: `2981`
- Invalid/missing meta GUID rows: `0`
- Duplicate GUID groups: `0`
- New directory/folder meta: `0`

## CHANGE SCOPE

- Applied Phase A inbox patch: `MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER`, version `1.0`
- Applied marker: `MapDesign/MCP_INBOX/MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER/.APPLIED`
- Assets files newer than the patch marker: `16`
- New C#: `8`
- New matching `.cs.meta`: `8`
- Existing Assets modifications: `0`
- Unexpected Assets changes: `0`
- New directory/folder meta: `0`
- asmdef/asmref, Authoring, scene/prefab, package, and project-setting changes: `0`
- Git commit/push: not performed

## OUT_OF_SCOPE_FINDINGS

- None.

## DONE CONDITIONS

- Immutable exact seed manifest and strict canonical CSV: PASS
- Exact in-memory bundle and safe D16 identity path: PASS
- Non-reexecuting recorder: PASS
- Atomic verified publisher/load boundary: PASS
- Ordered one-call replay verification and stable result codes: PASS
- 100-run static determinism and timing isolation: PASS
- Focused/regression/targeted/full EditMode verification: PASS
- Compile/console/meta/GUID/change-scope gates: PASS

## NEXT

- `MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER` remains the Current Task pending the separate finalize workflow.
- No next Task was started or unlocked.

## Recommended Commit

`feat(map): add grid seed replay bundles`
