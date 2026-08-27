# MAP06_02 Repair — OptionalRegionModels Boundary Allowlist

MAP06_02 FAIL 원인 하나만 보정하는 repair package다. Apply는 현재 `MAP06_02` Task 문서만 교체하고 Assets는 변경하지 않는다.

기준선:

```text
Current Task: MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS
Current Task SHA-256: d1f39196c3897f54611185eb0ccd95d64e60ed60c1a4b96e03671c799e2f68f0
Current Result: MAP06_02_ENUMERATE_OPTIONAL_ATTACHMENTS_RESULT.md
Current Result STATUS: FAIL
Current Result SHA-256: a5c93b16d551ce999aebea014d37fc1ac0bbb0e6fea1c790ae97eb83175ee3c2
State remains: 69 COMPLETE / MAP06_02 CURRENT / 135 LOCKED
```

Repair 범위:

- `OptionalRegionModelsTests.cs`를 기존 boundary test WRITE ALLOWLIST에 추가.
- MAP06_02 symbols는 허용하고 MAP06_03+ future symbols만 금지.
- 이미 생성된 OptionalAttachment implementation/tests/candidate output은 보존.
- MAP06_03은 시작하지 않음.
