#if LEGACY_DISABLED
using NUnit.Framework;
using StarNight.Core.Tools;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Tools.Core;
using StarNight.Tools.Inventory;
using StarNight.Tools.Items;
using StarNight.Tools.Spring;
using UnityEditor;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class SpringJumpExecutorProbe : MonoBehaviour, IPlayerSpecialJumpExecutor
    {
        public bool Accept { get; set; }
        public float LastVelocity { get; private set; }
        public float LastClearance { get; private set; }

        public bool TryLaunchSpecialJump(float verticalVelocity, float requiredHeadClearance)
        {
            LastVelocity = verticalVelocity;
            LastClearance = requiredHeadClearance;
            return Accept;
        }
    }

    public sealed class SpringEquipmentTests
    {
        private GameObject player;
        private GameObject springObject;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(springObject);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void SelectedSpaceJumpConsumesDurabilityOnlyAfterVelocityIsApplied()
        {
            HandToolDefinition definition = AssetDatabase.LoadAssetAtPath<HandToolDefinition>(
                "Assets/_Game/Tools/Data/Equipment/EQUIPMENT_SPRING.asset");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Tools/Prefabs/Equipment/SpringShoes.prefab");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.ItemId, Is.EqualTo(301));
            Assert.That(definition.UseCategory, Is.EqualTo(ItemUseCategory.JumpModifier));
            Assert.That(definition.MaxDurability, Is.EqualTo(8));
            Assert.That(prefab?.GetComponent<SpringJumpRuntime>(), Is.Not.Null);

            player = new GameObject("SpringPlayerRig");
            HandSlotPresenter presenter = player.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(player.transform);
            PlayerHandSlot slot = player.AddComponent<PlayerHandSlot>();
            slot.ConfigureForTests(presenter);
            SpringJumpExecutorProbe probe = player.AddComponent<SpringJumpExecutorProbe>();
            PlayerActionLock actionLock = player.AddComponent<PlayerActionLock>();
            EquipmentInventory inventory = player.AddComponent<EquipmentInventory>();
            inventory.ConfigureForTests(slot);

            springObject = new GameObject("SpringRuntime");
            SpringJumpRuntime spring = springObject.AddComponent<SpringJumpRuntime>();
            spring.Configure(definition);
            Assert.That(inventory.ResolvePickup(spring), Is.EqualTo(EquipmentPickupResult.Added));
            PlayerActionRouter router = player.AddComponent<PlayerActionRouter>();

            Assert.That(
                spring.TryPrimaryUse(slot, new PlayerActionContext(1, 0f, 0f, false), 1, 0),
                Is.False,
                "X primary use must not trigger spring equipment.");

            probe.Accept = false;
            Assert.That(router.TryExecuteSelectedJumpModifier(), Is.False);
            Assert.That(spring.CurrentResource, Is.EqualTo(8));

            actionLock.SetState(PlayerActionState.DialogueLocked);
            probe.Accept = true;
            Assert.That(router.TryExecuteSelectedJumpModifier(), Is.False);
            Assert.That(spring.CurrentResource, Is.EqualTo(8));
            actionLock.ResetToFree();

            Assert.That(router.TryExecuteSelectedJumpModifier(), Is.True);
            Assert.That(spring.CurrentResource, Is.EqualTo(7));
            Assert.That(probe.LastVelocity, Is.EqualTo(SpringJumpRuntime.SpringJumpVelocity));
            Assert.That(probe.LastClearance, Is.EqualTo(SpringJumpRuntime.RequiredHeadClearance));
            Assert.That(SpringJumpRuntime.SpringJumpVelocity, Is.EqualTo(11.25f));
            Assert.That(SpringEquipmentContract.GuaranteedJumpHeightCells, Is.EqualTo(2f));
            Assert.That(SpringEquipmentContract.SafeHorizontalGapCells, Is.EqualTo(4f));

            spring.ResourceState.ConfigureForTests(ToolResourceMode.Durability, 8, 0);
            Assert.That(router.TryExecuteSelectedJumpModifier(), Is.False);
            Assert.That(spring.CurrentResource, Is.Zero);
        }
    }
}

#endif
