# SV5_02_RULES Result

TASK: SV5_02_RULES
STATUS: PASS

## Outcome

R2 registered the active SV5 rule index, sentence coverage, and Task ReadSet.
It reopened neither the SV5 plan-registration boundary nor any later SV5 or
RMAP Task. This is documentation and traceability registration only; it does
not claim a Unity, Player, physics, progression, terrain, or bake pass.

## Input, binding, and predecessor identity

| Role | Path | SHA-256 |
| --- | --- | --- |
| R2 handoff INBOX | `MCP_INBOX/SV5_02_R2_START.md` | `558ca42645c58d3c2ed9bf4e6975caf4e4b1af4bc45f00f9cae65b2c0d68ae94` |
| R2 source specification | `MCP/INPUTS/SV5_02_R2/SV5_02_RULES.md` | `4b371aaddbfd4f1946fbf32b1da2096e522a7dacef45bd4958f0cb477653a321` |
| R2 manifest | `SV5_02_R2_FILES.json` | `a664020b4a0d548bfec05fca6ed20d6751fcea151c4fc98f225566e950b15bdf` |
| bound execution Task | `MCP/TASKS/SV5_02_RULES.md` | `b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1` |
| installed execution Task | `MCP/TASKS/SV5_02_RULES.md` | `b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1` |
| Archive Task | `MCP_ARCHIVE/SV5_02_RULES.md` | `b0bc1a19dc1ff100cdf8116ba5e1447af6080556a98011b05bbc84deb1619fc1` |
| predecessor Result | `MCP/REPORTS/SV5_01_APPROVAL_BASELINE_RESULT.md` | `78f9e84bf82b9640c51a5ee4bc2847610a296ebd26e0849706d3ce58601a37b6` |
| predecessor installed/Archive Task | `MCP/TASKS/SV5_01_APPROVAL_BASELINE.md` / `MCP_ARCHIVE/SV5_01_APPROVAL_BASELINE.md` | `c63acc8cf464ae837d5e83efef9976f5e12f42f1cf8a32f2634ba32cf587d5ed` |

The user-supplied R2 ZIP SHA is
`67cafe772bb8d0ca461d7142196b0d1639f6ea07431ea7d22ca0a1e3b23f60f6`.
The ZIP file itself was not present in this workspace, so that outer archive
SHA was not recomputed. The supplied R2 manifest was checked instead.

## Actual predecessor and R2 path correction

- The local SV5_01 PASS Result and installed/archive Task match the required
  predecessor SHA values; input reference copies were not substituted.
- SV5_01 Finalize commit is locally present as
  `9e6a71489fb4881c4112c378f7ca043bbbd85e85`, with parent
  `da1803447ac3275da4c674fc67a00844ff252ad5`.
- `python -X utf8 SV5_02_R2_VERIFY.py --manifest-sha
  a664020b4a0d548bfec05fca6ed20d6751fcea151c4fc98f225566e950b15bdf`
  returned `PASS_PACKAGE_ONLY` (74 source statements, 14 jump rules).
- The same command with `--local-precheck --map-root "MapDesign"` and the
  actual local SV5_01 Result returned `PASS_LOCAL_BYTES_ONLY`.
- R2 used the actual predecessor output root `MCP/GENERATED/SV5_01` and wrote
  this Task's evidence under `MCP/GENERATED/SV5_02`. No
  `MapDesign/GENERATED` alias, copy, or moved predecessor file was created.

## Apply and execution state

| Point | Total | COMPLETE | CURRENT | LOCKED | Current Task | Status SHA-256 |
| --- | ---: | ---: | ---: | ---: | --- | --- |
| before Apply | 285 | 239 | 0 | 46 | `NONE` | `143350621914313f3ba8ccf12dccada6b98078b25c7c4a7044892c9262c2071a` |
| after Apply / this Result | 285 | 239 | 1 | 45 | `SV5_02_RULES` | `372984f1c1034bacef90a948d408c1b8ec6bf46924fb625ef180cdb7892f4f69` |

Master remained `cbe616e54c85a05181759dc87997f68e94f5ee92ff1a2a9896396a9e3c937d4c`.
SV5_01 remained COMPLETE and SV5_03 remained LOCKED. Apply installed and
archived the bound Task byte-identically and removed only its bound inbox MD.

## Registered outputs and static validation

| Active output | SHA-256 | Result |
| --- | --- | --- |
| `MCP/SV5/03_RULES_V5.md` | `ecad8f6d292dcc5238d99f9b259f40fe57b554f253f1b920fb39476494641efe` | byte-identical R2 candidate |
| `MCP/SV5/04_RULE_COVERAGE_V5.csv` | `0aaed8e54f63946ff1680c2b7474f84e5c836aaacc8fc2aa13049604f1c4ce07` | byte-identical R2 candidate |
| `MCP/SV5/05_RULE_READSET_V5.json` | `271a659f44a10737478377a1b7d7f7d8f90830153594c0cbe8ac33828d22b8a9` | byte-identical R2 candidate |
| `MCP/SV5/02_PROTOCOL_V5.md` | `c77c776975f8416678f3f29cc6cc0af96b65dd95ede5894d4b9402cf565842b5` | one exact appended marker block |

- The protocol's pre-Apply SHA was
  `d0dca368acc3b961a09b1a54f1efa1a1ffce37d13734b0201b0e66098897201e`.
  Its source-lock role is `PRE_APPLY_ONLY`; it was not reimposed after the
  allowed append.
- Coverage has exactly 74 meaningful source statements and JUMP-01 through
  JUMP-14 exactly once. JUMP-07/JUMP-10 remain `AUTHORING_DEFAULT`, JUMP-05
  remains `UNSET_NUMERIC_RATIO`, and horizontal 3–4-cell movement remains an
  unverified candidate.
- ReadSet sources and SHA values match the R2 SOURCE_LOCK. The rule source SHA
  is `4cc8e65a23f511b05a91e74af75a5c2ade522f266bd9ce3a9bb5da8108f0ee54`.
- Generated registration evidence is in `MCP/GENERATED/SV5_02/`.

## Not run

Unity launch, compile, tests, build, bake, Player/progression validation, game
code work, RMAP18/19, SV5_03+, and push were not run.

## Finalize and commit timing

This immutable Result was written while SV5_02 was CURRENT. Finalize and the
task-owned Git commit have not occurred at this Result-writing time and are
therefore intentionally not represented here as completed.

## Next

Finalize only SV5_02 after this PASS Result, commit only this Task's owned
files, then stop with SV5_03 still LOCKED.
