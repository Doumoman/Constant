#if LEGACY_DISABLED
using System.Collections.Generic;
using NUnit.Framework;
using StarNight.Map.Placement;
using UnityEngine;

namespace StarNight.Map.Tests
{
    public sealed class MapE02ContractTests
    {
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var index = createdObjects.Count - 1; index >= 0; index--)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void DefinitionCreatesCompleteRuntimeDataContract()
        {
            var definition = CreateDefinition("COMMON_Test_Element");

            Assert.That(definition.Footprint, Is.Not.Null);
            Assert.That(definition.VisualProfile, Is.Not.Null);
            Assert.That(definition.CollisionProfile, Is.Not.Null);
            Assert.That(definition.BehaviorProfile, Is.Not.Null);
            Assert.That(definition.PlacementProfile, Is.Not.Null);
            Assert.That(definition.BudgetProfile, Is.Not.Null);
            Assert.That(definition.ToolReactions, Is.Not.Null);
            Assert.That(definition.MaruReaction, Is.Not.Null);
            Assert.That(definition.BakeMetadata, Is.Not.Null);
            Assert.That(MapBuildTag.Value, Does.StartWith("StarNight/Core-v2.1/Map-v1.0/"));
        }

        [Test]
        public void StateMachineRunsWarningActiveCooldownSequenceDeterministically()
        {
            var definition = CreateDefinition("COMMON_StateMachine_Test");
            definition.BehaviorProfile.WarningSeconds = 0.25f;
            definition.BehaviorProfile.ActiveSeconds = 0.5f;
            definition.BehaviorProfile.CooldownSeconds = 0.75f;

            var registryObject = CreateGameObject("Registry");
            var registry = registryObject.AddComponent<RoomElementRegistry>();
            var element = CreateElement("Element", definition, registry, "map_e02_edit_state");

            element.SetMapRoomState(MapRoomState.Active);
            Assert.That(element.CurrentState, Is.EqualTo(MapElementState.Idle));

            Assert.That(element.TrySetState(MapElementState.Warning), Is.True);
            element.StateMachine.Tick(0.25f);
            Assert.That(element.CurrentState, Is.EqualTo(MapElementState.Active));

            element.StateMachine.Tick(0.5f);
            Assert.That(element.CurrentState, Is.EqualTo(MapElementState.Cooldown));

            element.StateMachine.Tick(0.75f);
            Assert.That(element.CurrentState, Is.EqualTo(MapElementState.Idle));
        }

        private MapElementDefinition CreateDefinition(string elementId)
        {
            var definition = ScriptableObject.CreateInstance<MapElementDefinition>();
            definition.ElementId = elementId;
            definition.BehaviorProfile.InitialState = MapElementState.Idle;
            createdObjects.Add(definition);
            return definition;
        }

        private MapElementInstance CreateElement(
            string objectName,
            MapElementDefinition definition,
            RoomElementRegistry registry,
            string runtimeId)
        {
            var gameObject = CreateGameObject(objectName);
            gameObject.transform.SetParent(registry.transform, false);
            var occupier = gameObject.AddComponent<GridOccupier>();
            occupier.Configure(Vector2Int.zero, new CellFootprint(), OccupancyLayer.Fixture);
            gameObject.AddComponent<ElementRuntimeId>();
            gameObject.AddComponent<ElementStateMachine>();
            var instance = gameObject.AddComponent<MapElementInstance>();
            instance.Configure(definition, registry, runtimeId);
            return instance;
        }

        private GameObject CreateGameObject(string objectName)
        {
            var gameObject = new GameObject(objectName);
            createdObjects.Add(gameObject);
            return gameObject;
        }
    }
}

#endif
