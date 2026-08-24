# CHAR00_02 — Lock Gameplay Contracts And Test Fixtures

CHAR00_01 PASS/finalize 후 CHAR00_02 하나만 여는 patch package다. PATCH APPLY는 Master, Status, CHAR00_02 Task 문서만 설치하고 Assets와 계약 문서는 변경하지 않는다.

기준선:

```text
Prior Result: CHAR00_01_INVENTORY_CHARACTER_INPUT_PHYSICS_MAP_RESULT.md
Prior Result STATUS: PASS
Prior Result SHA-256: 1bc1a931d43030561014c8cdf49c4609ac635bfd57e27d568ec975abefcef6c0
Previous CHAR00_01 Task SHA-256: 08b8141effaf9c66b0cec28d3e8bfba21023fee3f46800062d3ff70ff640f0f8
Source Registry SHA-256: be6cadc40893c5a66503af056881631f751e4170686966ca266eb2da928aaeb7
Current CHAR00_02 Task SHA-256: 29db7d180e8675b17858601f8ba6e9e2aeae03059a24c4252b3efb24dc04b51b
State after apply: 1 COMPLETE / CHAR00_02 CURRENT / 24 LOCKED
```

Task 실행 범위:

- 게임플레이·입력·이동·상호작용·전투·MAP 의존성 계약 문서 확정
- 16개 고정 fixture ID와 setup/action/expected/failure 조건 확정
- Assets/C#/inputactions/asmdef/Scene/Prefab/MAP 구현 변경 0

`CHAR00_03_CHAR00_BASELINE_EXIT_AUDIT`는 PASS/finalize 후에도 LOCKED다.
