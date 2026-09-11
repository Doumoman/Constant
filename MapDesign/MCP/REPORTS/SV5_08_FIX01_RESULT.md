# SV5_08_FIX01 Result

TASK_ID: SV5_08_FIX01
STATUS: PASS

## 구현 결과

SV5_08의 연결 길이 누락을 교정했다. 기존 `ExternalCenterline`은 `HostAccess.Concat(Path).Distinct()` 뒤 새 방 소유 셀만 남겨 부모측 시작점과 자식 입구를 제외했으며, 후보 예산도 끝점을 할인했다. FIX01은 생성과 최종 validator 및 export가 `SV5_INFILL_LENGTH_RULE_V2`를 공유하도록 바꿨다.

정본 연결 중심선은 양 끝점 포함 ordered cardinal AIR 셀이다. root는 `HostAccess + Path.Skip(1)`, child는 `Path`이며, `Distinct`로 재방문을 숨기지 않는다. 빈 경로, 접합 불일치, 비-cardinal 이동, 끝점 불일치, 24셀 초과를 production 오류로 판정한다. 기존 `ExternalCenterline`은 하위 호환 소유 셀 진단으로만 유지했다. 정책과 정본 중심선은 profile/plan/link digest에 결합했다.

## 고정 맵 전수 검사

| case | new rooms/connections | new owned tiles | max inclusive cells | over 24 | endpoint errors | non-cardinal | termination |
|---|---:|---:|---:|---:|---:|---:|---|
| default | 217 | 62,806 | 24 | 0 | 0 | 0 | CANDIDATES_EXHAUSTED |
| repeat | 234 | 67,625 | 24 | 0 | 0 | 0 | CANDIDATES_EXHAUSTED |

default plan digest는 `04f47c94883ccec465d891d500841a5bc4f811d5299cae2562d4e19ca2c10d5f`, infill digest는 `fe3e1204f58e11ceccb3cd933d139e34c43b3d7e5d03156daf8187faae56e530`이다. repeat plan digest는 `06d95903a803eda03751fbbb3bd47d50da79388f740e7ca6ae8b22832795f5ea`, infill digest는 `315bd2b523d102c9fe203ab30699f80a4a5296cc631d981a1ae849718a9baea3`이다. 목표 256과의 차이 39/22는 상한을 완화하지 않고 후보 소진으로 남겼다.

동봉 `check_lengths.py`가 최종 CSV를 독립 재구성한 `length_audit.json`도 default 217개와 repeat 234개를 각각 전수 확인해 `PASS_LENGTH_CSV_ONLY`, 최대 24, 초과 0, 오류 0을 반환했다. 이 Python 결과의 `unity_verified=false`는 의도된 역할 구분이며, Unity 생산 검증은 아래 focused XML이 담당한다.

## F01~F07

- F01 `F01_InclusiveBoundaryAcceptsTwentyThreeAndTwentyFourButRejectsTwentyFiveAndTwentySix`: 23/24셀 허용, 25/26셀 production 거부 PASS.
- F02 `F02_RootNeckSidePortalAndChildCountOnlyTheirDefinedEndpoints`: root neck, side portal, child 양 끝점 정의 PASS.
- F03 `F03_CardinalExpansionJoinFailuresAndRevisitsCannotHideLength`: +1 계단 전개, 접합 오류, 대각/빈 경로, 재방문 비은폐 PASS.
- F04 `F04_RecordedTwentySixCellCounterexampleExposesTheOldOwnershipFilteredMetric`: 과거 26셀 반례가 기존 metric에서 24로 축소되며 새 validator에서 거부됨을 고정 PASS.
- F05 `F05_DefaultAndRepeatReconstructEveryActualConnectorAtTwentyFourOrLess`: 두 고정 맵의 모든 새 연결을 Path+HostAccess에서 독립 재구성해 끝점/AIR/부모 경계/24셀 상한 PASS.
- F06 `F06_ExportsBindAuthoritativeLengthPolicyCountsAndPlanDigest`: CSV/JSON의 정본 중심선·길이·정책·status와 plan digest 결합 및 결정성 PASS.
- F07 `F07_AllNinetyFivePredecessorTestsRemainDiscoverableAndEnabled`: 선행 95개 fullname이 모두 발견되고 enabled 상태임을 확인 PASS.

