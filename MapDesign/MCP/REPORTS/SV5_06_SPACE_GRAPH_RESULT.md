TASK: SV5_06_SPACE_GRAPH
STATUS: PASS

# SV5_06 공간 그래프 결과

## 수명주기와 선행 상태

- R2 패키지 manifest `335820c289e67829cd2897c15cb5623f86aeda9610e09f8b80cc5157b993fe63`를 지정 명령으로 검사해 `PASS_STAGED_ONLY`를 받았다.
- 교체 범위는 패키지의 `STAGE.py`, `SOURCE_LOCK.json`, `FILES.json`뿐이며 Task 본문 SHA는 `c8786c2a4b35702dc67e8b975b0647125b3c988cee9929baf7a86bfcd99a4925`로 유지됐다.
- 실제 선행 SV5_05_FIX01 Finalize/소유 commit은 `37c09eb8663656da182453c19487f65ea056c8f2`, parent는 `117d99a82eb0889ab0edfeda250e535c5f56c295`이다.
- Apply 전 상태는 `244 COMPLETE / 0 CURRENT / 42 LOCKED`, Current `NONE`이었다. Apply 후에는 `244 COMPLETE / 1 CURRENT / 41 LOCKED`, Current `TASKS/SV5_06_SPACE_GRAPH.md`이다.
- 입력 Task, 설치 Task, Archive는 바이트가 같고 각각 SHA-256 `c8786c2a4b35702dc67e8b975b0647125b3c988cee9929baf7a86bfcd99a4925`이다.
- Master SHA-256 `8a0d957aec361358b1f8e7b9cb9897c3af2d93c4ddaf7917c660b2ce1dc284d4`는 변하지 않아 승인된 작업 순서를 유지했다.

## 구현 결과

- 624×416 half-open, bottom-left 좌표계와 12×8 MicroChunk, 4×4 pattern index를 명시하는 `Sv5SpaceGraphPlan`을 구현했다.
- 실제 SV5_04의 핵심 8개 site, 2,432개 core cell, access/state geometry와 RMAP13/RMAP16의 11개 핵심 route identity를 보존했다.
- 대표 authoring profile로 핵심 8개, 대형 8개, 일반 6개 장소와 port 43개, 실제 connector 28개를 결정적으로 계획했다. 승인 예시의 101/166/seed 40921을 정답으로 복제하지 않았다.
- Start EXIT는 `UNUSED_WITH_REASON:RMAP16_PROTECTED_APPROACH_HAS_NO_DISTINCT_SAFE_BRANCH`로 명시했고, Start ENTRY와 Village 양방향 port를 쓰는 action-less 선택 회로를 계획했다.
- 공유 cell의 모든 서로 다른 route pair와 cardinal face의 서로 다른 route Cartesian product를 공용 열거기로 계산했다. 완전 접촉 pair는 5,347개다.
- 모든 접촉을 action-less split node로 투영하고, 분할된 모든 segment에 원래 RMAP13 predicate를 반복해 중간 진입 guard 우회를 막았다.
- 기존 `LogicalStateVerified`는 baseline/candidate FSM 의미로 유지하고 `ContactStateVerified`를 분리했다. predicate를 실제 평가하지 않은 과거 review/AIR 행의 `LogicalStateTransitionChecked` 표시는 false로 바로잡았다.
- 자원 6순서 각각에서 goal 도달과 모든 reachable state의 복귀 가능성을 같은 RMAP13 `EvaluateWithAnalysisNodes`/`ExploreWithAnalysisNodes` FSM으로 검사했다.
- 조건 gate 103개와 예약 row 26,489개를 기록했고, 미할당 237,192 tile은 `INFILL_PENDING`/SV5_08 소유로 남겼다.
- 계획 digest는 `fbbc847599b34b8b4695628d6e4970d0669938e04e35eb4b08220ef78d31cf84`이다.

## focused 검증

실행 명령:

```text
unity test . --mode EditMode --filter "StarNight.Map.Tests.EditMode.Sv5.Sv5SpaceGraphPlanTests|StarNight.Map.Tests.EditMode.Sv5.Sv5RouteStatePolicyTests|StarNight.Map.Tests.EditMode.Sv5.Sv5RouteStateFix01Tests|StarNight.Map.Tests.EditMode.Rmap13.RmapWorldGraphPlannerTests" --output "MapDesign/MCP/GENERATED/SV5_06/focused_results.xml" --no-color --non-interactive --timeout 1200
```

- Unity CLI `1.0.0-beta.9`, Editor `6000.3.8f1 (1c7db571dde0)`.
- 결과: 31 total / 31 passed / 0 failed / 0 skipped / 0 inconclusive.
- 시작 `2026-09-10T08:54:17Z`, 종료 `2026-09-10T08:55:32Z`, 74.5389726초.
- XML SHA-256 `34710c4474dc4c753ab3ea5102e1cb45672e75ec115407329c120a9b373c16bf`, 50,589 bytes.
- 구 SV5_05/FIX01 export는 `GENERATED/SV5_06/legacy_exports` 아래 두 격리 폴더에만 생성했다. 기존 SV5_05/FIX01 Task, Archive, Result, generated 바이트는 보존했다.

## 산출물과 준비도

- `space_graph.json`, 6종 CSV/JSON 증거, `validation.json`, focused XML을 동일 plan에서 출력했다.
- `preview/overview.svg`, `preview/A1.svg`~`D4.svg` 17개를 XML 파싱했고, 전체도와 B2 확대도를 실제 렌더링해 시각 점검했다.
- 계획/실제 connector 투영/접촉 상태 검증은 PASS다.
- `GEOMETRY_STATE_READY=false`, `PLAYER_VERIFIED=false`다. 실제 지형 합성, 이동 support, runtime gate, Player 검증은 후속 소유 범위이며 이번 PASS로 대체하지 않았다.
- 전체 회귀, PlayMode, build, Scene Bake, RMAP18/19, SV5_07 이후, push는 실행하지 않았다.

## Finalize

- 모든 필수 검사 PASS 후 상태를 `245 COMPLETE / 0 CURRENT / 41 LOCKED`, Current `NONE`으로 Finalize했다.
- 최종 Status SHA-256은 `cbf1f16e98c1ced26a7ce27e1e0e278e460111faa5c85b113efd243bec953e98`이다.
- atomic 소유 commit은 commit 후 review manifest 및 실행 보고에 기록한다.
