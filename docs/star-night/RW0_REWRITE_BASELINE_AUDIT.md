# 별을 물어오는 밤 — RW0 리라이트 기준선 감사

- 기준 기획서: `별을_물어오는_밤_게임시스템_전면리뉴얼_하네스_v1.0.md`
- 감사일: 2026-07-29
- 현재 브랜치: `main`
- 현재 HEAD: `d6a80fb`
- 단계: RW0-A~C 완료, RW0-D 진입 보류
- Unity 확인 버전: `6000.3.8f1`

## 1. 결론

이번 변경은 기존 M6의 점진적 리팩터링이 아니라 **별도 런타임으로 다시 만드는 것이 맞다.**

기존 M6는 플레이 가능한 참고본으로 봉인한다. 새 버전에서는 기존 씬의 배경 구성, 스프라이트, 애니메이션, 음향/대화 인터페이스 같은 표현 자산만 선별해서 가져오고, 플레이 규칙과 상태 모델은 새 어셈블리에서 다시 작성한다.

현재는 M6의 핵심 파일이 Git 미추적 상태이므로 아직 기준선 태그나 브랜치를 만들 수 없다. 이 파일들을 먼저 하나의 기준선 커밋으로 묶지 않고 태그를 만들면, 태그에서 M6 프로젝트가 복원되지 않는다.

따라서 안전한 순서는 다음과 같다.

1. 현재 M6 전체를 기준선 커밋으로 스냅샷한다.
2. 그 커밋에서 `legacy/m6-full-prototype` 브랜치와 태그를 만든다.
3. `rewrite/system-v1` 브랜치에서 새 코드 루트와 최소 부트 씬을 만든다.
4. 새 코드가 `StarFetchingNight.Runtime`을 참조하지 않는지 자동 검사한다.
5. 그 뒤에 RW1 플레이어와 경량 소지품을 구현한다.

## 2. 새 버전의 한 문장

> 주민에게 작은 약속 하나를 받고, 눈에 보이는 목적지를 향해 단순한 도구로 길을 만들며, 욕심낼지 돌아갈지 판단하다가 마루의 세 번째 종 전에 탈출하는 2D 원런 플랫폼 어드벤처.

이 문장은 이후 구현 판단의 최상위 필터다. 기능이 이 루프를 더 읽기 쉽고 긴장감 있게 만들지 못하면 새 코어에 넣지 않는다.

## 3. 리라이트 불변 규칙

### 플레이 상태

- 체력은 4칸이다.
- 소지 정보는 금화, 밧줄, 폭탄, 손도구 1개, 약속 물건 1개뿐이다.
- 밧줄과 폭탄의 소지 한도는 각각 6개다.
- 인벤토리 화면과 칸 정리는 없다.
- 영구 능력치 성장이나 메타 파워 성장은 없다.
- 한 챕터에서는 한 주민의 약속과 한 목적지가 주축이 된다.

### 도구

- 핵심 도구는 밧줄, 폭탄, 곡괭이, 삽, 물뿌리개, 절구, 갈고리, 우산의 8종이다.
- 밧줄과 폭탄은 소비형이다.
- 나머지 6종은 손도구 슬롯 하나를 공유한다.
- 필수 도구를 무작위로 요구하지 않는다.
- 태그 조합, 조합 레시피, 제작, 랜덤 등급/옵션은 새 코어에 없다.

### 시간과 추격

- 마루 타이머는 약속 수락 시 시작한다.
- 대화, 컷신, 상점 데모, 보스 연출, 휴식 중에는 멈춘다.
- 종은 전체 시간의 60%, 82%, 100%에서 울린다.
- 보스 출구 진입 시 최소 60초를 보장한다.
- 챕터 기준 시간은 CH1 480초, CH2 420초, CH3 450초, CH4 510초, CH5 510초다.

### 경로와 보스

- 별실은 메인 경로에서 분기하고 다시 합류한다.
- 별실의 유혹은 금화, 도구 보충, 손도구 교환처럼 즉시 이해되어야 한다.
- 목적지는 별실 안에 숨기지 않는다.
- 보스는 직접, 유도, 환경의 3가지 해법을 지원한다.
- 해법 선택에는 숨은 도덕성 감점이 없다.

