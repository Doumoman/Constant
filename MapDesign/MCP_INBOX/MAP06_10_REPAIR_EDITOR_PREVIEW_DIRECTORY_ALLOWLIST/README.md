# MAP06_10 Repair — Editor Preview Directory Allowlist

MAP06_10 BLOCKED 원인만 보정하는 repair package다. Apply는 현재 `MAP06_10` Task 문서만 교체하고 Master, Status, Assets, CSV, C#, test, asmdef는 변경하지 않는다.

기준선:

```text
Current Task: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS
Current Task SHA-256 before repair: 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
Current Result: MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS_RESULT.md
Current Result STATUS: BLOCKED
Current Result SHA-256: d02204b7515e4818052f6e5e8dad0fc0740803f3af5f0753f652b5c715e3119e
Revised Task SHA-256: 623da5aaf2f8c72dd830fb5f859c4b05a631a93b7f4fa2a3aa67adc823f95cdb
State remains: 77 COMPLETE / MAP06_10 CURRENT / 127 LOCKED
```

Repair 범위:

- `Assets/_Game/Editor/MapAuthoring/Preview/`가 없어서 발생한 allowlist 모순만 교정.
- canonical drawer target `Assets/_Game/Editor/MapAuthoring/Preview/OptionalRegionOverlaySceneDrawer.cs`는 유지.
- 신규 Editor preview directory와 folder meta `Assets/_Game/Editor/MapAuthoring/Preview.meta`를 exact 1개로 허용.
- nonexistent predecessor drawer `MandatoryRouteOverlaySceneDrawer.cs`는 required read allowlist에서 제거.
- required Assets meta gate를 `3311 -> 3322`에서 `3311 -> 3323`으로 정정.
- 신규 C#/matching `.cs.meta`는 기존대로 `11/11`; 추가 folder meta는 `1/1`; 그 외 directory/folder meta는 `0`.
- `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION`는 `LOCKED / DO NOT START` 유지.

Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 Authoring CSV 불변 조건을 유지한다.
