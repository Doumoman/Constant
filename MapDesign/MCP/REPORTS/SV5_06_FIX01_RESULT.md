# SV5_06_FIX01 Result

TASK: SV5_06_FIX01
STATUS: PASS

## 선행·등록·Apply

- 선행 SV5_06 Result SHA-256: `05af804fd2e231c91670bcb35a90833228b43064a2d22d3c8edd3345c9470c3c`
- 선행 installed Task SHA-256: `c8786c2a4b35702dc67e8b975b0647125b3c988cee9929baf7a86bfcd99a4925`
- 선행 소유 commit: `7b7dc99527b878eadc398388e27b3fce173971ef`
- 등록 전: 245 COMPLETE / 0 CURRENT / 41 LOCKED / Current NONE
- 등록 후: 245 COMPLETE / 0 CURRENT / 42 LOCKED / Current NONE
- Apply 후: 245 COMPLETE / 1 CURRENT / 41 LOCKED / Current `TASKS/SV5_06_FIX01.md`
- 등록 Status SHA: `cbf1f16e98c1ced26a7ce27e1e0e278e460111faa5c85b113efd243bec953e98` → `a708a1f64da9041e7c79049fec8e0553c64f6f49a02951da6dbe817984dc9e79`
- 등록 Master SHA: `8a0d957aec361358b1f8e7b9cb9897c3af2d93c4ddaf7917c660b2ce1dc284d4` → `083c324fe00531b47f140048f2b1729db2bdadaf9c880560d708f2ba95cc5b03`
- Task / Archive SHA-256: `e7695992c081b856fc2e05757cc68fa8121178a83b6b1c3e352d4ec172e74276`
- pre-registration, pre-stage, stage, post-readonly 검사: 모두 PASS

등록 diff의 첫 적용에서 시스템 `core.autocrlf`가 두 문서 전체를 CRLF로 바꿔 계약 after SHA가 달라졌다.
이 시도만 선행 `HEAD` 바이트로 롤백하고 동일 diff를 `core.autocrlf=false`, `core.eol=lf`로 다시 적용했다.
재적용 결과는 지정된 두 행 외 의미 변경이 없고 두 after SHA와 정확히 일치한다. 패키지·manifest·기대 SHA는 수정하지 않았다.

## 구현 결과

- 하나의 accepted reservation model이 후보 심사, 접촉 열거, 상태 투영, digest, JSON/CSV/preview를 공급한다.
- 새 통로는 중간 구간 전체 cardinal 폭을 검사하고, core FixedSolid 및 SOLID route support 충돌 후보를 거부한다.
- 포트는 기존 AIR passage를 보존하는 명시적 aperture adapter에서 안전한 전폭 구간으로 이행한다.
- route 정렬 뒤에도 접촉의 route별 좌표와 kind가 보존된다. shared/face pair는 독립 grid 대조기로 완전성을 검사한다.
- 동일 route pair·predicate의 접촉은 하나의 물리 boundary로 묶이고, 모든 blocking cell/face, 양측 anchor,
  방향, flow, predicate, OPEN/SEALED 상태를 가진다.
- optional/village 연결은 양방향으로 실제 projection되며 core FSM/action/guard는 그대로 재사용한다.
- semantic digest는 port boundary/anchor/flow/source/status, connection 전체 centerline/envelope/aperture,
  gate OPEN/SEALED geometry, route별 contact 좌표/kind/판정, proof identity/action을 length-prefix로 묶는다.

## 반례와 채택 계획

- N01: 보존 SV5_06 바이트에서 기존 5,347쌍, clearance 포함 10,093쌍, 누락 4,746쌍을 재현했다.
- N02: 보존 SV5_06 바이트에서 FixedSolid 충돌 72행/47셀과 SOLID route support 충돌 113행을 재현했다.
- 채택 plan digest: `5e8e12a9b389d9a686f787e759d466a03fd57712a98bfdebb3a0b1aac3f3e494`
- 28 connections / 16,222 contact input cells / 7,188 complete pairs / 27,629 reservation rows / 14 conditional boundaries
- contact coverage 오류 0 / reservation 충돌 0 / gate geometry 오류 0 / diagnostics 0
- 자원 6순서 모두 goal 도달 및 모든 reachable state의 goal 역도달 PASS
- `geometry_state_ready=false`, `player_verified=false`

수정 후 pair 수는 reroute와 port adapter가 반영된 채택 계획의 값이며, 과거 envelope를 단순 추가한 반례의
10,093을 정답으로 강제하지 않는다.

## Focused EditMode

- Unity Editor: `6000.3.8f1 (1c7db571dde0)`
- 명령: `unity --no-color --non-interactive test . --mode EditMode --filter "StarNight.Map.Tests.EditMode.Sv5" --output MapDesign/MCP/GENERATED/SV5_06_FIX01/focused_results.xml --timeout 1200`
- 발견/실행/통과/실패/스킵: 43 / 43 / 43 / 0 / 0
- XML worktree SHA-256: `8e4bc3239a4be60af250500e20c309a8f9e5eb2ee8c470224cb1850e98ca396c`
- commit blob SHA-256은 staging 후 review manifest에 별도 역할로 기록한다.
- 무필터 전체 회귀, PlayMode, build, Scene Bake, Player 검증은 실행하지 않았다.

## 증거

- BINDING SHA-256: `6877ecff085eb94b68a7e66356ea986e8fb8dc4a7ff0fe39389c283c60d5ecf9`
- `space_graph.json`: `81a561c7b37a0529f660101defa800cb4c549a82a0d851ef9c9d654b8dfe6081`
- `reservation_cells.csv`: `fcc13be86bace041baf275fe850824601de7bde0247daa4e7666955d734c46e9`
- `contact_checks.csv`: `1079f1a3af64c97cc141bede7a4cef1809bfb50b7db41d844bb968b050cf1dcb`
- `gate_geometry.json`: `cb84e4b725e7ddd0f9bcf4293f0d469b9ec86dd878ee16246f8788a3085b9e8a`
- `state_proofs.json`: `538520515414fac0b445b5900c000f969cffc7986b453732c63d73d493c9ac82`
- `validation.json`: `9d63628929ae8fbffd56fab6ef333d4a79502d5dc3adc721c3e50e7353419053`
- `obligations.csv`: `b14dbd83fce71946cd99ff8494d657cf363a834b8b0be39dddd879667dee85db`
- 활성 설명 `SV5/11_SPACE_GRAPH_FIX01.md`: `d6abfe8f30379144698ceaf0c948556899c4939f5f4ead9a0ff405843586d029`
- 기존 SV5_06 review ZIP SHA-256 보존: `1dbcfe2c92cc6e94f48b4b2af4e487a9cfa13c190c09cc79641620bb0c45f683`

## 남은 책임과 중지

SG06-F5의 22장소·대부분 INFILL_PENDING·단일 순환 구성은 해결됐다고 표시하지 않는다.
SV5_07은 family 분포, SV5_08은 일반 공간 밀도, SV5_09는 재합류 확장을 담당하고,
SV5_41은 합성 geometry, SV5_44는 Player 검증을 담당한다. SV5_07 이후 작업은 시작하지 않는다.
