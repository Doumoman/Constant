# MAP07_13 - MAP07 Starter And Exit Tests

MAP07_12 PASS/finalize 후 MAP07의 마지막 Task만 여는 patch package다. Apply는 Master, Status, `MAP07_13` Task 문서만 설치하고 Assets는 변경하지 않는다.

기준선:

```text
Prior Result: MAP07_12_CREATE_MICROCHUNK_PREVIEW_AND_REPORT_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 869e5e640495e1ec4f7e376133d2525c9e0efe669296e949c7fe7b7d37c92876
Previous MAP07_12 Task SHA-256: 73544122f13653fa3762e87fdc75b7a415482b7336e0ac3fe3a75760d51ec9b0
Current MAP07_13 Task SHA-256: 698a330dcd7a8ba14ec33cec51b68ea56be9382abd0eefde96eb2a516c81effb
State after apply: 90 COMPLETE / MAP07_13 CURRENT / 114 LOCKED
```

실행 범위:

- Starter microchunk catalog full validation.
- Import-preview-export temp round-trip.
- MAP07 focused/regression gates.
- MAP07 phase exit audit.
- No new production code, Authoring source mutation, generated CSV, MAP08 work, or Scene/Prefab/settings changes.

`MAP08_01_DEFINE_MOONPALACE_BIOME_PAIRS`는 PASS 전까지 `LOCKED / DO NOT START`다.
