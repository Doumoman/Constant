# MAP06_02 — Enumerate Optional Attachments

MAP06_01 PASS 후 MAP06의 두 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_02` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 8d8f2b8bae5b08c9bf5fd258a225db89d16bffa5ca8faa058ef78ac02334442e
State after apply: 69 COMPLETE / MAP06_02 CURRENT / 135 LOCKED
```

실행 범위:

- MAP05 mandatory route graph 인접 미사용 sector에서 optional attachment candidate를 deterministic 열거.
- 신규 Runtime production C# 6개, Runtime EditMode test C# 1개 생성.
- 기존 MAP05 boundary negative assertions는 MAP06_02 symbols를 허용하고 MAP06_03+만 금지하도록 필요한 파일만 수정 가능.
- Optional grower, Type0 route mask assignment, access/clue, reward, return, inactive, validator, overlay, generated CSV writer는 구현하지 않음.
- MAP05 mandatory graph/CSV/SectorCell/Authoring CSV/asmdef/Scene/Prefab/Packages/ProjectSettings 수정 금지.

Type4 기준은 계속 유지한다: U+D mandatory, L/R actual adjacency preserved, `UD/LUD/RUD/LRUD` all legal.
