```yaml
mcp_patch:
  format: single_task_v1
  task_id: RMAP03_GRAB
  task_file: TASKS/RMAP03_GRAB.md
  requires_current_task: NONE
  requires_completed_task: RMAP02_PLAYER
  requires_result:
    path: REPORTS/RMAP02_PLAYER_RESULT.md
    status: PASS
    sha256: 3c277b7973ef3afac4c5e2ed28138dc7446798f8b4818123a0d32a5f08ee5443
  requires_installed_task:
    path: TASKS/RMAP02_PLAYER.md
    sha256: 3a8b6602abc775bd18cef93069f32185c6b582ea7a592cd8b54a95462cca61eb
  sets_current_task: RMAP03_GRAB
```

# RMAP03_GRAB - 모서리 Grab과 움직이는 안전 고체

```text
TASK: RMAP03_GRAB
DOCUMENT: v4.2 / RMAP02 PASS 이후 실행 지시서 / 2026-09-08
STATUS: CURRENT
INPUT: MapDesign/MCP_INBOX/RMAP03_GRAB.md
EXPECTED_RESULT: MapDesign/MCP/REPORTS/RMAP03_GRAB_RESULT.md
NEXT: RMAP04_CLIMB
NEXT STATUS: LOCKED / DO NOT START
```

위 STATUS는 정상 Apply 이후의 실행 상태다. 배포된 MD만으로 저장소 상태가 바뀐 것은 아니다.
목표 분량은 약 1~2시간의 기능 책임 단위이며, 300줄은 문서 운영 상한이다.

## 1. User-Facing Goal / 이번 작업의 완료 모습

Unity에서 RMAP02 기반 시험 씬을 열고 Play하면 실제 Player가 안전한 타일 모서리를 잡고 매달릴 수 있다.
정적 고체, 파괴 가능 고체로 분류되는 안전 고체, 움직이는 안전 고체의 모서리를 잡고 유지하며, 점프로 이탈한다.
상승 중, Down 입력 중, 일방향 발판, 피해 표면, 압착/위험 상태, 비고체 장식은 Grab하지 않는다.
Grab 중 좌우 입력은 새 벽차기 상태를 만들지 않고 기존 공중 제어로 이어진다.
주 요구 ID는 P05, P06, P07, P08, P10이다. 아래에 해당 명세의 전체 구현 조건을 포함했다.
RMAP04 등반/사다리/폴/one-way 통과, RMAP05 낙하 피해/경직/사망, RMAP06 관찰 모드는 후속 책임이다.
RMAP07 이후 생성기/월드 조립/저장/Full Run도 이번 구현에 넣지 않는다.

## 2. Preflight / 정상 single_task_v1 적용

1. Unity 프로젝트 루트와 적용 AGENTS.md, MapDesign/MCP/00_MCP_ENTRYPOINT.md부터 읽는다.
2. MCP의 Apply, ChangeControl, Finalize 및 RMAP/02_PROTOCOL_V4_2.md를 읽고 위 필드와 경로 규격을 대조한다.
3. 위 메타데이터 내부 경로는 MapDesign/MCP 기준이다. 본문 Assets/MapDesign 경로는 프로젝트 루트 기준이다.
4. 최초 적용은 Current NONE, RMAP01/RMAP02 COMPLETE, RMAP03~19 LOCKED, 미적용 inbox 후보 본 MD 1개를 확인한다.
5. 기준 상태는 240행 = 223 COMPLETE / 0 CURRENT / 17 LOCKED다. 실제 변경 이력이 있으면 근거를 확인한다.
6. RMAP02 설치 Task/Archive의 실제 bytes/SHA와 PASS Result의 TASK/STATUS/SHA를 위 expected 값과 대조한다.
7. RMAP02 Result의 COMMIT 항목은 작업 전 HEAD와 CLI 최종 보고 경로를 적은 보고 형식이다. Result 경로의 git log로 실제 작업 commit을 확인한다.
8. expected Result SHA는 사용자 첨부 원본에서 계산했다. 현지 committed Result bytes와 다르면 자동 재저장으로 통과시키지 않는다.
9. RMAP03 planned 원본 SHA는 `edcb1e3f6e75d47a9be90d2747ffe03e3a9ea5576e899abb0d690c6860d3947c`이며, 이 MD는 선행 증거와 RMAP02 바인딩을 반영한 실행 지시서다.
10. 정상 Apply로 RMAP03만 LOCKED->CURRENT, Current NONE->RMAP03_GRAB으로 열고 Task를 바이트 동일하게 설치/Archive한다.
11. 다른 CURRENT, 선행 내용 변경, 서로 다른 Task/Archive, 미등록 ID/미지원 형식은 적용 전 중단한다. 적용기 규칙을 수정하지 않는다.
12. 같은 RMAP03 CURRENT 재개는 설치 Task/Archive/입력 동일성과 기존 결과를 확인한 뒤 현지 재개 규칙을 따른다.
13. 이미 RMAP03 COMPLETE+유효 PASS이면 기존 결과를 보고하고 STOP한다. 다음 Task를 자동으로 열지 않는다.
14. 무관한 변경은 보존한다. 관련 변경을 분리할 수 없으면 경로와 이유를 보고하며 reset/stash/강제 덮어쓰기를 하지 않는다.

