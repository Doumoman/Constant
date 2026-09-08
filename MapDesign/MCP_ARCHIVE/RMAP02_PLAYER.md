```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP02_PLAYER
  task_file: TASKS/RMAP02_PLAYER.md
  requires_current_task: NONE
  requires_completed_task: RMAP01_REBASE
  requires_result:
    path: REPORTS/RMAP01_REBASE_RESULT.md
    status: PASS
    sha256: 1d63e4b4812ba3e6aa8d6b398717b13f1b7b6e011d25e7fab924e402e4794b3a
  requires_installed_task:
    path: TASKS/RMAP01_REBASE.md
    sha256: 65cb0479876770d1aefabf1d48315744eba49e797433a9cf354018218cd24b8e
  sets_current_task: RMAP02_PLAYER
```

# RMAP02_PLAYER — 실제 Tilemap과 기본 Player 씬

```text
TASK: RMAP02_PLAYER
DOCUMENT: v4.2 / RMAP01 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP02_PLAYER.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP02_PLAYER_RESULT.md
NEXT: RMAP03_GRAB
NEXT STATUS: LOCKED / DO NOT START
```

위 STATUS는 정상 Apply 이후의 실행 상태다. 배포된 MD만으로 저장소 상태가 바뀐 것은 아니다.
목표 분량은 약 1~2시간의 기능 책임 단위이며, 300줄은 문서 운영 상한이다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 새 시험 씬을 열고 Play하면 기존 실제 Player로 달리기·Shift 걷기·점프·착지를 할 수 있다.
고체 벽과 천장에 충돌하고, 작은 시험 경로의 출구에 도달하며, 정확히 12×8타일 시야가 연속으로 따라온다.
주 요구 ID는 P01, P02, P03, C01, C02, E01이다. 아래에 해당 명세의 전체 구현 조건을 포함했다.
RMAP01에서 PROPOSED였던 실제 Tilemap/Collider 적용과 연속 카메라 추적을 이번에 구현한다.
기존 Player motor·입력·논리 Bake API를 먼저 재사용한다. Task 이름마다 새 Service를 만들지 않는다.
RMAP03 Grab, RMAP04 등반/일방향 발판, RMAP05 낙하 피해, RMAP06 Tab 관찰은 후속 책임이다.
RMAP07~11 역할형 후보/Seed 조립, RMAP12~19 전체 월드와 기믹·몬스터·경제·NET도 이번 구현에 넣지 않는다.

## 2. Preflight / 정상 single_task_v1 적용

1. Unity 프로젝트 루트와 적용 AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽는다.
2. MCP의 Apply·ChangeControl·Finalize 및 RMAP/02_PROTOCOL_V4_2.md를 읽고 위 필드·경로 규격을 대조한다.
3. 위 메타데이터 내부 경로는 MapDesign/MCP 기준이다. 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 최초 적용은 Current NONE, RMAP01 COMPLETE, RMAP02~19 LOCKED, 미적용 inbox 후보 본 MD 1개를 확인한다.
5. 기준 상태는 240행 = 222 COMPLETE / 0 CURRENT / 18 LOCKED다. 실제 변경 이력이 있으면 근거를 확인한다.
6. RMAP01 설치 Task·Archive의 실제 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 위 expected 값과 대조한다.
7. Task/Result가 committed 버전과 일치하는지 확인하고 현재 branch/HEAD 및 관련 dirty/staged 경로를 기록한다.
8. RMAP01 Result의 f4da626…는 작업 전 기준 HEAD다. 후속 HEAD로 강제하지 않고 Result 경로의 git log에서 commit을 확인한다.
9. expected Result SHA는 사용자 첨부 원본에서 계산했고, Task SHA는 원본과 RMAP01 보고값이 일치했다. 현지 committed 검사는 아직 필요하다.
10. SHA 불일치를 자동 수정·줄바꿈 재저장으로 통과시키지 않는다. 실제 bytes·Git 속성·차이를 보고한다.
11. RMAP01에서 기록한 과거 Last Completed RUN01 블록은 기존 stale 증거다. 이미 수용된 동일 항목을 새 충돌로 재발급하지 않는다.
12. 정상 Apply로 RMAP02만 LOCKED→CURRENT, Current NONE→RMAP02로 열고 Task를 바이트 동일하게 설치·Archive한다.
13. 다른 CURRENT, 선행 내용 변경, 서로 다른 Task/Archive, 미등록 ID/미지원 형식은 적용 전 중단한다. 적용기 규칙을 수정하지 않는다.
14. 같은 RMAP02 CURRENT 재개는 설치 Task·Archive·입력 동일성과 기존 결과를 확인한 뒤 현지 재개 규칙을 따른다.
15. 이미 RMAP02 COMPLETE+유효 PASS이면 기존 결과를 보고하고 STOP한다. 다음 Task를 자동으로 열지 않는다.
16. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 경로와 이유를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md.
- MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md, 06_IMPLEMENTATION_STATUS.md 및 현지 운영 규칙.
- RMAP01 Task/Archive/Result, MCP/GENERATED/RMAP01/file_bindings.csv와 필요한 inventory/dependency/reuse 행.
- 아래 표의 기존 파일과 직접 참조하는 입력 snapshot/Action asset, 이동 설정/프로필, Tile 정의, 좌표/Bake 모델.
- 관련 asmdef/asmref, 기존 Player 시험 씬/Prefab, 해당 EditMode/PlayMode fixture, 필요한 Physics2D/입력 설정.
- RMAP01에서 확인한 GeneratedTraversalProfile·GeneratedTileMovementGraphBuilder·GeneratedCompletionSearch의 직접 계약.
긴 파일은 rg로 심볼을 찾고 300줄 이하 구간으로 나눠 읽는다. 과거 전체 Task/CSV/코드 전수 재감사는 하지 않는다.

