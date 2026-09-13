---
mcp_patch:
  format: single_task_v1
  task_id: SV5_11_HUB_SHELL
  task_file: TASKS/SV5_11_HUB_SHELL.md
  requires_current_task: NONE
  requires_completed_task: SV5_10_SIDEPATH
  requires_result:
    path: REPORTS/SV5_10_SIDEPATH_RESULT.md
    status: PASS
    sha256: 13d2f388a33090a1367a714d19c3316b40714cf7a1f466718a2dd54c25e4ef81
  requires_installed_task:
    path: TASKS/SV5_10_SIDEPATH.md
    sha256: d3e266c6e6bfe55d6ce2e7a1f4e832da90e416fb4587b98ec1986db717b66300
  sets_current_task: SV5_11_HUB_SHELL
---

# SV5_11_HUB_SHELL — 여섯 갈래길 shell·Port·예약 공간

NEXT: SV5_12_TREE_GRAB LOCKED / DO NOT START

624×416 월드에 여섯 갈래길의 실제 1×1 shell, 외벽, 목, 지지 공간, 좌우 3개씩의 Port와 중앙 계수나무 예약을 만든다.
계수나무의 실제 Grab/등반 지형은 SV5_12에서 구현한다. 이 Task는 새 전역 solver나 폐기된 Sector 격자를 만들지 않는다.

## 적용 전

1. 현지 MCP의 00/01/05/07/08/APPLY/Finalize 규약을 읽는다.
2. 설치된 SV5_10 Task·Archive·Result·BINDING과 활성 SV5 문서를 읽는다.
3. 동봉 CONTRACT, HUB_PROFILE, SOURCE_LOCK, PROTOCOL_APPEND를 읽는다.
4. `STAGE.py --mode check`, `--mode stage`가 순서대로 PASS한 뒤 정상 Apply한다.
5. predecessor commit `3408a0ce540f0c9c947b5f2661f68122e06137c3`이 다르면 추측 복구하지 않고 BLOCKED로 중단한다.

## Write allowlist

- 기존 최소 통합:
  - `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlan.cs`
  - `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphPlanner.cs`
  - `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpaceGraphExport.cs`
  - `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5InfillExport.cs`
  - `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/Sv5SpacePhysicalMovement.cs`
- 신규 runtime:
  - `Sv5HubShell.cs`, `Sv5HubShellExport.cs`와 matching `.meta`
- 신규 EditMode:
  - `Sv5HubShellTests.cs`와 matching `.meta`
- 문서:
  - `MapDesign/MCP/SV5/19_HUB_SHELL_V5.md`
  - `MapDesign/MCP/SV5/02_PROTOCOL_V5.md` exact append
- 증거:
  - `MapDesign/MCP/GENERATED/SV5_11_HUB_SHELL/`
  - Result, Task/Archive/Status 정상 생명주기, 동봉 INPUTS

Master, 기존 SV5_10 Task/Archive/Result/INPUTS/GENERATED, Player, Scene/Prefab, ProjectSettings,
RMAP18/19, SV5_12+ 및 무관 dirty는 읽기 전용이다. 기존 loop/sidepath를 재분류하거나 재생성하지 않는다.

## 구현

1. 기존 room, reservation, occupancy, port, gate, loop, sidepath 자료를 profile별 한 번 인덱싱한다.
2. 기존 보호영역과 충돌하지 않으며 24×40 최초 footprint를 수용할 허브 후보를 deterministic하게 열거한다.
3. 실제 내부 인지 공간 약 12×30을 확보하고 shell 외벽·목·지지 영역을 실제 1×1 셀로 기록한다.
4. 모든 변경 셀에 4×4 MicroPattern owner와 HubId를 기록한다. 추상 bounds만 만들지 않는다.
5. 좌측 3개, 우측 3개의 socket을 서로 다른 세 높이대에 만들고 각 aperture 높이를 6~7칸으로 둔다.
6. 인접 실제 외부 공간과 연결 가능한 socket만 활성 Port로 승격한다.
7. 활성 Port는 서로 다른 외부 RoomId/SpaceGroupId에 연결해야 한다. 같은 공간 중복과 미사용 socket은 세지 않는다.
8. 실제 연결 6개를 우선한다. 4~6개를 허용하고 4개 미만 후보는 재선정하거나 미배치한다.
9. 중앙 계수나무용 연속 slot과 내부 circulation 공간을 예약한다. 실제 나무 collision/Grab/발판은 만들지 않는다.
10. 기존 core/loop/sidepath/gate/Type0/Protected 셀을 침범하거나 진행 조건을 우회하는 후보를 거부한다.
11. 후보 검사는 changed-cell overlay와 국소 adjacency를 사용한다. 후보마다 전체 월드 복사·전체 BFS를 금지한다.
12. 채택 batch에 대해서만 기존 전역 topology와 9 states×6 orders를 한 번 검증한다.
13. default/repeat export, SVG, 독립 checker, targeted tests를 만든다.

## 시험 및 시간

- 개발 중에는 신규 Hub targeted만 실행한다. 전체 SV5 회귀를 반복하지 않는다.
- 신규 독립 책임 시험을 최소 14개 추가하고 기존 139개 시험 이름을 보존한다.
- 신규 시험은 shell occupancy, 4×4 owner, 12×30 내부, 24×40 footprint, 6 socket, 세 높이대,
  6~7 aperture, 4~6 distinct 연결, 미사용 socket 제외, tree 예약, 보호영역, determinism,
  no-Sector, 기존 topology/product 보존을 각각 검증한다.
- 단일 default/repeat build가 5분을 넘으면 중단하고 단계별 시간을 보고한다.
- 동일 실패를 두 번 수정해 진전이 없거나 누적 작업이 60분을 넘으면 안전한 중간 진단을 보고한다.
- final source/export가 고정된 뒤 전체 `StarNight.Map.Tests.EditMode.Sv5`를 정확히 한 번 실행한다.
- 최종 discovered>=153, 전부 PASS, failed/skipped/inconclusive=0이어야 한다.
- 전체 회귀 timeout은 2700초다. 이후 source/export 변경 시 checker와 전체 회귀를 다시 실행한다.

## 완료

- default/repeat의 후보·채택·연결 수, distinct external space, socket/Port, footprint, 단계별 시간을 Result에 기록한다.
- per-candidate whole-world copy/BFS=0, active Sector symbol/export=0을 기록한다.
- 독립 checker 결과는 `PASS_INDEPENDENT_HUB_SHELL`이어야 한다.
- `TreeGrabGeometryReady=false`, `ComposedGeometryReady=false`, `PlayerVerified=false`를 유지한다.
- BINDING은 package/task/predecessor/source/export/XML/audit SHA를 묶는다.
- Finalize 전 post-readonly → native Finalize → Finalize 후 post-readonly → task-owned atomic commit → commit 기반 Review ZIP 순서다.
- 예상 최종 상태는 293=257 COMPLETE/0 CURRENT/36 LOCKED, Current NONE, SV5_12 LOCKED다.
- Review ZIP은 `_work`, 자기 자신, 이전 GENERATED 복제를 제외하고 이번 atomic commit의 task-owned 파일만 포함한다.
- push하지 않는다.
