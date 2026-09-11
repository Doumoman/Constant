# SV5_07_DIVERSITY Result

TASK: SV5_07_DIVERSITY
STATUS: PASS

동일 종류 독립 지형의 근접 반복 억제를 실제 Plan → PlaceFamily 후보 선택에 연결했다. Unity focused 발견/실행/통과 **82/82/82**, 실패 **0**, 스킵 **0**이다. 기존 70개 시험 이름이 그대로 발견되고 신규 V01~V12가 모두 통과했다. 반복 profile의 가까운 동굴 쌍은 **OFF 3 → ON 2**, 장소 24개와 개별 크기는 보존된다. 기본 profile은 **NO_ELIGIBLE_REPEAT**이며 0 → 0을 개선 성과로 계산하지 않는다.

## 실제 구현과 좌표

624×416, seed 1304, 기존 8개 core와 2,432개 core 셀을 유지한다. 정책 반경 36, 이웃 0/1/2/3+의 weight 100/70/49/34는 동봉 profile의 운영 기본값이다. 기존 후보 좌표/섹터 우선순위/고체·예약 충돌 조건은 유지하고 요청별 SHA-256 roll을 한 번 계산한다. 후보마다 roll을 새로 뽑지 않는다. 전부 soft 거절이면 최소 반복 합법 후보를 기존 안정 순서로 선택한다. 실제 default/repeat ON fallback은 각각 0건이며 강제 fallback 음성·양성 fixture도 통과했다.

| 요청 | OFF bounds (x,y,w,h) | ON bounds (x,y,w,h) |
|---|---|---|
| CAVE_BAND ordinal 0 | 64,272,60,24 | 64,272,60,24 |
| CAVE_BAND ordinal 16 | 48,240,60,24 | 48,240,60,24 |
| CAVE_BAND ordinal 32 | 64,212,60,24 | 160,272,60,24 |

반복 profile은 RepresentativeV1의 기존 14개 요청에 ordinal 16/32의 60×24 CAVE_BAND를 더한 사전 고정 입력이다. OFF/ON 요청·seed·core·크기·후보 생성 규칙은 동일하다. 이동한 장소의 포트와 ordered 통로는 같은 plan에서 다시 생성했다. 독립 지형을 삭제·축소·이름 변경하거나 다른 formation에 합쳐 수치를 줄이지 않았다. 작은 생산 선택 fixture의 seed 0~63에서는 가까운 선택이 OFF 64회 → ON 45회였다. 이는 64개 월드 전체 검증이 아니라 순수 선택 함수 시험이다.

현재 place ID를 formation ID로 재사용하고 분할은 원래 소유 ID를 전파한다. 4×4/12×8 분할은 독립 지형의 반복으로 세지 않으며 conflicting family/독립 place 병합 입력을 거부한다. 명시적 alias만 사용한다. 일반 연결 방은 ORDINARY_ROOM으로 집계하되 감점 면제, Core는 고정/면제다.

## 안전성 및 동일 plan 출력

| 실제 ON plan | 장소 | physical transition matrix | 자원 순서 | product 오류 | local gate 오류 |
|---|---:|---:|---:|---:|---:|
| default | 22 | 28,350행 | 6 | 0 | 0 |
| repeat | 24 | 33,810행 | 6 | 0 | 0 |

FIX04 RepairGateCorridors 및 변경하지 않은 GateGeometry/PhysicalMovement/PhysicalProduct/StateProjection/RMAP13을 소비했다. required approach/return, optional, gate 동시 적용, closed bypass, recovery와 6개 자원 순서를 실제 생산 validator/product로 검사했다. W01/W02 및 기존 FIX04 F01~F08 회귀가 통과했다. 보호 AIR 침범과 gate 제거로 생긴 bypass를 생산 검증기가 거부했다. Gate geometry는 OFF/ON에서 동일하다.

- default OFF digest: `6ee60821aba144aaf21e2d5539dfe30745ce044379c349d414b8d279389abe3d`
- default ON digest: `10080e53c3d4c47f6e93f49118c7c648f07c1f3162742ce866b6ecd8a0be3e46`
- repeat OFF digest: `5d775db39431fc1bcc482f3e867e97aa6eba6232965544715e4422cb7c746c93`
- repeat ON digest: `94c9f3a353e39984633a755af7842e87989e86dc97c3545ce87a4ac4da4397b7`

default/repeat ON WriteAll 한 벌씩과 diversity.json의 정확한 OFF/ON places/ports/connections/gates, decisions.csv, pairs.csv, preview를 만들었다. 정책 값·seed·실제 선택·formation·pair를 semantic digest에 연결했다. 출력 80개를 실제 두 번 생성해 동일 raw bytes를 확인했다. 별도 verify_outputs.py로 CSV 좌표와 실제 JSON geometry, pair gap/개수, gate 동일성, matrix 6순서/오류 0을 재검산했다.