## 3. Read Allowlist / 실제 연결점

- MapDesign/MCP/RMAP/{00_BASELINE_V4_2,01_SEQUENCE_V4_2,02_PROTOCOL_V4_2}.md.
- MapDesign/MCP/MASTER_IMPLEMENTATION_TASK_LIST.md, 06_IMPLEMENTATION_STATUS.md 및 현지 운영 규칙.
- RMAP02 Task/Archive/Result, MCP/GENERATED/RMAP02의 fixture manifest, 측정 로그, PlayMode/EditMode 결과.
- RMAP01의 file_bindings.csv에서 PLAYER_MOVE/PROFILE/SURFACE/SCENE 관련 행.
- RMAP02가 추가/수정한 실제 Player, Input, Movement, Tilemap, Camera, Scene, Test 파일과 직접 참조.
- 관련 asmdef/asmref, 실제 Input Actions, Rigidbody2D/CapsuleCollider2D 설정, LayerMask/Physics2D 접촉 설정.
- 기존 hazard/surface/destructible/moving-solid 표현이 있으면 해당 모델과 시험 fixture. 없으면 최소 fixture만 둔다.

긴 파일은 rg로 심볼을 찾고 필요한 구간으로 나눠 읽는다. 과거 전체 Task/CSV/코드 전수 재감사는 하지 않는다.

| 경로 | RMAP02 상태 | 이번 책임 |
|---|---|---|
| Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab | REUSE | 공용 본체 보존, 씬 인스턴스/Variant 기준 Grab 확인 |
| Assets/_Game/Live/Input/CharacterLiveControls.inputactions | ADAPT | Down/Grab 억제에 필요한 기존 Move/Action 입력 확인 |
| Assets/_Game/Live/Runtime/Input/CharacterLiveInputSource.cs | ADAPT | ConsumeFixedSnapshot에 Down held 또는 필요한 입력 상태 전달 |
| Assets/_Game/Live/Runtime/Player/CharacterLivePlayerRig.cs | EXISTING | Body, BodyCollider, InputSource, Movement 조합 접점 확인 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementSettings.cs | ADAPT | Grab 거리/유예/이탈 억제 등 단일 설정 소유 |
| Assets/_Game/Live/Runtime/Movement/CharacterLiveMovementDriver.cs | ADAPT | 실제 contact/capsule cast 기반 Grab 상태와 MovePosition 통합 |
| Assets/_Game/Map/Runtime/WorldGeneration/Baking/GeneratedUnityTilemapApplier.cs | EXISTING | RMAP02 Terrain TilemapCollider 소비. Grab 때문에 수정하지 않음 |
| Assets/_Game/Live/Runtime/Camera/CharacterLiveCameraFollowDriver.cs | EXISTING | Grab 중 연속 카메라 유지 확인. 카메라 책임 변경 금지 |
| Assets/_Game/Map/Scenes/MoonPalace/RMAP02/MoonPalacePlayerTilemapRun_RMAP02.unity | EXISTING | 필요 시 RMAP03 전용 복제/확장 씬의 기준 |
| Assets/_Game/Live/Editor/RMAP02/CharacterLiveMapRunSceneBuilder.cs | EXISTING | fixture 재사용 후보. RMAP03 전용 생성 도구가 필요하면 좁게 확장 |

EXISTING/ADAPT는 RMAP02 보고 기준이며 현지 경로와 직접 참조를 대조한다.
동명 파일이 다른 위치에 있으면 실제 경로와 이유를 Result에 적고, 추측 경로에 새 파일을 만들지 않는다.

## 4. Write Allowlist / 변경 경계

