# SV5_12_TREE_GRAB package

This package stages the already registered `SV5_12_TREE_GRAB` Task after the
completed `SV5_11_FIX01` commit.

It deliberately includes a non-negotiable Hub handoff gate. The reviewed FIX01
created real cells, but the default profile counted four logical Rooms at one
physical external anchor and its movement witness proved AIR continuity rather
than the player's jump/grab rules. SV5_12 corrects those integration defects as
part of building the central tree; it does not register another FIX Task.

Run `STAGE.py --check` first and `STAGE.py --stage` only after PASS. Then use the
repository's native `APPLY_PATCH_AND_RUN_CURRENT_TASK.md` flow. Package checks do
not implement, test, finalize, commit, push, or start a later Task.

Read `CONTRACT.md`, `TREE_GRAB_PROFILE.json`, and `SOURCE_LOCK.json` in full.

FIX01 correction: all predecessor review evidence is locked to the exact Git
blob at commit `aeedc340670843d0bbeb43d3aad853a7953ba3b9`. In particular,
`focused_results.xml` uses blob `4357d8978a1b840b4e9fd4da38f773e88fe42901`,
SHA-256 `03daa54bb0cd9f553259e0c733e0bc8b33acb459f5b5c452ee20cec952dd05bc`,
and 158507 bytes. The wrapper installer may replace the previous untracked
package only after proving it is the exact known faulty package; it never
touches Task state, Inbox, source, generated evidence, or tracked files.
