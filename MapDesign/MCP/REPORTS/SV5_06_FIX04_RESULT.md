# SV5_06_FIX04 Result

TASK: SV5_06_FIX04
STATUS: PASS
PHASE: TASK EXECUTION + NATIVE FINALIZE VERIFIED

최신 소스의 focused EditMode는 발견 70 / 실행 70 / Passed 70 / Failed 0 / Skipped 0이다.
2026-09-11 10:36:06Z~10:40:06Z, 240.157845초, CLI 종료 코드 0.
이 Result는 과거 중단 기록을 대체한다. 과거 DIAG 및 다른 시점의 XML은 현재 소스의 증거가 아니다.

## 실행 증거

```text
unity test . --mode EditMode --filter StarNight.Map.Tests.EditMode.Sv5 --output MapDesign/MCP/GENERATED/SV5_06_FIX04/focused_results.xml --timeout 1800
```

Unity 6000.3.8f1 (1c7db571dde0), Unity CLI 1.0.0-beta.9.
컴파일 및 focused 실행 성공. 별도 전체 프로젝트 Console audit / PlayMode / build / Bake는 수행 범위가 아니다.

- XML: `GENERATED/SV5_06_FIX04/focused_results.xml`
- raw SHA-256: `236168155519ce6721f05b874edf15e3ae581383ba49dcf71f93c6b29344447b`
- raw bytes: 75,993 (CRLF 557)
- Git text-normalized blob SHA-256: `5839db7d956ff83c61df119dcbba839ce89bc222ef83a029c85765bf380242b8`
- blob bytes: 75,436 (LF). 실제 staged blob의 SHA/bytes를 대조하여 일치를 확인했다.
- 실행 직전/직후 FIX04 출력 밖의 변경·미추적·삭제 경로 198개의 SHA/존재 상태 동일.
  실행 중 생산/시험 소스를 수정하지 않았다. 소스 바인딩은 `BINDING.json`에 기록한다.
- 먼저 생산 후보 회귀, Forge 단일 사례, FIX04 11개 회귀, seed/order 결정성 시험을 통과시킨 뒤 최종 focused를 실행했다.
  이전 좁은 실행은 최종 XML로 대체하며 최종 패키지의 현재 증거로 혼용하지 않는다.

## 원인과 실제 통로 수정

선택 포트 OpenCells에서 first를 순회하면서 모든 포트 OpenCells 집합에
`!Contains(first)`를 요구한 조건은 항상 거짓이었다.
`PortBoundaryCandidates`는 ordered 통로의 입구 바깥 7칸 이내에서,
양 끝이 aperture/보호 AIR 밖인 정확한 cardinal face만 후보로 반환한다.
보호 검사는 유지했다. C02의 생산 fixture는 후보 5개, 첫 face (12,10)↔(13,10),
보호 셀로만 구성된 통로의 후보 0개를 확인한다.

`ExpandGlobalCuts`와 누적 remote face 생성은 제거했다.
Seal/Boss의 전용 통로를 먼저 확보하고 normal/optional 및 Forge 통로를
실제 cardinal router로 재배치했다. 별도 전역 영역 체계나 room 크기 축소는 없다.
세 gate는 각각 현재 ordered path 위 단일 local neck face만 가진다.
후보는 foreign path edge/보호 입구를 피하고 전체 expected-open product를 보존해야 한다.
gate 상태나 canonical predicate를 바꾸지 않으며 닫힌 geometry는 전역 좌표에 적용한다.

Forge→Seal `SV5_CORE_CONN_29dc755094886d48`:

- 동일 source port: `RMAP15_SITE_FORGE_PORT_EXIT`, (522,134).
- 동일 target port: `RMAP15_SITE_SEALBOSS_PORT_ENTRY`, (533,312).
- exact FIX03 전: (522,134)→(532,134)→(532,312)→(533,312), centerline 190셀.
- FIX04 후: (522,134)→(563,134)→(563,322)→(562,322)→(562,323)→(530,323)→(530,314)→(532,314)→(532,313)→(533,313)→(533,312), 278셀.
- 통행 중심선 집합에서 기존 178셀 제거 / 새 266셀 추가. 과거 reservation 파일을 변경한 것이 아니다.
- Forge gate face: (530,316)↔(530,317), `SV5_GATE_81BAEC0F51B2FEE6443F`.
- Seal gate face: (532,318)↔(532,319), `SV5_GATE_D3E9DFDBAD3EFFEBB9CD`.
- Boss gate face: (351,302)↔(352,302), `SV5_GATE_CE3A6CEC888F775FDF94`.
- resource_mask=7 / forge=true / seal=false / boss=false에서 Forge 및 normal 연결 true,
  Seal/Boss 진입 false. 9개 guarded closed/open 검사 및 여섯 closed bypass 차단 유지.

`repair_before_after.svg`와 `deepstar_before_after.svg`를 1칸 격자 및 4×4 경계로 생성하고
렌더링하여 확인했다. 고체 / 예정 통로 / 미배치 / 닫힌 face / 탐색 witness를 구별한다.
BEFORE는 exact FIX03이고 과거 실패 후보의 좌표를 현재 충돌로 주장하지 않는다.
DeepStar INITIAL before=false, after=true. 미배치 구간을 완성 지형으로 칠하지 않았다.

## C01~C08 및 F01~F08

아래 시험은 모두 namespace `StarNight.Map.Tests.EditMode.Sv5.Sv5SpaceGraphFix04Tests`에서
실제 발견·실행되어 각각 1 Passed이다 (C01/C02/T01을 포함한 FIX04 클래스 11/11).

| 계약 | 실제 PASS 시험 | 확인 결과 |
|---|---|---|
| C01 / F01 | F01_ConditionalGatesHaveExactLocalBarriers | exact FIX03 225 contact 중 218 local barrier 오류 재현; FIX04 0 오류, core gate 3개, guarded closed 6개 |
| C02 / F02 | F02_SharedAndFaceUsePhysicalGeometry | SHARED no-cell / FACE remote-face / fake boundary 거부, exact cell/face 허용 |
| C03 / F03 | F03_DeepStarYeastApproachAndReturnReachable | 명시한 DeepStar approach/return 각각 FIX03 INITIAL false, no-gate true, FIX04 INITIAL true; DeepStar-first 두 순서 성공 |
| C05 / F04 | F04_BossForgeClosedPreservesNormalTransitions | gate 부분집합 8개에서 모든 normal 연결 보존, Forge-open 단일 상태의 모든 core 연결 기대값 일치 |
| C04 / F05 | F05_ResourceOrderStateProductIsComplete | 6개 실제 자원 획득 순서, INITIAL부터 물리 MOVE를 통과한 상태 및 완료 행동열 계산 |
| C04 / F06 | F06_OptionalCollateralLocksAreAbsent | 지정 optional 3개 양방향 초기 도달 및 전체 optional product 성공, collateral 0 |
| C07 / F07 | F07_RenameOrderDigestRegression | 실제 연결 ID rename/집합 역순 도달 불변, 순서 digest 불변, gate 제거 시 bypass 검출 및 digest 변화 |
| C07 / F08 | F08_ExportEvidenceBindsProductDigest | 실제 export 내용과 production serializer/digest 일치, exact FIX03 파일 SHA 불변, 전후 SVG 생성 |
| C08 | 전체 focused 70/70 | Failed 0, Skipped 0; 완료 검증 후에만 Finalize/atomic commit |

FIX02 `G01_W01OldSealEntryBlockIsReproducedAndForgeStateOpensOwnedGate`와
`G02_W02BossApproachOpensButExitRemainsSealedUntilBossComplete` 모두 Passed.
기존 lookup/다중 remote face 개수 가정은 reroute 후 의미가 없어 deterministic
생산 fixture 또는 세 gate의 실제 closed/open 증명으로 교체했다.
시험 삭제/skip/null 허용은 하지 않았다. 이유와 대체 검사는 활성 문서 및 시험 주석에 기록했다.

