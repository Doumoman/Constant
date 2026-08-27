# MAP05_03 Repair — Obsolete Route-Mask Negative Assertion

MAP05_03 FAIL 뒤 현재 Task 안에서 검증만 복구하는 repair package다. Apply는 현재 Task 문서만 교체하고 Master/Status/Assets는 변경하지 않는다.

원인은 MAP05_02 회귀 테스트가 `MandatoryConnectorTree` 심볼 부재를 계속 요구하는 오래된 negative assertion이다.

repair는 production을 바꾸지 않고 `MandatoryRouteMaskLookupBuilderTests.cs`의 해당 assertion만 MAP05_03 산출물 허용으로 수정한 뒤 MAP05_03 검증을 다시 실행한다.

MAP05_04는 계속 LOCKED다.
