# M6 자동 밸런스·회귀 QA 리포트

- 구조 감사: **PASS** (25/25)
- 누적 플레이 표본: **0런**
- 이 리포트의 구조 검증은 사람 플레이테스트의 이해도·체감 표본을 대신하지 않는다.

## 자동 구조 감사

| 결과 | 항목 | 근거 |
|---|---|---|
| PASS | 전체 여정 빌드 순서 | StarNight_Prologue → StarNight_MoonMill → StarNight_MagpieBridge → StarNight_CloudWhaleRanch → StarNight_StarPostOffice → StarNight_SleepingSunGarden → StarNight_PolarisObservatory |
| PASS | 일반 엔딩 시간 목표 | 45~60분 |
| PASS | 별길 엔딩 시간 목표 | 60~80분 |
| PASS | 챕터 시간 예산 7구간 | Prologue 5~7m, MoonRabbitMill 8~10m, MagpieBridge 8~10m, CloudWhaleRanch 8~10m, StarPostOffice 10~12m, SleepingSunGarden 8~10m, PolarisObservatory 12~18m |
| PASS | 방울 Alert 곡선 | GateActive 즉시 1차 → 90s 2차 → 180s 3차 |
| PASS | 일반/별길 정보량 분리 | 일반 3, 별길 7 정보 단위 |
| PASS | MoonRabbitMill Route A/B/C 프로파일 | CH1_ROUTE_MILL, CH1_ROUTE_MINE, CH1_ROUTE_STORAGE |
| PASS | MagpieBridge Route A/B/C 프로파일 | CH2_ROUTE_NEW_ANCHOR, CH2_ROUTE_STORM_ANCHOR, CH2_ROUTE_OLD_BRIDGE |
| PASS | CloudWhaleRanch Route A/B/C 프로파일 | CH3_ROUTE_RANCH_WHEEL, CH3_ROUTE_STORM_RIDGE, CH3_ROUTE_GURU_BREATH |
| PASS | StarPostOffice Route A/B/C 프로파일 | CH4_ROUTE_REGULAR_POST, CH4_ROUTE_DEAD_LETTER, CH4_ROUTE_SEALED_LETTER |
| PASS | SleepingSunGarden Route A/B/C 프로파일 | CH5_ROUTE_STORED_SUNLIGHT, CH5_ROUTE_GREENHOUSE_TOP, CH5_ROUTE_HAOREUM_WAKE |
| PASS | 씬 무결성 · StarNight_Prologue | GameObject 177, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 씬 무결성 · StarNight_MoonMill | GameObject 660, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 씬 무결성 · StarNight_MagpieBridge | GameObject 354, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 씬 무결성 · StarNight_CloudWhaleRanch | GameObject 339, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 씬 무결성 · StarNight_StarPostOffice | GameObject 436, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 씬 무결성 · StarNight_SleepingSunGarden | GameObject 290, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 씬 무결성 · StarNight_PolarisObservatory | GameObject 319, Camera 1, Directional Light 1, Missing Script 0 |
| PASS | 프롤로그 필수 사건 | PrologueJourneyBeat 6/6 |
| PASS | 공통 별문 루프 · StarNight_MoonMill | Route objective 3/3, Chapter telemetry 1/1 |
| PASS | 공통 별문 루프 · StarNight_MagpieBridge | Route objective 3/3, Chapter telemetry 1/1 |
| PASS | 공통 별문 루프 · StarNight_CloudWhaleRanch | Route objective 3/3, Chapter telemetry 1/1 |
| PASS | 공통 별문 루프 · StarNight_StarPostOffice | Route objective 3/3, Chapter telemetry 1/1 |
| PASS | 공통 별문 루프 · StarNight_SleepingSunGarden | Route objective 3/3, Chapter telemetry 1/1 |
| PASS | 북극성 최종전 상호작용 | 기록 5/5, 도구 5/5, 엔딩 4/4 |

## 전체 런 밸런스 표본

- runs=0; completed=0; general=0@0.0m; starRoad=0@0.0m; temptation=0 %(표본 대기); info=0.0/0.0; accidentContext=100 %
- 유혹 구역 목표: 55 %~70 %
- 일반 엔딩 목표: 45~60분
- 별길 엔딩 목표: 60~80분

### Route 선택률

| 챕터 | Route | 선택 횟수 | 챕터 내 비율 | 판정 |
|---|---|---:|---:|---|
| MoonRabbitMill | CH1_ROUTE_MILL | 0 | 0 % | 표본 대기 |
| MoonRabbitMill | CH1_ROUTE_MINE | 0 | 0 % | 표본 대기 |
| MoonRabbitMill | CH1_ROUTE_STORAGE | 0 | 0 % | 표본 대기 |
| MagpieBridge | CH2_ROUTE_NEW_ANCHOR | 0 | 0 % | 표본 대기 |
| MagpieBridge | CH2_ROUTE_STORM_ANCHOR | 0 | 0 % | 표본 대기 |
| MagpieBridge | CH2_ROUTE_OLD_BRIDGE | 0 | 0 % | 표본 대기 |
| CloudWhaleRanch | CH3_ROUTE_RANCH_WHEEL | 0 | 0 % | 표본 대기 |
| CloudWhaleRanch | CH3_ROUTE_STORM_RIDGE | 0 | 0 % | 표본 대기 |
| CloudWhaleRanch | CH3_ROUTE_GURU_BREATH | 0 | 0 % | 표본 대기 |
| StarPostOffice | CH4_ROUTE_REGULAR_POST | 0 | 0 % | 표본 대기 |
| StarPostOffice | CH4_ROUTE_DEAD_LETTER | 0 | 0 % | 표본 대기 |
| StarPostOffice | CH4_ROUTE_SEALED_LETTER | 0 | 0 % | 표본 대기 |
| SleepingSunGarden | CH5_ROUTE_STORED_SUNLIGHT | 0 | 0 % | 표본 대기 |
| SleepingSunGarden | CH5_ROUTE_GREENHOUSE_TOP | 0 | 0 % | 표본 대기 |
| SleepingSunGarden | CH5_ROUTE_HAOREUM_WAKE | 0 | 0 % | 표본 대기 |

## 씬 규모

| 씬 | GameObject | Camera | Directional Light | Missing Script |
|---|---:|---:|---:|---:|
| StarNight_Prologue | 177 | 1 | 1 | 0 |
| StarNight_MoonMill | 660 | 1 | 1 | 0 |
| StarNight_MagpieBridge | 354 | 1 | 1 | 0 |
| StarNight_CloudWhaleRanch | 339 | 1 | 1 | 0 |
| StarNight_StarPostOffice | 436 | 1 | 1 | 0 |
| StarNight_SleepingSunGarden | 290 | 1 | 1 | 0 |
| StarNight_PolarisObservatory | 319 | 1 | 1 | 0 |

## 수동 확인 잔여 항목

- 일반 엔딩 5세션과 별길 엔딩 5세션의 실제 시간 분포
- 한 Route의 선택률이 10% 미만인지 여부
- 유혹 구역 진입률이 55~70%인지 여부
- 강제 귀가 원인 이해도와 라니·마루 관계 해석
