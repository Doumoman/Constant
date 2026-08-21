#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Explosions;
using StarNight.Grid;
using StarNight.Objects;
using StarNight.Player;
using StarNight.Tiles;
using StarNight.World;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using UnityGrid = UnityEngine.Grid;

namespace StarNight.Tests.PlayMode
{
    public sealed class P2ObjectsPlayModeTests
    {
        [UnityTest]
        public IEnumerator CarrySystem_PicksUpDropsThrowsAndThrownObjectPressesPlate()
        {
            GameObject player = new GameObject("P2_Carry_Test_Player");
            Rigidbody2D playerBody = player.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;
            PlayerInputAdapter input = player.AddComponent<PlayerInputAdapter>();
            input.EnableTestInput(true);

            GameObject anchorObject = new GameObject("CarryAnchor");
            anchorObject.transform.SetParent(player.transform, false);
            anchorObject.transform.localPosition = new Vector3(0.6f, 0.1f, 0f);

            CarrySystem carrySystem = player.AddComponent<CarrySystem>();
            carrySystem.Configure(input, playerBody, anchorObject.transform);

            GameObject carriedObject = CreateCarryable(
                "P2_Carry_Test_Object",
                new Vector2(0.5f, 0f),
                out CarryableObject2D carryable,
                out Rigidbody2D carriedBody);

            Assert.That(carrySystem.TryPickup(carryable), Is.True);
            Assert.That(carrySystem.IsCarrying, Is.True);
            Assert.That(carrySystem.HeldObject, Is.SameAs(carryable));
            Assert.That(carryable.IsHeld, Is.True);
            Assert.That(carriedBody.simulated, Is.False);
            Assert.That(carryable.transform.parent, Is.SameAs(anchorObject.transform));

            Assert.That(carrySystem.DropHeld(), Is.True);
            Assert.That(carrySystem.HeldObject, Is.Null);
            Assert.That(carryable.IsHeld, Is.False);
            Assert.That(carriedBody.simulated, Is.True);
            Assert.That(carryable.transform.parent, Is.Null);

            Assert.That(carrySystem.TryPickup(carryable), Is.True);
            playerBody.linearVelocity = new Vector2(1.25f, 0.5f);
            Vector2 throwDirection = new Vector2(1f, 0.65f).normalized;
            Assert.That(carrySystem.ThrowHeld(throwDirection), Is.True);
            Assert.That(carrySystem.HeldObject, Is.Null);
            Assert.That(carryable.IsHeld, Is.False);
            Assert.That(carriedBody.simulated, Is.True);
            Assert.That(carriedBody.linearVelocity.x, Is.GreaterThan(1.25f));
            Assert.That(carriedBody.linearVelocity.y, Is.GreaterThan(0.5f));

            GameObject plateObject = new GameObject("P2_Carry_Test_Plate");
            BoxCollider2D plateTrigger = plateObject.AddComponent<BoxCollider2D>();
            PressurePlate2D plate = plateObject.AddComponent<PressurePlate2D>();
            plate.Configure(plateTrigger);

            int pressedEvents = 0;
            int releasedEvents = 0;
            plate.Pressed += () => pressedEvents++;
            plate.Released += () => releasedEvents++;

            Assert.That(plate.RegisterPresserForTests(carryable), Is.True);
            Assert.That(plate.IsPressed, Is.True);
            Assert.That(plate.PresserCount, Is.EqualTo(1));
            Assert.That(pressedEvents, Is.EqualTo(1));

            Assert.That(plate.UnregisterPresserForTests(carryable), Is.True);
            Assert.That(plate.IsPressed, Is.False);
            Assert.That(plate.PresserCount, Is.EqualTo(0));
            Assert.That(releasedEvents, Is.EqualTo(1));

            Object.Destroy(player);
            Object.Destroy(carriedObject);
            Object.Destroy(plateObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FallingObject_SupportMutationConvertsItToContinuousDynamicBody()
        {
            RuntimeTileWorld world = new RuntimeTileWorld(
                "P2_Falling_Test_World",
                new Vector2Int(8, 5));
            TileDefinition floor = world.CreateDefinition(
                "ReinforcedFloor",
                TileMaterialKind.ReinforcedWall,
                TileBreakMethod.None);
            TileDefinition support = world.CreateDefinition(
                "CrackedSupport",
                TileMaterialKind.CrackedWall,
                TileBreakMethod.Bomb | TileBreakMethod.Pickaxe);
            world.FillRow(0, floor);
            GridPos supportCell = new GridPos(4, 1);
            world.SetTile(supportCell, support);
            world.ConfigureMutationService(
                new GridPos(1, 1),
                new GridPos(6, 1));

            GameObject fallingObject = new GameObject("P2_Falling_Test_Rock");
            fallingObject.transform.SetParent(world.Root.transform, false);
            fallingObject.transform.position =
                world.World.CellToWorldCenter(new GridPos(4, 2));
            Rigidbody2D body = fallingObject.AddComponent<Rigidbody2D>();
            body.position = fallingObject.transform.position;
            fallingObject.AddComponent<BoxCollider2D>().size =
                new Vector2(0.82f, 0.82f);
            FallingObject2D falling =
                fallingObject.AddComponent<FallingObject2D>();
            falling.Configure(world.World, world.MutationService);

            yield return null;
            Assert.That(falling.SupportCell, Is.EqualTo(supportCell));
            Assert.That(falling.IsSupported, Is.True);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Static));
            Assert.That(body.gravityScale, Is.EqualTo(0f));

            int beganFallingCount = 0;
            falling.BeganFalling += _ => beganFallingCount++;
            world.MutationService.EnqueueDestroy(
                supportCell,
                TileBreakMethod.Bomb);
            TileMutationBatchReport mutation =
                world.MutationService.FlushPending();

            Assert.That(mutation.CommittedCount, Is.EqualTo(1));
            Assert.That(world.World.IsSolid(supportCell), Is.False);
            Assert.That(falling.IsFalling, Is.True);
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic));
            Assert.That(body.gravityScale, Is.GreaterThan(0f));
            Assert.That(
                body.collisionDetectionMode,
                Is.EqualTo(CollisionDetectionMode2D.Continuous));
            Assert.That(beganFallingCount, Is.EqualTo(1));

