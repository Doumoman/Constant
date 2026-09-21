# SV5_01_APPROVAL_BASELINE Result

TASK: SV5_01_APPROVAL_BASELINE
STATUS: PASS

## Outcome

The approved 624×416 spatial-composition baseline and all 45 SV5 plan IDs are
now registered. This first task opened and closed only SV5_01 through the
normal bound `single_task_v1` flow. It creates no gameplay feature: Unity,
Player traversal, physics, runtime generation, and progression are `NOT_RUN`.

## Source, binding, and byte identity

| Role | Path | SHA-256 |
| --- | --- | --- |
| root README | `SV5_README.md` | `d44aba4bc9ea03a2638df1682e4df5284ae4a88830050d55bd651f15a9b3c4d2` |
| input manifest | `SV5_FILES.json` | `74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456` |
| INBOX handoff | `MCP_INBOX/SV5_START.md` | `9f440662458d69a74127fa81c4af89e35649dd06426ea19071285f1fd0ddbbef` |
| source specification (`source_spec_v1`) | `MCP/INPUTS/SV5/tasks/SV5_01_APPROVAL_BASELINE.md` | `4a4f6a1c9dcb74ae63e4ee30269551f133aabdf256caff5ee4374276d385dcdf` |
| bound execution Task | `MCP/TASKS/SV5_01_APPROVAL_BASELINE.md` | `c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed` |
| installed execution Task | `MCP/TASKS/SV5_01_APPROVAL_BASELINE.md` | `c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed` |
| Archive Task | `MCP_ARCHIVE/SV5_01_APPROVAL_BASELINE.md` | `c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed` |

The source specification was not executed by filename substitution. It was
bound as a new 101-line Task whose metadata independently verified RMAP17.
The bound/installed/archive bytes are equal. `SV5_START.md` is a registered
`design_plan_handoff_v1` envelope, not the applied Task body.

## Actual local preconditions

- Baseline HEAD: `da1803447ac3275da4c674fc67a00844ff252ad5` (`RMAP17 bake static world Tilemap`).
- Before registration: Current `NONE`; Status `240 = 238 COMPLETE / 0 CURRENT /
  2 LOCKED`; Status SHA `b9a7398b4affb9c49ac2ca3705e40b80b7820effc2b7001a22d40069746588e8`;
  Master SHA `1f15a7f1ab539e98dfb4d3b8e961738ef5fabff2ad742418da0b14240cc38458`.
- RMAP17 was locally current evidence, not merely the package copy: installed
  Task SHA `5017331f8083ee8cc57cb8fed771c9d636df3248979ce39fa7efc43b2f018e46`,
  Result SHA `53109e3b24e3df8191e634d7ec2e3f06087108965e5f9bc2af937f030a563c6b`,
  independent `STATUS: PASS`, and Status `COMPLETE`.
- Pre-existing unrelated dirty files were preserved: VIS01 scene,
  `Constant.slnx`, three TerrainClusters `.meta` files, the MAP17 repair note,
  `MCP/INPUTS/R11FIX/`, two unrelated reports, and the prior RMAP handoff
  files in `MCP_INBOX/`. The supplied SV5 source package was separately
  recognized as task input; it was not rewritten.

## Registration, Apply, and Finalize state

1. The explicit SV5 first-registration boundary added exactly 45 unique SV5
   rows and matching Master entries as LOCKED: `285 = 238 COMPLETE / 0 CURRENT
   / 47 LOCKED`.
2. Normal Apply installed then archived the bound task byte-identically and
   changed only `Current NONE → SV5_01_APPROVAL_BASELINE` and
   `SV5_01 LOCKED → CURRENT`: `285 = 238 COMPLETE / 1 CURRENT / 46 LOCKED`.
3. This PASS Result permits Finalize to close only SV5_01. Final state is
   `285 = 239 COMPLETE / 0 CURRENT / 46 LOCKED`; SV5_02~SV5_45, RMAP18, and
   RMAP19 remain LOCKED.

## Static validation

- `python -X utf8 SV5_VERIFY.py --manifest-sha
  74d4435f8128d44943b54774416329e027a7c8f584e42225c0504e600e2f5456`
  returned `PASS_LOCAL_INPUTS`: 32 files, 45 tasks, 259,584 cells, 101 regions,
  declared S/O/A/R counts, and no errors. The UTF-8 switch is required on this
  Windows host because the platform default CP949 cannot decode the package.
- Independent CSV scan found header `x,y,cell,region`, 259,584 rows, 259,584
  unique in-bounds coordinates, zero invalid rows, 102 region IDs including
  outside `0`, and `S/O/A/R = 121051/20527/117430/576`.
- CSV/JSON/Markdown all contain 45 ordered, unique, identical SV5 IDs.
- The package-verified PDF, overview/detail PNG, original ZIP, and baseline
  CSV/JSON artifacts match `BASELINE_SHA.json`; their path, size, and digest
  are recorded in `GENERATED/SV5_01/BASELINE.json`.
- No Unity launch, compile, test, build, bake, Scene/Prefab/Assets mutation, or
  game-code change was performed.

## Task-owned output hashes

| Artifact | SHA-256 |
| --- | --- |
| `MCP/SV5/00_APPROVAL_BASELINE.md` | `a04f0c814273048db51c903d965e526c2bc987abb64294811675d301acf177fa` |
| `MCP/SV5/01_SEQUENCE_V5.md` | `27a0ab0942ee2671a5720a1fe232018b5356c2c304ad6e8229c7c8842b05d9be` |
| `MCP/SV5/02_PROTOCOL_V5.md` | `d0dca368acc3b961a09b1a54f1efa1a1ffce37d13734b0201b0e66098897201e` |
| `GENERATED/SV5_01/APPROVAL.md` | `a020a9eb92c8a2cd5787c61e944ef877b6dba67f558d70592f6356ed4924cef7` |
| `GENERATED/SV5_01/BASELINE.json` | `1f36a2dc80f5cd01427592641e854651280bc664e02c6c4bd363ce210e81bbac` |
| `GENERATED/SV5_01/PLAN_LINK.json` | `9c338ef451126acd10ad77f8a30747f216e71552fb697958fb5c5e5a03d8332b` |
| `GENERATED/SV5_01/BINDING.json` | `fe5ce5f6ac16857608b86c64ebb11930124f5d3f2ed2b8b4559096f1a269129c` |
| `GENERATED/SV5_01/static_validation.json` | `a39d5b9f7b8ccc5f3890e2e4c4da2281bd877f01e0a88a35ff938db51f004104` |
| Master after plan registration | `cbe616e54c85a05181759dc87997f68e94f5ee92ff1a2a9896396a9e3c937d4c` |
| Status while SV5_01 was CURRENT | `77a8d367cc5f7ba3dcef242590deae4dde5449a75249867f84e0d44c5d2f41d0` |

## Commit and boundary

The atomic commit is made only after Finalize and contains the byte-identical
Task/Archive, registered plan records, generated evidence, Result, and final
Status. The verified source package is pre-existing user input and remains
unchanged at its documented local paths rather than being rewritten by this
Task. The commit SHA is intentionally obtained after this immutable Result is
written; no amend is used to put it back into this file.

NEXT: Current `NONE`; SV5_02_RULES `LOCKED / NOT STARTED`; RMAP18/19
`LOCKED / NOT STARTED`; no push.
