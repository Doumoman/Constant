TASK: MAP14_09_EXPORT_DEBUG_AND_CREATE_GRAYBOX_TESTS
STATUS: PASS
MAP14_09: COMPLETE ELIGIBLE only when PASS
MAP14_10_MAP14_SECTOR_PLANNER_EXIT_TESTS: LOCKED / DO NOT START

## User-Facing Implementation Report

MAP14_09는 MAP14_01~08의 public planner 결과를 읽어 설명 가능한 immutable in-memory debug packet과 graybox fixture descriptor catalog를 만드는 단계로 구현했다. 이 결과는 Tilemap/Scene/Prefab/GameObject를 생성하거나 게임플레이를 실행하는 graybox가 아니며, MAP14_10 exit approval도 주장하지 않는다.

- 성공 debug export: 1개, success section 9개, spatial token 1,675개, compact text/grid payload 9개, canonical digest `5b8ed6a3c8b0a20fe2f2d05eea0b7731522aff30e691ca606c638adcdbd62d82`.
- success section: `SourceIdentity`, `RouteAccess`, `AnchorBoundarySpecial`, `SpineEnvelope`, `ClusterPattern`, `QuietActivityEvent`, `OwnershipPlanes`, `RetryRng`, `MutationProof`. Failure/coverage 전용 section은 각 전용 exporter/builder가 별도로 발행한다.
- failure 1-ring export: 1개, center 1개 + Moore-ring neighbor 8개, missing neighbor 0개, repair 0회, canonical digest `9c44265951f3f2f8bab300e0d7bf600c6b8c1877de72b4047efa5449855af0db`.
- graybox fixture: `OneSector` 9개 + `ThreeSector` 9개 + `FailureOneRing` 1개 = 총 19개. Coverage digest는 `2260118432dd001a57cfdefd243b7d247d09609226749c9db779eb2ba7288e94`이다.
- coverage required/covered/missing: RouteType/condition 7/7/0 (`Type0`~`Type4`, `Boundary`, `Special`), biome 4/4/0 (`MoonCrater`, `CassiaRoot`, `AbandonedMill`, `MoonDough`), MAP08 canonical boundary pair 6/6/0, SpecialRegion 6/6/0 (`Village`, `CoreResource`, `Forge`, `Boss`, deferred `Merchant`, deferred `Maru`), PacingRole 6/6/0, AccessClass 2/2/0, ownership plane 5/5/0, retry stage/terminal 8/8/0.
- ownership plane cell 수: Terrain 13,088, Protection 1,464, Reservation 425, Marker 1, Evidence 0.
- ownership winner cell 수: SpecialRegion 914, Boundary 63, Spine 1,758, TerrainCluster 1,920, MicroPattern 0, Quiet 10,322, ActivityMarker 1, EventMarker 0, ReservedNoWrite 0, ProtectedNoWrite 0, Empty 0.
- retry evidence 수: None 0, PatternCandidate 2, PatternTransform 1, ClusterVariant 2, ClusterFootprint 1, SectorAttempt 0, Abort 0; terminal은 `AcceptRecovered`. 0인 단계도 명시적 zero-count evidence로 보존했다.
- export 전후 `SectorPlannerInput`, pacing, fixed anchors, cluster placement, spine/envelope, role/pattern, render, quiet/activity/event, canvas ownership, retry plan digest와 MAP12 marker authority, route/access/socket, boundary/special, cluster/variant/footprint, ProtectedOpen, render/quiet cell 및 retry RNG trace identity가 모두 동일함을 focused test에서 검증했다.
- 새 RNG draw 0, retry execution 0, fallback carve 0, validation relaxation 0, sector/world rerandom 0, fixed-anchor/boundary-socket/SpecialRegion/ProtectedOpen mutation 0이다.
- generated debug file export 0, Tilemap write 0, Scene/Prefab/Tilemap/GameObject mutation 0, EditorWindow/overlay/inspector mutation 0, Activity/Event runtime spawn 0, reward/combat/crafting/inventory/NPC execution 0, MAP14 exit approval claim 0이다.
- 회귀 테스트는 실행하지 않았다. 이번 작업의 test selection은 `Game.Map.Tests.EditMode` assembly와 `MAP14_09` category의 교집합뿐이었다.

