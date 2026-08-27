# MAP02_02 Remediation v1.2 — Legacy Editor Folder Meta Acceptance

```yaml
status_control:
  supplements_task: MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS
  supersedes_gate: MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_REMEDIATION_V1_1.final_assets_drift_zero
  result_file: REPORTS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_RESULT.md
```

## Purpose

MAP02_02 구현, known vectors, 독립성, focused `103/103`, targeted `1026/1026`, full EditMode `1046/1046`, compile/Console, v1.1 read-scope incident acceptance는 모두 PASS다. 최신 Result의 유일 차단 사유는 필수 Unity force refresh가 MAP01_15부터 실제 C# 파일을 포함한 legacy Editor 디렉터리의 folder `.meta` exact 6개를 자동 복원했지만, v1.1이 remediation 중 Assets drift exact `0`을 요구한 충돌이다.

이 v1.2는 그 exact 6개만 Unity 관리 folder metadata로 영구 수용한다. 일반적인 Assets allowlist 확장이 아니며 C#, test, CSV, asmdef, Scene, Prefab 또는 다른 `.meta` 변경을 허용하지 않는다. exact 6개를 삭제하거나 새 GUID로 재작성하지 않는다.

## Mandatory Read Order

entrypoint/rules → Master → Status → original MAP02_02 Task → v1.1 addendum → 이 v1.2 addendum → 최신 BLOCKED Result 순서로 읽는다. Current Task는 계속 exact `TASKS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS.md`여야 한다.

## Exact Folder Meta Acceptance Set

아래 exact 6개만 v1.1 이후 Unity refresh side effect로 수용한다.

```text
Assets/_Game/Map/Editor.meta
Assets/_Game/Map/Editor/WorldGeneration.meta
Assets/_Game/Map/Editor/WorldGeneration/Data.meta
Assets/_Game/Tests/EditMode/Map/Editor.meta
Assets/_Game/Tests/EditMode/Map/Editor/WorldGeneration.meta
Assets/_Game/Tests/EditMode/Map/Editor/WorldGeneration/Data.meta
```

각 파일은 대응 디렉터리의 Unity folder metadata로만 사용한다. 대응 디렉터리는 이미 MAP01_15 Editor production/test 파일을 포함하므로 빈 임시 폴더나 MAP02_02 구현 산출물이 아니다.

## Exact Read Authorization

Original Task와 v1.1 allowlist에 더해 다음 읽기만 허용한다.

- 위 folder meta exact 6개의 전체 본문과 SHA-256
- 위 exact 6개 대응 디렉터리의 direct child name/type 목록; C# 본문은 읽지 않는다
- project 전체 `Assets/**/*.meta`의 `guid:` 값만 추출하는 duplicate-GUID audit
- change tracker에서 original MAP02_02 marker 및 v1.1 repair marker 이후 path/status/hash 조회
- v1.1 incident test 2개와 matching meta 2개의 SHA-256 재확인; 본문 재감사는 하지 않는다

새 broad content search, 다른 비승인 파일 본문 read, `.meta`의 `guid:` 외 project-wide 본문 출력은 금지한다.

## Folder Meta Validation

exact 6개 각각 아래를 모두 만족해야 한다.

1. 대응 디렉터리가 존재하고 해당 `.meta`가 regular file이다.
2. `fileFormatVersion: 2`, 정확히 하나의 lowercase 32-hex `guid`, `folderAsset: yes`를 가진다.
3. importer는 Unity folder meta 형식이며 script/importer payload나 임의 user data를 포함하지 않는다.
4. 여섯 GUID는 서로 다르고 project 전체 Assets meta GUID와 중복되지 않는다.
5. v1.2 감사 시작 hash와 final force refresh 후 hash가 `6/6` 동일하다.
6. 삭제, move, rename, GUID 재생성, 본문 정규화 또는 timestamp용 rewrite를 수행하지 않는다.

하나라도 실패하면 meta를 고치지 말고 `BLOCKED`로 보고한다.

## Revised Change-Scope Gate

v1.1의 `Final Assets drift: 0 beyond original exact 14 files`를 아래 exact gate로만 교체한다.

```text
Original MAP02_02 allowlist:
  Runtime C# 6 + EditMode test C# 1 + matching .cs.meta 7 = 14 files

Accepted legacy Unity folder metadata:
  exact folder .meta 6 = 6 files

Final Assets change set from original MAP02_02 baseline:
  exact 20 files; unexpected 0

v1.1 repair marker 이후 Assets drift:
  exact accepted folder .meta 6; unexpected 0

Final global Assets meta:
  2954 = original baseline 2941 + MAP02_02 matching meta 7 + accepted folder meta 6
```

다른 v1.1 acceptance gate와 original MAP02_02 contract는 모두 그대로 유지한다. 숫자, path 또는 hash가 실제 상태와 다르면 추측·삭제·보정하지 말고 `BLOCKED`다.

## WRITE ALLOWLIST / No-Code, No-Meta-Edit Repair

이 remediation 실행에서 C#, test, CSV, asmdef, Scene, Prefab, Package, ProjectSettings와 모든 `.meta` 본문 변경은 exact `0`이다. exact 6개 folder meta는 이미 존재하는 Unity 산출물로 유지할 뿐 create/edit/delete하지 않는다.

실행 중 갱신 허용:

```text
MapDesign/MCP/REPORTS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_RESULT.md
```

모든 gate PASS 후 standard STATUS FINALIZE에서만 Master/Status를 갱신한다. 테스트가 실패해도 이 addendum으로 production/test/meta를 고치지 않는다.

## Required Revalidation

아래 순서를 그대로 실행한다.

```text
1. exact six folder meta format/path/hash audit
2. DeterministicRngStreamTests: 103/103 PASS
3. Required InitialState/first/second vectors: 6/6 PASS each
4. MAP02_01 GeneratedWorldData: 56/56 PASS
5. Targeted Game.Map.Tests.EditMode: 1026/1026 PASS
6. Full EditMode: 1046/1046 PASS
7. final force refresh + requested compile
8. compile error / relevant warning: 0 / 0
9. isolated Console error / warning: 0 / 0
10. exact six folder meta final hash unchanged: 6/6
11. Authoring CSV/meta: 50/50 unchanged
12. project duplicate GUID groups: 0
13. revised exact Assets change set: 20, unexpected 0
14. v1.1 incident test/meta hashes unchanged: 4/4
15. PlayMode NOT RUN / Visual NOT APPLICABLE
```

focused/targeted/full 결과를 축소하거나 skip/ignore를 추가하거나 assertion을 완화하지 않는다. final force refresh가 exact six 이외의 drift를 만들면 삭제하지 말고 `BLOCKED`다.

## Result / Completion

existing Result를 갱신해 아래 섹션을 추가한다.

```text
REMEDIATION v1.2
LEGACY FOLDER META ACCEPTANCE
EXACT SIX-PATH VALIDATION
GUID / HASH EVIDENCE
REVISED CHANGE SCOPE
FINAL REVALIDATION
```

모든 gate가 PASS면 Result를 `STATUS: PASS`로 갱신하고 standard STATUS FINALIZE를 수행한다.

```text
MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS: COMPLETE
Current Task: NONE
Master/Status: 29 COMPLETE / 176 LOCKED / 0 CURRENT
MAP02_03_IMPLEMENT_GRID_INITIALIZATION_PASS: LOCKED
```

MAP02_03은 자동 시작하지 않는다. Recommended Commit은 `feat(map): add deterministic rng streams`를 유지한다.