## Unity 시험

최종 명령:

`unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_08_FIX01/focused_results.xml --timeout 1800 --format json`

- Unity Editor: 6000.3.8f1 (revision `1c7db571dde0`)
- Unity CLI: 1.0.0-beta.9
- 발견/실행/통과: 102 / 102 / 102
- 실패/스킵/inconclusive: 0 / 0 / 0
- 기존 시험/신규 회귀: 95 / 7
- 실행 시간: 960.2594794초
- 실행 시각: 2026-09-11T18:48:37Z ~ 2026-09-11T19:04:37Z
- XML worktree raw: SHA-256 `9a1a7b4ea5325957a272ebc8b4a1eabbab4f316bbb269ee914c0869c591e2b75`, 105,643 bytes, CRLF 790개
- XML Git LF blob: SHA-256 `6fe07cef57207c0c51a62277edab34f83f04114611f00500d94ff6752f9fa009`, 104,853 bytes, OID `3f605f031b048a266ab5128c98183278959fb091`
- 최종 tested source SHA-256: `9ebbb28334ebc58177662dc9e5aa7904ab39b8c707d6635beab5d71a53638a6f`

최종 실행 전에 별도 7개 FIX01 회귀와 9개 infill integration도 각각 7/7, 9/9 PASS했다. 해당 임시 XML은 최종 focused 실행으로 대체됐고 `_work` 정리 시 제거했으며, 정확한 명령·raw SHA·bytes는 `BINDING.json`에 남겼다.

## export와 실제 셀 비교

`comparison.json`은 불변 SV5_08의 actual-cell 결과(default 213 rooms, repeat 232 rooms)와 FIX01(default 217, repeat 234)을 함께 연결한다. `preview/before_after.svg`와 `repeat_before_after.svg`는 624×416 전체 실제 셀 전후 비교이고, `preview/detail.svg`는 기존 26셀 반례 창의 1×1 격자·4×4 경계·기존 거부 경로·현재 연결을 구분한다. A1~D4 16개 확대도가 모두 존재한다. UNKNOWN 및 미배치 셀은 완성 지형으로 표시하지 않았다.

`legacy_inventory.json`은 16개 실사용/증거 항목을 읽기 전용 조사했다. ACTIVE 7, COMPAT 4, HISTORICAL 5, RETIRE_CANDIDATE 0, UNKNOWN 0이며 기록된 모든 raw SHA/bytes가 최종 파일과 일치한다. 이번 Task에서 레거시 이동·삭제는 수행하지 않았다.

## MCP 보존 및 정리

- pre-registration, exact registration diff check/apply, stage, native single_task_v1 Apply: PASS.
- installed Task와 Archive SHA-256: `7ed6ed35d87410475b4a8a6c0c9af34b0aafc8ac8e68f596fffd9fc3d74ee2c3`, byte-identical.
- Unity 최종 실행 후 post-readonly: ALWAYS 780개 mismatch 0, 선행 commit blob 143개 검증 PASS.
- 선행 commit: `953b833cb22904721f9e8687283a65d5bae64cb7`; parent `ab82efcfb3d183c995d0184db9d3dc55f2f3acc9`.
- FIX01 `_work`: 344 files / 29 directories / 432,899,127 bytes만 제거. 기록된 명령으로 재생성 가능하다.
- 기존 SV5_08 COMPLETE와 과거 증거, 무관한 dirty 변경은 보존했다.
- `SV5_09_LOOPS`는 LOCKED이며 실행하지 않았다. push하지 않았다.

## 한계

현재 결과는 실제 1×1 셀 데이터와 정적/좌표 이동 검증이다. 합성된 최종 지형 및 Player 완주 증거는 아니다.

- ComposedGeometryReady=false
- PlayerVerified=false