| 경로 | RMAP01 상태 | 이번 책임 |
|---|---|---|
| Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab | EXISTING | 실제 Player 기준, 씬 인스턴스 또는 Variant로 재사용 |
| Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRig.cs | EXISTING | Rigidbody2D/Collider·입력·이동 조합 연결 |
| Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs | EXISTING | ConsumeFixedSnapshot 기반 Move/Jump/걷기 입력 연결 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs | EXISTING | 기존 fixed-step motor를 v4.2 기본 이동에 ADAPT |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs | EXISTING, 위치 현지 대조 | 이동 수치·SolidLayers의 단일 설정 소유 |
| Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedTilemapLayerBaker.cs | EXISTING | Bake(GeneratedTilemapBakeRequest) 논리 결과 소비 |
| Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedUnityTilemapApplier.cs | PROPOSED | 검증된 Bake plan을 실제 타일·충돌체에 적용 |
| Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraRoomDriver.cs | EXISTING | 기존 Room snap 보존, 새 씬의 추적과 동시 구동 금지 |
| Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraFollowDriver.cs | PROPOSED | 연속 추적·12×8 viewport·월드 clamp |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP02/MoonPalacePlayerTilemapRun_RMAP02.unity | PROPOSED | 독립된 물리 Player 시험 씬 |
| Assets/_Game/Tests/EditMode/Map/RMAP02/GeneratedUnityTilemapApplierTests.cs | PROPOSED | 실제 Tilemap 적용의 필요한 focused 검사 |
| Assets/_Game/Tests/PlayMode/Character/RMAP02/GeneratedTilemapPlayerRunPlayModeTests.cs | PROPOSED | 실제 입력·물리·카메라·출구 확인 |

EXISTING은 RMAP01 조사 결과이며 현지 존재 여부를 확인한다. PROPOSED는 아직 존재한다고 주장하지 않는다.
설정/입력 데이터의 정확한 정의 경로는 RMAP01 바인딩과 직접 참조에서 확인해 Result에 적는다. 동명 파일을 새로 복제하지 않는다.

## 4. Write Allowlist / 변경 경계

