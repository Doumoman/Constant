# MAP06_08 — Assign Inactive Buffers

MAP06_07 PASS/finalize 후 MAP06의 여덟 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_08` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_07_IMPLEMENT_RETURN_POLICY_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 2815e6b35df71be1477812594435ed4793c3c9a03c60f1ef602267e4a2e12329
Previous MAP06_07 Task SHA-256: 2ab50e5c150bc833395cd9e5f8acb017e8685d90f0b63d5cab394cf0e33b4956
Current MAP06_08 Task SHA-256: 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
State after apply: 75 COMPLETE / MAP06_08 CURRENT / 129 LOCKED
```

실행 범위:

- 169-cell world와 site/biome/mandatory/Type0/return source-chain 검증.
- protected ownership `ReservedSite/Mandatory/Type0 = 8/47/39`, union `94` 검증.
- 나머지 `75` cells를 immutable `SectorRole.InactiveBuffer` assignment로 발행.
- protected cardinal neighbor가 있으면 `DecorativeBoundary`, 없으면 `InteriorInactive`로 분류.
- 신규 Runtime production C# 6개, Runtime EditMode test C# 1개 생성.
- existing boundary assertions는 MAP06_08 symbols를 허용하고 MAP06_09+만 금지하도록 필요한 파일만 수정 가능.
- boundary profile/recipe/microchunk/tile/socket/edge, validator/overlay/exit/generated CSV는 구현하지 않음.

Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 Authoring CSV 불변 조건을 유지한다. `MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR`는 PASS 전까지 `LOCKED / DO NOT START`다.
