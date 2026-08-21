# MAP06_09 — Implement Optional Region Validator

MAP06_08 PASS/finalize 후 MAP06의 아홉 번째 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_09` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 43dd272802bfe6094ac5f1dff91ddb30229acf0c5a0885742509945a496bf58b
Previous MAP06_08 Task SHA-256: 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
Current MAP06_09 Task SHA-256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
State after apply: 76 COMPLETE / MAP06_09 CURRENT / 128 LOCKED
```

실행 범위:

- MAP06_01~08 source-chain을 immutable validation report로 검증.
- mandatory graph identity, Type0 `!(L&&R)`, returnability, clue, reward, inactive accounting 검증.
- approved reserved-adapter overlap `{0,28,106}` 및 full accounting `169 = 8 + 44 + 39 + 78` 보존.
- 신규 Runtime production C# 6개, Runtime EditMode test C# 1개 생성.
- existing boundary assertions는 MAP06_09 symbols를 허용하고 MAP06_10+만 금지하도록 필요한 파일만 수정 가능.
- overlay, exit, generated CSV, boundary profile/recipe/microchunk/tile/socket/edge는 구현하지 않음.

Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 Authoring CSV 불변 조건을 유지한다. `MAP06_10_MAP06_OVERLAY_AND_EXIT_TESTS`는 PASS 전까지 `LOCKED / DO NOT START`다.