### 명시적으로 폐기하는 규칙

- 태그 기반 조합과 레시피
- 랜덤 아이템 옵션과 등급
- 격자 가방
- 불안정도
- 2/2 문 기여
- 세 갈래 중 두 갈래 완료 구조
- 숨은 도덕성
- 복수 화폐와 복수 재료
- 완전 절차 생성 미로
- 동시에 추적하는 복수 독립 퀘스트
- 암기형 상성

## 4. 현재 프로젝트 감사 결과

### 자산 규모

| 구분 | 프로젝트 전체 | StarNight 전용 |
|---|---:|---:|
| Unity 씬 | 40 | 7 |
| 프리팹 | 238 | 0 |
| C# | 247 | 120 |
| asmdef | 4 | 2 |
| PNG | - | 18 |

StarNight 전용 프리팹이 0개라는 점이 중요하다. 현재 챕터는 에디터 빌더가 외부 2D Fantasy Bundle 프리팹을 씬에 직접 배치하는 구조다. 따라서 새 룸 시스템은 기존 씬을 런타임 단위로 재사용하지 않고, 검증된 구간을 참고해 별도의 룸 프리팹으로 다시 추출해야 한다.

### 기존 씬의 레거시 결합

| 씬 | 기존 StarNight 컴포넌트 수 |
|---|---:|
| StarNight_Prologue | 20 |
| StarNight_MoonMill | 62 |
| StarNight_MagpieBridge | 63 |
| StarNight_CloudWhaleRanch | 70 |
| StarNight_StarPostOffice | 78 |
| StarNight_SleepingSunGarden | 88 |
| StarNight_PolarisObservatory | 32 |
| 합계 | 413 |

가장 많이 배치된 기존 타입은 다음과 같다.

| 타입 | 배치 수 | 판정 |
|---|---:|---|
| `StarNightDiscoveryZone` | 93 | 레거시 전용 |
| `FableObject` | 61 | 폐기 |
| `StarNightCheckpoint` | 36 | 개념만 재구현 |
| `GateRouteObjective` | 15 | 폐기 |
| `SunGrowthState` | 10 | 레거시 전용 |
| `StarNightCombinationResolver` | 7 | 폐기 |
| `StarNightInventory` | 7 | 폐기 |
| `StarNightPlayerAgent` | 7 | 폐기 |
| `StarNightSimpleMotor` | 7 | 수치 참고 후 재구현 |
| `StarNightAtmosphere` | 7 | 비주얼 참고 후 재구현 |
| `StarNightJourneyNavigation` | 7 | 폐기 |
| `StarNightHUD` | 7 | 새 정보 구조로 재구현 |

### 코드 결합

- 기존 런타임 C# 110개 중 94개가 `StarNightRunState`를 직접 참조한다.
- 26개 파일이 `FableObject`를 참조한다.
- `StarNightRunState` 하나가 챕터, 행동 기록, 사고, 마루 경계도, 결과 해석, 붉은 실, 구름병, 배달, 해씨, 열기, 문 기여, 루프, 경로, 텔레메트리까지 소유한다.
- 기존 플레이어 에이전트는 조합 판정, 동사, 인벤토리, 전역 런 상태에 직접 결합되어 있다.

기존 코어에 어댑터를 덧대면 새 규칙이 이전 상태 모델을 계속 끌고 가게 된다. 따라서 런타임 코드의 직접 재사용보다 행위 감각, 수치, 연출 자산을 참고하는 편이 안전하다.

### Unity 기준선 검증

- 연결 인스턴스: `Constant@ced6e0dfc4a31d45`
- 프로젝트 루트: `C:/Users/user/Documents/GitHub/Optimal-Selection/Constant`
- 활성 씬: `Assets/Scenes/StarNight/StarNight_MoonMill.unity`
- 에디터 상태: Idle, 비재생, 컴파일 완료, 도구 사용 가능
- StarNight EditMode 테스트: 149/149 통과
- 실패/건너뜀: 0/0

