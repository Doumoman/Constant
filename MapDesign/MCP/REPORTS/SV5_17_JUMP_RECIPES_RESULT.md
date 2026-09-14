# SV5_17_JUMP_RECIPES Result

TASK: SV5_17_JUMP_RECIPES
TASK_ID: SV5_17_JUMP_RECIPES
STATUS: PASS

## Outcome

The exact R0 and MIRROR_X local jump recipes are implemented and all required
local, independent, visual, and accepted full-regression gates pass. The final
accepted regression ran in a dedicated Unity 6000.3.8f1 batchmode process,
returned exit code 0, and produced an official NUnit `Passed` result for all
356 expected SV5 EditMode test fullnames.

## Implemented contract

- Production recipes: `JUMP012_MIXED_R0` and `JUMP012_MIXED_MX` only.
- Coordinate scope: 24x32 local integer cells, not a Sector or world surface.
- Each recipe: 10 supports, 44 occupied cells, 9 ordered links, 4 ordered
  segments, and 1 explicit Grab edge.
- Catalog totals: 20 supports, 88 occupied cells, 18 links, and 2 explicit
  Grab edges.
- Per-recipe movement mixture: 7 `+1 JUMP`, 1 `+2 JUMP_GRAB`, and 1 level
  `JUMP`.
- MIRROR_X applies `x'=23-x` to every cell, support, route, and Grab coordinate
  and reverses left/right direction and face.
- Catalog digest:
  `19b4b3c37a1b1b11856eb1c946ce8ea64cbc16517dbfe9ace2ae66e4a5504022`.
- R0 digest:
  `74b495a0d33209e0455787e55307433f8480e419021629918d859f431665e687`.
- MIRROR_X digest:
  `9a2cd100798f52e5d216a5ec42b29b0191e00752ba59c2f9276b4eeb53da4874`.
- `SEG_ASCENT_A` partitions `JS_LINK_00..02`; `SEG_GRAB_TURN` partitions
  `JS_LINK_03..04`; `SEG_ASCENT_B` partitions `JS_LINK_05..06`; and
  `SEG_ASCENT_C` partitions `JS_LINK_07..08` for both variants.
- The #012 before/draft reference remains reference-only, non-production,
  non-equivalent, and not Player-verified, with exactly 141 changed cells.
- `JumpRecipeReady=true` and `ComposedGeometryReady=true`;
  `SweptClearanceReady=false`, `RecoveryReady=false`, and
  `PlayerVerified=false`.

## Ordered gates

1. Unity 6000.3.8f1 compile: PASS, zero Task-owned compile errors.
2. Targeted local suite: 46/46 PASS, failed/skipped/inconclusive 0/0/0,
   duration `1.379779` seconds; XML SHA-256
   `58dcb1008f9cc1216220b696c8d9983cf526ec52acd8327cfe6a58437ce0f343`.
3. Independent checker: `PASS_INDEPENDENT_JUMP_RECIPES`; audit SHA-256
   `f108b99124fb7b9d3174cedec2b2e2a8698e6e7b611813abd3d208e4429e32f3`.
4. Chrome-rendered SVG inspection: `PASS_VISUAL_INSPECTION`; visual audit
   SHA-256
   `3aac3509a08becb3421bc4104fa5e0def29a686e7f2d2fa222d3acc07288809d`.
   SVG SHA-256 is
   `04bec806eb4b6605c99b304644824dc9cfe399de5ced004938652e72ad313684`;
   rendered PNG SHA-256 is
   `3206d398df16ff903b0df1ef463260fd17c81b6452a442201cf5c0febab7b80c`.
5. Accepted full SV5 EditMode regression: 356/356 PASS,
   failed/skipped/inconclusive 0/0/0, official NUnit result `Passed`, NUnit
   duration `1670.9500318` seconds.

## Full-regression exception audit

Physical full SV5 EditMode executions: **3**. Accepted full-regression
executions: **1**.

1. Physical run 1 recorded 355 PASS / 1 FAIL. The sole F06 failure was caused
   by an MCP-FOR-UNITY disposed `NetworkStream` error log. It is classified
   `INFRASTRUCTURE_CONTAMINATED`, not accepted, and preserved byte-identically
   as `focused_results_infrastructure_contaminated.xml`, SHA-256
   `056db56768100799053347a80a6f5a879e50adc37b7ec0eee2f3b771ee52c593`.
2. The isolated F06 recheck passed 1/1 and remains preserved at SHA-256
   `0fc6b3e247f3547301284ea84cd2d3c6c556645bdb4cb74ea14a9e7161d01c88`.
3. Physical run 2 is classified `GUI_EDITOR_EXTERNAL_QUIT_NO_VERDICT`.
   GUI Editor PID 7044 exited orderly before `RunFinished`; no NUnit verdict
   or process exit code was retained, so this run is not accepted.
4. Physical run 3 is classified `DEDICATED_BATCHMODE_ACCEPTED`. It used
   `C:/Program Files/Unity 2022.3.22f1/6000.3.8f1/Editor/Unity.exe`, process
   PID 46784, a 3600-second non-killing wait limit, and no `-quit` or
   `-nographics`. It started at `2026-09-15T00:04:47.3197285+09:00`, ended at
   `2026-09-15T00:33:42.9800893+09:00`, returned exit code 0, and completed
   356/356 PASS.
5. The accepted `focused_results.xml` SHA-256 is
   `e4c3284ec5048e74e8f8553e82e4025b6114662abd2f93244087c3d359b89378`.
   The dedicated log SHA-256 is
   `0bd5334e99dbc2e6ff4fe0aaeaa92816193aee01fe80ccac6a0aad0762ac1529`.
   The sorted 356-fullname set exactly matches run 1; its canonical SHA-256 is
   `9718cfa489d4ffdc2373f3b601e93f1cac87762c0edfb1d4b783569461434242`.
   The log contains `RunFinished` and the exact focused result save path.

The complete audit is
`MapDesign/MCP/GENERATED/SV5_17_JUMP_RECIPES/regression_exception_audit.json`,
SHA-256
`588c694fb0de61c59f30185e82d5277be71ab2e1c61d33a939a80351ca48e378`.

The final binding is
`MapDesign/MCP/GENERATED/SV5_17_JUMP_RECIPES/BINDING.json`, SHA-256
`a5e1f73f3aa31ed39a009a5669a1d9ccf9afe7c432a8a337e80787d7a588624f`.

## Scope and lifecycle

- No fourth full regression was performed.
- Product code, tests, contract, Player, tree/Hub, world placement, scenes,
  prefabs, Packages, ProjectSettings, Master, and SV5_13-16 evidence were not
  modified for the final run.
- The accepted run used no Unity MCP command, GUI operation, or Unity Hub
  project open; Unity CLI MCP PID 34044 was not terminated.
- `SV5_18_JUMP_CLEARANCE` remains `LOCKED` and is not started.
- `STAGE.py --post-readonly` passed before Finalize with status
  `PASS_POST_READONLY`.
- Native Status Finalize passed: Current Task is `NONE`, SV5_17 is `COMPLETE`,
  and SV5_18 remains `LOCKED`.
- The single Task-owned commit and exact-blob Review ZIP follow this matching
  PASS Result and finalized status.
- Push is prohibited and was not performed.
