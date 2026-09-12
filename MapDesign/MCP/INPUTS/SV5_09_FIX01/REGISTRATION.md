# SV5_09_FIX01 explicit registration

SV5_09_LOOPS의 최종 Review에서 실제 topology 증거 결함이 발견되어 별도 교정 Task를 등록한다.
이는 SV5_10을 시작하는 승인이 아니다.

등록 전 정확한 상태:

- Status 292행 = 254 COMPLETE / 0 CURRENT / 38 LOCKED, Current NONE.
- SV5_09_LOOPS COMPLETE, SV5_10_SIDEPATH LOCKED.
- Master에 SV5_09_FIX01이 0회 존재한다.

등록은 두 파일에 한 행씩만 추가한다.

- Status에서 `| SV5_09_LOOPS | COMPLETE |` 바로 뒤에 `| SV5_09_FIX01 | LOCKED |`.
- Master에서 `| 09 | SV5_09_LOOPS | LOCKED |` 바로 뒤에 `| 09.F1 | SV5_09_FIX01 | LOCKED |`.

등록 후 293행 = 254 COMPLETE / 0 CURRENT / 39 LOCKED, Current NONE이다.
다른 상태/순서/문구/줄바꿈은 변경하지 않는다. STAGE의 `register` 모드만 이 정확한 등록을 수행한다.