- 위 Live Input/Player/Movement/Settings의 Grab 구현에 필요한 필드, 상태, 접촉 판정, 입력 snapshot 변경.
- 필요하면 Assets/_Game/Live/Runtime/Movement/ 아래에 CharacterLiveGrab* 또는 CharacterLiveSurface* 보조 타입을 추가한다.
- 움직이는 안전 고체 fixture가 필요하면 RMAP03 전용 Runtime/Test/Scene helper에 한정한다. 장치 풀이나 캠페인 콘텐츠로 확대하지 않는다.
- RMAP02 씬을 직접 훼손하지 않는다. 저장 씬이 필요하면 RMAP03 전용 씬 또는 RMAP02 fixture의 명확한 후속 복제본을 만든다.
- RMAP03 전용 시험 지형, 간단한 안전/위험 표면 marker, PlayMode fixture, 필요한 .meta.
- 기존 Test assembly의 RMAP03 전용 EditMode/PlayMode 테스트 및 직접 영향받은 기존 테스트의 최소 수정.
- MapDesign/MCP/GENERATED/RMAP03의 필요한 입력/측정/씬 확인 증거, 지정 Task/Archive/Result와 상태 기록.
- asmdef/asmref는 기존 assembly를 우선 사용한다. 컴파일에 필요한 최소 참조만 추가하고 순환을 만들지 않는다.

GeneratedUnityTilemapApplier, CameraFollow, run/walk/jump 수치, RMAP02 통과/카메라 증거는 소비 대상이다.
Grab 때문에 RMAP02 이동 목표를 다시 맞추거나 타일 적용기 책임을 재작성하지 않는다.
패키지 추가/업데이트, 전체 설정 재작성, 운영 프로토콜/과거 Task/Result 변경, build 설정 변경은 범위 밖이다.

## 5. P05 / Grab 허용 표면

- 완전 고체 사각 타일의 노출된 위쪽 모서리 중 접촉 피해가 없는 표면을 잡는다.
- Player는 실제 0.4×0.8 CapsuleCollider와 RMAP02 Terrain collider를 사용한다. Transform 순간 이동으로 접촉을 대체하지 않는다.
- static solid, destructible로 분류되는 solid, safe moving solid를 구분해 모두 허용 사례를 만든다.
- destructible은 이번에 파괴 시스템을 완성하라는 뜻이 아니다. 현지 기존 모델이 없으면 "파괴 가능으로 분류되는 안전 고체" fixture 수준에서 잡힘을 확인한다.
- moving solid는 1×1 이상 사각 안전 고체여야 하며, 현재 위험/압착 상태가 아니어야 한다.
- 모서리 후보는 타일/Collider의 world-up 기준으로 계산한다. 좌우 양쪽 모서리와 타일 경계에서 중복/흔들림을 확인한다.
- 허용 표면의 판정 근거, layer/tag/component/fixture 좌표, grab anchor 좌표를 Result에 기록한다.

## 6. P06 / Grab 금지 표면

- 일방향 발판, 가시/화염/회전날 등 피해 표면, 압착 중 피스톤, 닫히는 문, 위험 토글 벽, 비고체 장식은 Grab 대상이 아니다.
- RMAP04의 one-way 통과 구현을 시작하지 않는다. 필요한 경우 Grab 금지 분류용 간단한 fixture/marker만 만든다.
- 미구현 장치 전체를 만들지 않는다. 기존/간단한 시험 표면의 safe/unsafe/solid/non-solid 상태로 분류를 검증한다.
- 위험 상태가 바뀌는 표면은 현재 상태를 기준으로 Grab 유지 또는 즉시 해제를 결정한다.
- 장식 layer, marker layer, camera frame, exit trigger를 고체 Grab 후보로 잘못 소비하지 않는다.
- 금지 표면 옆에서 기존 벽/바닥 충돌은 유지되어야 한다. "Grab 금지"를 위해 실제 충돌 전체를 꺼버리지 않는다.
- 금지별 최소 한 사례와 실제 Player 관찰을 Result에 기록한다. 구현하지 않은 장치명은 "분류 fixture로 확인"과 "콘텐츠 풀 미구현"을 분리해 적는다.

## 7. P07 / Grab 진입과 억제

