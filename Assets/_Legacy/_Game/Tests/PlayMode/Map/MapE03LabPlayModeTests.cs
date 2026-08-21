#if LEGACY_DISABLED
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace StarNight.Map.Tests
{
    public sealed class MapE03LabPlayModeTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.Destroy(createdObjects[index]);
                }
            }

            createdObjects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpikeTriggerAndMovingPlatformSolidProduceRealPhysicsContacts()
        {
            var labRoot = CreateGameObject("MAP-E03_TestLab");
            var activeElement = CreateChild(labRoot.transform, "ActiveAuthoringElement");
            var playerObject = CreateChild(labRoot.transform, "PlayerMapTestRig").gameObject;
            var playerBody = playerObject.AddComponent<Rigidbody2D>();
            playerBody.gravityScale = 0f;
            playerBody.freezeRotation = true;
            var playerCollider = playerObject.AddComponent<BoxCollider2D>();
            playerCollider.size = new Vector2(0.5f, 0.5f);
            var playerRig = playerObject.AddComponent<MapElementLabPlayerRig>();
            var rig = labRoot.AddComponent<MapElementLabTestRig>();

            var spike = CreateSpikeDefinition();
            rig.Configure(spike, activeElement, playerRig);
            playerRig.BeginDemo(new Vector3(0f, 0.05f, 0f), Vector2.zero, false);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(playerRig.CollisionProbe.TriggerCollisionCount, Is.GreaterThan(0),
                "1×1 Spike damage trigger must contact the player in PlayMode.");

            var platform = CreateMovingPlatformDefinition();
            rig.SetDefinition(platform);
            yield return null;
            playerRig.BeginDemo(new Vector3(0.5f, 1.4f, 0f), new Vector2(0f, -4f), false);
            Physics2D.SyncTransforms();
            for (var index = 0; index < 16; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(playerRig.CollisionProbe.SolidCollisionCount, Is.GreaterThan(0),
                "2×1 Moving Platform solid collider must contact the player in PlayMode.");
        }

        private MapElementDefinition CreateSpikeDefinition()
        {
            var definition = CreateDefinition("COMMON_Test_Spike", ElementCategory.Hazard, Vector2Int.one);
            definition.Footprint.HazardCells.Add(Vector2Int.zero);
            definition.Footprint.TriggerCells.Add(Vector2Int.zero);
            definition.CollisionProfile.IsSolid = true;
            definition.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                OffsetCells = new Vector2(0f, -0.38f),
                SizeCells = new Vector2(0.96f, 0.22f),
            });
            definition.CollisionProfile.TriggerShapes.Add(new SerializedColliderShape
            {
                OffsetCells = new Vector2(0f, 0.06f),
                SizeCells = new Vector2(0.82f, 0.72f),
            });
            return definition;
        }

        private MapElementDefinition CreateMovingPlatformDefinition()
        {
            var definition = CreateDefinition(
                "COMMON_Test_MovingPlatform",
                ElementCategory.Platform,
                new Vector2Int(2, 1));
            definition.Footprint.OccupiedCells.Add(Vector2Int.right);
            definition.VisualProfile.RenderMode = ElementVisualRenderMode.TiledSprite;
            definition.VisualProfile.VisualSizeCells = new Vector2(2f, 1f);
            definition.VisualProfile.VisualOffsetCells = new Vector2(0.5f, 0f);
            definition.CollisionProfile.IsSolid = true;
            definition.CollisionProfile.SolidShapes.Add(new SerializedColliderShape
            {
                OffsetCells = new Vector2(0.5f, 0f),
                SizeCells = new Vector2(1.98f, 0.78f),
            });
            definition.BehaviorProfile.Path.Nodes.Add(Vector2.zero);
            definition.BehaviorProfile.Path.Nodes.Add(new Vector2(2f, 0f));
            definition.BehaviorProfile.Path.SpeedCellsPerSecond = 0.01f;
            definition.BehaviorProfile.Path.PingPong = true;
            return definition;
        }

        private MapElementDefinition CreateDefinition(
            string elementId,
            ElementCategory category,
            Vector2Int bounds)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = elementId;
            definition.DisplayName = elementId;
            definition.Category = category;
            definition.Footprint.BoundsSize = bounds;
            definition.Footprint.PivotCell = Vector2Int.zero;
            createdObjects.Add(definition);
            return definition;
        }

        private GameObject CreateGameObject(string objectName)
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static Transform CreateChild(Transform parent, string childName)
        {
            var child = new GameObject(childName).transform;
            child.SetParent(parent, false);
            return child;
        }
    }
}

#endif
