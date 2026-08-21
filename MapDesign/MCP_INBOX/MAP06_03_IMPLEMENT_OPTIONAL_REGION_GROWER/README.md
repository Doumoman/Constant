# MAP06_03 — Implement Optional Region Grower

MAP06_02 PASS/finalize 후 MAP06의 세 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_03` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 69b6dbc5b379de297805ba8d9b3523779e26486a9244b3f2306523e70c9c123c
Previous MAP06_02 Task SHA-256: e87e9d55254243eea6ff590b84fb68225077890d454fde978b330a0f4ad805da
Current MAP06_03 Task SHA-256: dbdde1bc53b615649c377c700a9c9d35f8de81baa2fcf79253f0e7d35974eb88
State after apply: 70 COMPLETE / MAP06_03 CURRENT / 134 LOCKED
```

실행 범위:

- accepted attachment `51`, digest `68b438c...ee6`에서 깊이 `1..4`의 connected optional region topology를 deterministic 성장.
- 각 accepted region은 mandatory graph와 exact one bridge만 허용하고, 모든 cell은 same-region L/R 이웃 동시 보유를 금지.
- 신규 Runtime production C# 4개, Runtime EditMode test C# 1개 생성.
- 기존 boundary assertions는 MAP06_03 symbols를 허용하고 MAP06_04+만 금지하도록 필요한 파일만 수정 가능.
- Type0 mask/access/clue/reward/return/inactive/validator/overlay/generated CSV는 구현하지 않음.
- MAP05/MAP06_01/MAP06_02 production, Authoring CSV/meta, asmdef, Scene, Prefab, Packages, ProjectSettings 수정 금지.

Type4 기준은 유지한다: U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal. Type0의 L/R 동시 관통 금지와 혼동하지 않는다.