콘솔에는 새 코드 컴파일 실패는 없었다. 확인된 항목은 구 Input Manager 사용 중단 예정 안내와 Unity 생성형 AI의 `NoSubscription` 메시지뿐이며, M6 런타임 테스트 결과와는 무관하다.

## 5. 자산 분류

### A. 직접 재사용 후보

- `Assets/2D Fantasy sprite bundle`의 배경/오브젝트 프리팹
- 기존 캐릭터 및 보스 애니메이션 클립과 스프라이트
- 배경 음악과 효과음 원본
- TMP 폰트와 기본 텍스트 스타일
- URP 2D 조명 설정
- 대화·사운드의 추상 인터페이스가 레거시 타입을 요구하지 않는 경우

현재 빌더가 사용하는 주요 번들:

- Forest V2.0
- Old Forest pack
- Cristal Dungeon sprite pack
- Abandoned station
- Desert pack
- Island pack
- Spring forest
- Underwater area pack
- Mount pack
- Dungeon pack
- Lava dungeon pack
- Bonus/Climbing elements/Chains

### B. 형태와 수치만 참고하고 재구현

- 플레이어 Rigidbody2D/Collider2D 크기와 이동 속도
- 카메라 화면 크기, 데드존, 배경 레이어
- 체크포인트 위치와 안전지대 배치 감각
- 기존 챕터의 랜드마크, 색상 팔레트, NPC 배치
- 종 연출, 마루 애니메이션, 보스 연출
- 씬 전환 순서
- 휴식 배경
- 현재 챕터의 검증된 점프 간격과 발판 높이

### C. 데이터만 변환

- 주민 이름, 약속 문구, 목적지 명칭
- 챕터별 색감과 테마
- 보스의 외형과 애니메이션 상태명
- 사고/행동 기록 중 새 엔딩 사실로 쓸 수 있는 서술 정보
- 폴라리스 결말의 서사 소재

데이터를 옮길 때 기존 `FableObject`, 태그, 문 기여, 숨은 도덕성 필드는 가져오지 않는다.

### D. M6 레거시 전용

- `StarNightRunState`
- `StarNightChapterState`
- 기존 Chapter Loop/Route Map
- `FableObject`
- `StarNightCombinationResolver`
- `StarNightInventory`
- Gate Contribution/Route Objective 계열
- Red Thread/Cloud Bottle/Delivery/Sun Seed 성장 계열
- 기존 Alert 기반 마루 시스템
- 기존 결과 해석/숨은 성향 시스템
- M2~M6 전용 텔레메트리와 검증 UI
- 기존 씬 빌더

이 파일들은 삭제하지 않고 `legacy/m6-full-prototype`에 보존한다. 새 런타임에서는 참조하지 않는다.

## 6. 새 코드 격리 계약

### 루트

```text
Assets/
  StarNightRewrite/
    Runtime/
      Core/
      Player/
      Tools/
      Promise/
      Maru/
      Economy/
      Rooms/
      Boss/
      UI/
    Editor/
    Tests/
      EditMode/
      PlayMode/
    Prefabs/
      Rooms/
      Player/
      UI/
    Data/
    ArtAdapters/
Assets/Scenes/StarNightRewrite/
```

### 네임스페이스

```text
StarNight.Rewrite.Core
StarNight.Rewrite.Player
StarNight.Rewrite.Tools
StarNight.Rewrite.Promise
StarNight.Rewrite.Maru
StarNight.Rewrite.Economy
StarNight.Rewrite.Rooms
StarNight.Rewrite.Boss
StarNight.Rewrite.UI
```

### 초기 어셈블리

RW0/RW1에서는 어셈블리를 과도하게 쪼개지 않는다.

```text
StarNight.Rewrite.Core
StarNight.Rewrite.Player
StarNight.Rewrite.Tests
```

Tools, Promise, Maru 등은 해당 RW 단계에 들어갈 때 분리한다. 초기부터 빈 어셈블리 10개를 만들면 컴파일 경계만 늘고 기능 경계 검증에는 도움이 되지 않는다.

