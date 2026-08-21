# 별을 물어오는 밤 · Package Audit

감사일: 2026-08-01

| 항목 | 고정 버전/상태 | 판정 |
|---|---|---|
| Unity Editor | 6000.3.8f1 | 고정 |
| Yarn Spinner for Unity | 3.2.2 | 통합 기준서의 3.x 요구 충족 |
| Unity Input System | 1.18.0 | 고정 |
| Universal RP | 17.3.0 설치 | 비활성; Built-in 사용 |
| 2D Animation | 13.0.4 | 고정 |
| 2D SpriteShape | 13.0.0 | 고정 |
| 2D Tilemap Extras | 6.0.1 | 고정 |
| Unity Test Framework | 1.6.0 | 고정 |
| Performance Testing | 3.2.0 | 고정 |
| 2D Fantasy Sprite Bundle | 원본 Unity 2022.3.57, 루트 GUID `7eeeb0d8f59d8a041b99e26e6ca878f3` | 원본 보호 |

## Yarn 3.x API 감사

- `Yarn.Unity.DialogueRunner`: `YarnSpinner.Unity` 어셈블리에서 확인
- `Yarn.Unity.DialoguePresenterBase`: `YarnSpinner.Unity` 어셈블리에서 확인
- 결론: Dialogue Runner + Dialogue Presenter 구조를 사용하는 3.x 구현 가능

## 렌더 파이프라인 감사

- `ProjectSettings/GraphicsSettings.asset`의 `m_CustomRenderPipeline`은 비어 있다.
- URP 패키지는 설치되어 있지만 활성 Render Pipeline Asset이 없으므로 현재 기준은 Built-in이다.
- 재구현 중 파이프라인을 임의 전환하지 않는다.

## 입력·레이어 기준

- 현재 입력 에셋: `Assets/StarNight/Input/StarNightControls.inputactions`
- 신규 기본 바인딩 계약은 방향키, Space, X, Z, C를 기준으로 후속 `CORE-03`과 `TOOL-01`에서 별도 에셋으로 구현한다.
- 현재 프로젝트 레이어와 하네스의 고정 Physics Layer 목록은 일치하지 않는다. Layer 추가/충돌 매트릭스 변경은 `TOOL-00` 승인 티켓에서만 수행한다.

## 빌드 씬 기준

감사 시점 Build Settings에는 기존 `Assets/StarNight/Scenes/Game` 씬 4개가 들어 있다. 새 에디터 전용 Lab 씬은 Build Settings에 추가하지 않는다. 정식 씬 목록 교체는 `CORE-01` 이후 단계별로 수행한다.

## 보호 규칙

- `Packages/manifest.json`과 `Packages/packages-lock.json`의 기존 사용자 변경을 이번 감사에서 수정하지 않는다.
- `Assets/2D Fantasy sprite bundle` 아래 원본 파일을 이동, 복사, 재색칠, 재임포트 설정 변경하지 않는다.
- 변형 에셋은 후속 `Assets/_Game/ArtAdapters` 아래에만 만든다.

