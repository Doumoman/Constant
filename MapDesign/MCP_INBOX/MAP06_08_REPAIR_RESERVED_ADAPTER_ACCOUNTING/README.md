# MAP06_08 Repair — Reserved Adapter Accounting

MAP06_08 BLOCKED 원인만 보정하는 repair package다. Apply는 현재 `MAP06_08` Task 문서만 교체하고 Master, Status, Assets, CSV, C#은 변경하지 않는다.

기준선:

```text
Current Task: MAP06_08_ASSIGN_INACTIVE_BUFFERS
Current Task SHA-256: 778d5beb1944ddd01e4541254f6d63d55ce255c3eaeab0f79143ee4de2de9ec7
Current Result: MAP06_08_ASSIGN_INACTIVE_BUFFERS_RESULT.md
Current Result STATUS: BLOCKED
Current Result SHA-256: 759de495f3e2608fba844e5cca5ab3c6d7cd0479a73c8a3928c1ac4b964045fa
Revised Task SHA-256: 0e45ed924cd515ca497abca85e0ede2a6efddefa9648c72c21b0d00a93647340
State remains: 75 COMPLETE / MAP06_08 CURRENT / 129 LOCKED
```

Repair 범위:

- 기존 `169 = 8 + 47 + 39 + 75`, protected union `94`, zero-overlap gate를 폐기.
- approved reserved adapters `0,28,106`의 `Site ∩ Mandatory` source overlap을 허용.
- source counts는 `ReservedSite/Mandatory/Type0 = 8/47/39`로 보존.
- exclusive projection은 `ReservedSite/MandatoryOnly/Type0/InactiveBuffer = 8/44/39/78`, protected union `91`.
- 기존 MAP06_08 구현 산출물과 boundary test 변경은 같은 allowlist 안에서 교정.
- `MAP06_09_IMPLEMENT_OPTIONAL_REGION_VALIDATOR`는 `LOCKED / DO NOT START` 유지.

Type4 U+D mandatory, L/R independent, `UD/LUD/RUD/LRUD` all legal 규칙과 Authoring CSV 불변 조건을 유지한다.
