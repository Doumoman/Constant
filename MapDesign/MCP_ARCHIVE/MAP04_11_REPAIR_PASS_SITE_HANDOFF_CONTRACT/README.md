# MAP04_11 Repair — PASS_SITE Handoff Contract

MAP04_11의 실패를 같은 Task 안에서 교정하는 repair package다. MAP05를 열지 않는다.

## 판정

현재 `MultiSeedBiomeGrower / InsufficientAggregateCapacity`는 MAP04_05가 정의한 정상 `RetryRequired` 결과다. 동일한 P01 예약을 100회 재시도한 뒤에도 이 결과가 유지되면 MAP04 내부 오류나 생성 성공으로 위장하지 않고, 상위 단계가 새 P01 예약으로 다시 시작해야 하는 `PASS_SITE handoff required`로 분류한다.

기존 exit test는 이 정상 경계를 `1000/1000 same-P01 completion` 실패로 간주해 MAP04 exit를 과도하게 제한했다. repair는 production 코드를 바꾸지 않고 기존 `Map04ExitTests.cs`의 최종 disposition과 assertion만 수정한다.

## 적용 전 필수 상태

- Current Task: `TASKS/MAP04_11_MAP04_BATCH_AND_EXIT_TESTS.md`
- MAP04_11: `CURRENT`; MAP05_01 이후: `LOCKED`
- 현재 Task SHA-256: `1740f43b49a9e91675dc024d460690bba3f375929dfde0e33a9c4e96a9e66ef7`
- FAIL Result SHA-256: `26e949c34c01091a66e5727b408ef413483c267ab48854b20bcfe04f2173eedf`

## 적용 범위

Patch apply는 Task 파일 하나만 SHA 조건부 교체한다. Master, Status, Result, Assets는 적용 단계에서 변경하지 않는다. 적용 직후에도 상태는 `56 COMPLETE / 1 CURRENT / 148 LOCKED`다.

실행 단계에서는 기존 exit test C# 한 파일과 현재 Result만 수정할 수 있다. test meta/GUID, production, CSV, asmdef, Scene, Prefab, Packages, ProjectSettings는 그대로 둔다.

`RUN_MAP04_11_PROMPT.md`로 현재 MAP04_11을 다시 실행한다. 모든 gate가 PASS일 때만 MAP04_11을 finalize하며, MAP05_01은 별도 patch 전까지 LOCKED다.