- 위 기존 Live Rig/Input/Movement/Settings의 이번 요구에 필요한 수정, 새 TilemapApplier/CameraFollowDriver.
- 직접 참조하는 기존 입력 snapshot/Action asset·이동 프로필은 걷기·점프·공유 설정 배선에 필요한 필드만 수정 가능하다.
- GeneratedTraversalProfile와의 설정 변환은 확인된 기존 연결점에 최소 적용한다. 검증기/생성기 알고리즘은 재작성하지 않는다.
- 논리 Baker는 원칙적으로 소비한다. 실제 plan 소비에 필요한 좁은 API 보완만 근거와 함께 허용한다.
- 위 RMAP02 Scene, Assets/_Game/Live/Prefabs/RMAP02/의 필요 Variant·설정 asset, 해당 씬용 Tile asset.
- 초기화 코드가 실제 필요하면 Assets/_Game/Live/Runtime/Player/CharacterLiveMapRunBootstrap.cs를 PROPOSED 후보로 사용한다.
- 씬 재현 도구가 필요하면 Assets/_Game/Live/Editor/RMAP02/CharacterLiveMapRunSceneBuilder.cs를 PROPOSED 후보로 사용한다.
- 위 테스트 경로, 해당 기존 test assembly의 RMAP02 전용 fixture 및 직접 영향받은 기존 동작 테스트의 최소 수정.
- 위 허용 asset과 신규 폴더의 Unity 생성 .meta. 기존 GUID와 무관한 Scene/Prefab은 보존한다.
- asmdef/asmref는 기존 Game.Map.Tests.EditMode, Game.Character.Live.PlayMode.Tests 및 실제 Live/Map assembly를 우선 사용한다.
- 컴파일에 필요한 참조가 없을 때만 직접 소유 assembly의 최소 reference를 추가하고 순환/Editor→Runtime 역참조를 만들지 않는다.
- MapDesign/MCP/GENERATED/RMAP02/의 검증 증거, 지정 Task/Archive/Result와 정상 Apply/Finalize가 요구하는 상태 기록.
신규 코드 후보는 의무 파일 수가 아니다. 기존 조합 코드가 있으면 재사용하고 실제 추가/변경 경로와 책임을 보고한다.
공용 Player prefab은 우선 인스턴스/Variant로 튜닝한다. 공용 수정이 필요하면 참조 씬 영향과 변경 이유를 확인한다.
전역 Physics2D/Time/Input/Layer 설정 변경은 우선 피한다. 기존 layer/국소 설정으로 불가능하면 필요한 변경만 근거와 함께 적용한다.
패키지 추가·업데이트, 전체 설정 재작성, 운영 프로토콜/과거 Task·Result 변경, build 설정 변경은 범위 밖이다.

## 5. P01 / Collider·기본 입력·공중 제어

- 타일은 1×1 월드 단위다. 실제 Collider 크기는 0.4×0.8타일, Pivot은 발바닥 중앙으로 맞춘다.
- 기존 Rigidbody2D/CapsuleCollider2D 구성을 재사용한다. root 발바닥 기준 offset과 실측 bounds를 기록한다.
- 기본 좌우 이동은 달리기, Shift 유지 시 걷기다. 기존 Input System 소유권과 ConsumeFixedSnapshot 흐름을 유지한다.
- 입력 방향으로 직관적으로 전환하며 지상과 유사하게 자유로운 좌우 공중 제어를 제공한다.
- 급반전 브레이크 상태·브레이크 애니메이션을 새로 만들지 않는다. 기존 가감속은 목표에 맞춰 최소 조정한다.
- 1타일 계단은 자동 상승하지 않고 직접 점프해야 한다. 1타일 높이 통로는 기어가기 상태 없이 통과할 수 있다.
- 실제 벽/천장/바닥 충돌을 사용한다. collider를 끄거나 transform 순간 이동으로 장애물을 통과시키지 않는다.
- 실제 키 배치와 Game View 입력 포커스를 Result의 사용자 조작 안내에 명시한다.

## 6. P02 / 기본 점프와 측정 기준

| 점프 | 높이 | 수평 거리 목표 |
|---|---|---|
| 정지 | 약 1.3타일 | 좌우 입력이 없으면 거의 수직 |
| 걷기 | 약 1.3타일 | 약 2타일 |
| 달리기 | 약 1.3타일 | 최대 약 4타일 |

- 출발/착지 타일 중심 간 거리로 비교하며, 같은 높이의 발판과 최대 점프 유지 입력을 사용한다.
- 기존 root/발바닥 좌표로 높이를 재고, 타일 중심 간 구간 시험과 실제 이륙/착지 변위는 구분해 함께 기록한다.
- 수평 튜닝 비교는 평지에서 해당 이동 속도에 도달한 뒤 시행하고 입력·fixedDeltaTime·측정 오차 조건을 공개한다.
- 높은 곳에서 낙하하며 이동한 전체 수평 거리를 4타일로 잘라내지 않는다. 공중 제어를 막아 거리 목표를 맞추지 않는다.
- 약이라는 목표를 숨은 판정 상수로 바꾸지 않는다. 허용오차와 실측치를 Result에 적고 실패 후 편의상 넓히지 않는다.

