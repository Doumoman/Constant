# 《별을 물어오는 밤》 v0.2 순차 리팩터링 실행안

기준 문서:
`별을_물어오는_밤_P4_리팩터링_통합기획서_v0.2.md`

기준 구현:
P0~P4가 연결된 현재 StarNight 프로토타입

## 진행 상태

| 단계 | 상태 | 검증 |
|---|---|---|
| M0 기준선과 실행안 | 완료 | 기존 P0~P4 구조 및 변경 충돌 지점 확인 |
| M1-A 공통 상태 골격 | 완료 | EditMode 51/51 통과, 컴파일 오류 0 |
| M1-B CH1 세 경로 연결 | 완료 | EditMode 55/55, CH1 씬 및 Route C 반환·Route A+B 가동 직접 검증 |
| M1-C 방울과 마루 추격 | 완료 | EditMode 60/60, 선행 냄새 92 및 Alert 30/60 직접 검증 |
| M2 CH1 플레이테스트 | 내부 1차 완료 | EditMode 66/66, 세 Route 조합·A+C 씬 스모크 완료, 사람 5세션 표본 대기 |
| M3-1 CH2 까치다리 이식 | 내부 완료 | EditMode 73개 완료·실패 0, A+B/A+C/B+C 및 A+C 실제 씬 스모크 |
| M3-2 CH4 별 우체국 이식 | 내부 완료 | EditMode 82개 완료·실패 0, 실제 배송·수동 별문·진실 보관소·Bell 2·출항 스모크 |
| M3-3 CH3 구름고래 목장 이식 | 내부 완료 | EditMode 92개 완료·실패 0, 실제 하중 이동·수동 별문·무지개 목장·Bell 2·출항 스모크 |
| M3-4 CH5 잠든 해님의 정원 이식 | 내부 완료 | EditMode 102개 완료·실패 0, 실제 저장/온실 빛·수동 별문·멈춘 방·Bell 2·출항 스모크 |
| M4 프롤로그·여행 티켓·전체 흐름 | 내부 완료 | EditMode 114/114, 프롤로그 12구역·6비트, Prologue→CH1 상태 보존, 씬/PlayMode 오류·경고 0 |
| M5 북극성 관측소·네 엔딩 | 내부 완료 | EditMode 132/132, 관측소 23구역·기록 5개·도구 복구 5단계·엔딩 4종, 별길 PlayMode 스모크 |
| M6 밸런스·전체 회귀 QA | 내부 완료 | EditMode 158/158, 자동 구조 감사 25/25, 두 씬 PlayMode 오류·경고 0, 사람 10세션 표본 대기 |

## 1. 이번 리팩터링의 결정

현재 P0~P4 콘텐츠와 생활 도구 시스템은 폐기하지 않는다.
기존 런타임 위에 공통 챕터 루프를 어댑터로 추가하고, CH1에서 먼저 검증한 뒤 다른 챕터로 이식한다.

새로운 한 챕터의 고정 문장은 다음과 같다.

> 세 경로 중 두 개를 해결해 별문 부품을 직접 장착하고, 별문을 켠 뒤 안전하게 떠날지 유혹 구역에 남을지 결정한다.

플레이어에게 보이는 순서는 아래로 고정한다.

```text
도착
→ 현재 별문 목표 확인
→ 세 경로 중 두 경로 완료
→ 별문 기여 물건 2개 직접 장착
→ GateReady
→ 플레이어가 손잡이를 당겨 GateActive
→ 출구와 유혹 구역 동시 개방
→ 첫 번째 방울
→ 즉시 출항 또는 추가 탐험
→ 두 번째 방울
→ 세 번째 방울과 직접 추격
→ 출항 / 강제 귀가
```

## 2. 현재 구현과 v0.2의 차이

| 영역 | 현재 구현 | v0.2 목표 | 처리 |
|---|---|---|---|
| CH1 출항 목표 | 달떡 3개를 연료통에 투입 | 세 경로 중 두 개의 길떡 기여 | 공통 별문 어댑터로 교체 |
| 준비와 가동 | `DepartureReady`만 존재 | `GateReady`와 `GateActive` 분리 | 상태 확장 |
| 유혹 구역 | `DepartureReady`에서 개방 | `GateActive` 이후 개방 | 조건 교체 |
| 출항 | 준비 완료 시 달배 사용 | 별문을 직접 켠 뒤 출항 | 출항문 조건 교체 |
| 추격 시작 | 냄새 임계치 또는 경고 5초 후 | GateActive 이후에만 방울 진행 | MaruDirector 래핑 |
| 긴장 UI | 별냄새 5단계와 숫자 | 세 번의 방울과 환경 변화 | 내부 수치는 유지, 표현 교체 |
| 월드 진행 | 현재 챕터 중심 | 여행 티켓과 별문 0/5 | RunState 확장 |
| 경로 결과 | 개별 플래그와 출항 자원 | Route A/B/C와 기여 물건 | 기존 플래그 매핑 |
| 사고 기록 | 행동과 냄새 중심 | Route/Gate/Bell 문맥 포함 | ActionRecord 확장 |
| P4 정원 목표 | 별길나무 빛 3회 | 길꽃 2/2 뒤 선택 탐험 | CH1 검증 후 마지막 이식 |

## 3. 보존할 책임

- `StarNightCombinationResolver`: 다섯 생활 도구의 동사 해석을 유지한다.
- `FableObject`: 물체 속성과 변형 기록을 유지한다.
- `StarNightActionRecorder`: 기존 행동 기록을 유지하고 문맥만 확장한다.
- `StarNightAccidentReportBuilder`: 기존 사고 인과를 유지한다.
- `StarNightConsequenceResolver`: 기존 플래그를 다음 챕터 modifier로 바꾸는 책임을 유지한다.
- `MaruDirector`: 표적 선택, 유인, 물기, 강제 귀가를 유지한다.
- 현재 P0~P4 방과 2D Fantasy 배치를 최대한 재사용한다.
- 편지, 구루, 해치, 해오름, 정원 과열 플래그를 삭제하지 않는다.

## 4. 새 공통 런타임 구조

### 4.1 상태

```csharp
public enum ChapterLoopState
{
    Arrival,
    RuleIntro,
    RouteOpen,
    RouteProgress,
    GateReady,
    GateActive,
    Bell1,
    Bell2,
    Bell3,
    Departure,
    Intermission,
    ForcedReturn
}

public enum GateRouteState
{
    Locked,
    Available,
    Complete,
    Contributed,
    Invalidated
}

public enum GateRouteArchetype
{
    Cooperation,
    Exploration,
    Appropriation
}
```

### 4.2 신규 모듈

| 모듈 | 책임 |
|---|---|
| `ChapterLoopDirector` | 챕터 공통 상태 전환과 불가능한 전환 차단 |
| `StarGateController` | 기여 접수, 2/2, 수동 가동, 출구 및 유혹 구역 개방 |
| `GateRouteObjective` | Route A/B/C의 완료와 기여 물건 발급 |
| `GateContributionInventory` | 일반 가방과 분리된 미장착 기여 물건 보관 |
| `BellChasePresenter` | 내부 별냄새를 방울 1~3과 마루 단계로 번역 |
| `RunRouteMap` | 복구 별문 수, 여행 티켓 도장, 마루 위치 |
| `TemptationGate` | GateActive 전에는 선택 구역을 잠금 |
| `MandatoryStoryBeatController` | 각 챕터의 마루 구조 장면 완료 보장 |