Editor 가시성은 Unity Test Runner 결과와 Console에 출력되는 deterministic section/token/grid/coverage 수치뿐이다. EditorWindow, overlay, inspector, Scene asset 또는 generated visualization file은 없다. 게임 가시성은 없다. Runtime assembly에 있는 데이터 모델/API지만 Tilemap renderer나 gameplay loop에 연결하지 않았다.

아직 구현하지 않은 범위는 MAP14_10 exit test/approval, 실제 tile reachability, player physics/collider traversal, production seed 승인, 169-sector production solve, Tilemap bake, MicroChunk slicing/streaming, JSON/CSV/debug file export, Editor visualization, gameplay spawn 및 reward/combat/crafting/inventory/NPC 실행이다. 이 범위의 downstream owner는 잠금 상태의 MAP14_10이다.

## Responsibility and Added Functions

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerDebugExport.cs`

- `SectorPlannerDebugExportKind`, `SectorPlannerDebugSectionKind`, `SectorPlannerDebugTokenKind`, `SectorPlannerDebugSeverity`, `SectorPlannerGrayboxFixtureKind`, `SectorPlannerGrayboxCoverageKind`, `SectorPlannerDebugExportErrorCode`: debug packet의 export/section/token/severity/fixture/coverage/error vocabulary를 정의한다. enum value 입력 -> culture-independent semantic label 출력이다.
- `SectorPlannerDebugExportError`: error code/subject/detail 입력 -> stable comparable error 출력이다. `CompareTo`, `Equals`, `GetHashCode`, `ToString`은 error 입력 -> canonical ordering/identity/text 출력이다.
- `SectorPlannerDebugFact`: key/value 입력 -> immutable fact 출력이다. `CompareTo`/`ToString`은 fact 입력 -> canonical ordering/text 출력이다.
- `SectorPlannerDebugToken`: kind, sector/local coordinate, source/owner/label 입력 -> bounds와 source identity를 보존하는 immutable spatial token 출력이다. `Identity`/`CompareTo`는 token 입력 -> stable identity/order 출력이다.
- `SectorPlannerDebugGridPayload`: sector, compact rows, legend 입력 -> defensive-copy된 in-memory grid payload 출력이다. `CompareTo`는 payload 입력 -> sector 기반 canonical order 출력이다.
- `SectorPlannerDebugSection`: section metadata, facts, tokens 입력 -> stable-sorted immutable section과 digest 출력이다. `CompareTo`는 section 입력 -> section kind/id 기반 canonical order 출력이다.
- `SectorPlannerDebugMutationProof`: before/after identity와 금지 mutation counter 입력 -> immutable no-mutation proof 출력이다.
- `SectorPlannerDebugExport`: kind, sections, grids, legend, proof 입력 -> immutable export와 aggregate token/grid count/digest 출력이다.
- `SectorPlannerDebugExportRequest`: MAP14_08 retry plan과 optional caller token/claim 입력 -> exporter가 검증할 immutable request 출력이다.
- `SectorPlannerDebugExportResult`: export/failure-ring/fixture/coverage/error 입력 -> 성공 시 해당 immutable payload, 실패 시 partial payload 없는 stable-sorted error result 출력이다.
- `SectorPlannerDebugExporter.Export`: public MAP14_01~08 chain을 가진 request 입력 -> 9개 success section, 1,675 tokens, 9 grid payload 및 mutation proof를 가진 atomic debug export 출력이다. missing source, duplicate/out-of-bounds token, forbidden claim 또는 identity mismatch 입력 -> null payload와 deduped errors 출력이다.
- `SectorPlannerDebugCanonicalDigest.ComputeSection`: section 입력 -> lower-hex SHA-256 section digest 출력이다.
- `SectorPlannerDebugCanonicalDigest.ComputeExport`: export 입력 -> stable-sorted canonical export digest 출력이다.
- `SectorPlannerDebugCanonicalDigest.Hash`: culture-invariant text 입력 -> lower-hex SHA-256 출력이다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerFailureRingExporter.cs`