- 하강 중 또는 수평 이동 중 안전 모서리에 닿으면 자동 Grab한다.
- 상승 중에는 자동 Grab하지 않는다. 점프 상승 중 모서리를 스쳐도 기존 점프/충돌 흐름을 유지한다.
- Down 입력 중에는 Grab하지 않고 떨어진다. 기존 Move.y 입력을 우선 사용하고, 필요 시 InputSnapshot에 DownHeld만 좁게 추가한다.
- Grab 허용 거리, 접촉 normal/velocity 조건, 입력 프레임 유예는 CharacterLiveMovementSettings 한 곳에서 튜닝한다.
- Coyote/buffer/run/jump 수치를 복제하지 않는다. 기존 movement settings와 snapshot 흐름에서 함께 소비한다.
- 자동 Grab은 벽 안쪽 흡착, 천장 관통, 코너 위 teleport, exit trigger 오동작을 만들면 안 된다.
- 실제 값과 선택 근거를 Result에 적고, 실패 후 테스트에만 유리하게 거리/유예를 넓히지 않는다.

## 8. P08 / Grab 유지와 이탈

- Grab 유지에는 시간 제한이 없다. 입력이 없으면 같은 anchor에 안정적으로 매달린다.
- Grab 중 Jump는 수직으로 최대 약 1.3타일 상승한 뒤 일반 공중 좌우 제어를 사용한다.
- Grab 중 좌우 처리는 별도 벽차기 상태 없이 기존 공중 제어에 연결한다.
- Down, Jump, 표면 소멸/위험 전환, 이동 고체 접촉 상실 등 명시 이탈 경로를 정한다.
- 이탈 직후 즉시 재Grab으로 이탈 입력이 무효화되지 않도록 짧은 재진입 억제나 같은 anchor 무시 조건을 둔다.
- 이 억제는 일반 낙하 중 다른 안전 모서리 Grab을 과도하게 막지 않아야 한다.
- Grab 상태가 RMAP05 낙하 피해/경직에서 사용할 "마지막 안전 접촉/Grab 성립" 정보를 제공할 수 있게 최소 공개 접점을 둔다.

## 9. P10 / 움직이는 고체와 운반

- 안전 고체를 Grab했을 때 Player는 모서리 anchor와 상대 위치를 유지하며 함께 운반된다.
- 움직이는 고체는 실제 Collider/Rigidbody2D 또는 현지 기존 이동 표면 경로를 사용한다. 위치를 매 프레임 임의 보정만으로 통과시키지 않는다.
- 고체가 사라지거나 위험/압착 상태가 되거나 비고체로 바뀌면 Grab을 해제한다.
- 이동 중 Player와 Terrain/ceiling/wall의 실제 충돌이 함께 작동해야 한다. 운반 때문에 다른 고체를 관통하지 않는다.
- 이번 Task는 이동 플랫폼 콘텐츠 풀, 피스톤/문/토글 벽의 전체 gameplay를 만들지 않는다.
- safe moving solid fixture의 경로, 이동 범위, 속도, 실제 유지/이탈 관찰을 Result에 기록한다.

## 10. 구현 순서 / 한 작업 안에서 완료

1. 정상 Apply와 RMAP02 PASS 체인 확인 후 현재 Player input/motor/contact 흐름을 읽는다.
2. surface classification과 Grab 후보 추출 위치를 정하고, 설정 값을 한 asset/type에서 소유하게 한다.
3. Grab enter/hold/exit 상태를 기존 fixed-step movement에 통합한다.
4. 정적/파괴 가능 분류/금지 표면 fixture를 RMAP03 전용 씬 또는 테스트 fixture에 추가한다.
5. 움직이는 안전 고체 fixture와 relative anchor 유지/해제 조건을 연결한다.
6. focused EditMode/PlayMode 확인으로 실패를 좁게 고친다.
7. 사용자 조작 안내, 파일별 책임, 요구 ID별 증거를 Result에 쓰고 PASS일 때만 Finalize/commit한다.

## 11. Focused Checks / 실제 실행 증거

필요한 새 테스트만 기존 assembly에 둔다. 테스트 수를 고정하거나 문서/메타데이터 숫자를 검사하는 테스트를 추가하지 않는다.
- EditMode: surface classification, exposed corner 후보, 금지 layer/marker 제외, moving anchor 계산을 확인한다.
- PlayMode: 실제 Player/Rigidbody2D/CapsuleCollider2D와 실제 Collider 표면으로 Grab enter/hold/exit를 확인한다.
- P05: static, destructible-classified, safe moving solid의 허용 Grab을 각각 확인한다.
- P06: one-way marker/피해 표면/압착 또는 위험 상태/비고체 장식의 Grab 금지를 확인한다.
- P07: 하강/수평 진입은 Grab, 상승 중과 Down held는 Grab 안 됨을 실제 fixed-step 입력으로 확인한다.
- P08: 무제한 유지, Jump 이탈 높이, 좌우 공중 제어, 즉시 재Grab 억제를 확인한다.
- P10: moving solid 운반, 접촉 상실 또는 위험 전환 해제를 확인한다.
- RMAP02 run/walk/jump/camera의 직접 영향 회귀는 필요한 최소 범위만 재확인하고 이유를 적는다.
- Unity refresh/compile은 수행한다. broad build, legacy 19347, full/unfiltered regression, 불필요한 Player build는 실행하지 않는다.
- 자동 입력은 기존 Input System->snapshot->motor 흐름을 통과시킨다. 초기 배치 외 teleport/검사 marker로 성공을 대신하지 않는다.