### 4.3 기존 모듈 확장

`StarNightRunState`

- `RunRouteMap`을 소유한다.
- 복구된 별문 수와 여행 티켓 도장을 보존한다.
- 현재 마루 정거장 위치를 보존한다.
- 새 런에서 공통 루프 모듈을 초기화한다.

`StarNightChapterState`

- 기존 `departureProgress`와 `departureReady`는 호환 필드로 당분간 유지한다.
- 새 구현에서는 `gateContributions`, `gateReady`, `gateActivated`, `bellPhase`를 정본으로 사용한다.
- 기존 출항 진척도는 `gateContributions`의 미러 값으로 갱신한다.

`StarActionContext`와 `StarActionRecord`

- `routeId`
- `gateContributions`
- `gateReady`
- `gateActivated`
- `bellPhase`

위 문맥을 추가한다.

`MaruDirector`

- 표적 점수와 이동 로직은 유지한다.
- 자체 냄새 임계치와 5초 자동 직추격을 제거하지 않고 비활성화 가능한 호환 경로로 남긴다.
- v0.2 챕터에서는 `BellChasePresenter`가 다음 상태를 명시적으로 전달한다.

```text
Bell 0: 마루 비활성
Bell 1: 소리와 배경 흔적만 표시
Bell 2: 같은 정거장 진입, 물건과 NPC 우선
Bell 3: 플레이어 직접 추격과 GateClosing
```

## 5. 별냄새와 방울의 초기 규칙

기존 `scent` 코드명은 당장 바꾸지 않는다.
GateActive 이전에도 기존 행동 기록과 modifier 계산을 위해 별냄새 수치는 누적할 수 있지만,
방울과 마루 추격은 절대 시작하지 않는다.

M1의 초기 튜닝은 다음으로 고정한다.

| 시점 | 방울 | 내부 Alert |
|---|---:|---:|
| GateActive 직후 | 1 | 30 |
| 추가 Alert 30 도달 | 2 | 60 |
| 추가 Alert 60 도달 | 3 | 90 |

- GateActive 이전에 쌓인 냄새 때문에 즉시 Bell 2나 Bell 3으로 점프하지 않는다.
- GateActive 시점의 값을 기준점으로 저장하고 이후 증가분만 방울 진행에 사용한다.
- 이전 챕터 modifier는 GateActive 이후 증가 배율과 시작 보너스로 적용한다.
- 첫 방울은 별문 가동의 결과이므로 즉시 출항해도 기록된다.
- Bell 1과 Bell 2에서는 시간만 지났다는 이유로 직접 추격을 시작하지 않는다.
- Bell 3에서만 마루가 플레이어를 직접 목표로 삼을 수 있다.

## 6. M0 — 기준선과 안전장치

### 현재 확인된 기준선

- P0~P4 씬이 Build Settings 0~4에 연결되어 있다.
- 현재 CH1 `requiredDepartureItems`는 3이다.
- `MoonMillFuelPedestal`이 달떡을 직접 소비한다.
- `MoonMillTemptationDoor`는 `DepartureReady`만 확인한다.
- `MoonMillDepartureGate`도 `DepartureReady`만 확인한다.
- `MaruDirector`는 기존 `StarScentStage.Bell`에서 소환되고 경고 시간이 끝나면 직접 추격한다.
- 현재 P4 씬은 누락 스크립트와 깨진 프리팹 없이 검증되었다.
- 현재 EditMode 기준선은 43개 테스트 통과다.

### M0에서 코드에 적용하지 않는 것

- 기존 플래그 삭제
- 클래스명 대규모 변경
- `scent` 필드명 변경
- CH2~CH5 씬 재생성
- 기존 P4 정원 로직 삭제
- 최종 엔딩 구현

### 브랜치 안전장치

실제 M1 코드 변경 전에 현재 작업 트리를 별도 브랜치나 태그로 보존한다.
현재 작업 트리에는 StarNight 외 파일 변경도 있으므로 자동 커밋하지 않고,
사용자가 원하는 커밋 범위를 확인한 뒤 수행한다.

## 7. M1 — CH1 공통 루프 어댑터

### 7.1 Route A: 방앗간 수리

```text
작은 톱니 확보
→ 방앗간 수리
→ 새 길떡 생산
→ CH1_PATH_CAKE_MILL 발급
```

- 기존 `moonmill.mill.repaired` 플래그를 유지한다.
- 새 호환 플래그 `CH1_ROUTE_MILL_COMPLETE`를 함께 기록한다.
- 길떡을 별문에 넣기 전까지는 겨울 저장고 길떡과 교체할 수 있다.
- 보상은 되돌아오는 달떡 또는 기존 절구 강화로 연결한다.

### 7.2 Route B: 달광산 탐색

```text
기존 폭발열매·결정 지하 진입
→ 별가루 광석 운반
→ 절구로 광산 길떡 제작
→ CH1_PATH_CAKE_MINE 발급
```

- 현재 별가루 지하와 깊은 저장고 방을 재사용한다.
- 신규 방을 만들기보다 경로 시작 표식, 광석 목표, 제작대만 추가한다.
- 추락과 폭발열매 과부하를 실패 위험으로 사용한다.

### 7.3 Route C: 겨울 저장고

```text
겨울 저장고 진입
→ 저장 길떡 차용
→ CH1_PATH_CAKE_STORAGE 발급
```

- 가장 빠른 경로로 유지한다.
- 가져간 수량과 경고 확인 여부를 기록한다.
- 별문 장착 전에는 돌려놓을 수 있다.
- 방앗간 수리 후 새 길떡과 교체하면 식량 페널티를 제거한다.

### 7.4 CH1 별문 허브

현재 별 연료통 위치를 별문 허브로 전환한다.

상호작용은 상태에 따라 달라진다.

```text
기여 없음: 보유한 길떡 기여 물건 장착
1/2: 두 번째 기여 물건 장착
2/2: GateReady 안내
GateReady: 손잡이 당기기
GateActive: 별문 상태 확인
```

GateActive 시 동시에 수행한다.

- 출항 달배 활성화
- 달 뒤편 저장고 입구 활성화
- Bell 1 재생
- GateActive 이후 Alert 증가 시작
- `gate_activated` 행동 기록

### 7.5 CH1 유혹 구역

현재 달 뒤편 창고와 안쪽 분기 방을 재사용한다.

- GateReady만으로는 열리지 않는다.
- GateActive 이후에만 열린다.
- 가방 슬롯, 자동 절구, 벽화 단서를 단계적으로 배치한다.
- 유혹 구역 입장과 자동 절구 작동은 Alert를 올린다.

### 7.6 CH1 출항