            float startingY = body.position.y;
            for (int frame = 0; frame < 3; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.position.y, Is.LessThan(startingY));
            world.DestroyAll();
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplosionCompletion_RequestsAndResolvesExitBlockerRelocation()
        {
            RuntimeTileWorld world = new RuntimeTileWorld(
                "P2_ExitBlocker_Test_World",
                new Vector2Int(7, 3));
            TileDefinition reinforced = world.CreateDefinition(
                "ReinforcedCorridor",
                TileMaterialKind.ReinforcedWall,
                TileBreakMethod.None);
            world.FillRow(0, reinforced);
            world.FillRow(2, reinforced);
            GridPos start = new GridPos(1, 1);
            GridPos exit = new GridPos(5, 1);
            world.ConfigureMutationService(start, exit, new[] { exit });

            ExplosionService2D explosionService =
                world.Root.AddComponent<ExplosionService2D>();
            explosionService.Configure(
                world.World,
                world.MutationService,
                100,
                0f,
                0);

            GameObject blockerObject = CreateCarryable(
                "P2_ExitBlocker_Test_Crate",
                world.World.CellToWorldCenter(new GridPos(3, 1)),
                out CarryableObject2D blocker,
                out Rigidbody2D blockerBody);
            blockerObject.transform.SetParent(world.Root.transform, true);
            blockerBody.gravityScale = 0f;
            blockerBody.linearVelocity = Vector2.zero;

            GameObject resolverObject = new GameObject("Exit_Protection_Zone");
            resolverObject.transform.SetParent(world.Root.transform, false);
            BoxCollider2D resolverTrigger =
                resolverObject.AddComponent<BoxCollider2D>();
            resolverTrigger.isTrigger = true;
            ExitBlockerResolver2D resolver =
                resolverObject.AddComponent<ExitBlockerResolver2D>();
            resolver.Configure(
                world.World,
                world.MutationService,
                explosionService,
                null,
                start,
                exit,
                new[] { exit });

            GameObject bombObject = new GameObject("P2_ExitBlocker_Test_Bomb");
            bombObject.transform.SetParent(world.Root.transform, false);
            bombObject.transform.position =
                world.World.CellToWorldCenter(new GridPos(3, 1));
            Rigidbody2D bombBody = bombObject.AddComponent<Rigidbody2D>();
            bombBody.gravityScale = 0f;
            Bomb2D bomb = bombObject.AddComponent<Bomb2D>();
            bomb.Configure(
                explosionService,
                ExplosionConstants.BombFuseSeconds,
                false,
                false,
                1);

            int resolutionEvents = 0;
            int eventRelocatedCount = -1;
            resolver.ResolutionCompleted += count =>
            {
                resolutionEvents++;
                eventRelocatedCount = count;
            };

            ExplosionChainReport explosion = bomb.DetonateForTests();

            Assert.That(explosion.ProcessedBombCount, Is.EqualTo(1));
            Assert.That(explosion.HardCapReached, Is.False);
            Assert.That(resolver.ResolutionRequested, Is.True);
            Assert.That(resolver.ResolveNow(), Is.EqualTo(1));
            Assert.That(resolver.ResolutionRequested, Is.False);
            Assert.That(resolver.LastRelocatedCount, Is.EqualTo(1));
            Assert.That(resolver.ExitRouteReachable, Is.True);
            Assert.That(resolutionEvents, Is.EqualTo(1));
            Assert.That(eventRelocatedCount, Is.EqualTo(1));
            Assert.That(
                world.World.WorldToCell(blockerBody.position),
                Is.Not.EqualTo(new GridPos(3, 1)));
            Assert.That(
                world.World.WorldToCell(blockerBody.position),
                Is.Not.EqualTo(exit));

            world.DestroyAll();
            yield return null;
        }

