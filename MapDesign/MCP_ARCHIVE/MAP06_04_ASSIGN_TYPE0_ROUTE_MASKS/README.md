# MAP06_04 — Assign Type0 Route Masks

MAP06_03 PASS/finalize 후 MAP06의 네 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_04` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_03_IMPLEMENT_OPTIONAL_REGION_GROWER_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 370a15f504d46492a591d064ee70dbc35d27b5b55ab4b621617aedae95d489b0
Previous MAP06_03 Task SHA-256: dbdde1bc53b615649c377c700a9c9d35f8de81baa2fcf79253f0e7d35974eb88
Current MAP06_04 Task SHA-256: 320870304bc61d7414a10473978ae11472adefd88c6f8cd76bb6f909ac136cea
State after apply: 71 COMPLETE / MAP06_04 CURRENT / 133 LOCKED
```

실행 범위:

- MAP01 typed route definitions의 exact 12 active Type0 rows를 immutable catalog로 검증.
- MAP06_03의 12 regions / 39 cells에 same-region internal adjacency와 exact match하는 registered mask ID를 배정하고 attachment→mandatory boundary는 base-closed로 예약.
- Type0 L/R 동시 개방, 임의 bool 조합, extra open side, partial publication 금지.
- 신규 Runtime production C# 7개, Runtime EditMode test C# 1개 생성.
- 기존 boundary assertions는 MAP06_04 symbols를 허용하고 MAP06_05+만 금지하도록 필요한 파일만 수정 가능.
- MAP05/MAP06_01~03 production, mandatory graph/mask, OptionalRegionCell, SectorCell, Authoring/generated CSV, asmdef, Scene, Prefab, Packages, ProjectSettings 수정 금지.
- access/clue/reward/return/inactive/validator/overlay/generated CSV는 구현하지 않음.

Type4 기준은 유지한다: U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal. Type0의 L/R 동시 개방 금지와 혼동하지 않는다.


