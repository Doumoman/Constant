# SV5_20_JUMP_PLAYER Result

TASK: SV5_20_JUMP_PLAYER
TASK_ID: SV5_20_JUMP_PLAYER
STATUS: PASS

## Outcome

Verified both accepted 24x32 local jump recipes with the actual live Player
prefab, Rigidbody2D, 0.72 x 0.9 CapsuleCollider2D, FIX04-approved movement
driver, physical Grab, one-way collision, and fixed-step input. All 18 main
links, two continuous routes, and 18 recovery cases reach their bound terminal.

The result is itemless and one-way. Reverse completion is not required, jump
plus Grab gains at most two cells, and post-START position/velocity correction
count is zero. `PlayerVerified=true` while world placement remains outside this
Task.

## Geometry and Player observations

- JS_LINK_02 uses the authored AIR passage.
- JS_LINK_03 enters the SOLID side Grab while descending, exits with fresh
  Space, and lands naturally on the same upper support.
- JS_LINK_04 launches from non-grabbable TOP_ONLY support.
- R0 and MIRROR_X occupancy, endpoints, directions, and terminals match.
- Actual fixed timestep is 0.02 seconds; jump velocity is 7.2; run/walk speed
  is 5.5/2.8; Grab probe/window is 0.35/0.45.
- Player settings, prefab, capsule, Physics2D settings, and movement constants
  were not retuned.

## Verification

- Unity 6000.3.8f1 compile: PASS.
- Canonical targeted EditMode: 32/32 PASS; XML SHA-256
  `e60954ca85bf997ec643bfa37ce96c02b0873668aa8e1c89aa1f440ff668c0be`.
- Canonical targeted PlayMode: 38/38 PASS; Grab 4/4, one-way landings 36,
  deterministic terminal delta 0; XML SHA-256
  `89e690b032377e35253f88a5f34efc766af3c96bdb8b84b8536dc0db6dafaba0`.
- Focused FULL_R0/FULL_MX: 2/2 PASS; XML SHA-256
  `33e635e7570f7f1deb8016c82fdb30358b5b3b46557404d473d167e1461bb44a`.
- Independent checker: `PASS_INDEPENDENT_JUMP_PLAYER`; audit SHA-256
  `afead90ba740043191edf4edf988199dbe48cde29d9764f2cf56365f8ac80643`.
- Visual/export audit: `PASS_VISUAL_EXPORT_AUDIT`; audit SHA-256
  `e9d39ef3cc60e954cb933e5690d664831090500dfaf9b9e16e6269a5fa97f484`.
- Direct Player regression: exactly 21/21 PASS (RMAP02 5, RMAP03 10,
  RMAP04 6), zero skipped; duration 17.178512 seconds; XML SHA-256
  `945fd76500922790b405702655e31e151203dacd2e85cc29631634aec71b79e7`.
- Approved independent RMAP03 10/10 evidence SHA-256
  `17989038f1387d93e754367cc21b71c8d38a1b5023d9a6fc4405c7ca84659ff4`.
- Full historical SV5 EditMode regression: 443/443 PASS, zero skipped;
  duration 1509.85393 seconds; XML SHA-256
  `10c480636d84a5c04c57f81d1475bf31c6021fd1943e02940b323132cbea0412`.
- Lifecycle pre-finalize check: `PASS_POST_READONLY` using FILES.json SHA-256
  `6d43bf1b93864d37627102b6786b97fca113807e57c74b32bfb5e2eea0efdc11`.

## Execution audit

One additional targeted EditMode invocation was accidentally made while
distinguishing the case-sensitive `...EditMode.SV5` targeted namespace from
the historical `...EditMode.Sv5` regression namespace. It passed 32/32 with
duration 1.7120725 seconds and transient XML SHA-256
`170a2b112ea906ac2209013e6b7ce9cde7202fb494fc586e26089ed67d58b36a`.
It is audit-only, was not rerun, and is not used in place of the canonical
targeted evidence.

## Scope

- Whole-world builds/searches/global endpoint comparisons: 0/0/0.
- SV5_17 through SV5_19 canonical source and evidence were not changed.
- Scenes, Packages, ProjectSettings, Master, and world placement were not
  changed.
- FIX05 changes are limited to RMAP02/RMAP03 test-owned scene and fixture
  lifecycle isolation; the four temporary RMAP03 diagnostics were removed.
- Push performed: NO.
- SV5_21 started: NO.