## 7. P03 / 가변 점프와 입력 유예

- 버튼을 일찍 놓으면 낮게, 유지하면 최대 높이로 뛰는 가변 점프를 구현한다.
- Coyote Time 초기값 0.08초, Jump Buffer 초기값 0.10초를 동일 이동 설정에서 소비한다.
- 최소 높이·점프 컷 곡선·속도/가속/감속은 기존 구현과 P02 목표를 기준으로 튜닝한다.
- 유예 구간 안/밖, 착지 직전 buffer, 버튼 유지에 의한 의도하지 않은 재점프를 실제 fixed-step 입력으로 확인한다.
- Player와 생성 검증기에 서로 다른 이동 상수를 복제하지 않는다. 기존 공용 모델/변환이 있으면 재사용한다.
- 기존 검증기의 표현 범위가 다르면 이번 기본 이동의 설정 연결과 남은 차이를 적고, 후속 RMAP09 이동 검증 완료로 주장하지 않는다.

## 8. 실제 Tilemap/Collider Bake와 E01 시험 씬

1. 첫 시험 맵은 60×40(가로 5·세로 5 MicroChunk)으로 시작한다. 가로 12배수·세로 8배수의 양의 크기로 변경 가능하다.
2. 60×24/5×3/15화면 고정은 최신 합의로 대체됐다. 카메라 시야 12×8과 Full Run 624×416 목표는 유지한다.
3. 후속 30타일 낙하 시험을 수용할 공간을 확보한다. 낙하 피해·경직·사망은 RMAP05 책임으로 남긴다.
4. 이번 지형은 명시적으로 직접 배치한 fixture다. Seed 생성기·역할형 500개 풀·Full Run 승인으로 포장하지 않는다.
5. RMAP01의 GeneratedCellPlacementPlan/Bake request 연결점을 재사용해 논리 일곱 layer와 셀 좌표를 유지한다.
6. GeneratedUnityTilemapApplier는 검증된 plan을 받아 Tilemap.SetTiles 등 실제 타일 쓰기를 수행한다.
7. 고체 layer에 실제 TilemapCollider2D와 필요한 CompositeCollider2D/Rigidbody2D를 구성하고 Player SolidLayers와 연결한다.
8. 장식/배경/마커 layer에 고체 충돌을 잘못 부여하지 않는다. Tile ColliderType·layer mask·trigger·scale을 확인한다.
9. 현재 Unity 버전의 API로 collider 갱신 완료 후 Player를 활성화한다. 첫 fixed step에 바닥이 없어 낙하하는 경합을 막는다.
10. Spawn은 발바닥과 지면에 맞추고 Collider 중첩을 확인한다. 잘못된 Spawn을 광범위 굴착으로 수리하지 않는다.
11. 같은 씬의 재실행/재적용에서 이전 tile·collider·Player·카메라 구동체가 누적되지 않도록 이번 소유 객체만 정리한다.
12. Map 쪽 plan/적용기는 특정 Player/Camera 인스턴스에 의존하지 않는다. Live 쪽 조합 계층에서 연결한다.

| 시험 지형 | 구체적인 관찰 |
|---|---|
| 넉넉한 평지와 Start | 달리기/걷기 차이, 정지/반전, 0.4×0.8 Collider |
| 1타일 높이 계단 | 직진만 하면 막힘, 수동 점프하면 올라감 |
| 높이 1타일의 수평 통로 | 서 있는 동일 Collider로 통과, 천장/모서리 걸림 확인 |
| 같은 높이의 점프 발판 | 타일 중심 간 2/4타일 거리, 걷기/달리기 최대 유지 입력 비교 |
| 벽과 낮은 천장 | 실제 충돌, 천장에 맞으면 상승 중단, 자동 관통 없음 |
| x=12/24… 및 y=8/16… 경계 인근 | 이동 중 연속 카메라, 방 전환/강제 snap 없음 |
| 월드 가장자리와 출구 | 전체 시야 clamp, 실제 Player가 연결된 경로로 출구 도달 |

