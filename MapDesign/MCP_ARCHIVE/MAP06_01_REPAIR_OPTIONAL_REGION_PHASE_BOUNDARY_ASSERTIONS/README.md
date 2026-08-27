# MAP06_01 Repair — Optional Region Phase Boundary Assertions

MAP06_01은 아직 PASS가 아니므로 MAP06_02 패치를 만들지 않는다. 이 패키지는 현재 `MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS` Task 파일 한 개만 교체하는 repair package다.

기준선:

```text
Current Task: TASKS/MAP06_01_IMPLEMENT_OPTIONAL_REGION_MODELS.md
Current Task SHA-256: 79b806802dab4a86f3cdc0b6193be4c8f5c97a2e6a9cc8bcc023259752b49a62
Current Result STATUS: FAIL
Current Result SHA-256: 254092c80abdec87d20c9276854539ca7225e33738dfbe2419384a48710fb553
```

repair 내용:

- MAP06_01에서 정식 생성된 `OptionalRegion*` model symbols만 기존 MAP05 boundary tests에서 허용한다.
- MAP06_02+ 구현 심볼, mutable static state, UnityEditor leakage, filesystem/RNG/cache/root/generator boundary audit는 유지한다.
- production model, CSV, generated output, asmdef, Scene, Prefab, Packages, ProjectSettings는 수정하지 않는다.
- MAP06_02는 계속 LOCKED다.

기대 rerun:

```text
OptionalRegionModelsTests 194/194 PASS
Existing MAP05 aggregate 1959/1959 PASS
Actually executed total 2153/2153 PASS
failed/skipped 0/0
Assets meta 3254
```
