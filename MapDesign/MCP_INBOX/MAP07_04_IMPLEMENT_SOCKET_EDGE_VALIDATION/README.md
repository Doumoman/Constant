# MAP07_04 — Implement Socket Edge Validation

MAP07_03 PASS/finalize 후 MAP07의 네 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_04` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_03_IMPLEMENT_MICROCHUNK_TRANSFORMS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 062206bf753f1dce3a9c6a43107e24090bf9abdc253fc9e69eec478a2fafa505
Previous MAP07_03 Task SHA-256: f9aee2e6fe0c0a3222eae894cb562ef2100813c4a91e16461fd03e5d5d4cb170
Current MAP07_04 Task SHA-256: a563b469ebcfe9bea8f7f280398f20aa4464fd2aed9ff5ac2000c60f773eb0a6
State after apply: 81 COMPLETE / MAP07_04 CURRENT / 123 LOCKED
```

실행 범위:

- Socket `side`, `band_id`, `traversal_kind`, `edge_signature_id`, `mandatory_allowed`, `tool_requirement`, `minimum_safe_tiles` 검증.
- L/R socket은 horizontal band/signature, U/D socket은 vertical band/signature와 일치해야 함.
- 실제 outer tile opening과 inward safe depth clearance 검증.
- `EDGE_SOLID`은 socket row에서 금지.
- Object slot validation, 96-cell validator, reachability, editor UI, CSV import/export는 구현하지 않음.

`MAP07_05_IMPLEMENT_OBJECT_SLOT_VALIDATION`은 PASS 전까지 `LOCKED / DO NOT START`다.
