# MAP05_11_MAP05_BATCH_AND_EXIT_TESTS Result

TASK: MAP05_11_MAP05_BATCH_AND_EXIT_TESTS
STATUS: PASS

## SUMMARY

- Repair v1.1 corrected the three obsolete MAP05_10 overlay phase-boundary audits without changing production code or reducing coverage.
- The focused repairs, existing MAP05 aggregate, new exit suite, 10,000-seed batch, current Game/Scene visual gate, compile/Console gate, and approved Diagnostics folder-meta branch all passed.
- MAP05 is approved for exit. MAP06 is eligible only for a separate patch; MAP06_01 remains locked and was not read or started.

## PATCH APPLY / REPAIR AUTHORITY

- Repair patch: `MAP05_11_REPAIR_OVERLAY_PHASE_BOUNDARY_AND_DIAGNOSTICS_META` v1.1.
- Manifest SHA-256: `7dab3a6110924e351d4bff7cd24556cb1dd16f5d8b6c3a4021775bccfc1b981a`.
- Applied Task SHA-256: `32d5a4e791af5378e01bdaf3028de8198fa566bac68d01d5ae53d0f3eeff3366`.
- Superseded FAIL Result SHA-256: `817d049e6f4ec5bec5641fb1de42cc561ecdf26578a4d16efca9c456e5a58863`.
- Patch payload and destination were byte/SHA verified; `.APPLIED` records the accepted patch.

## REPAIR

- Updated exactly three existing runtime-surface audits:
  - `HorizontalBackboneRouterTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols`.
  - `VerticalGatewayPlannerTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols`.
  - `UpDownConflictResolverTests.RuntimeSurfaceHasNoMutableStaticUnityEditorOrMap05_11PlusSymbols`.
- Removed only the completed `MandatoryRouteOverlay` symbol from the later-task forbidden set.
- Kept `MandatoryRoutePass`, `SectorRouteMaskAssigner`, `OptionalRegion`, mutable static state, `UnityEditor`, filesystem, RNG/cache, root-adapter, retry/batch, and generated-writer boundaries forbidden.
- Test counts, assertions, and all three test meta GUIDs were preserved.
- Production, Graph, CSV, `SectorCell`, Authoring data, asmdefs, Scene, Prefab, Packages, and ProjectSettings were not modified.

## TEST EVIDENCE

```text
HorizontalBackboneRouterTests      142/142 PASS
VerticalGatewayPlannerTests        156/156 PASS
UpDownConflictResolverTests        194/194 PASS
Existing MAP05 focused aggregate  1827/1827 PASS
New Map05ExitTests                  132/132 PASS
MAP05 phase aggregate              1959/1959 PASS
failed / skipped                         0 / 0
```

- Existing aggregate covered all 11 required MAP05 runtime/editor classes, including runtime overlay `142` and Scene drawer `26`.
- Phase aggregate is the union of the actually executed existing `1827` and exit `132` selections.
- No skip, ignore, explicit, assumption, exclusion, or catch-all bypass was used.

## 10,000-SEED BATCH

- World seeds / attempt ordinal: `ulong 0..9999 / 0`.
- Total / completed: `10000 / 10000`.
- Retry / unresolved / invalid: `0 / 0 / 0`.
- Terminal reachability / route-mask mismatch failures: `0 / 0`.
- Type4 U+D missing / L-R preservation mismatch: `0 / 0`.
- Edge reciprocity / generated-edge bijection failures: `0 / 0`.
- Validation failures: `0`; overlay snapshots: `10000/10000`.
- Type4 token aggregate UD/LUD/RUD/LRUD: `170000/0/0/20000`.
- Graph digest aggregate: `08fe445a875777b7bb783690f88f415b60f0be255823f9f5d0cbbab1a07d2ca0`.
- Stable overlay digest: `cabf02949930b9142d5000df17ed4e1bc13d38a9c9027f6b82c3babdc267e5a3`.
- Canonical batch SHA-256: `802b413ba75812a0de7cb0b49c5f83723e02d071f6faa292ad0d1f5831c14815`.
- Authoritative batch case duration: `59.67s`; retained job completed normally after one bounded MCP wait timeout.

## KNOWN VECTOR / TYPE4

- Graph nodes / directed / undirected / route cells: `47/96/48/47`.
- Masks T1/T2/T3/T4_UD/T4_LUD/T4_RUD/T4_LRUD: `20/4/4/17/0/0/2`.
- Mandatory terminals reachable / accepted loops represented: `7/7 / 2/2`.
- Generated sector bytes / edge bytes / edge rows: `16838/7094/96`.
- Validation rules / passed / violations / errors / warnings: `12/12/0/0/0`.
- Type4 `UD/LUD/RUD/LRUD` legality, mandatory U+D, and independent L/R preservation: PASS.

## VISUAL ACTUAL

- Current Game View checklist: `9/9 PASS`.
- Current Scene View checklist: `9/9 PASS`.
- Combined visual checklist: `18/18 PASS`.
- Verified full 13x13 grid, `PASS_ROUTE 12/12 | V/E/W 0/0/0`, route colors, T1/T2/T3/T4 tokens, side glyphs, BFS distance labels, loop markers, deterministic ordering, and the shared snapshot.
- Visual snapshot values matched nodes/edges/routes `47/96/48/47`, Type4 `17/0/0/2`, terminals `7/7`, loops `2/2`, CSV `16838/7094/96`, and validation `12/12/0/0/0`.
- Transient overlay object, popup Scene View, and seven generated capture assets were removed; play mode is off and the active Scene is clean/unsaved-mutation-free.

## UNITY / COMPILE / CONSOLE

- Unity: `6000.3.8f1`.
- Forced import / domain reload / compile: PASS.
- Compile errors / final Console errors / final relevant warnings: `0/0/0`.

## ASSET / META / SCOPE

- Preferred strict branch: Assets meta `3246`.
- Approved deterministic Diagnostics folder-meta branch used: final Assets meta `3247`.
- Exact approved meta: `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics.meta`.
- Diagnostics GUID `77438650185b6304fb7534540e4ca3a8`: unique; exact regenerated file retained without delete/recreate loop.
- `Map05ExitTests.cs/meta` preserved: `1/1`; test meta GUID remains `e2aca6644f59475b98bdc73c341bb1e6`.
- Modified existing test C#: `3`; unexpected existing/folder meta beyond the approved Diagnostics meta: `0`.
- Authoring CSV / CSV companion meta: `50/50`; missing companions: `0`; duplicate GUID groups: `0`.
- Generated capture/CSV residue: `0/0`.
- Production / Scene / Prefab / asmdef / Packages / ProjectSettings modifications: `0/0/0/0/0/0`.

## EXIT DECISION

STATUS: PASS
MAP05 EXIT: APPROVED
MAP06 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS: LOCKED / DO NOT START

Recommended Commit: NONE — no commit or push requested.
