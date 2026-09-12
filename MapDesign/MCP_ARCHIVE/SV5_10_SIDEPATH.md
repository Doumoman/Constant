---
mcp_patch:
  format: single_task_v1
  task_id: SV5_10_SIDEPATH
  task_file: TASKS/SV5_10_SIDEPATH.md
  requires_current_task: NONE
  requires_completed_task: SV5_09_FIX01
  requires_result:
    path: REPORTS/SV5_09_FIX01_RESULT.md
    status: PASS
    sha256: 48d2ca855e3ba25f71e6df0f2c98102039f29b7627736a0c0cf6905a4f38956b
  requires_installed_task:
    path: TASKS/SV5_09_FIX01.md
    sha256: 1497392f93d1d3bdce56ab63166d3ff2855b77d869cb2e5cb914249d26bb97fb
  sets_current_task: SV5_10_SIDEPATH
---

# SV5_10_SIDEPATH — Sector-local 불규칙 2칸 샛길

NEXT: SV5_11_HUB_SHELL LOCKED / DO NOT START

FIX01이 확정한 실제 occupancy·RoomId·supported-foot graph 위에 20~50 AIR 중심선 셀의 불규칙한 샛길을 만든다.
이 Task는 624×416 전체 후보를 매번 다시 훑는 새 전역 solver를 만들지 않는다. sector-local 후보 인덱스와 국소 overlay를
사용하고, 최종 선택 batch만 전역 진행 상태로 검증한다.

## 적용 전 읽기와 선행

현지 MCP 00/01/05/07/08/APPLY/Finalize, SV5 활성 규칙·계획, 설치된 09/FIX01 Task·Result·Binding·활성 문서를 읽는다.
동봉 CONTRACT, SIDEPATH_PROFILE, SOURCE_LOCK, PROTOCOL_APPEND를 읽고 STAGE의 package/check/stage를 통과한 뒤 정상 Apply한다.
FIX01 commit `f6fef1c4b258b7b45ca17ea9d5ffc1f1be81b76f`과 Result/Task hash가 일치하지 않으면 추측 복구하지 말고 BLOCKED로 중단한다.

## Write allowlist

- 기존 SectorPlanning 최소 통합: `Sv5SpaceGraphPlan.cs`, `Sv5SpaceGraphPlanner.cs`, `Sv5SpaceGraphExport.cs`,
  `Sv5InfillExport.cs`, `Sv5SpacePhysicalMovement.cs`, `Sv5SpaceLoops.cs`, `Sv5LoopTopology.cs`, `Sv5LoopExport.cs`.
- 신규: `Sv5SpaceSidepaths.cs`, `Sv5SidepathSpatialIndex.cs`, `Sv5SidepathExport.cs`와 matching `.meta`.
- 신규 EditMode: `Sv5SpaceSidepathTests.cs`와 matching `.meta`.
- `MapDesign/MCP/SV5/18_SIDEPATHS_V5.md`, `02_PROTOCOL_V5.md` exact append.
- `GENERATED/SV5_10_SIDEPATH/`, Result, 정상 Task/Archive/Status 생명주기, 동봉 INPUTS.

Master와 `GENERATED/SV5_09_FIX01`, 과거 Task/Archive/Result/INPUTS/GENERATED, Player, Scene/Prefab, Packages,
ProjectSettings, RMAP18/19, SV5_11+ 및 무관 dirty는 읽기 전용이다. 기존 FIX01 loop를 다시 분류·재생성하지 않는다.

## 구현 순서

1. FIX01 final occupancy·RoomId·supported foot context를 profile별 한 번 로드한다.
2. 48×32 sector별 실제 room boundary endpoint와 사용 가능 셀을 인덱싱한다.
3. endpoint Manhattan 하한 49 이하가 가능한 sector pair만 비교한다. normalized sector/endpoint pair는 한 번만 평가한다.
4. ordered centerline을 실제 AIR 셀 20~50개로 만든다. 길이에는 support/aperture/approach를 넣지 않는다.
5. 각 중심선 셀의 AIR·머리 여유를 검사하고, `SUPPORTED_FOOT` 착지 셀의 SOLID 지지와 +1 step 전환을 검사한다.
6. 비대칭 굴곡과 짧은 평탄·상승·작은 공동을 섞고 반복 sawtooth·긴 수직관·무단 self-cross를 거부한다.
7. 변경 셀 overlay와 주변 foot node만 검사한다. 후보마다 월드 복사·전체 BFS를 하지 않는다.
8. Protected/Type0/core/gate/reservation/기존 connection을 침범하거나 진행 조건을 우회하는 후보를 거부한다.
9. 재합류와 복귀 가능한 말단을 별도 분류한다. 비용 감소 때만 RANDOM_SHORTCUT 보조 분류를 붙인다.
10. profile 운영 minimum을 만족하도록 deterministic packing한다. 불가능하면 수치를 완화하지 말고 rejection을 보고한다.
11. 최종 batch에 전역 topology와 9×6 product를 한 번 적용한다. 모든 기존 핵심 정상 경로를 보존한다.
12. export 후 동봉 독립 checker와 targeted tests를 통과시킨다.

## 시간·시험 규칙

- 개발 중에는 신규 sidepath targeted만 실행한다. 기존 125 전체 회귀를 반복하지 않는다.
- 단일 default/repeat production build가 5분을 넘으면 안전하게 중단하고 단계별 시간을 보고한다.
- 동일 실패를 두 번 수정했는데 진전이 없거나 누적 작업이 60분을 넘으면 중간 진단을 보고하고 기다린다.
- 최종 소스·export 고정 뒤 전체 `StarNight.Map.Tests.EditMode.Sv5`를 정확히 한 번 실행한다.
- 기존 125개 이름을 모두 보존하고 신규 독립 책임 시험을 최소 13개 추가한다. 최종 discovered>=138, 전부 PASS, skip=0.
- 전체 회귀 timeout은 2700초다. 이후 소스/export가 바뀌면 checker와 전체 회귀를 최종 상태에서 다시 실행한다.

## 완료와 인계

default/repeat 각각 accepted>=8, returning>=5, sectors>=6, 길이 20~50, bypass/protected/type0 위반 0을 기록한다.
sector index의 비교 pair 수, 중복 0, candidate 수, 단계별 시간, whole-world copy/BFS per candidate=0을 Result에 남긴다.
독립 checker는 `PASS_INDEPENDENT_SIDEPATH`여야 한다. BINDING은 최종 source/export/XML/audit SHA를 묶는다.

Finalize 전 post-readonly, native Finalize, Finalize 후 post-readonly, task-owned atomic commit, commit 기반 Review ZIP 순서다.
최종 상태는 293=256 COMPLETE/0 CURRENT/37 LOCKED, Current NONE, SV5_11 LOCKED다. push하지 않는다.
Review ZIP은 `_work`, 자기 자신, 이전 GENERATED 복제를 제외하고 이번 atomic commit의 task-owned 파일만 포함한다.
일반 코드/테스트 실패는 수정 대상으로 처리한다. source/state/hash/권한 충돌만 BLOCKED로 종료한다.