발판과 경로의 실제 셀 좌표·Spawn/Exit·맵 크기를 증거에 남긴다. 표는 필수 관찰 지형이며 새 방 계층이 아니다.
막힌 시험 지형은 실패로 기록하고 저작한 fixture의 해당 부분만 수정한다. 런타임 자동 굴착/무한 reroll을 넣지 않는다.
출구 도달은 기존 trigger/판정 접점을 재사용하거나 작은 확인 지점으로 기록한다. 캠페인/저장/씬 전환 시스템은 만들지 않는다.

## 9. C01/C02 / 정확한 시야·연속 추적·Clamp

- 월드가 보이는 유효 viewport는 정확히 가로 12·세로 8타일이다. 넓은 화면에서 추가 월드 타일을 보여주지 않는다.
- 직교 카메라 기준 orthographicSize=4, 유효 aspect=12/8을 사용하거나 같은 월드 투영 범위를 보장한다.
- 남는 영역은 Letterbox/단색 프레임/UI 영역으로 처리한다. 추가 미술 제작이나 새 UI 시스템은 필요하지 않다.
- 해상도/화면비가 바뀌면 viewport를 갱신한다. 최소 3:2, 16:9, 20:9에서 실제 월드 표시 범위를 확인한다.
- Player body를 연속 추적하며 12×8 MicroChunk 경계에서 화면을 전환하지 않는다.
- 카메라 중심은 월드 실제 원점/크기 기준으로 x=[minX+6,maxX-6], y=[minY+4,maxY-4] 안에 둔다.
- 보간/추적 처리 후 최종 위치를 clamp해 전체 시야가 월드 내부에 남게 한다. 12×8 최소 크기는 중심이 고정된다.
- 기존 CameraRoomDriver의 MoveToRoom은 새 씬에서 추적과 경쟁하지 않게 배선하고 과거 Preview 동작을 보존한다.
- Input/Camera는 로컬 Player 계층 책임이다. 카메라 위치를 월드 생성 입력·저장 상태로 사용하지 않는다.

## 10. 구현 순서 / 한 작업 안에서 완료

1. 정상 Apply와 좁은 코드 확인 후 기존 motor/입력/설정의 재사용 위치를 정한다.
2. 검증된 논리 plan→실제 Tilemap/Collider 적용→안전한 Player spawn을 먼저 연결한다.
3. 기존 motor를 P01~P03에 맞추고 fixture에서 실제 입력·충돌·점프를 조정한다.
4. 연속 CameraFollow/viewport/clamp를 연결하고 직접 열 수 있는 RMAP02 씬을 저장한다.
5. 다음 focused 확인과 실제 Player 출구 통과 증거를 확보하고 필요한 최소 실패만 수정한다.
6. 사용자 조작 안내·파일별 책임·요구 ID별 증거를 Result에 쓰고 PASS일 때만 Finalize/commit한다.

## 11. Focused Checks / 실제 실행 증거

필요한 새 테스트만 기존 assembly에 둔다. 테스트 수를 고정하거나 문서/메타데이터 숫자를 검사하는 테스트를 추가하지 않는다.
- EditMode: 논리 셀→실제 타일 좌표/고체 layer 적용, 비고체 layer 분리, 같은 소유 대상 재적용의 stale tile 제거를 확인한다.
- PlayMode: 실제 Rigidbody2D/Collider/Tilemap을 사용해 이동·벽/천장 충돌·계단·1타일 통로·착지·출구를 확인한다.
- P02/P03: 정지/걷기/달리기 점프, hold/release 높이, 공중 방향 전환, Coyote/Buffer 안/밖을 측정한다.
- C01/C02: 화면비별 실제 월드 범위, 경계 통과 중 연속 추적, 월드 모서리 clamp를 확인한다.
- 자동 입력은 기존 Input System→snapshot→motor 흐름을 통과시킨다. transform 이동/teleport/검사 마커로 통과를 대신하지 않는다.
- 초기 배치 외에는 실제 이동을 우회하지 않는다. 측정용 직접 API 검사는 사용자 키 입력 배선의 증거와 분리한다.
- 새 RMAP02 category 또는 명시한 test 이름만 선택하고 filter·job ID·실제 discovered/executed/pass/fail을 기록한다.
- Unity에서 실제 씬을 열고 Play한 확인과 Game View 증거를 남긴다. 스크린샷 한 장만으로 이동 성공을 판정하지 않는다.
- 기존 소스 수정에 따른 회귀는 실제 영향받은 동작/재현된 문제의 최소 범위만 확인하고 이유를 적는다.
- 기존 전체 회귀·legacy 19347·full/unfiltered test·테스트 수 채우기·불필요한 Player build는 실행하지 않는다.
- Scene·Prefab·설정 연결이 포함되므로 필요한 Unity refresh/compile은 수행한다. 새 compile/runtime 오류는 해결하거나 실패로 보고한다.
Unity 도구 부재/미실행은 NOT RUN 또는 BLOCKED로 기록한다. 논리 Bake/BFS/Preview/과거 PASS를 실제 물리 증거로 대체하지 않는다.