[전체 비교 및 격자 확대](../GENERATED/SV5_07/preview/index.html)는 실제 좌표를 사용한다. 렌더링한 전체/확대 그림을 시각 확인했다. 외곽은 예정 장소, 선은 예정 통로, 내부는 미조립이다. 일반 +1/Jump+Grab 최대 +2 수치는 변경하지 않았다. 바닥·착지·머리 여유를 갖춘 실제 지형 조립이나 Player 완주를 주장하지 않는다.

ComposedGeometryReady=false
PlayerVerified=false

validation의 PLANNED_LAYOUT/DIVERSITY/CONTACT/PHYSICAL_PRODUCT와 COMPOSED_GEOMETRY/PLAYER를 분리했다. SV5_08 밀도, SV5_09 loop, 후속 조립/Player 작업은 하지 않았다.

## 실제 Unity 증거

```text
unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_07/focused_results.xml --timeout 1800
```

Unity CLI 1.0.0-beta.9, Editor 6000.3.8f1 (1c7db571dde0), EditMode.
UTC 2026-09-11 12:30:45Z ~ 12:37:18Z, XML duration 392.2471696초.
발견 82 / 실행 82 / Passed 82 / Failed 0 / Skipped 0 / Inconclusive 0.
종료 코드 0. 실제 batch compile/test 완료이며 PlayMode/build/Scene Bake 및 전체 Console 무오류를 별도로 주장하지 않는다.

- XML: `GENERATED/SV5_07/focused_results.xml`
- 원본 raw SHA-256: `f032d5ca73fde2a83150758704ce5381a7c7b804626eb6a20f0ad9e8d19c4ade`, **86,909 bytes**
- Git blob SHA-256: `fcf004fc01785d675d24c0f75087795c966fa86448cba9023f65c4b55d4b8037`, **86,264 bytes**
- Git blob OID: `541c20bf5b640f136cfbd7098ff0d980ac369d5f`
- 기존 `text=auto` 속성의 Git blob과 raw XML은 구분한다. raw XML을 수정하거나 SOURCE_LOCK 기대값을 바꾸지 않았다. index/commit blob은 소유 commit 단계에서 별도로 일치 검사한다.
- 기존 FIX04 XML의 70개 fullname은 최종 XML에 모두 존재한다. 기존 시험 8개 파일의 diff는 출력 경로 격리만이며 assertion/이름/분기/카테고리 변경 0이다.

신규 시험의 전체 namespace는 `StarNight.Map.Tests.EditMode.Sv5.Sv5SpaceDiversityTests`다.

| ID | 실제 발견·실행한 시험 이름 | 결과 |
|---|---|---|
| V01 | `V01_EmptyBoundsGapUsesInclusiveRadiusAndExactTileSpacing` | PASS |
| V02 | `V02_FormationSubdivisionsCountOnceAndConflictingOwnershipIsRejected` | PASS |
| V03 | `V03_FamilyAliasesIgnoreDisplayVariantsAndOrdinaryRoomsRemainExempt` | PASS |
| V04 | `V04_RequestScopedRollAndStableSelectionMatchTheFrozenProfile` | PASS |
| V05 | `V05_ProductionSelectorActuallyMovesAwayFromNearbyRepeatsAcross64Seeds` | PASS |
| V06 | `V06_AllSoftFailuresUseMinimumRepeatLegalFallbackNotInvalidOrMissingPlaces` | PASS |
| V07 | `V07_DefaultRealPlanKeepsFix04GeometryAndReportsNoEligibleRepeat` | PASS |
| V08 | `V08_FrozenThreeCaveIntegrationBuildsBothActualWorldPlans` | PASS |
| V09 | `V09_RealRepeatedPlanReducesPairsWithoutDeletingShrinkingOrRelabeling` | PASS |
| V10 | `V10_BothChangedPlansUseUnchangedProductionSafetyAndRejectNegativeGeometry` | PASS |
| V11 | `V11_ExportsBindActualSelectionsGeometryAndPolicyAndAreByteDeterministic` | PASS |
| V12 | `V12_HistoricalAlwaysBytesAndSeventyExistingTestNamesRemainUnchanged` | PASS |

## 보존·정리·계약 완료

STAGE/native Apply는 앞선 단계에서 정확히 한 번 수행되었으며 이번 정리 후 재개에서는 재Apply/재stage하지 않았다. 설치 Task/Archive SHA는 `bd7940748dd4e25234f724db118cb7226bf13177488176e194a185045ee5ffcd`, package manifest SHA는 `c560a05ea06588043c3bce4875838de778de21cc21a09638fecdd1a79fdfd14a`다. 선행 commit은 `c96f76b8097c0baaa7e5d5e7a92465e38124ad2a`다.