- GateReady 상태에서는 출항할 수 없다.
- GateActive 이후에는 언제든 즉시 출항할 수 있다.
- Bell 3에서는 문 닫힘 연출과 제한 시간을 적용한다.
- Bell 3 이전 출항은 일반 도장, 닫히는 중 탈출은 `간발의 차` 도장을 기록한다.

## 8. M1 파일 작업 범위

### 신규 예정

- `Assets/Scripts/Content/StarNight/Core/ChapterLoopTypes.cs`
- `Assets/Scripts/Content/StarNight/Core/ChapterLoopDirector.cs`
- `Assets/Scripts/Content/StarNight/Core/RunRouteMap.cs`
- `Assets/Scripts/Content/StarNight/Gameplay/StarGateController.cs`
- `Assets/Scripts/Content/StarNight/Gameplay/GateRouteObjective.cs`
- `Assets/Scripts/Content/StarNight/Gameplay/GateContributionInventory.cs`
- `Assets/Scripts/Content/StarNight/Gameplay/BellChasePresenter.cs`
- `Assets/Scripts/Content/StarNight/Gameplay/TemptationGate.cs`
- `Assets/Scripts/Content/StarNight/Gameplay/MoonMillRouteObjectives.cs`

### 수정 예정

- `StarNightTypes.cs`
- `StarNightRunState.cs`
- `StarNightChapterState.cs`
- `StarNightActionSystems.cs`
- `MaruDirector.cs`
- `StarNightHUD.cs`
- `MoonMillChapterBootstrap.cs`
- `MoonMillRepairStation.cs`
- `MoonMillFuelPedestal.cs`
- `MoonMillTemptationDoor.cs`
- `MoonMillDepartureGate.cs`
- `StarNightFantasyExpansionBuilder.cs`
- `StarNightCoreTests.cs`

## 9. M1 자동 테스트

최소 아래 테스트를 추가한다.

1. 세 Route 중 임의의 두 개로만 GateReady가 된다.
2. 같은 Route 기여를 중복 장착할 수 없다.
3. GateReady 전에는 가동할 수 없다.
4. GateReady 상태에서도 GateActive 전에는 출항할 수 없다.
5. GateReady 상태에서도 마루 방울은 시작되지 않는다.
6. GateActive가 되면 출구와 유혹 구역이 동시에 열린다.
7. GateActive 직후 Bell 1만 발생한다.
8. GateActive 이전 냄새가 높아도 Bell 2로 즉시 점프하지 않는다.
9. Bell 2에서 마루가 물건과 NPC를 우선한다.
10. Bell 3에서만 플레이어 직접 추격이 시작된다.
11. Route C 기여를 장착하기 전 반환할 수 있다.
12. Route C를 새 길떡으로 교체하면 식량 페널티가 제거된다.
13. 출항 보고서에 Route, GateActive, BellPhase가 기록된다.
14. 기존 다섯 생활 도구의 회귀 테스트가 모두 통과한다.

## 10. M2 — CH1 플레이테스트 게이트

M1이 컴파일되고 자동 테스트를 통과해도 바로 CH2로 넘어가지 않는다.

확인 질문:

1. 시작 후 60초 안에 길떡 2개가 목표임을 이해하는가?
2. 세 경로가 안전, 위험, 빠른 전용으로 구분되는가?
3. 2/2와 별문 가동이 서로 다른 단계임을 이해하는가?
4. 별문을 켜는 행동이 추격의 시작임을 이해하는가?
5. 달 뒤편 저장고가 필수가 아니라 욕심으로 느껴지는가?
6. 세 번째 방울에서 실패한 이유를 설명할 수 있는가?

중단 기준:

- 목표 이해도 80% 미만
- 별문 가동을 추격 시작으로 이해한 비율 60% 미만
- 한 Route의 선택률이 10% 미만
- 유혹 구역이 강제라고 느끼는 비율 40% 이상

위 항목 중 두 개 이상 실패하면 CH2 이식을 중단하고 CH1을 다시 조정한다.

## 11. 이후 이식 순서

### M3-1. CH2 까치다리

- 새 닻
- 폭풍탑 예비 닻
- 옛 물류 다리 전용
- 해치는 별문 기여와 분리된 필수 사건으로 유지

### M3-2. CH4 별 우체국

- 정규 주소 조각
- 반송 불가 주소 조각
- 봉인 편지 주소 인장
- 메인 목표와 진실 탐색 분리가 작동하는지 조기 검증

### M3-3. CH3 구름고래 목장

- 목장 수차의 맑은 바람
- 폭풍 능선의 거센 바람
- 구루 강제 기상의 숨결
- 구루 해방은 별문 기여와 별도 사건으로 유지

### M3-4. CH5 잠든 해님의 정원

- 저장 햇빛
- 온실 꼭대기 햇빛
- 해오름 강제 기상
- 현재 별길나무 3회 성장은 길꽃 기여 2/2로 교체
- 과열, 화재, 복구, 자연 기상, 강제 기상 결과는 유지
- 해바라기 꼭대기는 GateActive 유혹 구역으로 전환

### M4. 프롤로그와 여행 티켓

- 마루의 우주선 구조 장면
- 다섯 별문과 북극성 최종 목표
- 우주선 휴식 구간
- 마루 발자국 한 정거장 이동

### M5. 북극성 관측소

- 다섯 별문 복구 조건
- 중심별 선점 추격
- 기존 도구 다섯 개 종합
- 네 엔딩 행동 검증

### M6. 밸런스와 회귀 QA

- 일반 클리어 45~60분
- 별길 엔딩 60~80분
- 방울 Alert 곡선
- 경로 선택률
- 선택 탐험 진입률
- 기존 조합과 결과 modifier
- 사고 보고서의 Gate/Bell 문맥

## 12. M1-A 구현 결과와 다음 작업 단위

M1-A에서 아래 공통 골격을 구현했다.

- `ChapterLoopState`, `GateRouteState`, `GateRouteArchetype`, `StarBellPhase`
- `ChapterLoopDirector`
- `StarGateController`
- `GateRouteObjective`
- `GateContributionInventory`
- `RunRouteMap`
- `StarNightRunState`와 `StarNightChapterState` 호환 어댑터
- Route/Gate/Bell 문맥을 포함하는 ActionRecord
- 기존 챕터를 보존하는 `useGateLoop=false` 기본값
- 공통 상태 전환 EditMode 테스트 8개

검증 결과:

- 전체 EditMode 테스트 `51/51` 통과
- C# 컴파일 오류 0
- GateReady와 GateActive 분리 확인
- GateActive 이전 출항 차단 확인
- 수동 가동 시 출구와 유혹 구역 동시 개방 확인
- GateActive 이전 냄새를 가동 기준선으로 보존
- 방울 단계 건너뛰기 차단
- 세 번째 방울에서 GateClosing 전환
- 레거시 `AddDepartureProgress` 동작 유지

다음 작업은 **M1-B CH1 세 경로와 별문 허브 연결**이다.

