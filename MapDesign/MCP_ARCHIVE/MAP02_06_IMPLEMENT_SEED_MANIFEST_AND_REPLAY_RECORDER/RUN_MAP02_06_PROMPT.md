# RUN MAP02_06

`MapDesign/MCP/00_MCP_ENTRYPOINT.md`부터 locked/work/CSV/Unity/change/patch/finalize rules, Master, Status, 현재 `TASKS/MAP02_06_IMPLEMENT_SEED_MANIFEST_AND_REPLAY_RECORDER.md`, MAP02_05 PASS Result를 순서대로 읽어라.

Task의 exact READ/WRITE ALLOWLIST를 준수해 successful exact `PASS_GRID` recorded execution을 immutable `SeedManifest`와 replay bundle로 기록하라. bundle의 final file set은 exact `seed_manifest.csv`, `generated_world_sectors.csv` 두 개뿐이다. existing serializer bytes를 재사용하고 caller-supplied full generated-output root 아래 `GeneratedWorlds/{world_profile_id}/{seed D16}`에 deterministic staging/backup directory swap으로 publish/load하라.

replay는 bundle/manifest/content hash/generator build ID precondition을 먼저 검사하고 모두 PASS한 뒤에만 `ExecuteThroughRecorded(..., "PASS_GRID")`를 exact 1회 호출하라. replayed `generated_world_sectors.csv`만 byte-for-byte 비교하고 manifest의 UTC/duration은 진단값으로 분리하라. Root/record/RNG/artifact/Registry/content-hash 기존 코드를 수정하지 말고, 후속 generated CSV/edges placeholder/JSON/overlay/approval을 구현하지 마. 새 directory/folder meta를 만들지 말고 Assets meta `2973`, accepted legacy folder meta `6/6`을 pre-task baseline으로 사용하라.

focused >=64, 기존 `56/103/90/84/77`, targeted >=1341, full EditMode >=1361, compile/Console `0/0`, Authoring `50/50`, final Assets meta `2981`, duplicate GUID `0`, exact Assets changes `16`, existing Assets modification `0`을 모두 PASS하라.

전부 PASS일 때만 MAP02_06 COMPLETE/Current Task NONE으로 finalize하고 MAP02_07은 LOCKED로 유지하라.
