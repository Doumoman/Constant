#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Map;
using StarNight.Tools.Bomb;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class BombReactionProbe : MonoBehaviour, IToolReactionReceiver
    {
        public int Calls { get; private set; }
        public ToolReactionContext LastContext { get; private set; }

        public ToolReactionResult TryReact(ToolReactionContext context)
        {
            Calls++;
            LastContext = context;
            return new ToolReactionResult
            {
                Accepted = true,
                ChangedState = true,
                ConsumeToolResource = false,
                Feedback = FeedbackId.Accepted,
            };
        }
    }

    public sealed class BombRuntimeTests
    {
        private BombDefinition definition;

        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<BombDefinition>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
            foreach (BombRuntime bomb in Object.FindObjectsByType<BombRuntime>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(bomb.gameObject);
            }
            foreach (BombReactionProbe probe in Object.FindObjectsByType<BombReactionProbe>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(probe.gameObject);
            }
            foreach (BombResidualSimulation residual in Object.FindObjectsByType<BombResidualSimulation>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(residual.gameObject);
            }
            foreach (PlayerHandSlot slot in Object.FindObjectsByType<PlayerHandSlot>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(slot.gameObject);
            }
        }

        [Test]
        public void ApprovedLaunchesUseExactDirectionValues()
        {
            BombLaunchSolution placed = definition.ResolveLaunch(
                new PlayerActionContext(1, 0f, -1f, true), 1);
            BombLaunchSolution upward = definition.ResolveLaunch(
                new PlayerActionContext(2, 0f, 1f, false), -1);
            BombLaunchSolution left = definition.ResolveLaunch(
                new PlayerActionContext(3, -1f, 0f, false), 1);
            BombLaunchSolution facing = definition.ResolveLaunch(
                new PlayerActionContext(4, 0f, 0f, false), 1);

            Assert.That(placed.Kind, Is.EqualTo(BombLaunchKind.Place));
            Assert.That(placed.Velocity, Is.EqualTo(Vector2.zero));
            Assert.That(upward.Velocity, Is.EqualTo(new Vector2(-1.5f, 6.5f)));
            Assert.That(left.Velocity, Is.EqualTo(new Vector2(-5.2f, 1.8f)));
            Assert.That(facing.Velocity, Is.EqualTo(new Vector2(5.2f, 1.8f)));
        }

        [Test]
        public void OneHundredBombsArmAndExplodeWithExactlyNineCells()
        {
            for (int index = 0; index < 100; index++)
            {
                BombRuntime bomb = CreateBomb(Vector2.zero, true);
                Assert.That(bomb.Arm(definition, null, Vector2.zero, index + 1), Is.True);
                BombExplosionReport report = bomb.ExplodeNow();
                Assert.That(report.Cells, Is.EqualTo(9));
                Assert.That(bomb.IsExploded, Is.True);
                Object.DestroyImmediate(bomb.gameObject);
            }
        }

        [Test]
        public void ExplosionTouchesOnlyThreeByThreeAndUsesBombHeavyImpact()
        {
            BombRuntime source = CreateBomb(Vector2.zero, true);
            BombReactionProbe inside = CreateReactionProbe(new Vector2(1f, 0f));
            BombReactionProbe outside = CreateReactionProbe(new Vector2(2f, 0f));
            Physics2D.SyncTransforms();

            source.Arm(definition, null, Vector2.zero, 10);
            BombExplosionReport report = source.ExplodeNow();

            Assert.That(report.Cells, Is.EqualTo(9));
            Assert.That(inside.Calls, Is.EqualTo(1));
            Assert.That(inside.LastContext.Tags, Is.EqualTo(ToolTag.Bomb | ToolTag.HeavyImpact));
            Assert.That(outside.Calls, Is.Zero);
        }

        [Test]
        public void ExplosionNeverDispatchesToOuterBoundaryOrPortalSupport()
        {
            Assert.That(LayerMask.NameToLayer("UnbreakableBoundary"), Is.GreaterThanOrEqualTo(0));
            Assert.That(LayerMask.NameToLayer("PortalBoundary"), Is.GreaterThanOrEqualTo(0));
            BombRuntime source = CreateBomb(Vector2.zero, true);
            BombReactionProbe outerBoundary = CreateReactionProbe(
                new Vector2(1f, 0f),
                "UnbreakableBoundary");
            BombReactionProbe portalSupport = CreateReactionProbe(
                new Vector2(0f, 1f),
                "PortalBoundary");
            Physics2D.SyncTransforms();

            source.Arm(definition, null, Vector2.zero, 15);
            BombExplosionReport report = source.ExplodeNow();

            Assert.That(report.Cells, Is.EqualTo(9));
            Assert.That(report.Reactions, Is.Zero);
            Assert.That(outerBoundary.Calls, Is.Zero);
            Assert.That(portalSupport.Calls, Is.Zero);
        }

        [Test]
        public void ExplosionReducesOtherBombFuseToPointOneFive()
        {
            BombRuntime source = CreateBomb(Vector2.zero, true);
            BombRuntime chained = CreateBomb(Vector2.right, false);
            source.Arm(definition, null, Vector2.zero, 20);
            chained.Arm(definition, null, Vector2.zero, 21);
            Physics2D.SyncTransforms();

            BombExplosionReport report = source.ExplodeNow();

            Assert.That(report.ChainedBombs, Is.EqualTo(1));
            Assert.That(chained.RemainingFuse, Is.EqualTo(0.15f).Within(0.0001f));
        }

        [Test]
        public void HeldBombStillExplodesAndClearsHandSlot()
        {
            var player = new GameObject("PlayerHandSlotTest");
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            PlayerHandSlot slot = player.AddComponent<PlayerHandSlot>();
            var socket = new GameObject("CarrySocket");
            socket.transform.SetParent(player.transform);
            presenter.ConfigureForTests(socket.transform);
            slot.ConfigureForTests(presenter);
            BombRuntime bomb = CreateBomb(Vector2.zero, false);
            bomb.Arm(definition, player, Vector2.zero, 30);

            Assert.That(slot.TryAttach(bomb), Is.True);
            bomb.TickFuse(definition.FuseSeconds);

            Assert.That(bomb.IsExploded, Is.True);
            Assert.That(slot.IsEmpty, Is.True);
        }

        [Test]
        public void ResidualSimulationTicksFuseThenFreezesSnapshot()
        {
            BombRuntime bomb = CreateBomb(Vector2.zero, false);
            bomb.Arm(definition, null, Vector2.zero, 40);
            var residualObject = new GameObject("BombResidualSimulationTest");
            BombResidualSimulation residual = residualObject.AddComponent<BombResidualSimulation>();
            int snapshots = 0;
            residual.SnapshotReady += values => snapshots = values.Count;

            residual.Begin(new[] { bomb });
            residual.TickResidual(definition.FuseSeconds);

            Assert.That(bomb.IsExploded, Is.True);
            Assert.That(residual.State, Is.EqualTo(ResidualSimulationState.Frozen));
            Assert.That(snapshots, Is.EqualTo(1));
        }

        private BombRuntime CreateBomb(Vector2 position, bool withDispatcher)
        {
            var gameObject = new GameObject("BombTest");
            gameObject.transform.position = position;
            int layer = LayerMask.NameToLayer("DynamicObject");
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
            Rigidbody2D body = gameObject.AddComponent<Rigidbody2D>();
            gameObject.AddComponent<CircleCollider2D>().radius = 0.2f;
            BombExplosionDispatcher dispatcher = withDispatcher
                ? gameObject.AddComponent<BombExplosionDispatcher>()
                : null;
            BombRuntime bomb = gameObject.AddComponent<BombRuntime>();
            bomb.ConfigureForTests(definition, body, dispatcher);
            return bomb;
        }

        private static BombReactionProbe CreateReactionProbe(
            Vector2 position,
            string layerName = "DynamicObject")
        {
            var gameObject = new GameObject("BombReactionProbe");
            gameObject.transform.position = position;
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                gameObject.layer = layer;
            }
            gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.4f;
            return gameObject.AddComponent<BombReactionProbe>();
        }
    }
}

#endif