1. `MoonMillChapterBootstrap`에 v0.2 Route A/B/C 데이터를 연결한다.
2. 방앗간 수리 완료 시 새 길떡을 발급한다.
3. 기존 별가루 지하를 달광산 Route B로 묶는다.
4. 겨울 저장고 길떡을 Route C 차용·반환 대상으로 바꾼다.
5. 기존 별 연료통을 2/2 별문 허브로 전환한다.
6. 달배와 달 뒤편 창고를 GateActive 조건으로 교체한다.
7. CH1 씬을 재생성하고 직접 플레이로 2개 이상의 경로 조합을 검증한다.

M1-B에서는 아직 `MaruDirector`의 직접 추격 규칙을 교체하지 않는다.
방울과 추격은 M1-C에서 별도로 연결한다.

## 13. M1-B 구현 결과와 다음 작업 단위

M1-B에서 확장판 CH1을 v0.2 공통 별문 루프에 연결했다.

- Route A `CH1_ROUTE_MILL`: 방앗간 수리 뒤 방앗간 길떡을 생산한다.
- Route B `CH1_ROUTE_MINE`: 깊은 결정 분기에서 별가루 광석을 가져와 광산 길떡을 생산한다.
- Route C `CH1_ROUTE_STORAGE`: 겨울 저장고 길떡을 빠르게 차용하며, 별문 장착 전에는 반환할 수 있다.
- 기존 연료통은 서로 다른 길떡 두 개를 받는 `StarGateController`로 교체했다.
- `2/2`는 `GateReady`일 뿐이며, 플레이어가 다시 상호작용해 손잡이를 당겨야 `GateActive`가 된다.
- 달배 출구와 달 뒤편 유혹 구역은 `GateActive`에서만 동시에 열린다.
- GateActive 전의 물리 장벽은 가동 직후 비활성화된다.
- 겨울 저장고 길떡을 실제 장착하면 CH2 시작 별냄새 `+3`, 증가 배율 `×1.04`가 남는다.
- Route 완료·기여·반환과 Gate 가동을 행동 및 라니 기록 문맥에 추가했다.
- HUD가 세 Route 상태, `0/2`~`2/2`, 수동 가동 단계와 미장착 길떡을 표시한다.

호환 처리:

- 확장판 CH1 빌더만 `useGateLoopV02=true`를 사용한다.
- 기존 챕터와 레거시 빌더는 `useGateLoop=false` 기본 경로를 유지한다.
- M1-C 전까지 v0.2 CH1에서는 기존 냄새 임계치 기반 마루 자동 추격을 정지한다.
- 방울 단계와 새 마루 행동은 이번 단계에 임시 구현하지 않았다.

검증 결과:

- 전체 EditMode 테스트 `55/55` 통과
- C# 컴파일 오류 0
- 재생성한 `StarNight_MoonMill` 씬 누락 스크립트 0, 깨진 프리팹 0
- Main Camera와 Directional Light 존재 확인
- 시작 상태 `RouteOpen`, Route 3개, 기여 `0/2`, 출구·유혹 구역 잠금 확인
- Route C 차용 뒤 기여 `1`, 반환 뒤 Route `Available` 및 기여 `0` 복원 확인
- Route A+B 완료와 기여 뒤 `GateReady` 확인
- 수동 가동 뒤 `GateActive`, 출구·유혹 구역 개방, 물리 장벽 해제 확인
- 새 방울 시스템 전 단계이므로 위 과정에서 마루 직접 추격이 시작되지 않음을 확인

다음 작업은 **M1-C 방울과 마루 추격 연결**이다.

1. `BellChasePresenter`를 구현하고 GateActive 시점의 냄새를 기준선으로 저장한다.
2. GateActive 직후 Bell 1만 발생시키고 직접 추격은 금지한다.
3. 가동 후 Alert 증가분 30/60에서 Bell 2/3으로 전환한다.
4. Bell 2에서는 마루가 같은 정거장의 물건·NPC를 우선하도록 한다.
5. Bell 3에서만 플레이어 직접 추격과 GateClosing을 시작한다.
6. 방울·환경 변화·마루 상태를 HUD와 행동 기록에 연결한다.
7. GateActive 이전 높은 냄새가 Bell 단계를 건너뛰지 않는지 자동·직접 플레이로 검증한다.

## 14. M1-C 구현 결과와 M2 진입 조건

M1-C에서 별문 가동 이후의 긴장 곡선을 실제 CH1 플레이에 연결했다.

- GateActive 시점의 기존 별냄새를 기준선으로 저장한다.
- 전역 별냄새가 100에 도달해도 경보가 멈추지 않도록 `PostGateAlert`를 별도 누적한다.
- GateActive 뒤 발생한 양의 별냄새만 경보에 더하며, 마루가 물건을 가져가 낮춘 냄새는 경보를 되감지 않는다.
- GateActive 직후 Bell 1이 발생하지만 마루 본체는 숨고 지붕의 흔적만 나타난다.
- 경보 30에서 Bell 2가 발생하고 마루가 정거장에 진입해 물건과 주민만 우선 추적한다.
- 경보 60에서 Bell 3가 발생하고 마루가 플레이어를 직접 추적하며 `GateClosing`이 시작된다.
- 한 번의 큰 사고로 경보 60을 넘더라도 Bell 2와 Bell 3 행동 기록을 순서대로 남긴다.
- 방앗간지기 묘월을 `MaruNpcTarget`으로 연결해 Bell 2의 주민 표적을 실제 월드 대상으로 만들었다.
- Bell 1 흔적, Bell 2 붉은 그림자, Bell 3 닫히는 별문 빛을 CH1 씬에 추가했다.
- HUD의 가동 이후 표시를 숫자형 별냄새에서 세 개의 방울과 마루 행동 설명으로 전환했다.
- Bell 3에서는 카메라가 약하게 떨리고 달배 상호작용 문구가 `닫히는 별문으로 뛰어들기`로 바뀐다.
- Bell 3 출항은 `CH1_NARROW_ESCAPE`로 기록한다.
- 챕터 출항 행동에는 최종 기여 수, GateReady, GateActive, BellPhase가 함께 보존된다.

호환 처리:

- 기존 P0~P4 레거시 챕터는 냄새 임계치와 5초 경고 뒤 직접 추격하는 기존 경로를 유지한다.
- `useGateLoop=true`인 v0.2 챕터에서만 `BellChasePresenter`가 마루 단계를 명시적으로 지시한다.
- Bell 1과 Bell 2에서는 시간 경과만으로 플레이어 직접 추격으로 바뀌지 않는다.

검증 결과:

- 전체 EditMode 테스트 `60/60` 통과
- C# 컴파일 오류 0
- 재생성한 CH1 씬 검증 오류 0, 누락 스크립트 0, 깨진 프리팹 0
- `BellChasePresenter`, `MaruDirector`, `MaruNpcTarget` 각 1개 연결 확인
- 가동 전 별냄새 92 → GateActive 기준선 92, `PostGateAlert 0`, Bell 1 확인
- 가동 후 경보 30 → Bell 2, `StationHunt`, 플레이어 표적 금지 확인
- Bell 2의 한 프레임 추적 판정이 실제 `Object`를 선택함을 확인
- 누적 경보 60 → Bell 3, `PlayerHunt`, GateClosing과 닫힘 시각 효과 확인
- 플레이 모드 콘솔 오류 0