시험 전후 및 사용자 정리 후 post-readonly: **543 ALWAYS raw files / 47 선행 commit blobs PASS, mismatch 0**. BINDING 비소유 변경 371개 항목도 mismatch 0이다. FIX04 및 이전 Task/Archive/Result/GENERATED, 무관한 modified/deleted/untracked 변경을 보존했다. SOURCE_LOCK/STAGE/profile/동봉 Task 원본을 수정하지 않았다. 기존 시험 export는 SV5_07/_work/legacy_exports의 fixture별 경로에만 생성했다.

환경 정책으로 자동 삭제가 차단되었던 SV5_07/_work는 사용자가 직접 정리했다. 재개 시 해당 경로의 부재, 최종 산출물 보존, 시험 소스 SHA 일치를 확인했다. 중간 XML/임시 이미지/legacy 복제 출력은 최종 패키지에 포함하지 않는다. 최종 XML과 실제 출력만 유지하며 시험을 불필요하게 재실행하지 않았다.

D01~D10 및 V01~V12 완료: 실제 반복 억제/식별/거리·weight/생산 선택/연결 안전성/허용 소스/필수 시험/실측 export/불변·정리/최종 증거 준비 PASS. 활성 15_DIVERSITY_V5 문서를 작성하고 02_PROTOCOL에는 승인된 suffix를 원본 바이트 뒤에 정확히 한 번 추가했다. 추가 helper 3개는 INPUTS/SV5_07/tools에 한정하며 BINDING에 소유·SHA를 기록했다.

## 시험 소스 raw SHA

최종 실행 전 snapshot과 실행 후·사용자 정리 후 현재 바이트가 동일하다. 이후 변경은 BINDING/활성 문서/이 Result/정상 Status finalize와 패키징뿐이며 생산·시험 소스를 다시 수정하지 않는다.

| 파일 | raw bytes | SHA-256 |
|---|---:|---|
| Sv5SpaceGraphExport.cs | 67891 | `c0aef258ca40f846dcc9913961ddfaff9af836db37723330879308c5ffce578d` |
| Sv5SpaceGraphPlan.cs | 34309 | `e9dfe1fdfc734d9b0ac7548def117e00e3b0f4de1cd4df95830a51c7e22cd861` |
| Sv5SpaceGraphPlanner.cs | 56561 | `0c6ad926d5f06f2f484bcd71d34cdef6ef3e5c0052061354eb8dcd3e012803f2` |
| Sv5SpaceDiversity.cs | 14312 | `c2d845a3fd94d170a6e7c241b1d8e7e0a6cf0536e2c2c05948c36f7561dbe2f4` |
| Sv5CoreReservationPlanTests.cs | 16261 | `bde10b01efa79485d970f59d4aaa8ddc801e3aada1c7790ff2d17a071358eb75` |
| Sv5RouteStateFix01Tests.cs | 18430 | `76a4cdd0d6b78a0ef5aaf8385ccb2eb239435c8e00ca95ecae4c3d5956154141` |
| Sv5RouteStatePolicyTests.cs | 20006 | `7a97f085511c23c52016013bac85c1d04fd2adffc72ef90f7e93dec2caee2fe3` |
| Sv5SpaceDiversityTests.cs | 21229 | `85566d9b007db00cfb061d213205cac18331a92cf618bd6b8b3b91bb5c3d565c` |
| Sv5SpaceGraphFix01Tests.cs | 13948 | `7441019becedc59d30399ea9f14620e95593759107186267bc8da919b364660d` |
| Sv5SpaceGraphFix02Tests.cs | 18197 | `cb6c135119ef09c99f10262cbb9dd1be56e69554eac30fcd31f8156d7e01ad1b` |
| Sv5SpaceGraphFix03Tests.cs | 15478 | `21bf03526c7ec04c118b48b48c62c3f7dd091fcc67692ce986005da89dba052c` |
| Sv5SpaceGraphFix04Tests.cs | 18007 | `d32348d5779f635cdbb4c158a97162fd0935401e71cca7a5db09728a23b1cd6d` |
| Sv5SpaceGraphPlanTests.cs | 19316 | `4c36969b25b7314a373a54246205f9352c7d047263ebc6a0c122201d77e86de1` |

## Native handoff

이 PASS Result를 근거로 별도 Phase C에서 SV5_07_DIVERSITY만 COMPLETE, Current NONE으로 닫고 Phase D에서 이번 소유 파일만 하나의 atomic commit으로 기록한다. 최종 검증 조건은 **290 = 250 COMPLETE / 0 CURRENT / 40 LOCKED**, SV5_08_INFILL LOCKED다. 실제 commit/parent와 파일별 raw/blob SHA/OID는 commit 후 Review manifest에 기록하여 Result 자기참조를 피한다. Review ZIP은 commit 후 생성한다. push/다음 Task 시작 없음.

