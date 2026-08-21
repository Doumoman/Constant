# 별을 물어오는 밤 · Project Baseline

기준 고정일: 2026-08-01 (Asia/Seoul)

```text
UnityEditorVersion=6000.3.8f1
YarnSpinnerVersion=3.2.2
InputSystemVersion=1.18.0
RenderPipeline=BuiltIn
FantasySpriteBundleVersion=OriginalUnityVersion 2022.3.57 / LocalRootGuid 7eeeb0d8f59d8a041b99e26e6ca878f3
TargetResolution=1920x1080
TargetFrameRate=60
```

## 고정 범위

- 구현 브랜치: `rewrite/system-v1`
- 기준 커밋: `5682934`
- 프로젝트 루트: `C:/Users/user/Documents/GitHub/Optimal-Selection/Constant`
- 런타임 렌더 파이프라인: Built-in (`m_CustomRenderPipeline.fileID = 0`)
- URP `17.3.0`은 설치되어 있으나 현재 활성 파이프라인이 아니다.
- Yarn Spinner 3.x 계약은 사용 가능하다. Unity 런타임에서 `Yarn.Unity.DialogueRunner`와 `Yarn.Unity.DialoguePresenterBase`를 확인했다.
- 입력 기준 에셋: `Assets/StarNight/Input/StarNightControls.inputactions`
- 입력 기준 SHA-256: `F39EA0372E7EE4A34242D85260F871B5B739DA9AB9C9A1303962F1C6F53CCA30`
- 2D Fantasy Sprite Bundle 원본 루트: `Assets/2D Fantasy sprite bundle`
- 외부 에셋 원본 텍스처, 프리팹, 머티리얼 및 임포트 설정은 직접 수정하지 않는다.

## 문서 간 버전 충돌 결정

맵 요소 하네스는 현재 프로젝트를 Unity 2022.3 LTS로 가정하지만 실제 프로젝트와 상위 통합 기준서는 현재 에디터 버전을 유지하도록 요구한다. 따라서 이 재구현에서는 Unity `6000.3.8f1`을 고정하며 업그레이드·다운그레이드를 수행하지 않는다. Unity 2022.3 전용 Editor API 가정은 구현 시마다 Unity 6 리플렉션과 컴파일로 검증한다.

## 작업 경계

- 전역 코어 변경 하네스 v3.0을 현재 최우선 요구사항 기준으로 사용한다.
- 이전 구현의 스크립트는 파일 존재와 격리 상태를 확인하는 범위를 넘어서 설계 근거로 사용하지 않는다.
- 현재 작업 시작 시 Git working tree에는 기존 사용자 변경이 1,314개 존재한다. 이 변경은 재구현 작업에서 되돌리거나 덮어쓰지 않는다.

## GCORE-00 기준선 (2026-08-04)

```text
Harness=전역 코어 로직 변경 하네스 v3.0
Branch=rewrite/system-v1
Head=568293463bd2d87a226fb63f80aa77a45cb993f8
UnityEditorVersion=6000.3.8f1
ActiveScene=Assets/_Game/Scenes/00_Boot.unity
WorkingTree=1295 deleted, 15 modified, 39 untracked
Stage11Baseline=폐기됨; 승인 Seed와 영상 재사용 안 함
```

- 미커밋 사용자 작업을 임의로 커밋하거나 태그하지 않는다. 위 SHA와 작업 트리 상태를 논리 기준선으로 사용한다.
- `PlayerHandSlot` 중심 도구 계약은 GCORE-03에서 `Legacy/Disabled` 대상으로 격리한다. 물리 운반 슬롯 책임은 유지한다.
- 기존 3셀 높이 점프 상수는 GCORE-01에서 제거한다.
- 문서의 `내구도 0 → 복사본 소비·자동 교체` 규칙은 사용자 지시로 폐기한다.
- 중복 도구 획득은 보유 항목의 내구도를 최대치로 회복한다. 파손 순간에는 다른 복사본을 자동 소비하지 않는다.

## CORE-00 게이트

- Unity 연결: PASS
- Yarn 3.x API 사용 가능: PASS
- Console Error 0: PASS (감사 시점)
- 외부 패키지 원본 수정 없음: PASS
- 패키지 버전 기록: PASS
- 기준 씬: `Assets/_Game/Scenes/99_GridLab.unity`

## MAP-E00 맵 제작 기준

```text
MapBuildTag=StarNight/Core-v2.1/Map-v1.0/MAP-E02
MapHarnessVersion=1.0
UnityEditorVersion=6000.3.8f1
DirectPackageDependencyCount=57
PackageManifestSha256=7A5112428282ED6CB4256C37A0B907430BC2C0C2621E86C8EF09DF86E5AF2B6A
YarnSpinnerVersion=3.2.2
InputSystemVersion=1.18.0
UnityTestFrameworkVersion=1.6.0
UGUIVersion=2.0.0
UnityMcpRevision=efaf786e8772a8591940fdb341524588470469ed
```

- 전체 직접 패키지 목록의 단일 기준은 `Packages/manifest.json`이며 위 SHA-256으로 변경 여부를 판별한다.
- `MapAuthoring.Editor`는 Editor 플랫폼 전용이며 `Game.Map.Runtime`만 참조한다.
- `00_MapElementLab.unity`, `01_StageLayoutLab.unity`는 Build Settings 등록 금지 대상이다.

## MAP-E01 게이트

- `GridCell`, `CellFootprint`, `GridOccupier`, `RoomElementRegistry`: 구현 완료
- 1×1, 2×1, 비정형 L자 Footprint 등록·해제: PASS
- 중복 셀·비호환 점유의 등록 전 감지: PASS
- 충돌 실패 시 부분 등록 없음: PASS
- 관련 EditMode 테스트: 6/6 PASS

## MAP-E02 게이트

- `MapElementDefinition`, Profile 데이터, `MapElementInstance`, `ElementStateMachine`: 구현 완료
- Dormant 상태의 Animator·Physics·Timer 정지와 정확한 재개: PASS
- RoomRuntime 재방문 시 Broken·Moved 상태 복구: PASS
- Broken 상태의 점유 해제 유지: PASS
- 관련 EditMode 테스트: 2/2 PASS
- 관련 PlayMode 테스트: 2/2 PASS