이제 M1 전체가 연결되었으므로 다음 단계는 **M2 CH1 플레이테스트 게이트**다.

1. 첫 60초 안에 세 경로 중 두 길떡이라는 목표가 읽히는지 측정한다.
2. Route A/B/C가 협력·위험·빠른 전용으로 구별되는지 확인한다.
3. `2/2`와 손잡이 가동이 별개의 선택으로 이해되는지 확인한다.
4. Bell 1에서 즉시 출항과 유혹 구역 잔류가 실제 선택으로 갈리는지 확인한다.
5. Bell 2의 물건·주민 위험과 Bell 3의 직접 추격이 시각적으로 구분되는지 확인한다.
6. 세 번째 방울 실패 시 플레이어가 원인을 설명할 수 있는지 확인한다.

## 15. M2 내부 플레이테스트 1차 결과

자동·에이전트 플레이로 검증할 수 있는 기술 게이트와 첫 가독성 조정을 완료했다.

추가한 계측:

- `ChapterPlaytestTelemetry`
- 첫 Route 완료 시각
- GateReady와 GateActive 시각
- Bell 2와 Bell 3 도달 시각
- 선택 창고 진입 여부와 시각
- 실제 장착 Route 조합
- 출항·마루 강제 귀가 결말
- 완료 시 Unity Console의 `[M2 Playtest]` 한 줄 요약

첫 조정:

- Route 표시를 `A 안전·협력`, `B 위험·탐색`, `C 빠름·차용`으로 통일했다.
- 시작 화면에 세 역할과 `세 경로 중 두 곳만 선택`을 별도 줄로 표시했다.
- 두 번째 길떡 장착 토스트가 `2/2는 준비일 뿐`이며 다시 상호작용하면 추격이 시작됨을 설명한다.
- 별문 월드 표지가 `0/2 → 2/2 손잡이 → 방울 1~3`으로 실시간 전환된다.
- 선택 창고 문구를 `출항을 미루고 들어가는 위험한 선택`으로 명시했다.
- 세 번째 방울 강제 귀가 기록과 결말 문장이 실패 원인을 직접 설명한다.

내부 검증:

- A+B, A+C, B+C 세 조합 모두 GateReady 도달
- A+C 실제 씬 스모크에서 Route 조합, GateReady, GateActive, 선택 창고, Bell 2, 출항 기록 확인
- 동적 별문 표지 `길떡 2/2 · 손잡이 → 별문 가동 · 방울 ●○○` 확인
- 전체 EditMode 테스트 `66/66` 통과
- C# 컴파일 오류 0
- CH1 씬 누락 스크립트 0, 깨진 프리팹 0

자동 스모크의 시간값은 실제 플레이 시간이 아니므로 60초 목표 이해도 판정에는 사용하지 않는다.
목표 이해도, 추격 시작 이해도, Route 선택률, 선택 창고 강제 인식은
`M2_CH1_PLAYTEST_CHECKLIST.md`의 실제 사람 5세션 표본을 채운 뒤 최종 판정한다.

## 16. M3-1 CH2 까치다리 이식 결과

기존 P1 붉은 실 물리 퍼즐과 해치 선택을 삭제하지 않고 공통 별문 루프에 연결했다.

세 경로:

- Route A `CH2_ROUTE_NEW_ANCHOR`: 까치들과 새 닻 설치
- Route B `CH2_ROUTE_STORM_ANCHOR`: 폭풍탑 예비 닻 회수
- Route C `CH2_ROUTE_OLD_BRIDGE`: 옛 물류 다리의 낡은 닻 전용
- 세 경로 중 서로 다른 닻 두 개만 별문에 장착한다.
- Route C는 GateReady 전에 다시 상호작용하면 대체 닻을 설치해 다음 챕터 물류 감소를 수습한다.
- GateReady 이후에는 대체 닻 설치를 막아 선택 비용을 고정한다.

별도 사건:

- 해치의 떠날 권리는 별문 기여 수를 올리지 않는다.
- 해치 사건을 직접 해결하지 않으면 GateActive 뒤에도 별기차가 출항하지 않는다.
- 문 잠금, 붉은 실 구속, 길을 열어 두는 기존 선택과 라니의 편향 기록은 유지한다.

별문과 유혹:

- 별문 허브의 표지가 `닻 0/2 → 닻 2/2 손잡이 → 방울 1~3`으로 전환된다.
- 별문을 직접 켠 뒤에만 별기차 출구와 까마득한 별사다리 선택이 활성화된다.
- 별사다리는 가동만으로 통과되지 않으며, 입구에서 위험한 잔류를 직접 선택해야 봉인이 열린다.
- 끊어지지 않는 매듭은 별사다리 진입 기록이 없으면 획득할 수 없다.
- Bell 2에서는 빛나는 짐과 해치·지친 까치가 마루의 비플레이어 표적이 된다.

내부 검증:

- A+B, A+C, B+C 세 조합 모두 GateReady 도달
- 실제 CH2 씬 A+C 스모크에서 `닻 2/2`, 수동 GateActive, 별사다리 진입, Bell 2 확인
- 해치 미해결 시 출항 차단, 직접 선택 뒤 출항 문구 복구 확인
- CH2 완료 시 여행 티켓의 까치다리 별문 도장과 Route 조합 계측 확인
- EditMode 테스트 73개 완료, 실패 0
- C# 컴파일 오류 0
- CH2 씬 누락 스크립트 0, 깨진 프리팹 0

M2 사람 5세션 표본은 여전히 미수집 상태다. 사용자의 진행 승인에 따라 M3-1 기술 이식을
선행했지만, 이후 밸런스 확정 시 `M2_CH1_PLAYTEST_CHECKLIST.md` 결과를 다시 반영한다.

## 17. M3-2 CH4 별 우체국 이식 결과

기존 P3의 별 우표 배송, 마지막 편지, 라니 반응과 분류기 결과를 보존하면서
주소 조각 두 개를 선택해 별문을 복구하는 v0.2 루프를 연결했다.

세 경로:

- Route A `CH4_ROUTE_REGULAR_POST`: 손상된 달 모양 소포를 `MOON` 주소로 정상 배송해 정규 주소 조각을 얻는다.
- Route B `CH4_ROUTE_DEAD_LETTER`: 수신자 없는 위험 소포를 `VAULT` 주소로 보내 폐기 주소 조각과 희귀 노선 우표를 얻는다.
- Route C `CH4_ROUTE_SEALED_LETTER`: 라니의 마지막 편지 봉인에서 붉은별 주소 인장을 얻는다.
- Route C의 보존 해법은 Route A의 주소 격자 지식을 요구하며, 편지를 열지 않고 주소만 복사한다.
- 빠른 해법은 봉인을 주소판에 직접 찍어 조각을 즉시 얻지만 봉인 훼손과 라니 논쟁을 남긴다.
- 세 경로 중 서로 다른 주소 조각 두 개만 별문에 장착한다.

메인 기록과 선택 진실:

