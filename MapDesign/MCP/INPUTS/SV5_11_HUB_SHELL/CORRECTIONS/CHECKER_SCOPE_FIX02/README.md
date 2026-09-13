# SV5_11 CHECKER_SCOPE_FIX02

FIX01이 필수 정책 문서의 금지 문구 `SectorId`까지 실제 의존성으로 오인한 문제를 교정한다.
원본 INPUTS와 FIX01은 변경하지 않는다.

문서는 exact append/hash 증거로 검증하고 retired-grid 의존성 검사는 다음에만 적용한다.

- SV5_11 신규 runtime/test C#의 실제 코드 식별자
- 기존 C# 통합 파일에서 predecessor 이후 추가된 실제 코드 식별자
- runtime C#이 쓰는 금지 export 파일명
- 최종 generated CSV header, JSON key, 파일명

C# 주석·일반 문자열과 정책 문서의 금지 설명은 실제 의존성으로 세지 않는다.
