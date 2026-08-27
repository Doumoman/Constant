# MAP05_11 Repair — Overlay Phase Boundary + Diagnostics Meta

MAP05_11은 아직 PASS가 아니므로 MAP06 패치를 만들지 않는다. 이 패키지는 현재 `MAP05_11_MAP05_BATCH_AND_EXIT_TESTS` Task 파일 한 개만 교체하는 repair package다.

기준선:

```text
Current Task: TASKS/MAP05_11_MAP05_BATCH_AND_EXIT_TESTS.md
Current Task SHA-256: f0720d2df2f8807b2868b1c6074fb05efbe77ff0391bf3dd86a43c8d9957780f
Current Result STATUS: FAIL
Current Result SHA-256: 817d049e6f4ec5bec5641fb1de42cc561ecdf26578a4d16efca9c456e5a58863
```

repair 내용:

- 기존 obsolete phase-boundary tests 3개에서 MAP05_10 overlay symbols만 허용한다.
- MAP06+ symbols, mutable static state, UnityEditor leakage, filesystem dependency audit는 계속 유지한다.
- `Assets/_Game/Tests/EditMode/Map/WorldGeneration/Diagnostics.meta`는 strict cleanup 또는 exact regenerated folder meta policy로만 처리한다.
- Production, CSV, `SectorCell`, asmdef, Scene, Prefab, Packages, ProjectSettings, MAP06는 수정하지 않는다.

Type4 규칙은 유지한다: U+D는 필수, L/R은 actual adjacency를 보존하며 `UD`, `LUD`, `RUD`, `LRUD` 모두 legal이다.
