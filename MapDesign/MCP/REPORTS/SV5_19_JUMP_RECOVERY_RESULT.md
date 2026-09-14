# SV5_19_JUMP_RECOVERY Result

TASK: SV5_19_JUMP_RECOVERY
TASK_ID: SV5_19_JUMP_RECOVERY
STATUS: PASS

## Outcome

Implemented a deterministic representative-miss recovery proof for all 18
main links in the two immutable 24×32 SV5_18 local recipes. This is an
itemless one-way recovery layer, not reverse completion and not a live Player
physics claim.

Each probe selects the lower-middle ordered `AIR` sample using `(n-1)//2`,
casts straight down, and lands above the first occupied `SOLID` or `TOP_ONLY`
cell. All 18 probes are caught with clear body/head cells and no nearer surface
is skipped.

## Overlay and routes

- Recovery overlay: 16 cells / 6 groups total, exactly 8 cells / 3 horizontal
  groups per recipe.
- R0 groups: `(3..5,1)`, `(9..11,2)`, and `(16..17,4)`; MIRROR_X applies
  `x'=23-x` exactly.
- Catch source distribution: 6 recovery-overlay catches and 12 accepted base
  catches.
- Routes: 18/18 PASS; 10 `LINKED` and 8 `ALREADY_AT_CHECKPOINT`.
- Recovery movement: 10 links and 34 trace states, using only `WALK` and
  `DROP`; `JUMP` remains permitted only up to one-cell rise.
- Checkpoint orders used: `0,1,2,3,6`. Every rejoin is the same or earlier
  than the failed link.
- Orders `4,5,7,8` in each recipe land exactly at the chosen checkpoint and
  emit zero movement links.
- Reverse recovery, reverse main-route completion, and item use are false.

The overlay is uniquely owned, exactly mirrored, disjoint from SV5_17
occupancy, all SV5_18 body/head trace cells, and both Grab contacts. All 18
main clearance proofs remain passing, strict recovery diagonal supercover is
preserved, and no SOLID 6×6 fill exists.

Recovery digest:
`f8b2e69686dbd9b7a29b82e4e4a8ca2c46c6183a005a5373e986e04b21f4288b`.

## Negative proof

The 50 local tests reproduce missing catch, skipped nearer surface, blocked
landing body/head, overlay/main-trace overlap, bad mirror, disconnected route,
link discontinuity, unsupported endpoint, jump rise above one, rising DROP,
later-checkpoint shortcut, single-checkpoint collapse, reverse-required claim,
6×6 fill, invalid readiness, invalid ALREADY_AT_CHECKPOINT movement, diagonal
clipping, Player verification, missing probe, bad probe origin, and clearance
identity failures. They also cover all 18 success probes/routes, determinism,
export round-trip, exact mirror, main-clearance preservation, and readiness.

## Verification

- Native Apply: `PASS_NATIVE_APPLY`.
- Unity 6000.3.8f1 compile errors: 0.
- Targeted physical runs: 1.
- Targeted suite: 50/50 PASS; failed/skipped/inconclusive = 0/0/0;
  NUnit duration `1.6332541` seconds; Unity exit code 0; XML SHA-256
  `a0dceff8cae0320f57b4d2ca749f49167d97fd4cb78018f6f8c9eba6b38e85c1`.
- Independent checker: `PASS_INDEPENDENT_JUMP_RECOVERY`; 16 overlay cells,
  6 groups, 18 probes, 18 routes, and 10 recovery links; audit SHA-256
  `fd0aa7797c31b9924bec8a1f86f049e2ac4c153bc76e45e162d44a3418224a64`.
- SVG render and visual inspection: `PASS_VISUAL_INSPECTION`; SVG SHA-256
  `11d933aba1a528b25a483bca40cb47bc769a49728a834e6b7a625b9ecdd15857`;
  rendered PNG SHA-256
  `8c0ce455fafe956e4d0ff18e2063cf2209f912eb20ca5d8f92d4e055dc988df8`.
- Full SV5 regression physical/accepted runs: 1/1.
- Full regression: 443/443 PASS; failed/skipped/inconclusive = 0/0/0;
  NUnit duration `1643.0996026` seconds; wrapper elapsed `1696.969` seconds;
  Unity exit code 0.
- Full XML SHA-256:
  `71565055590728b4c41fae1e5e27cf66d90bec81cc6d20c8a39a3ecddf37c4d2`.
- Full log SHA-256:
  `6e29ebb5df8e3648a420fe020a0f5b584659ce48101beefbe4650ebfcc62c381`.
- Previous 393 fullname values were preserved and the only additions were the
  50 SV5_19 tests. The 443-name set SHA-256 is
  `6d96ae34e25d6766f3ab1088e29d3ae0553b3c884ee8f00f2840ec87cf87b489`.
- Lifecycle pre-finalize check: `PASS_POST_READONLY`.

## Boundaries

- `JumpRecipeReady=true`
- `ComposedGeometryReady=true`
- `SweptClearanceReady=true`
- `RecoveryReady=true`
- `PlayerVerified=false`
- Whole-world builds/searches and global endpoint comparisons: 0
- SV5_17/18 source and evidence, Player, scenes, prefabs, Packages,
  ProjectSettings, Master, and world placement were not changed.
- SV5_20 remains `LOCKED`.
- Push performed: NO

The atomic commit title is `SV5_19_JUMP_RECOVERY: add local missed-jump recovery`.
The exact-blob Review ZIP is created only after that commit and its SHA-256 is
reported in the final handoff.
