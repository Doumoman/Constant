# RMAP v4.2 Issuing and Evidence Protocol

## Normal follow-up issue contract

Starting with RMAP02, each inbox file must use the exact existing
`single_task_v1` metadata schema. Its predecessor fields must contain the
verified installed predecessor Task SHA-256 and PASS Result SHA-256; values are
read from committed files, never copied from a plan or guessed.

Required normal checks remain unchanged:

- filename, `task_id`, `task_file`, and `sets_current_task` agree;
- Current Task is `NONE`, predecessor row is `COMPLETE`, new row is exactly one
  `LOCKED`, and the new ID is exactly one Master entry;
- predecessor Result has an independent `STATUS: PASS` and both SHA-256 values
  are lowercase 64-hex matches;
- installed Task and archive are byte-identical to the inbox body;
- patch apply changes only `NONE -> CURRENT` and one `LOCKED -> CURRENT` row;
- Finalize changes only that `CURRENT -> COMPLETE` row and `Current Task -> NONE`;
- the next row remains `LOCKED`, and no push occurs.

## RMAP01-only registration record

RMAP01 is the one-time exception because none of the 19 new IDs existed in
Master/Status before this contract. Its source SHA-256, baseline HEAD, first
registration status delta, byte-identical Task/archive evidence, and future
lock rule are recorded in `GENERATED/RMAP01/baseline_evidence.json`. This does
not create a general unregistered-ID bypass.

## Result requirements

Every RMAP Result leads with `TASK` and a single `STATUS`. It must report the
user-facing outcome first, then file responsibilities, reuse/adapt decisions,
evidence actually inspected, tests not run and why, out-of-scope observations,
and the exact next lock. `PASS` means only that task's documented scope passed;
it does not claim future gameplay/world completion.

## RMAP02 binding contract

RMAP02 must consume existing Player/input/movement code through public APIs,
turn a verified logical bake into a real Tilemap and Collider path, and use an
isolated RMAP02 scene. It must not silently treat preview marker movement or
editor-only `SetTile` visualization as physical Player integration. Exact
existing and proposed paths are maintained in
`GENERATED/RMAP01/file_bindings.csv`.

## Evidence and ownership

- CSV uses UTF-8, a header, project-relative paths, and stable ordering.
- `EXISTING`, `PROPOSED`, and `UNRESOLVED` are distinct; a proposed path is not
  an implementation commitment.
- Existing source retains ownership of its current contract. A later RMAP task
  may adapt it only through its own write allowlist and evidence.
- No task weakens SHA, locking, archive collision, Finalize, or atomic commit
  rules by editing this document.