- 메인 동선의 라니 명령 일부는 다음 챕터 맥락에 필수이며, 읽지 않으면 출항할 수 없다.
- 메인 기록은 “떠난 아이들을 모두 집으로 데려와.”까지만 보여 준다.
- GateActive 이후 반송 불가 심층 보관소를 직접 열면 라니가 동생 실종 직후 명령을 내렸다는 전체 맥락을 선택적으로 확인할 수 있다.
- 심층 진실과 희귀 우표는 보관소 진입 없이 우회 획득할 수 없다.
- 심층 보관소를 열고도 전체 기록을 읽지 않고 떠난 경우 미해결 사건으로 행동 기록에 남는다.

별문과 방울:

- 실제 소포 배송 이벤트가 Route A/B 완료를 판정한다.
- 월드 표지는 `주소 0/2 → 주소 2/2 손잡이 → 방울 1~3`으로 전환된다.
- 세 번째 미장착 주소 조각이 남아 있어도 GateActive 프롬프트는 가동 상태를 우선한다.
- Bell 1은 우표 번짐, Bell 2는 마루의 편지·빛 가로채기, Bell 3은 목적지 우체통 이동으로 표현한다.
- 기존 마지막 편지의 열기·보존·분해·라니 배송 결과와 다음 챕터 수정자는 삭제하지 않았다.

내부 검증:

- A+B, A+C, B+C 세 조합 모두 GateReady 도달
- 실제 CH4 씬에서 Route A/B 소포를 지정 주소로 배송하고 Route C 봉인 보존을 확인
- 주소 2/2 뒤 수동 GateActive, 메인 기록 미확인 출항 차단, 심층 보관소와 전체 진실 확인
- Bell 2 진입과 정상 출항, 별 우체국 여행 티켓 도장, Route 조합 계측 확인
- 별 우체국 씬 구성: Route objective 3, 배송 tracker 2, 봉인 선택 2, 진실 기록 2, 별문 1
- Main Camera 1, Directional Light 1
- EditMode 테스트 82개 완료, 실패 0
- C# 컴파일 오류 0
- CH4 씬 누락 스크립트 0, 깨진 프리팹 0

다음 기술 이식은 **M3-3 CH3 구름고래 목장**이다.
M2 사람 5세션 표본은 계속 별도 미완료 항목으로 유지한다.

## 18. M3-3 CH3 구름고래 목장 이식 결과

기존 P2의 구름병 무게 보존, 비구름 수차 3개, 구루의 닻과 이동식 수차 결과를
삭제하지 않고 출항 돛에 서로 다른 바람 두 개를 채우는 v0.2 루프로 연결했다.

세 경로:

- Route A `CH3_ROUTE_RANCH_WHEEL`: 첫 비구름에 실제 하중을 옮겨 목장 수차를 복구하고 `맑은 바람`을 얻는다.
- Route B `CH3_ROUTE_STORM_RIDGE`: 폭풍 구역의 비구름을 거센 바람 속 수차까지 내려 `거센 바람`을 얻는다.
- Route C `CH3_ROUTE_GURU_BREATH`: 구루의 방울을 세 번 울려 `구루의 숨결`을 가장 빠르게 얻는다.
- 중간 비구름 수차는 Route 기여 없이 목장의 비와 주민 상태를 안정시키는 보조 복구로 남긴다.
- Route C 완료 뒤 GateReady 전에는 자장가 하중 분산 장치로 구루를 다시 재워 폭풍 피해를 수습할 수 있다.
- GateReady 뒤에는 숨결이 별문에 고정되므로 사후 무비용 수습을 차단한다.

별도 사건과 필수 정보:

- 구루의 닻 해방은 바람 기여 수를 올리지 않는 별도 개입이다.
- 구루를 풀어 준 뒤 이동식 비구름 장치를 만들면 가뭄과 폭풍 결과를 수습한다.
- 장치를 만들지 않고 떠나면 별 우체국 가뭄과 미해결 사건 기록이 남는다.
- 시작 장면에서 마루가 길 잃은 새끼 고래를 어미 곁에 돌려놓고 바람별을 가져가는 필수 정보를 자동 기록한다.
- 이 장면은 마루가 악의로만 움직이지 않는다는 일반 진행 핵심 정보다.

별문과 유혹:

- 세 경로 중 두 바람만 별문 출항 돛에 직접 장착한다.
- `바람 2/2`는 GateReady이며, 손잡이를 직접 당긴 뒤에만 출항과 무지개 위쪽 목장이 열린다.
- 무지개 목장은 GateActive만으로 통과되지 않고 입구에서 위험한 잔류를 직접 선택해야 열린다.
- 큰 구름병 보상은 무지개 목장 진입 기록 없이 우회 획득할 수 없다.
- 월드 표지는 `바람 0/2 → 바람 2/2 손잡이 → 방울 1~3`으로 전환된다.

방울 변형:

- Bell 1: 구름이 한쪽으로 밀리는 흔적
- Bell 2: 마루가 공중 물체와 새끼 고래를 우선 추적
- Bell 3: 전체 풍향이 뒤집히고 별문이 닫히기 시작함

내부 검증:

- A+B, A+C, B+C 세 조합 모두 GateReady 도달
- 실제 CH3 씬에서 구름병으로 하중을 A/C 비구름에 옮겨 Route A+B 완료
- 바람 2/2 뒤 수동 GateActive, 가동 전 출항 차단, 무지개 목장 직접 선택 확인
- 구루 해방이 기여 수를 올리지 않으며 이동식 수차 복구 뒤 가뭄이 남지 않음을 확인
- Bell 2, 정상 출항, 구름고래 목장 여행 티켓 도장과 Route 조합 계측 확인
- 씬 구성: Route objective 3, 수차 3, 구루 방울 1, 수습 장치 1, 무지개 입구 1, 필수 장면 1, 별문 1
- Main Camera 1, Directional Light 1
- EditMode 테스트 92개 완료, 실패 0
- C# 컴파일 오류 0
- CH3 씬 누락 스크립트 0, 깨진 프리팹 0

다음 기술 이식은 **M3-4 CH5 잠든 해님의 정원**이다.
M2 사람 5세션 표본은 계속 별도 미완료 항목으로 유지한다.

## 19. M3-4 CH5 잠든 해님의 정원 이식 결과

기존 P4의 햇빛 씨앗, 성장 단계, 해오름 기상, 정원 과열·화재·복구 결과를
삭제하지 않고 길꽃 두 송이를 별문에 심는 v0.2 루프로 연결했다.

세 경로:

- Route A `CH5_ROUTE_STORED_SUNLIGHT`: 서로 다른 저장원 세 곳의 햇빛을 모아 `고른 빛`을 얻는다.
- Route B `CH5_ROUTE_GREENHOUSE_TOP`: 과성장 덩굴 상단을 올라 우산 반사 각도를 두 번 맞춰 `높은 빛`을 얻고, 덩굴·빛나방·귀환 장벽이 실제로 활성화된다.
- Route C `CH5_ROUTE_HAOREUM_WAKE`: 저장 햇빛을 소비해 해오름을 강제로 깨우고 `해오름 빛`을 얻는다.
- 세 경로 중 서로 다른 빛 두 개만 길꽃 별문에 직접 심는다.
- 자연 기상은 해오름의 자율성과 안정 광원을 보존하지만 Route C 완료로 위장하지 않는다.
- 강제 기상은 정원 열 `+45`, 해오름 피로, 큰 별냄새를 그대로 남긴다.

