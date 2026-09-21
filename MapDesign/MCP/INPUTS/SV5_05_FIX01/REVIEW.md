# SV5_05 검토: 공간 그래프 전에 고칠 두 가지

판정: CORRECTION_REQUIRED_BEFORE_SV5_06.
첨부 소스·데이터·테스트 XML 19개 SHA가 Result 또는 ZIP source manifest와 일치했다.
동봉 XML은 14/14 Passed, failed/skipped 0/0이다. 이 작성 환경에서는 C#/Unity를 실행하지 않았다.
아래 반례는 고정된 C# 원문·조건 CSV·실제 보고 trace를 대조한 정적 검토다.
REVIEW_FINDINGS.json에 실제 ID·조건·trace·source SHA와 상세 위치를 담았다.

## FIX01-F1 / P1 — 봉인 전 보스 구역 진입을 허용

Sv5RouteStatePolicy.ValidateCandidate는 목적지 physical port 조건만 ParsePortRequirement로 검사한다.
Seal과 Boss는 같은 physical ENTRY를 사용하며 그 조건은 ALL_RESOURCES_AND_FORGE다.
그러나 실제 Seal→Boss graph edge에는 별도로 RequiresSeal=true가 있다.
Forge→Boss 후보에 resourceMask=7, RequiresForge=true, RequiresSeal=false를 주면
현재 target-port 검사는 통과하고 reverse-of-one-way 검사에도 걸리지 않는다.

첨부 정상 trace에는 Forge 제작 직후 resourceMask=7, cursor=3, ForgeMade=true,
SealOpen=false, BossComplete=false인 Forge 위치가 실제로 있다.
여기서 해당 후보를 타면 봉인을 열지 않은 Boss 위치에 도달한다.
Boss 완료 action은 SealOpen이 필요하고 유일한 출구 edge는 BossComplete가 필요해 논리상 돌아갈 길도 없다.
이는 논리 모델의 조기 진입/막다른 상태이며, 실제 Player가 빠지는 장면을 실행해 확인했다는 뜻은 아니다.

후보집합 검사는 기존 graph에 후보를 더한 뒤 여섯 개 성공 경로가 존재하는지만 확인한다.
기존 성공 경로를 삭제하지 않으므로, 새 불법 진입이나 막다른 가지가 생겨도 기존 경로로 PASS할 수 있다.
따라서 모든 관련 도달 상태의 접근 불변식/복귀 가능성 검사가 별도로 필요하다.
기존 T03 이름에는 Boss가 있지만 실제 입력 배열은 Forge/Seal/Exit 세 사례뿐이다.

## FIX01-F2 / P2 — 서로 다른 후보가 같은 analysis digest를 공유

Sv5RouteStateAnalysis.CanonicalLines는 후보의 ID·판정 코드·동일한 설명 문자열만 넣는다.
후보의 실제 from/to/kind/direction/resourceMask/Forge·Seal·Boss 조건/provenance는 넣지 않는다.
같은 ID의 일반 통로 목적지를 Ore에서 Sap으로 바꿔도 두 집합 모두 Allowed이고
나머지 core/proof/contact 입력이 같아 CanonicalLines와 digest가 같아진다.
해시 알고리즘 충돌 문제가 아니라, 해시할 입력에 의미 있는 내용이 빠진 문제다.
후속 공간 그래프가 다른 연결을 이전 검증과 같은 결과로 취급하지 않도록 입력 자체를 정규화해 묶어야 한다.

## 기존 증거를 해석하는 범위

원래 Result의 PASS와 보고된 14개 시험 결과를 삭제하거나 FAIL로 다시 쓰지 않는다.
그 당시 시험 범위와 이번 검토에서 추가로 발견한 누락을 구분해 보완 Result를 만든다.
review_source_manifest는 commit 117d99a82eb0889ab0edfeda250e535c5f56c295를 보고한다.
이 값의 실제 git 존재·포함 파일·정상 Finalize는 현지에서 확인한다. 현재 HEAD와 같다고 가정하지 않는다.
원래 Result는 Finalize 전 기록이므로 그대로 보존한다.

경로 contact는 현재 any edge guard/route ID 존재에 따른 분류이며 실제 상태별 접촉 증명은 아니다.
보완에서는 검사한 논리 전이와 검사하지 못한 실제 geometry를 명확히 구분한다.
세 좌표/109-edge AIR witness는 유지하며 물리 차단이 생겼다고 표시하지 않는다.
SV5_06: Start EXIT 사용/미사용, Village 선택적 접근/복귀, 조건 경계의 실제 배치.
SV5_09: loop 이후 조건 재검사. SV5_41: 합성 geometry 재검사. SV5_44: 전체 Player 검증.
기존 obligations의 41번 항목을 전체 Player 완료로 해석하지 않는다.

이번 ZIP은 보완 Task의 INBOX·원본 명세다. 게임 코드 수정이나 현지 Apply는 아직 하지 않았다.
