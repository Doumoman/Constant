# MAP06_01 — Implement Optional Region Models

MAP05_11 PASS 후 MAP06의 첫 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_01` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP05_11_MAP05_BATCH_AND_EXIT_TESTS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 5fdd4354d1ceee50376c3a8cd535e391af4db10baa148c682cf70247b19b40ff
State after apply: 68 COMPLETE / MAP06_01 CURRENT / 136 LOCKED
```

실행 범위:

- OptionalRegion ID, depth, access rule, reward tier, return policy, attachment/cell/region/snapshot model 정의.
- 신규 Runtime production C# 6개, Runtime EditMode test C# 1개만 생성.
- Optional attachment enumeration, grower, Type0 mask assignment, access/clue assignment, reward calculation, return device, inactive buffer, validator, overlay는 구현하지 않음.
- MAP05 mandatory graph/CSV/SectorCell/Authoring CSV/asmdef/Scene/Prefab/Packages/ProjectSettings 수정 금지.

Type4 기준은 계속 유지한다: U+D mandatory, L/R actual adjacency preserved, `UD/LUD/RUD/LRUD` all legal.