        private static GameObject CreateCarryable(
            string name,
            Vector2 position,
            out CarryableObject2D carryable,
            out Rigidbody2D body)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.position =
                new Vector3(position.x, position.y, 0f);
            body = gameObject.AddComponent<Rigidbody2D>();
            body.position = position;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.82f, 0.82f);
            carryable = gameObject.AddComponent<CarryableObject2D>();
            carryable.Configure(
                null,
                body,
                collider,
                WorldObjectTraits.Carryable | WorldObjectTraits.Pullable,
                1f,
                6.5f);
            return gameObject;
        }

        private sealed class RuntimeTileWorld
        {
            private readonly List<Object> transientAssets = new List<Object>();
            private readonly List<TileDefinition> definitions =
                new List<TileDefinition>();

            public RuntimeTileWorld(string name, Vector2Int size)
            {
                Root = new GameObject(name);
                Root.transform.position = new Vector3(200f, 20f, 0f);
                UnityGrid layout = Root.AddComponent<UnityGrid>();

                GameObject terrainObject = new GameObject("Terrain");
                terrainObject.transform.SetParent(Root.transform, false);
                Terrain = terrainObject.AddComponent<Tilemap>();
                terrainObject.AddComponent<TilemapRenderer>();

                World = Root.AddComponent<GridWorld>();
                World.Configure(
                    layout,
                    Terrain,
                    null,
                    Vector2Int.zero,
                    size);
                MutationService = Root.AddComponent<TileMutationService>();
            }

            public GameObject Root { get; }
            public GridWorld World { get; }
            public Tilemap Terrain { get; }
            public TileMutationService MutationService { get; }

            public TileDefinition CreateDefinition(
                string id,
                TileMaterialKind kind,
                TileBreakMethod breakableBy)
            {
                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.name = id + "_Tile";
                TileDefinition definition =
                    ScriptableObject.CreateInstance<TileDefinition>();
                definition.name = id + "_Definition";
                definition.Configure(
                    id,
                    tile,
                    kind,
                    true,
                    breakableBy);
                transientAssets.Add(definition);
                transientAssets.Add(tile);
                definitions.Add(definition);
                return definition;
            }

            public void FillRow(int y, TileDefinition definition)
            {
                for (int x = World.CellBounds.xMin;
                    x < World.CellBounds.xMax;
                    x++)
                {
                    SetTile(new GridPos(x, y), definition);
                }
            }

            public void SetTile(GridPos cell, TileDefinition definition)
            {
                Terrain.SetTile(
                    new Vector3Int(cell.X, cell.Y, 0),
                    definition != null ? definition.Tile : null);
            }

            public void ConfigureMutationService(
                GridPos start,
                GridPos exit,
                GridPos[] protectedCells = null)
            {
                MutationService.Configure(
                    World,
                    Terrain,
                    null,
                    null,
                    null,
                    null,
                    definitions.ToArray(),
                    start,
                    exit,
                    protectedCells ?? new GridPos[0]);
            }

            public void DestroyAll()
            {
                Object.Destroy(Root);
                for (int index = 0; index < transientAssets.Count; index++)
                {
                    if (transientAssets[index] != null)
                    {
                        Object.Destroy(transientAssets[index]);
                    }
                }
            }
        }
    }
}

#endif
