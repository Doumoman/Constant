# MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP Result

TASK: MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP
STATUS: PASS

## SUMMARY

- Built immutable P03 `MANDATORY_ROUTE_MASK_LOOKUP` from MAP01 typed `SectorRouteMaskDefinition` objects.
- Published exactly three mandatory masks in route-type order `Type1 -> Type2 -> Type3`.
- Structural preflight accumulates, ordinal-sorts, and deduplicates invalid input errors before output allocation.
- RNG draws / source mutations / filesystem or installed CSV reads: `0 / 0 / 0`.
- No connector tree, router, gateway, conflict, loop, graph, generated CSV, validator, overlay, root, Scene, or Prefab work was started.

## PATCH APPLY

- Applied `MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP` v1.0.
- Manifest SHA-256: `971f41e3ae64704b58d7a518b192ba2e3286d873cd1aee95f0fdf4be3b5ba690`.
- Master/Status/Task payloads matched destinations byte-for-byte after apply.
- Patch apply itself changed no Assets, CSV, C#, tests, asmdef, Scene, or Prefab.

## READ

- Read the mandatory global rules, Master, current Status, this Task, and exact MAP05_01 PASS Result in required order.
- MAP05_01 Result SHA-256 matched `a5ea4a2a3e7ac29de825e45e4b75a816ae2d8f5a6d4824fabf6a0676d62b2069`.
- Read only Task-allowlisted MAP01 route APIs, P03 terminal contracts, focused test/assembly data, and permitted inventories/hashes.
- Full installed Map Package v1.0 reference set was absent; installed Authoring CSV bodies were not read.

## CREATED

- Runtime production C#: `8`.
- Runtime EditMode test C#: `1`.
- Matching `.cs.meta`: `9`.
- Result: `REPORTS/MAP05_02_IMPLEMENT_ROUTE_MASK_LOOKUP_RESULT.md`.

## MODIFIED

- Existing Assets / Authoring CSV / existing meta / asmdef / Scene / Prefab: `0`.

## PREEXISTING_IDENTICAL

- Existing MAP01 typed definitions, MAP05_01 terminal contracts, focused regressions, and assembly definitions remained byte-untouched.
- Authoring CSV/meta remained `50/50`.
- Progress test Scene and accepted legacy meta remained untouched.

## ROUTE MASK IDS

1. `ROUTE_T1_LR` — route type `1`, kind `Type1`.
2. `ROUTE_T2_LRD` — route type `2`, kind `Type2`.
3. `ROUTE_T3_LRU` — route type `3`, kind `Type3`.

## OPEN MASK MATRIX

- Type1: `L=true, R=true, U=false, D=false`.
- Type2: `L=true, R=true, U=false, D=true`.
- Type3: `L=true, R=true, U=true, D=false`.
- Missing horizontal run, simultaneous U+D, unsupported shape, duplicate ID/type/mask, and unregistered mandatory masks are rejected.
- Type0 rows are counted and ignored; they are never accepted by the mandatory lookup.

## LOOKUP API

- Exact immutable records/order/count: `Type1 / Type2 / Type3 / 3`.
- ID, route type, open mask, and required kind lookups preserve one record identity.
- Lookup construction enforces exact required IDs, route types, kinds, masks, uniqueness, active, and mandatory-allowed state.

## SOURCE IDENTITY

- Every record preserves the exact source `SectorRouteMaskDefinition` reference.
- Source rows and nested MAP01 provenance were neither cloned into a competing source nor mutated.

## DETERMINISM

- Shuffled input, fresh/reused builder, `en-US`/`tr-TR`, and parallel builds produced one exact signature.
- Equality/order/hash use fixed bits or ordinal string logic only.
- RNG method calls/raw draws: `0/0`.

## IMMUTABILITY

- Records, diagnostics, result errors, record list, and lookup dictionaries expose read-only state.
- Public instance properties have no setters; static mutable fields/cache/current set: `0`.
- Source mutation count: `0`.

## TEST

- `MandatoryRouteMaskLookupBuilderTests`: `127/127 PASS` (required `>=112`; final code rerun).
- `MandatoryTerminalBuilderTests`: `120/120 PASS`.
- `SiteReservationValidatorTests`: `268/268 PASS`.
- `BiomePatchValidatorTests`: `196/196 PASS`.
- `Map04ExitTests`: `110/110 PASS`.
- Required existing regression job: `694/694 PASS`.
- Actually executed required total: `821/821 PASS`; failed/skipped `0/0`.
- Game.Map targeted discovery arithmetic: `5612 = 5485 + 127`, required `>=5597`.
- Full EditMode discovery arithmetic: `5723 = 5596 + 127`, required `>=5708`.
- Large aggregate suites were not executed under the user-directed reduced profile.

## UNITY

- Unity: `6000.3.8f1`.
- Final forced refresh/compile: PASS.
- Compile errors / Console errors / relevant warnings: `0/0/0`.
- PlayMode tests: not required.
- Scene/Prefab changes: `NONE`.

## ASSET META

- Assets meta: `3161 -> 3170`.
- New meta format/GUID: `9/9`; duplicate GUID groups: `0`.
- Authoring CSV/meta: `50/50`.
- Authoring CSV aggregate digest: `4ffef6dbbea5151889d1c9114a500eba6cb54828ba47c9de508bad95dddc4ac3`.
- Authoring meta aggregate digest: `a3cebbdd83484bea161983320bd5b3a1756f6c82de774f1dc0508d327c85c291`.

## CHANGE SCOPE

- Exact new Assets changes: `18 = 8 Runtime C# + 1 test C# + 9 meta`.
- Existing Assets modifications / unexpected Assets changes / new folder meta: `0/0/0`.
- Forbidden later-task production symbol and UnityEditor/IO/time/random dependency audit: `0`.

## OWNERSHIP AUDIT

- Input ownership remains `WORLD_ROUTE_DEFINITIONS.RouteMasks`.
- Output ownership is only `MANDATORY_ROUTE_MASK_LOOKUP` for `PASS_ROUTE`.
- MAP05_01 terminal artifact is regression context only and was not modified or embedded.

## OUT_OF_SCOPE_FINDINGS

- NONE.

## DONE CONDITIONS

- Contract / compile / focused tests / required regressions / determinism / immutability / meta / change scope: PASS.
- MAP05_02 is eligible for STATUS FINALIZE.

## NEXT

- Finalize MAP05_02 only to COMPLETE and Current Task NONE.
- Keep `MAP05_03_IMPLEMENT_MINIMUM_CONNECTOR_TREE` LOCKED; do not auto-start it.

Recommended Commit: `feat(map): add mandatory route mask lookup`