Unity 도구 부재/미실행은 NOT RUN 또는 BLOCKED로 기록한다. 논리 Bake/BFS/Preview/과거 PASS를 실제 Grab 증거로 대체하지 않는다.

## 12. Required Outputs / Result

- 실제 Player가 잡을 수 있는 안전 모서리와 잡을 수 없는 표면 분류.
- Grab enter/hold/exit 상태, 입력 억제, 재Grab 억제, moving solid relative anchor 유지.
- RMAP03 전용 씬/fixture 또는 RMAP02 기반 확장 씬과 필요한 증거 파일.
- MapDesign/MCP/GENERATED/RMAP03에는 필요한 입력, 측정, 씬 확인 증거만 저장한다.
- Result에 다음 항목을 포함한다. 실행하지 않은 기능/검사를 PASS로 적지 않는다.

```text
TASK: RMAP03_GRAB
STATUS: PASS 또는 FAIL 또는 BLOCKED 또는 STATUS_CONFLICT
USER-FACING IMPLEMENTATION REPORT: 가능한 Grab 조작, 씬 여는 경로, 키 배치, 관찰 방법
RESPONSIBILITY AND FILES: 실제 경로 | 추가/수정 | 책임 | 소유하지 않는 책임
PRECONDITIONS: 현재 HEAD/branch, 선행 Task/Archive/Result SHA와 commit, 적용 전후 상태
CHANGED: 재사용/ADAPT/신규 판단, 공개 API/설정/Prefab/Scene/assembly 변경 이유
REQUIREMENT EVIDENCE: P05/P06/P07/P08/P10 각각 산출물, 실측/관찰, 판정
VALIDATION: 실제 filter/job/count, Play 확인, 실패/최소 수정, 미실행과 이유
UNITY VISIBLE OUTPUT: 씬, fixture 좌표, surface 분류, Player 상태, moving solid 증거
OUT-OF-SCOPE FINDINGS: 등반/one-way 통과/피해/관찰/자동 생성/Full Run의 남은 책임
RMAP04 BINDINGS: 확인된 등반/사다리/폴/one-way 접점과 API, EXISTING/PROPOSED 구분
FOLLOWUP: 현지 single_task_v1 메타데이터 필드/경로 규칙, 설치 Task의 실제 SHA-256
FINAL EVIDENCE: 필수 미확인 유무, Current/상태, Task와 Archive bytes 동일 여부
NEXT: RMAP04_CLIMB LOCKED / NOT STARTED
COMMIT: 작업 전 HEAD, 실제 생성 commit SHA 또는 미생성 이유
```

## 13. PASS / 실패 / Finalize / STOP

P05/P06/P07/P08/P10 구현과 실제 Player/물리/Grab의 필수 확인이 모두 있어야 PASS다.
상태/선행 체인 충돌은 STATUS_CONFLICT, 필수 자료/도구 부재는 BLOCKED, 기능/확인 기준 불충족은 FAIL로 보고한다.
확인된 필수 경로 단절/충돌 실패를 검증 완화나 침묵 수리로 숨기지 않는다. 어려운 배치 자체는 허용하지만 필수 유일 경로 단절은 실패다.
PASS Result 후 기존 Finalize로 RMAP03 CURRENT->COMPLETE, Current->NONE만 수행한다. RMAP04~19는 LOCKED다.
기준선에서 상태는 적용 후 223 COMPLETE / 1 CURRENT / 16 LOCKED, 완료 후 224 COMPLETE / 0 CURRENT / 16 LOCKED다.
Task/Archive/Result/허용된 코드/asset/증거/상태 변경만 commit한다. 무관한 staged 변경을 포함하거나 임의 unstage하지 않는다.
commit 메시지: RMAP03 implement corner grab and moving safe solids
최종 CLI 보고에는 Result 경로, 실제 commit SHA, Result SHA-256, 설치 Task SHA-256, Current NONE, RMAP04 LOCKED를 적는다.
Git push와 RMAP04 실행 없이 STOP한다.
