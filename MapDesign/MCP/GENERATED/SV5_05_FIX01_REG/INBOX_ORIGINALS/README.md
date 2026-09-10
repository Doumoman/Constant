# RMAP11 지정 25개 수정 패키지

## 시작 방법

1. 이 ZIP을 임시 폴더에 푼다.
2. 내부 MapDesign 폴더를 Unity 프로젝트 루트의 MapDesign에 합친다.
3. README.md와 SHA256SUMS.txt는 패키지 원본 폴더에 함께 보관한다.
4. 로컬 Codex에 아래 지시를 전달한다.

```text
MapDesign/MCP_INBOX/RMAP11_FIX.md를 읽고 현재 RMAP11_POOL500을 재개해줘.
이 파일은 새 Task를 등록하는 Apply 대상이 아니라 기존 CURRENT의 사용자 수정 지시야.
RMAP11_FIX.md 전체 SHA-256은
d66a140d8f384642414b44b0fd7a26bd60e2b22e2116dfa7314820bd6a4e85a9
이야. 실제 파일과 먼저 대조해줘.
지정 25개를 적용하고, 1칸 점프 / 2칸 점프+매달림으로 실제 통과를 검증해줘.
RMAP11 PASS·Finalize·commit까지 확인되면 동봉 RMAP12_PREP.md에 따라
실제 선행 해시와 바인딩으로 RMAP12_WORLD_DATA 실행 MD를 준비하고 정상 Apply·구현해줘.
RMAP13은 열지 말고 각 Task의 결과와 실제 commit을 따로 보고해줘. push는 하지 마.
```

## 이번 수정 범위

- 도감 번호: 002 007 008 013 014 016 018 019 036 038 042 044 046
  047 049 173 174 182 188 190 197 205 207 219 221.
- 오른쪽 경사 7 / 왼쪽 경사 8 / 왼쪽 벽 5 / 오른쪽 벽 5 = 25개.
- 기존 나머지 475개와 합쳐 500개의 서로 다른 16셀 배열을 유지한다.
- 1칸은 점프, 2칸은 점프→안전한 모서리 매달림→점프 이탈로 오른다.
- 한 번의 상승은 최대 2칸이며, 높은 벽은 중간 디딤면을 통해 나누어 오른다.
- 25개 중 19개는 2칸 매달림 구간, 6개는 최대 1칸 상승 구간으로 설계했다.
- 모든 형상은 하부 지지가 이어지는 표면 규칙으로 만들었다. 임의 비트/숨은 구멍으로 수를 채우지 않았다.
- 머리 여유·진입 바닥·반대편 착지는 이웃 지형을 포함해 검사한다.

## 파일 안내

- MapDesign/MCP_INBOX/RMAP11_FIX.md: RMAP11 재개·적용·검증·완료·후속 진행 지시. 164줄.
- MapDesign/MCP/INPUTS/R11FIX/patch25.csv: 25개의 정확한 원본/수정 16셀과 이동 조건.
- MapDesign/MCP/INPUTS/R11FIX/preview500.csv: 기존 도감 번호를 유지한 수정 후 전체 배열 예상본.
- MapDesign/MCP/INPUTS/R11FIX/review.json: 실제 사용자 검수 요청/보완 원문과 기준선 해시.
- MapDesign/MCP/INPUTS/R11FIX/RMAP12_SPEC.md: 기존 RMAP12 전체 기획 원문. 설치 전 명세.
- MapDesign/MCP/INPUTS/R11FIX/RMAP12_PREP.md: 실제 RMAP11 PASS 후 실행 MD 바인딩과 RMAP12 진행 절차.
- SHA256SUMS.txt: 이 파일을 제외한 패키지 구성 파일의 SHA-256.

## 현재 확인된 것과 다음 실행

로컬 문서 작업에서 25개 매핑·원본 일치·수정 배열·상승 높이·Grab 모서리·500개 고유성을 확인했다.
수정 전후 PDF의 800개 셀도 입력 배열과 대조했다. 실제 Unity 적용·물리 검증은 로컬 Codex가 수행한다.
새 Candidate ID와 실제 역할은 프로젝트의 기존 API로 산출해야 한다. preview500.csv를 최종 catalog로 직접 덮어쓰지 않는다.
2개는 RMAP07 첫 풀에서 유래한 후보이므로, 첫 풀 원본은 보존하면서 RMAP11 최종 소비에서 교체한다.
기존 대표 Run에는 지정 25개가 쓰이지 않았으므로, 대표 Run 검사 외에 25개 별도 실제 fixture 검사도 필요하다.
RMAP11 완료 전 RMAP12를 시작하지 않으며, 실제 결과를 확인하지 않고 PASS/새 선행 SHA를 작성하지 않는다.
