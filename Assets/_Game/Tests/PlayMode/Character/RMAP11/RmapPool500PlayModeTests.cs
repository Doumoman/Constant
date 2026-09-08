#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.Live.Input;
using StarNight.Character.Live.Movement;
using StarNight.Character.Live.Player;
using StarNight.Map.WorldGeneration.MicroPatterns;
using StarNight.Map.WorldGeneration.MoonPalace.RunGeneration;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace StarNight.Character.Tests.PlayMode.Rmap11
{
    [Category("RMAP11")]
    public sealed class RmapPool500PlayModeTests
    {
        private const string ScenePath = "Assets/_Game/Map/Scenes/MoonPalace/RMAP11/MoonPalacePool500_FIX25_RMAP11.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";

        [UnityTest]
        public IEnumerator ActualPool500SceneBakesAnOutsideFirst48CandidateIntoTilemapCollider()
        {
            yield return LoadScene();
            Tilemap terrain = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Single(value => value.name == "RMAP11_FIX25_BaseTerrain");
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            RmapSmallRunPlan plan = RmapSmallRunHarness.Generate(new RmapSmallRunRequest(1107, 36, 24,
                RmapSmallRunRecipe.PortGalleryV1));
            RmapSmallRunChunk chunk = plan.Chunks.First(value => value.Selections.Any(selection =>
                pool.TryGetCandidate(selection.CandidateId, out RmapPatternPool500Entry entry) && !entry.IsInitialPool));
            RmapSmallRunPatternSelection selection = chunk.Selections.First(value =>
                pool.TryGetCandidate(value.CandidateId, out RmapPatternPool500Entry entry) && !entry.IsInitialPool);
            int solid = selection.FinalCells.Select((cell, index) => new { cell, index })
                .First(value => value.cell == RmapPatternBaseCell.Solid).index;

            Assert.That(plan.Success, Is.True, plan.FailureSummary);
            Assert.That(plan.PoolVersion, Is.EqualTo(RmapPatternPool500.DataVersion));
            Assert.That(terrain.GetComponent<TilemapCollider2D>(), Is.Not.Null);
            Assert.That(terrain.GetTile(new Vector3Int(chunk.OriginX + selection.SlotX + (solid % 4),
                chunk.OriginY + selection.SlotY + (solid / 4), 0)), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<CharacterLivePlayerRig>().name, Is.EqualTo("RMAP11_FIX25_Player"));
            Assert.That(Object.FindFirstObjectByType<CharacterLiveMapRunExit>().name, Is.EqualTo("RMAP11_FIX25_Exit"));
        }

        [UnityTest]
        public IEnumerator ActualProductionPlayerReachesRmap11ExitThroughTheGeneratedTilemapCollider()
        {
            yield return LoadScene();
            Object.FindFirstObjectByType<CharacterLivePlayerRig>().gameObject.SetActive(false);
            CharacterLiveMapRunExit exit = Object.FindFirstObjectByType<CharacterLiveMapRunExit>();
            CharacterLivePlayerRig player = CreateActualPlayer(new Vector2(1.5f, 1f));
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            for (var index = 0; index < 520 && !exit.HasBeenReached; index++)
                yield return Feed(player, 1f);

            Assert.That(exit.HasBeenReached, Is.True, "Production Player must reach RMAP11_FIX25_Exit. position=" +
                player.Body.position + " grounded=" + movement.IsGroundedNow + " velocity=" + movement.Velocity);
            Assert.That(exit.ReachedBy, Is.EqualTo(player));
            Assert.That(player.Body.position.x, Is.GreaterThanOrEqualTo(33.5f));
            Assert.That(movement.IsDriving, Is.True);
            Object.Destroy(player.transform.parent.gameObject);
        }

        private static IEnumerator LoadScene()
        {
            EditorSceneManager.LoadSceneInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return new WaitForFixedUpdate();
            Physics2D.SyncTransforms();
        }

        private static CharacterLivePlayerRig CreateActualPlayer(Vector2 feet)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var host = new GameObject("RMAP11_ActualPhysicsPlayer");
            host.SetActive(false);
            GameObject instance = Object.Instantiate(prefab, host.transform);
            CharacterLivePlayerRig player = instance.GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = instance.GetComponent<CharacterLiveMovementDriver>();
            player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            player.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            host.SetActive(true);
            player.Body.position = feet;
            movement.ResetMotion();
            player.InputSource.enabled = false;
            Physics2D.SyncTransforms();
            return player;
        }

        private static IEnumerator Feed(CharacterLivePlayerRig player, float horizontal)
        {
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(horizontal, false, false, false, idle, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }
    }

    /// <summary>
    /// FIX25 uses the real production rig against individually baked TilemapCollider2D cells.
    /// Each fixture owns only its 4x4 candidate plus declared entry/landing neighbour terrain;
    /// no movement setting, boost, or mid-run relocation is used.
    /// </summary>
    [Category("RMAP11")]
    public sealed class RmapPool500Fix25PlayModeTests
    {
        private const string PlayerPrefabPath = "Assets/_Game/Live/Prefabs/CharacterLivePlayer.prefab";
        private const string TerrainTilePath = "Assets/_Game/Live/Prefabs/RMAP02/Tiles/RMAP02_Terrain.asset";

        [UnityTest]
        public IEnumerator Fix25_All25BakedTilemapFixturesUseTheExactFinalCells()
        {
            foreach (FixtureSpec spec in Fixtures)
            {
                Fixture fixture = CreateFixture(spec);
                Assert.That(fixture.CellColliders.Count, Is.EqualTo(fixture.Entry.BaseCells.Count(cell => cell == RmapPatternBaseCell.Solid)),
                    "solid TilemapCollider count for atlas " + spec.AtlasNo);
                Assert.That(fixture.CellColliders.All(collider => collider is TilemapCollider2D), Is.True);
                Assert.That(fixture.GrabSurface == null, Is.EqualTo(!spec.RequiresGrab));
                if (spec.RequiresGrab)
                    Assert.That(fixture.GrabSurface.IsGrabAllowed, Is.True, "safe solid grab surface at atlas " + spec.AtlasNo);
                Object.Destroy(fixture.Root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Fix25_SixOneTileRoutesUseActualJumpWithoutGrab()
        {
            foreach (FixtureSpec spec in Fixtures.Where(value => !value.RequiresGrab))
            {
                Fixture fixture = CreateFixture(spec);
                CharacterLivePlayerRig player = CreatePlayer(fixture.Root.transform, spec.EntryFeet);
                float startY = player.Body.position.y;
                float peakY = startY;
                for (var frame = 0; frame < 72; frame++)
                {
                    yield return Feed(player, spec.Direction, false, frame == 0, frame < 10);
                    peakY = Mathf.Max(peakY, player.Body.position.y);
                }
                Assert.That(peakY, Is.GreaterThan(startY + 0.8f), "one-tile jump apex for atlas " + spec.AtlasNo);
                Assert.That(player.GetComponent<CharacterLiveMovementDriver>().IsGrabbing, Is.False,
                    "one-tile route has no grab surface at atlas " + spec.AtlasNo);
                Object.Destroy(fixture.Root);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Fix25_NineteenTwoTileRoutesObserveJumpGrabJumpAndLanding()
        {
            foreach (FixtureSpec spec in Fixtures.Where(value => value.RequiresGrab))
            {
                Fixture fixture = CreateFixture(spec);
                CharacterLivePlayerRig player = CreatePlayer(fixture.Root.transform, spec.PreGrabFeet);
                CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
                bool observedGrab = false;
                for (var frame = 0; frame < 150 && !observedGrab; frame++)
                {
                    yield return Feed(player, spec.Direction, false, frame == 0, frame < 10);
                    observedGrab |= movement.IsGrabbing;
                }
                Assert.That(observedGrab, Is.True, "jump must reach exposed solid Tilemap corner for atlas " + spec.AtlasNo +
                    " start=" + spec.PreGrabFeet + " final=" + player.Body.position + " grounded=" + movement.IsGroundedNow);
                Assert.That(Vector2.Distance(movement.LastSafeGrabAnchor, spec.GrabCorner), Is.LessThan(0.001f),
                    "grab must use the declared exposed corner for atlas " + spec.AtlasNo);

                yield return Feed(player, spec.Direction, false, true, true);
                Assert.That(movement.IsGrabbing, Is.False, "jump explicitly exits grab for atlas " + spec.AtlasNo);
                bool landed = false;
                for (var frame = 0; frame < 180; frame++)
                {
                    // The authored jump travels onto the next support, then releases horizontal input to land there;
                    // keeping a direction held would intentionally run beyond one-cell crest supports.
                    yield return Feed(player, frame < 12 ? spec.Direction : 0f, false, false, false);
                    landed |= movement.IsGroundedNow && ((spec.Direction > 0f && player.Body.position.x >= spec.GrabCorner.x + 0.4f) ||
                        (spec.Direction < 0f && player.Body.position.x <= spec.GrabCorner.x - 0.4f));
                }
                Assert.That(landed, Is.True, "grab-jump must make a stable far-side landing for atlas " + spec.AtlasNo +
                    " final=" + player.Body.position + " grounded=" + movement.IsGroundedNow +
                    " anchor=" + movement.LastSafeGrabAnchor);
                Object.Destroy(fixture.Root);
                yield return null;
            }
        }

        private static Fixture CreateFixture(FixtureSpec spec)
        {
            RmapPatternPool500Snapshot pool = RmapPatternPool500.BuildFinalPool();
            RmapPatternPool500Entry entry = pool.Candidates.Single(value => value.PoolIndex == spec.PoolIndex);
            Assert.That(entry.BaseCells16, Is.EqualTo(spec.Cells), "FIX25 geometry at atlas " + spec.AtlasNo);
            var fixture = new Fixture { Root = new GameObject("RMAP11_FIX25_" + spec.AtlasNo, typeof(Grid)), Entry = entry };
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TerrainTilePath);
            Assert.That(tile, Is.Not.Null);
            for (var y = 0; y < 4; y++)
            for (var x = 0; x < 4; x++)
            {
                if (entry.GetCell(x, y) != RmapPatternBaseCell.Solid) continue;
                TilemapCollider2D collider = CreateCell(fixture.Root.transform, tile, new Vector3Int(x, y, 0),
                    "Pattern_" + x + "_" + y);
                fixture.CellColliders.Add(collider);
                if (spec.RequiresGrab && spec.GrabCell == new Vector3Int(x, y, 0))
                {
                    fixture.GrabSurface = collider.gameObject.AddComponent<CharacterLiveGrabSurface>();
                    fixture.GrabSurface.Configure(CharacterLiveGrabSurface.SurfaceKind.StaticSafe);
                }
            }
            CreateNeighbourFloor(fixture.Root.transform, tile, spec.EntryFeet, spec.Direction < 0f ? 4 : -1, "EntryFloor");
            CreateNeighbourFloor(fixture.Root.transform, tile, spec.LandingFeet, spec.Direction < 0f ? -2 : 5, "LandingFloor");
            Physics2D.SyncTransforms();
            return fixture;
        }

        private static TilemapCollider2D CreateCell(Transform parent, Tile tile, Vector3Int coordinate, string name)
        {
            var host = new GameObject(name, typeof(Tilemap), typeof(TilemapCollider2D));
            host.transform.SetParent(parent, false);
            host.GetComponent<Tilemap>().SetTile(coordinate, tile);
            return host.GetComponent<TilemapCollider2D>();
        }

        private static void CreateNeighbourFloor(Transform parent, Tile tile, Vector2 feet, int x, string name)
        {
            CreateCell(parent, tile, new Vector3Int(x, Mathf.FloorToInt(feet.y) - 1, 0), name + "A");
            CreateCell(parent, tile, new Vector3Int(x + (x < 0 ? -1 : 1), Mathf.FloorToInt(feet.y) - 1, 0), name + "B");
            CreateCell(parent, tile, new Vector3Int(x + (x < 0 ? 1 : -1), Mathf.FloorToInt(feet.y) - 1, 0), name + "C");
            CreateCell(parent, tile, new Vector3Int(x + (x < 0 ? 2 : -2), Mathf.FloorToInt(feet.y) - 1, 0), name + "D");
        }

        private static CharacterLivePlayerRig CreatePlayer(Transform parent, Vector2 feet)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var host = new GameObject("RMAP11_FIX25_Player");
            host.transform.SetParent(parent, false);
            host.SetActive(false);
            CharacterLivePlayerRig player = Object.Instantiate(prefab, host.transform).GetComponent<CharacterLivePlayerRig>();
            CharacterLiveMovementDriver movement = player.GetComponent<CharacterLiveMovementDriver>();
            player.BodyCollider.size = new Vector2(0.4f, 0.8f);
            player.BodyCollider.offset = new Vector2(0f, 0.4f);
            movement.ConfigureRmap02(1 << LayerMask.NameToLayer("Default"));
            host.SetActive(true);
            player.Body.position = feet;
            movement.ResetMotion();
            player.InputSource.enabled = false;
            Physics2D.SyncTransforms();
            return player;
        }

        private static IEnumerator Feed(CharacterLivePlayerRig player, float horizontal, bool down, bool jumpPressed, bool jumpHeld)
        {
            var jump = new CharacterLiveButtonFrame(jumpPressed, false, jumpHeld);
            var idle = new CharacterLiveButtonFrame(false, false, false);
            player.InputSource.Adapter.AccumulateFrame(horizontal, down, jump, idle, idle, idle);
            yield return new WaitForFixedUpdate();
        }

        private static readonly FixtureSpec[] Fixtures =
        {
            new FixtureSpec(2, 11, "AASSAAASAAAAAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), null),
            new FixtureSpec(7, 51, "ASSSASSSAAASAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), new Vector2(1f, 2f)),
            new FixtureSpec(8, 52, "ASSSAASSAASSAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), new Vector2(2f, 3f)),
            new FixtureSpec(13, 57, "ASSSAAASAAAAAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), null),
            new FixtureSpec(14, 58, "ASSSAASSAAASAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), null),
            new FixtureSpec(16, 60, "ASSSASSSAASSAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), new Vector2(1f, 2f)),
            new FixtureSpec(18, 62, "AASSAASSAAASAAAA", 1f, new Vector2(-.5f, 0f), new Vector2(5.5f, 0f), new Vector2(2f, 2f)),
            new FixtureSpec(19, 3, "SSAASAAAAAAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), null),
            new FixtureSpec(36, 77, "SSSASAAAAAAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), null),
            new FixtureSpec(38, 79, "SSAASAAASAAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), new Vector2(1f, 3f)),
            new FixtureSpec(42, 83, "SSSASAAASAAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), new Vector2(1f, 3f)),
            new FixtureSpec(44, 85, "SSSASSSASSAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), new Vector2(3f, 2f)),
            new FixtureSpec(46, 87, "SSAASSAAAAAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), new Vector2(2f, 2f)),
            new FixtureSpec(47, 88, "SSAASSAASAAAAAAA", -1f, new Vector2(4.5f, 0f), new Vector2(-1.5f, 0f), new Vector2(2f, 2f)),
            new FixtureSpec(49, 90, "SSSSSSSASAAAAAAA", -1f, new Vector2(4.5f, 1f), new Vector2(-1.5f, 0f), null),
            new FixtureSpec(173, 140, "SSSSSSAASAAASAAA", -1f, new Vector2(4.5f, 1f), new Vector2(-1.5f, 0f), new Vector2(1f, 4f)),
            new FixtureSpec(174, 141, "SSSSSSAASSAASAAA", -1f, new Vector2(4.5f, 1f), new Vector2(-1.5f, 0f), new Vector2(2f, 3f)),
            new FixtureSpec(182, 149, "SSSSSSSASAAASAAA", -1f, new Vector2(4.5f, 1f), new Vector2(-1.5f, 0f), new Vector2(1f, 4f)),
            new FixtureSpec(188, 155, "SSSSSSSASSSASSAA", -1f, new Vector2(4.5f, 1f), new Vector2(-1.5f, 0f), new Vector2(3f, 3f)),
            new FixtureSpec(190, 157, "SSSSSSSASSSASAAA", -1f, new Vector2(4.5f, 1f), new Vector2(-1.5f, 0f), new Vector2(3f, 3f)),
            new FixtureSpec(197, 161, "SSSSASSSASSSAAAS", 1f, new Vector2(-.5f, 1f), new Vector2(5.5f, 0f), new Vector2(1f, 3f)),
            new FixtureSpec(205, 169, "SSSSAASSAASSAAAS", 1f, new Vector2(-.5f, 1f), new Vector2(5.5f, 0f), new Vector2(2f, 3f)),
            new FixtureSpec(207, 171, "SSSSAASSAAASAAAS", 1f, new Vector2(-.5f, 1f), new Vector2(5.5f, 0f), new Vector2(3f, 4f)),
            new FixtureSpec(219, 183, "SSSSASSSAAASAAAS", 1f, new Vector2(-.5f, 1f), new Vector2(5.5f, 0f), new Vector2(3f, 4f)),
            new FixtureSpec(221, 185, "SSSSASSSAASSAASS", 1f, new Vector2(-.5f, 1f), new Vector2(5.5f, 0f), new Vector2(2f, 4f)),
        };

        private sealed class FixtureSpec
        {
            public FixtureSpec(int atlasNo, int poolIndex, string cells, float direction, Vector2 entryFeet,
                Vector2 landingFeet, Vector2? grabCorner)
            {
                AtlasNo = atlasNo; PoolIndex = poolIndex; Cells = cells; Direction = direction;
                EntryFeet = entryFeet; LandingFeet = landingFeet; GrabCorner = grabCorner.GetValueOrDefault();
                RequiresGrab = grabCorner.HasValue;
                GrabCell = RequiresGrab ? new Vector3Int(direction > 0f ? Mathf.RoundToInt(GrabCorner.x) : Mathf.RoundToInt(GrabCorner.x) - 1,
                    Mathf.RoundToInt(GrabCorner.y) - 1, 0) : new Vector3Int(-99, -99, 0);
            }
            public int AtlasNo { get; } public int PoolIndex { get; } public string Cells { get; } public float Direction { get; }
            public Vector2 EntryFeet { get; } public Vector2 LandingFeet { get; } public Vector2 GrabCorner { get; }
            public Vector3Int GrabCell { get; } public bool RequiresGrab { get; }
            public Vector2 PreGrabFeet { get { return PreGrabFeetByAtlas.TryGetValue(AtlasNo, out Vector2 value) ? value : EntryFeet; } }
        }

        private static readonly System.Collections.Generic.Dictionary<int, Vector2> PreGrabFeetByAtlas =
            new System.Collections.Generic.Dictionary<int, Vector2>
            {
                [7] = new Vector2(.5f, 0f), [8] = new Vector2(1.5f, 1f), [16] = new Vector2(.5f, 0f),
                [18] = new Vector2(1.5f, 0f), [38] = new Vector2(1.5f, 1f), [42] = new Vector2(1.5f, 1f),
                [44] = new Vector2(3.5f, 0f), [46] = new Vector2(2.5f, 0f), [47] = new Vector2(2.5f, 0f),
                [173] = new Vector2(1.5f, 2f), [174] = new Vector2(2.5f, 1f), [182] = new Vector2(1.5f, 2f),
                [188] = new Vector2(3.5f, 1f), [190] = new Vector2(3.5f, 1f), [197] = new Vector2(.5f, 1f),
                [205] = new Vector2(1.5f, 1f), [207] = new Vector2(2.5f, 2f), [219] = new Vector2(2.5f, 2f),
                [221] = new Vector2(1.5f, 2f),
            };

        private sealed class Fixture
        {
            public GameObject Root;
            public RmapPatternPool500Entry Entry;
            public readonly System.Collections.Generic.List<Collider2D> CellColliders = new System.Collections.Generic.List<Collider2D>();
            public CharacterLiveGrabSurface GrabSurface;
        }
    }
}
#endif
