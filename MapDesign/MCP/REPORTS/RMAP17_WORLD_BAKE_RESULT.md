# RMAP17_WORLD_BAKE Result

STATUS: PASS

## Scope and handoff verification

- Applied only `RMAP17_WORLD_BAKE`. The installed Task and Archive are byte-identical at SHA-256 `5017331f8083ee8cc57cb8fed771c9d636df3248979ce39fa7efc43b2f018e46`.
- RMAP16 was already finalized in commits `1a226ed4ae339bc8700d422c0fc9590025a0b904` and `8044a7c3c3ba2f1ede9492d1cbd0c4b4819bdaf7`. Its current PASS Result SHA-256 is `e25ebd333873179c437ddc38cd61e338819d999b4cc703f4d197e9aa19ba9514`; its installed/archive Task SHA-256 is `9b7c78e2c8257a5baf1d679b562763a9db4e478f42676e4d94e730ef4b2691ca`.
- `RmapWorldBakeExecutor` consumes `RmapClusterAssemblyPlanner.CreateBakeSnapshot` directly. It rejects changed snapshot references/digests, validates the complete 624 x 416 coordinate set, compares every RMAP15 protected cell to the final base state, and requires the RMAP15 Spawn cell to be AIR over a SOLID support cell.

## Implemented static scene

- `Assets/_Game/Map/Scenes/MoonPalace/RMAP17/MoonPalaceWorldBake_RMAP17.unity` is a dedicated, saved full-world scene. Its builder removes only `MoonPalace_WorldBake_RMAP17` before recreation; a second batch build removed exactly one prior builder root.
- The direct RMAP16 source has 259,584 base cells: 150,064 SOLID, 109,420 AIR and 100 ONE_WAY. SOLID is baked to one actual TilemapCollider2D + static Rigidbody2D + CompositeCollider2D layer. ONE_WAY is baked to its own TilemapCollider2D + PlatformEffector2D + CharacterLiveOneWayPlatform layer.
- Overlay provenance records are kept exact: 1,627 LADDER, 2 GRAB_EDGE and 3 marker records. The 1,627 ladder records intentionally resolve to 1,236 unique world coordinates in a Tilemap, while preserving every source record; 17 contiguous ladder trigger segments provide the existing `CharacterLiveClimbSurface` convention. The two static-safe grab affordances and three static marker overlays remain separate from base terrain.
- The production Player prefab is placed at RMAP15 Start `(419,337)` as `(419.5,337)` using its 0.4 x 0.8 foot-pivot collider and `ConfigureRmap02`. The supported base-solid tile is present at `(419,336)`.
- The production follow-camera driver is bound to that Player with full-world bounds `(0,0,624,416)` and its established 12 x 8 viewport.

## Unity verification

- Unity 6000.3.8f1 batch scene builder completed twice successfully. The saved scene inventory reports 150,064 solid tiles, 100 one-way tiles, 1,236 unique ladder tiles, 2 grab tiles, 3 marker tiles, 17 usable climb trigger segments, 23 scene colliders, 14,570 solid composite shapes and 100 one-way collider shapes.
- Focused Unity EditMode filter `StarNight.Map.Tests.EditMode.Rmap17.RmapWorldBakeExecutorTests` passed 3/3. It checks exact RMAP16 snapshot identity and protected cells, RMAP15 Start support plus overlay bounds, and deterministic full-bake output without a second terrain path.
- Visual review completed for the full terrain/overlay overview and enlarged Start and ladder areas. The overview includes the full-world border, terrain, one-way, ladder/grab/marker overlays and Start marker.

## Evidence hashes

| Artifact | SHA-256 |
|---|---|
| RMAP17 scene | `aebfc498e3c3d4314b773604fd925c48e82eb4f42cbda58a36959b750c66b24c` |
| bake executor | `b95579618edf0b4021502feebc29ff1fe3550fbe63ab0d6403adcef8af638ea4` |
| scene builder | `bd1ca9ef0e84a0bdf801d8faabd54007a5bbad8f2e54476eca9c2daed6e8a085` |
| focused RMAP17 tests | `c8a5361ac0fd0a9d28cb46192a44b8def2da432988402609f1bfbf8d049a5c28` |
| bake manifest | `7a8c59d5220b0938692413b0101ec9e0a3ee9715464320183b03d1fcdc3b9e4a` |
| focused EditMode XML (3/3 PASS) | `15c282c84e6177f8fbb23587cef4b968ac41493e93131b7effb01f84cb469c81` |
| terrain overview PNG | `58077dfbe6bc95c32c36d1fa9305a7a4386069586a4c685113bad0258e50e413` |
| Start enlarged PNG | `0714bad12a1522765101937bf6a4c94f56efc48af5afabaae53c963396aef33d` |
| ladder enlarged PNG | `3a61266fd52731f4ea855b5ddcba188a0e1010b07d2455e96d5c55a7a2c59c46` |
| layer inventory CSV | `1971ad2ac31717d8d9a25ce623cc2906e79c84ab51685562192615ede49bd814` |

## Delivered review and boundary

`MapDesign/MCP/GENERATED/RMAP17/RMAP17_REVIEW.zip` contains this Result, the current manifest, layer inventory, focused XML and all three review images. The batch log was deliberately excluded and removed because it can contain a transient Unity session credential.

This is a static Tilemap/collider/Player/camera bake. Mutable breakables, progression state, regeneration and save lifecycle are explicitly deferred to RMAP18. RMAP18 remains LOCKED and no push was issued.
