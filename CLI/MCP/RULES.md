# CLI Rules

## Core

1. Run one task at a time.
2. Do not auto-open the next task.
3. Each RESULT must contain one independent `STATUS: PASS`, `STATUS: FAIL`, or `STATUS: BLOCKED` line.
4. Do not hide failed tests, console errors, build errors, or scope violations.

## Locked Character Rules

Do not add:

```text
basic attack
melee
shoot
dash
wall jump
double jump
new ActionId values
```

Do not rewrite completed pure Character policies.

## Allowed Direction

```text
Unity scene/prefab/input layer -> Character runtime contracts -> MAP public contracts
```

MAP runtime must not depend on Character runtime.

## Change Control

Write only inside the current task allowlist.

Do not commit or push unless the user explicitly asks for this live sequence.