### 금지 규칙

- 새 asmdef는 `StarFetchingNight.Runtime`을 참조하지 않는다.
- 새 C#에서 `using StarFetchingNight`를 사용하지 않는다.
- 새 씬과 프리팹에는 기존 StarNight MonoBehaviour를 붙이지 않는다.
- 레거시 싱글턴이나 `Ensure` 호출을 래핑하지 않는다.
- 기존 씬을 새 부트 씬으로 이름만 바꾸어 사용하지 않는다.

이 규칙은 EditMode 검사로 자동화한다.

## 7. RW0 산출물과 상태

| 항목 | 상태 | 비고 |
|---|---|---|
| 새 기획 불변 규칙 추출 | 완료 | 본 문서 2~3절 |
| 씬/스크립트/asmdef 수량 감사 | 완료 | 본 문서 4절 |
| 주요 코드 결합 감사 | 완료 | `StarNightRunState` 참조 94개 |
| 재사용/변환/폐기 분류 | 완료 | 본 문서 5절 |
| 새 코드 격리 계약 | 완료 | 본 문서 6절 |
| M6 기준선 커밋 | 보류 | M6 대부분이 Git 미추적 |
| M6 레거시 브랜치/태그 | 보류 | 기준선 커밋 후 생성 |
| 새 rewrite 브랜치 | 보류 | M6 봉인 후 생성 |
| 빈 CH1 부트 씬 | 보류 | rewrite 브랜치에서 생성 |
| 레거시 참조 자동 검사 | 보류 | 새 asmdef 생성과 함께 추가 |
| Unity Editor 기준선 검증 | 완료 | Unity 6000.3.8f1, EditMode 149/149 통과 |
| Codex 등록 MCP 클라이언트 복구 | 보류 | 서버는 정상이나 등록 클라이언트 초기화만 실패; HTTP MCP로 교차 검증 |

## 8. 기준선 커밋 전 확인할 범위

현재 Git 상태에는 StarNight 외 변경도 함께 있다.

- `Assets/Scenes/Constant/Constant_Sylmare.unity`
- `Assets/TextMesh Pro/Fonts/NeoDunggeunmoPro-Regular.asset`
- `Constant.slnx`
- `ProjectSettings/EditorBuildSettings.asset`
- StarNight 씬, 런타임, 에디터, 테스트, 문서, 스크린샷
- `Assets/_Recovery`

기준선 커밋은 “현재 M6를 완전히 재현하는 데 필요한 파일”만 포함해야 한다. `_Recovery`와 임시 스크린샷은 별도 보관 여부를 판정하고, 기존 Constant 씬 변경이 M6 실행에 필요한지 diff로 확인한 뒤 포함 여부를 정한다.

## 9. 다음 체크포인트

### RW0-D: M6 봉인

사용자 승인 후 다음 작업을 수행한다.

1. 변경 파일을 M6 필수/증빙/임시로 다시 분류한다.
2. M6 필수 파일만 기준선 커밋한다.
3. `legacy/m6-full-prototype` 브랜치와 태그를 만든다.
4. `rewrite/system-v1` 브랜치를 만든다.

### RW0-E: 깨끗한 리라이트 부트

1. `StarNight.Rewrite.Core`와 `StarNight.Rewrite.Player` asmdef를 만든다.
2. `RW_CH1_Bootstrap.unity`를 만든다.
3. 카메라, 2D 조명, 빈 시작점만 둔다.
4. 기존 StarNight 컴포넌트 0개를 확인한다.
5. 컴파일, EditMode 검사, PlayMode 부팅을 검증한다.

### RW1 진입 조건

- M6 기준선이 독립적으로 복원되고 실행된다.
- rewrite 브랜치의 부트 씬이 레거시 참조 없이 열린다.
- 새 어셈블리가 컴파일된다.
- 레거시 참조 금지 검사가 통과한다.

이 네 조건 전에는 플레이어 모터나 인벤토리를 구현하지 않는다.