필수 정보와 유혹:

- 시작 장면에서 마루가 작은 해씨를 화분에 돌려놓고 “모두 집으로. 아무도 잃지 않게.”를 반복한다.
- 이 장면은 한 번만 기록되며 메인 동선에서 반드시 발생한다.
- `길꽃 2/2`는 GateReady이고, 손잡이를 직접 당겨야 GateActive와 첫 방울이 시작된다.
- 해바라기 너머의 멈춘 방은 GateActive만으로 통과되지 않고 입구에서 위험한 잔류를 직접 선택해야 열린다.
- 멈춘 방 안에서만 마루의 최초 명령 원본과 `CH5_FINAL_LIGHT_SUPPORT`를 얻을 수 있다.
- 강한 광원, 과성장, 불길은 GateActive 이후 경보를 올려 두 번째·세 번째 방울을 앞당긴다.

방울 변형:

- Bell 1: 꽃들이 출구 반대편을 바라보는 흔적
- Bell 2: 마루가 빛나는 씨앗과 깨어난 생명체를 우선 추적
- Bell 3: 모든 광원을 잇는 눈길이 숨은 길과 플레이어를 동시에 드러냄

내부 검증:

- A+B, A+C, B+C 세 조합 모두 GateReady 도달
- 실제 CH5 씬에서 저장 햇빛 세 곳과 온실 반사 두 번으로 Route A+B 완료
- 길꽃 2/2 뒤 수동 GateActive, 가동 전 출항 차단, 멈춘 방 직접 진입 확인
- 최초 명령 원본과 최종전 빛 보조, Bell 2, 정상 출항과 북극성 관측소 전환 기록 확인
- 정원 과열·화재·복구, 해오름 자연/강제 기상, 별길 나무 레거시 결과 보존
- 씬 구성: GameObject 290, Route objective 3, 성장 대상 10, 별문 1, 길꽃 표시 1, 멈춘 방 입구 1, 필수 장면 1
- Main Camera 1, Directional Light 1
- EditMode 테스트 102개 완료, 실패 0
- C# 컴파일 오류 0
- CH5 씬 누락 스크립트 0, 깨진 프리팹 0
- 실제 PlayMode 스모크 오류·경고 0

다음 기술 단계는 **M4 프롤로그·여행 티켓·전체 흐름 통합**이다.
M2 사람 5세션 표본은 계속 별도 미완료 항목으로 유지한다.

## 20. M4 프롤로그·여행 티켓·전체 흐름 통합 결과

### 프롤로그 사건 연쇄

새 빌드 인덱스 0에 `StarNight_Prologue`를 추가했다.
플레이어가 사실을 순서대로 확인해야 다음 비트가 열리며, 장면을 건너뛰어 원인과 목표가 분리되지 않는다.

1. 고장 난 여행 우주선과 산소 누출
2. 귀환떡 표지판과 라니의 진단 확인
3. 귀환떡을 엔진에 사용
4. “집으로 돌아가려는 힘” 때문에 우주선 폭주
5. 마루가 우주선을 입으로 붙잡아 조심스럽게 내려놓는 구조
6. 마루가 길잡이별을 물어오고 다섯 항로가 꺼짐
7. 라니가 여행 티켓의 다섯 별문과 북극성 목표를 공개
8. CH1 달토끼 방앗간으로 출항

핵심 사실은 `PROLOGUE_USED_RETURN_CAKE`, `PROLOGUE_CHECKED_SIGN`,
`PROLOGUE_CHECKED_COMPANION`, `PROLOGUE_MARU_RESCUE_SEEN`,
`PROLOGUE_GUIDE_STAR_TAKEN`, `TICKET_MAP_UNLOCKED`에 보존한다.
CH1 부트스트랩은 이미 활성화된 런을 다시 초기화하지 않으므로 프롤로그의 원인과 목격 정보가 사라지지 않는다.

### 전체 런 여행 티켓

- 5개 일반 별문과 북극성 관측소를 한 패널에 표시한다.
- 되찾은 별문 수, 플레이어 위치, 마루 위치를 함께 표시한다.
- 프롤로그에서는 목표 공개 전까지 숨기고, 이후에는 모든 챕터에서 `T`로 접고 펼칠 수 있다.
- 챕터 시작과 별문 도장 시 플레이어·마루 표식을 갱신한다.
- 마루 표식은 정거장 사이를 발자국 이동처럼 보간한다.
- `RunRouteMapSnapshot`은 도장과 양쪽 위치를 JSON 왕복 가능한 형태로 보존한다.

### 통일한 우주선 휴식 순서

모든 챕터의 출항 요약을 다음 6단계로 고정했다.

1. 여행 티켓 도장
2. 대표 행동 짧은 되감기
3. 라니의 사실 문장
4. 플레이어의 챕터별 응답
5. 마루 발자국의 다음 정거장 이동
6. 다음 목적지와 목표

CH1~CH4 씬 전환 대기 시간은 공통 상수 `5.2초`를 사용한다.
CH5 요약은 북극성 관측소를 다음 목표로 가리키고, 5.2초 뒤 M5 관측소 씬을 직접 불러온다.

### 내부 검증

- EditMode 114개 완료, 실패 0
- 프롤로그 사건 선행 조건, 티켓 해금, 도장 중복 방지, 스냅샷 왕복, 6단계 휴식 순서 자동 검증
- 프롤로그부터 다섯 챕터까지 모의 완료 후 별문 `5/5`, 플레이어/마루 북극성 위치 확인
- 빌드 순서 `Prologue → CH1 → CH2 → CH3 → CH4 → CH5`
- 프롤로그 씬 12개 스토리 구역, 핵심 상호작용 6개
- Main Camera 1, Directional Light 1
- 누락 스크립트 0, 깨진 프리팹 0
- 실제 PlayMode에서 프롤로그 전 비트 실행 후 CH1 로드, 핵심 사실 4종과 보고서 유지 확인
- 실제 PlayMode 콘솔 오류·경고 0

다음 기술 단계는 **M5 북극성 관측소와 네 엔딩 통합**이다.
M2 사람 5세션 표본은 계속 별도 미완료 항목으로 유지한다.

## 21. M5 북극성 관측소와 네 엔딩 통합 결과

CH5 출항 뒤 런을 종료하던 임시 처리를 제거하고
`StarNight_PolarisObservatory`를 빌드 인덱스 6에 연결했다.
프롤로그와 다섯 챕터에서 만든 선택은 최종전의 진입 조건, 추격 시간,
별길 엔딩의 네 조건으로 다시 사용된다.

### 최종전 진행

