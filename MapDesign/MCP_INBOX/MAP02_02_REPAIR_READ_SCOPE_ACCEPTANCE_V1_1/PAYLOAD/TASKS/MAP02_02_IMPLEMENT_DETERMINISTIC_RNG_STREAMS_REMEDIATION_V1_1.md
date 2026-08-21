# MAP02_02 Remediation v1.1 — Read-Scope Incident Acceptance

```yaml
status_control:
  supplements_task: MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS
  result_file: REPORTS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_RESULT.md
```

## Purpose

MAP02_02 implementation·tests·Unity·change scope는 모두 PASS였지만, approved type path를 찾는 non-mutating preflight `rg`가 원 Task READ allowlist 밖 existing test 2개의 match lines를 출력해 `STATUS: BLOCKED`가 됐다. 이 addendum는 해당 exact 2개 test reference를 remediation audit에만 명시적으로 허용하고, 구현에 사용되지 않았음과 변경 `0`을 재확인해 같은 MAP02_02를 종료하는 절차다.

이는 일반적 READ scope 확장이 아니며, 이미 공개된 exact incident 1건만 supersede한다.

## Mandatory Read Order

entrypoint/rules → Master → Status → original MAP02_02 Task → 이 v1.1 addendum → existing BLOCKED Result 순서로 읽는다. Current Task는 계속 exact `TASKS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS.md`여야 한다.

## Exact Remediation Read Allowlist

Original Task read allowlist에 더해 아래 2개를 incident audit용으로만 읽을 수 있다.

```text
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/ContentVersionHashCalculatorTests.cs
Assets/_Game/Tests/EditMode/Map/WorldGeneration/Data/StaticDataRegistryBuilderTests.cs
```

허용 목적:

- preflight에서 출력된 exact type-reference match의 성격 확인
- 두 파일이 EditMode test assembly에만 속하고 Runtime dependency가 아님을 확인
- current file hash/change tracker로 MAP02_02 작업 중 수정 `0` 확인
- MAP02_02 Runtime/test에 해당 test helper/code 복사·참조·dependency가 없음을 확인

이 2개 외의 새로운 non-allowlisted file 본문을 읽지 마. broad `rg`/recursive content search를 다시 실행하지 말고 exact path로만 검사한다.

## Acceptance Gate

아래를 모두 증명하면 original READ condition은 이 v1.1에서 PASS로 수용한다.

1. incident는 type reference path 확인을 위한 read-only `rg`였고, 두 파일에서 match lines만 출력됐다.
2. 두 파일은 작업 전/후 수정 `0`; matching meta도 수정 `0`이다.
3. 해당 출력은 RNG production/test 설계·코드·assertion에 사용되지 않았다.
4. MAP02_02 final Assets change는 original allowlist exact 14 files = Runtime C# 6 + test C# 1 + matching meta 7뿐이다.
5. Runtime assembly에서 test assembly/type/namespace로의 dependency는 `0`이다.
6. six known vectors, stream independence, focused/targeted/full regression, Unity compile/Console이 재실행에서 계속 PASS다.

다음 중 하나라도 확인되면 `BLOCKED`를 유지한다: match line 이외의 무승인 파일 본문을 추가로 읽음, 두 test의 변경, 코드/헬퍼 유입, test assembly dependency, Asset drift, 테스트/컴파일 실패.

## WRITE ALLOWLIST / No-Code Repair

이 remediation에서 Assets 변경은 exact `0`이다. RNG Runtime 6, test 1, 기존 test 2, 모든 meta/CSV/asmdef/Scene/Prefab/Package/ProjectSettings를 수정하지 마.

실행 단계에서 갱신 허용:

```text
MapDesign/MCP/REPORTS/MAP02_02_IMPLEMENT_DETERMINISTIC_RNG_STREAMS_RESULT.md
```

PASS 후 standard STATUS FINALIZE만 허용한다. 테스트가 실패해도 이 addendum으로 production/test를 고치지 말고 `BLOCKED`로 보고한다.

## Required Revalidation

```text
DeterministicRngStreamTests: 103/103 PASS
Required InitialState/first/second vectors: 6/6 PASS each
MAP02_01 GeneratedWorldData: 56/56 PASS
Targeted Game.Map.Tests.EditMode: 1026/1026 PASS
Full EditMode: 1046/1046 PASS
Compile error / relevant warning: 0 / 0
Console error / warning: 0 / 0
Authoring CSV/meta: 50/50 unchanged
Final Assets drift: 0 beyond original exact 14 files
Duplicate GUID groups: 0
PlayMode NOT RUN / Visual NOT APPLICABLE
```

실행 순서를 바꾸거나 focused 결과만 재사용하지 말고 focused → targeted → full → final compile/Console/change-scope 순으로 재확인한다. 테스트 skip/ignore/filter 축소, assertion 완화 금지.

## Result / Completion

existing Result를 갱신해 아래 섹션을 추가한다.

```text
REMEDIATION v1.1
READ-SCOPE INCIDENT
EXACT TWO-FILE ACCEPTANCE
NON-USE / NON-MUTATION EVIDENCE
REVALIDATION
```

Acceptance gate와 재검증이 모두 PASS면 Result를 `STATUS: PASS`로 갱신하고 MAP02_02 COMPLETE, Current Task NONE으로 finalize한다. MAP02_03은 LOCKED로 유지하고 자동 시작하지 마.

Recommended Commit은 원 Task의 `feat(map): add deterministic rng streams`를 유지한다.
