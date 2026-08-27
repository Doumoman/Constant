# MAP01_17 Remediation v1.2 — Failure Fixture Source Consumer

원 Task와 `MAP01_17_MAP01_EXIT_AUDIT_REMEDIATION_V1_1.md`를 함께 읽는다. v1.1의 Battery typed definition/Registry 계약은 그대로 유지하며, preflight에서 확인된 required test consumer 한 파일만 추가 승인한다.

## Additional WRITE ALLOWLIST

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/CsvFailureFixtureFactory.cs
```

이 외 v1.1 allowlist 확장은 없다.

## Exact Consumer Correction

- private `MicrochunkPopulationFiles` exact 16 inventory를 폐기한다.
- `MicrochunkPopulationItemDefinitionSource.ExpectedFileNames`의 public exact 17 inventory를 single source of truth로 사용한다.
- factory가 temp package에서 위 17개 successful parsed sources를 ordinal order로 수집해 `MicrochunkPopulationItemDefinitionBuilder.Build(...)`에 모두 전달한다.
- `battery_profiles.csv` 누락, duplicate, unexpected source가 없도록 exact set equality를 preflight한다.
- fixture mutation target selection, read-only Authoring copy, temp containment/cleanup, deterministic descriptor, Registry isolation은 변경하지 않는다.
- compatibility exact-16 branch, conditional skip, battery dummy source, generic record fallback은 금지한다.

## Required Regression

- factory valid seed가 exact 17 source로 production builder/Registry publish까지 성공한다.
- MAP01_16 fixture suite 기존 `37/37 PASS`를 유지한다.
- duplicate/enum/int/float/FK/BOM/header/compound mutation semantics와 previous Registry preservation이 그대로 PASS한다.
- v1.1 Battery repair tests 최소 `20/20`, MAP01_17 audit `40/40` PASS.
- targeted 최소 `861`, full EditMode 최소 `881`, compile/warning 0/0.
- actual fixed 50 import, Battery typed `5/5`, visual, CSV/meta preservation과 MAP01 phase gate 조건은 v1.1 원 계약대로 실행한다.

## Completion

기존 Result의 `REMEDIATION v1.2`에 factory before/after inventory source, exact changed file/SHA, fixture 37/37와 전체 regression을 기록한다. 모든 v1.1/v1.2 및 원 audit gate가 PASS일 때만 MAP01_17 PASS/COMPLETE, Current Task NONE, `MAP01 PHASE GATE APPROVED`로 finalize한다. MAP02_01은 LOCKED로 유지하고 시작하지 않는다.