1. 여행 티켓의 별문 도장 `5/5`가 아니면 관측소 진입 상태가 잠긴다.
2. 기록 회랑에서 CH1~CH5의 대표 행동을 한 번씩 확인한다.
3. 닫힌 관측실에서 라니의 최초 명령과 마루의 오해를 확인한다.
4. 중심별 카운트다운이 시작되고 마루는 이전 런에서 가장 많이 쓴 도구를 역이용한다.
5. 절구 → 붉은 실 → 구름병 → 별 우편 도장 → 햇빛 씨앗 순서로 별길을 복구한다.
6. 중심별을 선점한 뒤 월드의 네 상호작용 중 하나를 실제로 선택한다.

카운트다운은 기본 150초다. 해오름 자연 기상, 정원 복구, 최종 빛,
안정된 별길 나무는 시간을 늘리고 강제 기상, 방치한 화재, 과성장·소실한
별길 나무는 시간을 줄인다. 시간이 끝나면 마루가 먼저 중심별에 도달해
`닫힌 우주`로 강제 확정된다.

### 네 엔딩

- `길을 끊는 사람`: 마루와 중심별의 연결을 잘라 항로를 복구하지만 마루와 라니의 책임을 남긴다.
- `새 목줄`: 붉은 실로 마루의 명령권을 플레이어에게 연결한다.
- `닫힌 우주`: 떠나거나 시간 초과로 중심별을 마루에게 돌려보내 모든 여행을 닫는다.
- `별길`: 기억, 연결, 배송, 빛의 네 조건을 충족해 라니를 전장으로 보내고 라니가 직접 명령을 거둔다.

별길의 기억 조건은 파괴되지 않은 마지막 편지 또는
`라니 명령 전체 맥락 + 보존된 동생 화분`의 대체 기억으로 충족한다.
연결에는 붉은 실과 마루의 최초 명령, 배송에는 별 우편 도장과
라니 배송 가능 상태·북극성 항로, 빛에는 CH5 최종 빛 보조가 필요하다.

### 라니의 최종 반박

기록 회랑은 과거 행동을 삭제하지 않고 수습과 문맥을 함께 남긴다.
최종 반박은 아래 세 문장으로 고정했다.

> 당신이 왜 붙잡았는지는 이해해요.  
> 하지만 이해한다고 계속 붙잡게 둘 수는 없어요.  
> 놓아주는 말은 당신이 직접 해야 해요.

### 씬과 검증

- 2D Fantasy Station, Crystal, Island, Spring Forest 아트를 사용한 가로형 23구역 관측소
- 대표 기록 5개, 진실 기록 1개, 생활 도구 복구 노드 5개, 엔딩 선택 4개
- Main Camera 1, Directional Light 1, 체크포인트 6개
- 빌드 순서 `Prologue → CH1 → CH2 → CH3 → CH4 → CH5 → Polaris`
- 누락 스크립트 0, 깨진 프리팹 0
- EditMode 132개 완료, 실패 0
- 실제 PlayMode에서 기록 5/5, 진실 확인, 복구 5/5, 별길 조건 4/4,
  `JourneyComplete`, `POLARIS_RANI_DELIVERED`, `POLARIS_MARU_RELEASED`,
  마지막 행동 `RaniCommandWithdrawn` 확인

다음 기술 단계는 **M6 밸런스와 전체 회귀 QA**다.
M2 사람 5세션 표본은 계속 별도 미완료 항목으로 유지한다.

## 22. M6 밸런스·전체 회귀 QA 구현 결과

### 런 전체 계측과 목표 프로파일

- `StarNightBalanceProfile`에 일반 엔딩 `45~60분`, 별길 엔딩 `60~80분`과
  프롤로그부터 북극성까지 7개 챕터의 시간 예산을 한곳에 고정했다.
- 일반 엔딩은 핵심 정보 `3`단위, 별길 엔딩은 추가 조건을 포함한 `7`단위로 분리해
  엔딩별 정보량을 같은 기준으로 비교할 수 있다.
- `StarNightRunTelemetry`는 한 챕터가 아니라 전체 런의 시간, 챕터별 경과,
  실제 장착 Route, 유혹 진입, 최고 방울, 종료 Alert, 엔딩, 사고 문맥을 기록한다.
- 완료된 정상 PlayMode 런만 `StarNightTelemetryStore`에 누적한다.
  관측소 직접 실행과 M6 스모크처럼 `POLARIS_DIRECT_DEBUG_RUN` 표식이 있는 런은
  사람 플레이 표본에 섞이지 않는다.
- 유혹 진입 목표는 `55~70%`, Route 중단 기준은 챕터 내 선택률 `10% 미만`으로 정했다.

### 방울과 사고 문맥

- GateActive 순간 첫 방울이 울린다.
- 행동으로 Alert를 더하지 않아도 가동 후 `90초`에 두 번째, `180초`에 세 번째 방울이 울린다.
- 냄새를 줄이는 행동은 아직 울리지 않은 다음 방울까지 시간을 벌어 주지만,
  이미 울린 방울 단계는 되돌리지 않는다.
- 세 번째 방울은 Alert `60`과 GateClosing을 고정하며 더 이상 취소되지 않는다.
- 사고 기록은 사고 당시 `GateActivated`와 `BellPhase`를 보존하고
  보고서에 `[별문 가동 · 방울 N]` 문맥을 붙인다.

### 자동 감사와 직렬화 안정화

- `Tools > Star Night > Run M6 Balance & Regression Audit`는 7개 빌드 씬,
  챕터 시간 예산, 방울 곡선, 정보량, 15개 Route 프로파일,
  Camera/Directional Light/Missing Script, 프롤로그 사건,
  다섯 공통 별문 루프와 북극성 상호작용을 검사한다.
- 결과는 `docs/star-night/M6_AUTOMATED_QA_REPORT.md`에 다시 쓴다.
- 북극성 상호작용 컴포넌트는 각 클래스명과 같은 `.cs` 파일로 분리했다.
  도메인 리로드 뒤에도 기록 `5/5`, 도구 `5/5`, 엔딩 `4/4`가 고정 GUID로 복원된다.
- 현재 자동 구조 감사는 `25/25 PASS`, 누락 스크립트는 7개 씬 모두 `0`이다.

### 최종 검증

- 전체 EditMode 테스트 `158/158` 통과
- Moon Mill PlayMode: 런/플레이어 초기화, 미지원 글리프 `0`, 오류·경고 `0`
- 북극성 PlayMode: 기록 `5`, 도구 `5`, 엔딩 `4`, Finale 활성, 누락 스크립트 `0`,
  오류·경고 `0`
- M6 상태 스모크: 두 Route 장착, 방울 `First → Second → Third`,
  Alert `20 → 10 → 30 → 60`, 유혹 진입, 문맥 사고 `1/1`
- 디버그 스모크 전후 누적 사람 표본 `0 → 0`

M6의 기술 구현과 자동 회귀 게이트는 완료했다.
다음 게이트는 `M6_PLAYTEST_CHECKLIST.md`에 따른 일반 엔딩 5세션과
별길 엔딩 5세션이다. 현재 누적 표본은 `0런`이므로 실제 플레이타임,
Route 선택률, 유혹 진입률은 아직 합격으로 판정하지 않는다.
