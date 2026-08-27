# MAP06_10 — MAP06 Overlay And Exit Tests

MAP06_09 PASS/finalize 후 MAP06의 마지막 Task만 여는 patch package다. Apply는 Master, Status, `MAP06_10` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 51a6f0dd621db698628ceef6ba7e7f2f18988b213ad564e7b35e00c52041d62a
Previous MAP06_09 Task SHA-256: e5f430c29dcba4344feb1ba12fff73fc9052c3f3a386d672a7e8a3b016a2c97e
Current MAP06_10 Task SHA-256: 205ce60e1e591036a80bc7dc10a939ea95d0237d09babe106e86c09b78e70605
State after apply: 77 COMPLETE / MAP06_10 CURRENT / 127 LOCKED
```

실행 범위:

- MAP06 source-chain과 validation report를 immutable optional region overlay snapshot으로 표시.
- access color, depth label, attachment/contact marker, return witness arrow, reward marker, inactive D/I, validation issue marker 검증.
- MAP06 phase exit tests 완료 및 PASS Result에서 `MAP06 PHASE EXIT: APPROVED` 기록.
- 신규 Runtime diagnostics C# 7개, Editor preview C# 1개, EditMode test C# 3개 생성.
- existing boundary assertions는 MAP06_10 symbols를 허용하고 MAP07+만 금지하도록 필요한 파일만 수정 가능.
- generated CSV writer, boundary profile/recipe/microchunk/tile/socket/edge, Scene/Prefab/asmdef/ProjectSettings는 구현하지 않음.

Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 Authoring CSV 불변 조건을 유지한다. `MAP07_01_IMPLEMENT_MICROCHUNK_DEFINITION`는 PASS 전까지 `LOCKED / DO NOT START`다.