- `SectorPlannerFailureRingSector`: public sector snapshot와 center 여부/relative coordinate 입력 -> route/socket/boundary/special/ownership protection-reservation identity를 담은 immutable ring-sector descriptor 출력이다. `CompareTo`는 descriptor 입력 -> coordinate 기반 stable order 출력이다.
- `SectorPlannerFailureRingSnapshot`: failed trace, center/ring sector, missing-neighbor reason, failure section 입력 -> immutable center + available Moore-ring debug snapshot와 digest 출력이다. `CenterSector`/`RingSectors`/count properties는 snapshot 입력 -> 분리된 center/ring 수치 출력이다.
- `SectorPlannerFailureRingExporter.ExportFailureRing(request, nodeTrace, contexts)`: export request + public retry node trace + public sector contexts 입력 -> failure owner/code/detail, retry/RNG, center 1 + ring 8, missing 0, mutation 0인 atomic result 출력이다.
- `SectorPlannerFailureRingExporter.ExportFailureRing(request, attemptTrace, contexts)`: request + public attempt trace + contexts 입력 -> matching public node trace를 해석한 동일 형식의 atomic failure-ring result 출력이다.

### `Assets/_Game/Map/Runtime/WorldGeneration/SectorPlanning/SectorPlannerGrayboxFixtureCatalog.cs`

- `SectorPlannerGrayboxFixture`: fixture ID/kind/center/neighbors/coverage tags/source identities/expected route-access-pacing-biome-boundary-special-ownership-retry/debug digest 입력 -> stable-sorted immutable descriptor 출력이다. `CompareTo`는 fixture 입력 -> fixture ID/kind 기반 canonical order 출력이다.
- `SectorPlannerGrayboxCoverageAudit`: required/covered/missing maps, fixture counts, zero-count evidence 입력 -> defensive-copy된 coverage audit와 digest 출력이다. `RequiredFor`, `CoveredFor`, `MissingFor`, `CoveredIn`은 coverage kind/fixture kind 입력 -> immutable coverage set 출력이다.
- `SectorPlannerGrayboxFixtureCatalogBuilder.Build`: request + successful debug export + failure-ring export + public sector contexts 입력 -> `9 OneSector + 9 ThreeSector + 1 FailureOneRing` descriptors와 coverage audit 출력이다. missing coverage, duplicate fixture, adjacency/bounds/private/file-write claim 오류 입력 -> catalog 없는 atomic error result 출력이다.

### `Assets/_Game/Tests/EditMode/Map/WorldGeneration/SectorPlanning/SectorPlannerDebugGrayboxTests.cs`

