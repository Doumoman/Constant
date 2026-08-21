#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Player.Motor;
using UnityEngine;

namespace StarNight.Player.Tests
{
    public sealed class PlayerTraversalSolverTests
    {
        private const int TerrainLayer = 7;
        private const int ApprovalAttempts = 20;

        [TearDown]
        public void TearDown()
        {
            foreach (BoxCollider2D collider in Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            {
                if (collider.name.StartsWith("GCORE01_"))
                {
                    Object.DestroyImmediate(collider.gameObject);
                }
            }
        }

        [Test]
        public void T0_OneCellStep_PassTwentyOfTwenty()
        {
            CreateSolid("GCORE01_Takeoff", new Vector2(-2f, -0.5f), new Vector2(4f, 1f));
            CreateSolid("GCORE01_OneCellStep", new Vector2(2f, 0.5f), new Vector2(4f, 1f));

            AssertAttempts(startCenter: new Vector2(-1.2f, 0.461f), minimumLandingX: 0.36f, expected: true);
        }

        [Test]
        public void T0_TwoCellStep_FailTwentyOfTwenty()
        {
            CreateSolid("GCORE01_Takeoff", new Vector2(-2f, -0.5f), new Vector2(4f, 1f));
            CreateSolid("GCORE01_TwoCellStep", new Vector2(2f, 1.5f), new Vector2(4f, 1f));

            AssertAttempts(startCenter: new Vector2(-1.2f, 0.461f), minimumLandingX: 0.36f, expected: false);
        }

        [Test]
        public void T0_ThreeCellGap_PassTwentyOfTwenty()
        {
            CreateSolid("GCORE01_Takeoff", new Vector2(-1f, -0.5f), new Vector2(2f, 1f));
            CreateSolid("GCORE01_ThreeCellLanding", new Vector2(4f, -0.5f), new Vector2(2f, 1f));

            AssertAttempts(startCenter: new Vector2(-0.361f, 0.461f), minimumLandingX: 3.36f, expected: true);
        }

        [Test]
        public void T0_FourCellGap_FailTwentyOfTwenty()
        {
            CreateSolid("GCORE01_Takeoff", new Vector2(-1f, -0.5f), new Vector2(2f, 1f));
            CreateSolid("GCORE01_FourCellLanding", new Vector2(5f, -0.5f), new Vector2(2f, 1f));

            AssertAttempts(startCenter: new Vector2(-0.361f, 0.461f), minimumLandingX: 4.36f, expected: false);
        }

        private static void AssertAttempts(Vector2 startCenter, float minimumLandingX, bool expected)
        {
            Physics2D.SyncTransforms();
            for (int attempt = 0; attempt < ApprovalAttempts; attempt++)
            {
                PlayerTraversalResult result = PlayerTraversalSolver2D.SimulateFullSpeedJump(
                    Physics2D.defaultPhysicsScene,
                    startCenter,
                    1,
                    1 << TerrainLayer);
                bool reachedTarget = result.Landed && result.FinalCenter.x >= minimumLandingX;
                Assert.That(reachedTarget, Is.EqualTo(expected), $"attempt={attempt}, final={result.FinalCenter}");
            }
        }

        private static void CreateSolid(string objectName, Vector2 position, Vector2 size)
        {
            GameObject solid = new GameObject(objectName);
            solid.layer = TerrainLayer;
            solid.transform.position = position;
            BoxCollider2D collider = solid.AddComponent<BoxCollider2D>();
            collider.size = size;
        }
    }
}

#endif