## Accepted plan / 실제 product 수치

- seed 1304, 624×416; connections 28, complete contact pairs 14,734.
- contact coverage / reservation / gate geometry / gate state 오류 모두 0.
- ConditionalGate **접촉** 0 (합법적 reroute/Join), **core portal gate** 3, 실제 gate state 검사 9.
  빈 contact 목록만을 PASS 근거로 사용하지 않고 F01/F02 및 전체 closed/open product로 검증한다.
- 6 resource orders, 순서마다 실제 도달 105 states / 219 transitions / 역도달 105 states / dead-end 0.
- physical transition matrix 28,350행 = 순서마다 4,725행.
- expected-open 26,646행 / expected-closed 1,704행; 각각 오류 0.
- unintended dead-end 0, optional collateral lock 0.
- plan digest: `79447cbf21957effa060878c3618d3f831a383e151422458c1c214e27869e4bd`
- physical product digest: `8fdb961cb2ba599297d724d90587203f6a4c392c84009901b5afb6892bd690df`

RMAP13의 원래 FSM/action/CanTraverse는 변경하지 않았다.
logical proof의 1/0/1 상수 복사가 아니라 실제 물리 이동 결과로 INITIAL BFS와 goal 역도달을 계산한다.
matrix는 각 order/state/connection/direction/predicate/open·closed gate/도달 결과와
실제 witness 좌표 참조 또는 차단 이유를 포함한다.
connections/ports/segments/gate/local/contact/state/product/validation/preview는 동일 accepted plan에서 생성했다.
validation은 계산 결과 PASS이며 수동 상태 변경이 아니다.

## 보존 / 종료 경계

최종 Unity 실행 전후 `PRECHECK --mode post-readonly` 실제 PASS:
전체 ALWAYS 299개, 선행 commit blob 35개, mismatch 0.
FIX03 GENERATED 161개는 전체 검사에 포함된다. F08은 FIX03의 현존 파일 SHA도 전후 대조한다.
선행 commit `a0dc996a11d349927917a60410fb356871df15f2` 및 parent 검증 통과.
과거 Task/Archive/Result, SOURCE_LOCK, core/state 생산 코드와 무관한 dirty 변경을 보존한다.
기존 삭제 13개와 무관한 미추적 자료를 복원·삭제·stage하지 않는다.
모든 legacy export는 FIX04의 시험별 고유 `legacy_exports` 경로에 격리했다.

등록 / native Apply / INBOX stage / FIX03 복원을 반복하지 않았다.
native Finalize 완료 후 post-readonly를 다시 실행하여 전체 299개/35 blob 및
290행 = 249 COMPLETE / 0 CURRENT / 41 LOCKED를 확인했다.
Current NONE, SV5_07_DIVERSITY LOCKED다. 다른 상태 행은 바꾸지 않았다.
task-owned 247개 exact path의 staged inventory가 일치하고 범위 밖 path는 0개다.
staged diff whitespace 검사도 통과했다. atomic commit 뒤 Review ZIP에
commit SHA와 각 파일의 raw/blob manifest를 담는다.
`_work`, 기존 DIAG, 구형 중간 XML, Unity Library/Temp는 최종 증거에서 제외하며 삭제하지 않는다.
push와 다음 Task 실행은 하지 않는다.

ComposedGeometryReady=false
PlayerVerified=false

완료 범위는 연결 통로 계획·국소 문 geometry·canonical 진행과 좌표 이동의 일치다.
1~2칸 연결 기준과 고정 큰 방 크기를 유지한다. 일반 상승 +1 / Jump·Grab 최대 +2는 변경하지 않았다.
실제 4×4 지형 조립, 바닥·착지·머리 여유의 전 구간 완성 및 Player 완주는 미완료이며 주장하지 않는다.