## 12. Required Outputs / Result

- 저장된 MoonPalacePlayerTilemapRun_RMAP02.unity와 필요한 실제 Player/Tilemap/Collider/Camera 연결.
- MapDesign/MCP/GENERATED/RMAP02/에는 필요한 입력·측정·씬 확인 증거만 저장한다. 별도 대규모 갤러리는 만들지 않는다.
- replay/측정 표에는 맵 크기·좌표·입력 조건·설정 값·허용오차·실측치·판정을 담고 원본 로그/스크린샷 경로를 연결한다.
- MapDesign/MCP/REPORTS/RMAP02_PLAYER_RESULT.md 첫 두 섹션은 한국어 사용자 구현 보고와 파일별 책임 표다.
- Result에 다음 항목을 포함한다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.

```text
TASK: RMAP02_PLAYER
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 이제 가능한 조작, 씬 여는 경로, 키 배치, 관찰 방법
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: 현재 HEAD/branch, 선행 Task/Archive/Result SHA와 commit, 적용 전후 상태
CHANGED: 재사용/ADAPT/신규 판단, 공개 API/설정/Prefab/Scene/assembly 변경 이유
REQUIREMENT EVIDENCE: P01/P02/P03/C01/C02/E01 각각 산출물·실측·판정
VALIDATION: 실제 filter/job/count, Play 확인, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 씬·Spawn/Exit·Tilemap/Collider·카메라 증거, 화면비별 결과
OUT-OF-SCOPE FINDINGS: Grab/등반/피해/관찰/자동 생성/Full Run의 남은 책임
RMAP03 BINDINGS: 확인된 Grab/접촉/상태/설정/fixture 경로와 API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 메타데이터 필드/경로 규칙, 설치 Task의 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, Current/상태, Task와 Archive bytes 동일 여부
NEXT: RMAP03_GRAB LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 작업 commit 조회 경로; 실제 생성 SHA는 CLI 최종 보고
```

## 13. PASS / 실패 / Finalize / STOP

P01~P03/C01~C02/E01 구현과 실제 Player·물리·시야의 필수 확인이 모두 있어야 PASS다.
상태/선행 체인 충돌은 STATUS_CONFLICT, 필수 자료·도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
RMAP01에서 이미 PROPOSED로 정한 기능 부재와 필요한 ADAPT는 정상 작업 대상이다. 반복 감사만 하고 종료하지 않는다.
확인된 필수 경로 단절·충돌 실패를 검증 완화/침묵 수리로 숨기지 않는다. 낮은 천장 등 시험 난도 자체는 허용한다.
PASS Result 후 기존 Finalize로 RMAP02 CURRENT→COMPLETE, Current→NONE만 수행한다. RMAP03~19는 LOCKED다.
기준선에서 상태는 적용 후 222 COMPLETE/1 CURRENT/17 LOCKED, 완료 후 223 COMPLETE/0 CURRENT/17 LOCKED다.
Task·Archive·Result·허용된 코드/asset/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP02 connect physical tilemap player and continuous camera
자기 commit SHA를 Result 안에 넣으려고 amend/추가 commit을 반복하지 않는다. Result 경로의 git log로 연결한다.
최종 CLI 보고에는 Result 경로, 실제 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP03 LOCKED를 적는다.
Git push와 RMAP03 실행 없이 STOP한다.
