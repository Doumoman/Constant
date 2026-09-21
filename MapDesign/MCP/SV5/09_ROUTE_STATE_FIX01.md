# SV5_05_FIX01 Route State 보완

SV5_05의 기존 Result는 보존한다. 이 문서는 `Sv5RouteStatePolicy.Analyze`의
보완된 논리 상태 검사와 소비 경계를 설명한다.

## API와 논리 범위

- `Sv5WorldGraphPlanner.ExploreWithAnalysisNodes`는 기존 SV5 상태·action·edge
  predicate를 재사용해 후보 분석 노드를 포함한 모든 도달 상태와 전이를 읽기 전용으로 열거한다.
- `Sv5RouteStatePolicy.Analyze`는 baseline 6순서 proof와 candidate-set augmented proof를
  분리한다. 후보 집합은 각 order에서 EXIT goal 역도달 가능성까지 검사한다.
- Boss 후보 진입에는 AllResources, ForgeMade, SealOpen이 필요하며, Exit 진입에는
  AllResources, ForgeMade, SealOpen, BossComplete가 필요하다. 이는 기존 baseline
  edge/action을 변경하지 않는 후보 진입 검사다.
- `Sv5RouteStateAnalysis.Digest`는 V2 length-prefix schema로 core/graph identity,
  candidate payload, baseline·candidate proof/trace, review contacts 및 순서가 있는
  AIR witness, contact 판정과 readiness를 결속한다.

## 증거와 readiness

`MCP/GENERATED/SV5_05_FIX01/analysis.json`과 CSV는 같은 focused-test 분석 객체에서
생성된다. `LogicalStateVerified`는 baseline과 augmented 6순서, candidate-set safety,
유효한 logical contact/witness check가 모두 통과한 경우에만 true다.

`GeometryStateReady`와 `PlayerVerified`는 이 Task에서 항상 false다. 세 기존 review
contact와 109-edge AIR witness는 geometry/Player proof가 아니라 진단 증거다. AIR
witness는 cardinal 연속성, 고정 AIR, SEALED 제외를 확인하지만 Player 이동을 주장하지
않는다.

## 후속 소비

SV5_06은 Village/Start port와 condition contact geometry를, SV5_09는 loop 후 route
boundary 재검사를, SV5_41은 composed geometry를, SV5_44는 whole-world Player 검증을
소유한다. 각 Task는 본 Task의 실제 PASS Result·Finalize·commit과 generated evidence를
읽은 뒤에만 자체 binding을 시작한다.
