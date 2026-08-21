# MAP04_11_MAP04_BATCH_AND_EXIT_TESTS Result

STATUS: PASS

## Apply / Repair

- Applied `MAP04_11_REPAIR_SPLIT_RUNTIME_SCENE_ADAPTER` v1.5.
- Manifest SHA-256: `b16cc666ef538c03ddc32bbafba3488bd575b4fd2a60c0636bee42d686354a8d`.
- Task payload/destination SHA-256: `fc6880ee69407ebbaf38eb1cc2a57636db33d9e454eb276005ea5178298d0a30`.
- Prior Editor-assembly MonoBehaviour conflict was resolved by splitting an attachable Runtime state adapter from the Editor custom inspector/action runner.

## Modified / New

- Overlay snapshot: `981e2a4ea6d3b81fb2d8976f006577f6f78f6eea00d82669472b93eefe5be510`.
- New Runtime adapter: `a858889e2bbed61979a88a11b1fa626c6be3cee73d070618b6db1d1b88f02f8c`.
- Adapter meta SHA/GUID: `2c0e1a1f7af3b908635f5b7be845f011508175d694291115b469cd9c924d47f9` / `f7c1871a26265bd428cc622f2f16035b`.
- Editor harness: `8d6b6e801e9f41afbd76094e1cd23042a904d5e74459435eae6b96a70522d4dd`.
- Runtime overlay tests: `87c6d303f345a02a3e9acb941bd10d49d889b3b0efca67e7779a5a71b1f5bfd1`.
- Editor scene tests: `9919108d184de02430644bd8823066216eb3fc223687a1a62a1b150d266c91fb`.
- Scene: `28e2c12f452756ac920b3af8a36ec855d857faba8ff94c15088659f618388701`.
- Scene meta SHA/GUID preserved: `57798017bf5c765d3c09f5414535ec03338ee05d761b3b4922d8485806b10f45` / `a1269082887679f4d9fedbd018b57c07`.
- Scenes folder meta SHA/GUID preserved: `d39cc37491dc1dd85be0ea30fc07e6d4ca8d01ae3db93f8c01e2bc3fcd28c5e2` / `1383e085e820139469e1a0e6073a24ee`.

## Variable Overlay

- Approved actual inventories `15/16/17/18/19` projected successfully.
- Core/Satellite/Intrusion totals matched actual diagnostics and conserved each actual patch total.
- Exact `169`, assigned/unassigned `165/4`, Core bindings `4`, validation `15/15`, row linkage, deterministic projection, and malformed rejection remained enforced.
- No production fixed range `15..19` was introduced.

## Progress Scene

- Root exact components: Transform, WorldTopologyOverlay, SiteReservationOverlay, BiomePatchOverlay, MapGenerationProgressSceneAdapter.
- Root tag `EditorOnly`; one orthographic solid-dark Main Camera; Canvas/EventSystem/build-list entries `0/0/0`.
- Known viable: three snapshots `169/169/169`, patches `17=4/10/3`, assigned/unassigned `165/4`, rules `15/15`, RNG `1912`.
- Exactly one overlay enabled per tab; generation calls `1`; scene dirty delta `0`.
- Clear/reload: snapshots `0/0/0`, generation calls `0`, scene dirty false.

## Batch / Determinism

- Full exit job `d64892afe6a945f7b9278d8bca1c28c2`: `1/1 PASS`.
- Worlds: `1000`; Completed/Handoff/Invalid: `49/951/0`.
- Attempts: `97640`; retry worlds `49`; maximum ordinal `99`; patch range `13..19`.
- Overlay ExactProjectionRejected: `0`; PatchCleanup InvalidSourceSnapshot: `0`.
- Terminal counts: GrowthFrontierExhausted `19802`, InsufficientAggregateCapacity `76985`, MinimumGrowthBlocked `804`.
- Batch digest: `f8ef573825b4b7b3fc5aa608b81b91f446cd663e60f2c87ca9d5fae38fa06910`.
- Invalid ledger count/digest: `0` / `e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855`.
- Map04ExitTests job `3163911d8a814e3eb335566be62da731`: `110/110 PASS`, including 102 determinism cases and frozen known vectors.

## Tests / Visual / Discovery

- BiomePatchOverlayTests: `155/155 PASS`.
- BiomePatchOverlaySceneDrawerTests: `28/28 PASS`.
- Overlay combined: `183/183 PASS`.
- MAP04 focused: `1464/1464 PASS`.
- MAP04 phase actually executed: `1574`; failed/skipped `0/0`.
- Game.Map discovery: `>=5365`; full EditMode discovery: `5477`.
- Variable 15 and 19 Scene View overlays: `18/18` each.
- Progress scene known viable / three tabs / status / Clear / reload: `12/12`.

## Compile / Asset / Scope

- Unity: `6000.3.8f1`.
- Final forced compile / Console errors / relevant warnings: `0/0/0`.
- Assets meta: `3151 -> 3152`; duplicate GUID groups: `0`.
- New Runtime adapter C#/meta: `1/1`; modified Editor harness/scene: `1/1`.
- Existing touched metas preserved; Authoring CSV/meta remains `50/50`.
- Prefab/asmdef/Packages/ProjectSettings/generated file changes: `0`.

## Exit Decision

MAP04 EXIT: APPROVED
MAP PROGRESS TEST SCENE: READY
MAP05 ENTRY: ELIGIBLE FOR SEPARATE PATCH
MAP05_01_BUILD_MANDATORY_TERMINALS: LOCKED / DO NOT START

NEXT: Finalize MAP04_11 to COMPLETE and Current Task NONE. Do not start MAP05_01.
