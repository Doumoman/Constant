# SV5_03_BINDINGS Result

TASK: SV5_03_BINDINGS
STATUS: PASS

## Outcome

SV5_03 bound and audited existing code and MapDesign data boundaries for the
42 still-locked SV5 Tasks. This is documentation and traceability only, not a
feature-completion claim; no existing code or data was changed.

## Package, predecessor, and normal Apply

| Role | Path or identity | SHA-256 / result |
| --- | --- | --- |
| handoff INBOX | `MCP_INBOX/SV5_03_START.md` | `5c6ef9b3b8192166c3a4117ca6534ce52a3aaa5eed42642abd3918f1d62f8b73` |
| source specification | `MCP/INPUTS/SV5_03/SV5_03_BINDINGS.md` | `5bdc2b45cc0c4dc51a4ea347ad28be12c57aa2d9c53d17df1c41f2a8fef1bce9` |
| package manifest | `MCP/INPUTS/SV5_03/FILES.json` | `c789119d6ff5b9de78f69d0811d7e64d97e8a82a3950b14cf29cc4e1af78a670` |
| package verification | `VERIFY.py --manifest-sha` | `PASS_PACKAGE_ONLY` |
| local predecessor verification | actual `MCP/REPORTS/SV5_02_RULES_RESULT.md` | `PASS_LOCAL_BYTES_ONLY` |
| SV5_02 Result | `MCP/REPORTS/SV5_02_RULES_RESULT.md` | `ed4e9ee6fd6f271bd81915040e1d6e103832a3737bd90d5eb90cc9371956a496` |
| SV5_02 installed/Archive Task | `MCP/TASKS` and `MCP_ARCHIVE` | `b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1` |
| SV5_02 Finalize / task-owned commit | `ffd81805cf3840b56c74944a284d0a4d3f512297` | parent `9e6a71489fb4881c4112c378f7ca043bbbd85e85` |
| bound, installed, and Archive Task | `SV5_03_BINDINGS.md` | `b8f6f5865dae26d512a50a3f62061c1370daec8bc12e9c56578ebda3565677b3` |

The packaged verifier was run first. It was then run with `--local-precheck`
and `--prior-result` pointing to the actual SV5_02 Result at the MapDesign
path above; no input copy or generated-path substitute was used. The normal
Apply installed the bound Task, marked only SV5_03 CURRENT, and archived the
same Task bytes.

## Actual state

| Point | Total | COMPLETE | CURRENT | LOCKED | Current Task | Status SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| before Apply | 285 | 240 | 0 | 45 | `NONE` | `ea5f1b205c712a6c093cfeefaa8e3160a79498ac714b7d17382992450e12b68f` |
| after Apply / this Result | 285 | 240 | 1 | 44 | `SV5_03_BINDINGS` | `42338ae570be9b4a28a49f836a9146014a42ba813880c0701a9d090996be8b00` |

`MCP/00_MASTER_INDEX.md` remained byte-identical at
`cbe616e54c85a05181759dc87997f68e94f5ee92ff1a2a9896396a9e3c937d4c`.
SV5_02 is COMPLETE and SV5_04 through SV5_45 remain LOCKED.

## Audit result

- 13 audit groups produced 19 source/data bindings: 13 `REUSE`, 4 `ADAPT`,
  and 2 `NEW` absence observations. The `NEW` rows only assign future
  ownership: word-boundary source searches found no dedicated Stair/Elevator/
  StairMotor or Rail/Train/RailRide/RailCrash implementation in `Assets/_Game`.
- `CORE_BINDINGS.csv` records the actual eight RMAP15 physical sites: Start,
  MooncoreOre, CondensedCoefficientSap, DeepStarYeast, Village, Forge, shared
  SealBoss, and Exit. Their protected-cell evidence totals 2,432 cells.
  SealBoss remains one physical site with separate sealed/open state geometry;
  its logical Seal and Boss bindings were not collapsed into a second site.
- The 12 data-schema rows and all recorded source SHA values were checked
  against their live local bytes. `TASK_COVERAGE.csv` maps all 42 future,
  still-locked SV5 Tasks without executing any of them.

## Registered outputs

| Output | SHA-256 | Validation |
| --- | --- | --- |
| `MCP/SV5/06_BINDINGS_V5.md` | `b71c553032de446ce7c7d7c65e806879bdd360ab77389c684f201a0f5d6ccf21` | audited entrypoint |
| `MCP/SV5/07_FILE_FLOW_V5.md` | `611f604f8d65a7ac15a70f20ab848a4ed40ecf5d7fafa4428f3a77167c7f43a4` | exact input-byte copy |
| `MCP/SV5/02_PROTOCOL_V5.md` | `0331d3364cbb7d9b9c2d615f861924f50474539f69800ebd01439eef3526cc02` | one exact append block |
| `MCP/GENERATED/SV5_03/BINDINGS.csv` | `387334b77c82b2aa62e9e9b6c11330d3b83f4106abc4bf8916f5fe6afecdefee` | 19 rows; source SHA checked |
| `MCP/GENERATED/SV5_03/CORE_BINDINGS.csv` | `3063f808d294e09e5b03f33a2b2357a1f4f2ba510c38c56dac4ed0e10ccfac35` | eight physical sites |
| `MCP/GENERATED/SV5_03/DATA_SCHEMAS.csv` | `083f91e9d4cfc5118d9cdeba4a0f1cc7184d0b6d3cacd653609076b763d5542c` | 12 source-backed rows |
| `MCP/GENERATED/SV5_03/TASK_COVERAGE.csv` | `86309a520cf32fc268f0c9cabd25c68568fdc3b868ccf6b95c19611cba4644c6` | 42 unique future Tasks |
| `MCP/GENERATED/SV5_03/SOURCE_SNAPSHOT.json` | `1021386d9e1dd20f267a6f917f857e55ad431d612a1f3709da9f4df394fd49ac` | observed byte snapshot |
| `MCP/GENERATED/SV5_03/BINDING.json` | `6eb339dea116ce1b82e20407793ae77568e01cf09014ab5a771e08edbd2c685b` | binding identity |
| `MCP/GENERATED/SV5_03/validation.json` | `87bb4af251e023d03a4f4c02079fb74ebc09686718f7388ce829b50f34248b96` | execution-time record |

The protocol pre-Apply SHA was
`c77c776975f8416678f3f29cc6cc0af96b65dd95ede5894d4b9402cf565842b5`.
Its source-lock role was PRE_APPLY only; the allowed append is represented by
the post-Apply SHA above. No task temporary directory remains, and helper
files were created only under `MapDesign/MCP`.

## Static verification and exclusions

The execution-time static contract check passed 66/66: output presence and
CSV structure, binding and schema source bytes, eight-site evidence, 42-task
coverage, exact file-flow copy, one exact protocol append, input identities,
and the CURRENT state counts. Pre-existing unrelated working-tree changes
were retained and were not staged.

Unity launch, compile, tests, build, bake, game-code/data modification,
RMAP18/19, SV5_04 and later Tasks, and push were not run.

## Finalize timing

This immutable Result was written while SV5_03 was CURRENT. Finalize and this
Task's local commit have not occurred at Result-writing time. Finalize only
SV5_03 next, commit only its owned paths, and stop.
