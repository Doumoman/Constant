# RUN MAP07_02 REPAIR v1.2

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP07_02_IMPLEMENT_TILE_LAYER_RULES.md`, 그리고 최신 BLOCKED Result를 순서대로 읽어라.

Exact repair gates:

```text
Current Task: MAP07_02_IMPLEMENT_TILE_LAYER_RULES
Current Status: 79 COMPLETE / 1 CURRENT / 125 LOCKED
MAP07_02 Result STATUS: BLOCKED
MAP07_02 Result SHA-256: 5d51872d14f925bea341cd880755ce87ae4bb2bf23da3da410ac4db3ac681e7c
Current v1.1 Task SHA-256: 18d7d4c330b7a3614f155914aea8247412f65eb9ba04335ad5ea1dfffd5231f4
Revised v1.2 Task SHA-256: c9cb155bdb0b9f2d047b8305c35f32392d691988f612bc107849d0a9f3292edb
Prior MAP07_01 Result SHA-256: b11e740b808effe5a528a68497527edd0ab92fcc8c1a823dd6baa0d39363f474
Prior MAP07_01 Task SHA-256: 912028220492f7e9dff40db93711dd590dcd73531131d133cd0270c4862d368c
```

Current Task가 MAP07_02가 아니거나 어느 SHA라도 다르면 `BLOCKED`하고 변경하지 마. MAP07_03 이후 Task body는 읽거나 시작하지 마.

Repair apply는 Task 문서만 교체한다. Master/Status/Assets/CSV/C#/test/asmdef는 patch apply 단계에서 수정하지 마.

이후 MAP07_02를 같은 Current Task로 재개한다. v1.1에서 허용한 모든 writes는 그대로 유지한다. v1.2가 추가로 허용하는 existing test C#은 아래 1개뿐이다:

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Generation/Map06ExitTests.cs
```

허용 수정:

```text
Map06ApprovedSourceChainAndPhaseExitRemainExact에서 obsolete MicrochunkTileLayerRules absence entry만 MicrochunkTransformer로 교체
Map06ExitTests fixture case count remains unchanged
MAP06 aggregate remains 2746/2746 PASS
```

금지:

```text
Any assertion weakening, deletion, skip, or ignore
MAP07_03 implementation
MicrochunkTransformer implementation
socket edge validator
object slot semantic validator
96-cell validator
reachability probe
CSV importer/exporter
editor UI
Scene/Prefab/ProjectSettings/asmdef changes
```

PASS이면 MAP07_02만 COMPLETE/Current Task NONE으로 finalize한다. `MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS`는 LOCKED로 유지한다.