- `SectorPlannerDebugGrayboxTests.BuildReferencePacket`: 공개 MAP14_01~08 API로 구성한 deterministic 3x3 reference input -> 성공 export, center+8 failure ring, 19-fixture catalog 출력이다. production private state는 읽지 않는다. 테스트 fixture용 기존 RNG definition 생성에만 test-local reflection을 사용한다.
- `DebugExportPublishesSuccessfulPlanSectionsTokensAndDigest`: valid reference packet 입력 -> 9 required sections, 1,675 tokens, 9 grids, immutable/lower-hex digest PASS 출력이다.
- `FailureRingExportsCenterAndAvailableNeighborContextWithoutRepair`: failed retry-node + 3x3 contexts 입력 -> center 1/ring 8/missing 0/repair 0 PASS 출력이다.
- `GrayboxCatalogCoversEveryRouteTypeInOneAndThreeSectorFixtures`: seven public route conditions 입력 -> OneSector와 ThreeSector 각각 coverage/missing 0 PASS 출력이다.
- `GrayboxCatalogCoversEveryBiomeInOneAndThreeSectorFixtures`: four public biome IDs 입력 -> 양 fixture kind coverage/missing 0 PASS 출력이다.
- `GrayboxCatalogCoversEveryBoundaryPairInOneAndThreeSectorFixtures`: six MAP08 canonical pairs 입력 -> 양 fixture kind coverage/missing 0 PASS 출력이다.
- `GrayboxCatalogCoversEverySpecialConditionInOneAndThreeSectorFixtures`: Village/CoreResource/Forge/Boss/Merchant/Maru 입력 -> 양 fixture kind coverage/missing 0 PASS 출력이다.
- `CoverageIncludesOwnershipPlanesAndRetryStages`: MAP14_07 ownership + MAP14_08 retry evidence 입력 -> plane 5/5/0, retry stage/terminal 8/8/0, fixture 9/9/1 PASS 출력이다.
- `DebugExportPreservesAllUpstreamIdentityAndDoesNotDrawRng`: MAP14_01~08 before/after identities 입력 -> 모두 equal, RNG draw/retry execution 0 PASS 출력이다.
- `InvalidMissingSourceDuplicateTokenMissingCoverageAndFileWriteClaimsFailAtomically`: missing source/duplicate token/missing coverage/file-write claim 입력 -> null partial payload, empty digest, stable errors, mutation 0 PASS 출력이다.
- `PublicationIsDeterministicAcrossRepeatReverseAndTurkishCulture`: repeat/reversed/`tr-TR` 입력 -> export/failure/catalog digest 동일 PASS 출력이다.
- `NoTilePhysicsScenePreviewGameplayOrExitApprovalMutation`: mutation proof 입력 -> Tile/physics/Scene/Prefab/GameObject/Editor/file/gameplay/exit counter 전부 0 PASS 출력이다.

Production 변경은 Runtime C# 신규 3개와 matching meta 3개뿐이다. Focused EditMode test C# 신규 1개와 matching meta 1개를 추가했다. 기존 production/Editor/test/CSV/Scene/Prefab/Tilemap/asmdef/Settings/Packages 파일 수정은 0개이며, MAP14_01~08 upstream 파일 수정도 0개이다.

## Focused Verification

```text
Unity: 6000.3.8f1
mode: EditMode
assembly_names: [Game.Map.Tests.EditMode]
category_names: [MAP14_09]
job_id: aa9726e971c8434db6ca48f426e47cb6
durationSeconds: 4.5567057
discovered: 11
executed: 11
passed: 11
failed: 0
skipped: 0
inconclusive: 0
compile errors: 0
relevant Console errors after final verification/clear: 0
relevant Console warnings after final verification/clear: 0

REGRESSION TRIGGER DETECTED: NO
PRIOR TASK TEST SELECTIONS: 0
LEGACY 19347 SELECTIONS: 0
PLAYMODE SELECTIONS: 0
UNFILTERED TEST SELECTIONS: 0
```

## Static and Workflow Verification

- 단일 inbox candidate만 검증했고 installed Task와 archive의 SHA-256은 모두 `ed399de25b62bc6d59f9e4912859e7e39f8cbebd53f0ca0b3157b75efaff72a1`로 원본과 일치한다.
- 시작 조건은 MAP14_08 Result PASS/SHA 일치, installed Task SHA 일치, MAP14_08 COMPLETE, MAP14_09 CURRENT, MAP14_10 LOCKED, unrelated staged 0이었다.
- 신규 Runtime 구현은 `System.IO`, Editor API, Tilemap/Scene/Prefab/GameObject write API를 사용하지 않는다. debug/grid/catalog는 메모리 모델뿐이다.
- 현재 Task-owned compile error 0, final clear 후 relevant Console error/warning 0이다.
- 관련 없는 기존 worktree 변경은 수정하거나 stage하지 않는다.

Commit subject: `MAP14_09: export debug and create graybox tests`

Push: NOT PERFORMED
