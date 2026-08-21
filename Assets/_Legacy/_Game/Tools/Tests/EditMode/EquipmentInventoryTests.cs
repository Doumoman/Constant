#if LEGACY_DISABLED
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using StarNight.Core.Inventory;
using StarNight.Interaction.HandSlot;
using StarNight.Map;
using StarNight.Tools.Core;
using StarNight.Tools.Inventory;
using StarNight.Tools.Items;
using StarNight.Tools.Pickaxe;
using UnityEngine;

namespace StarNight.Tools.Tests
{
    public sealed class EquipmentInventoryTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    Object.DestroyImmediate(created[index]);
                }
            }
            created.Clear();
        }

        [Test]
        public void DurableEquipmentContractHasExactFieldsAndNoQuantity()
        {
            string[] fields = typeof(DurableEquipmentEntry)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Select(field => field.Name)
                .OrderBy(name => name)
                .ToArray();

            CollectionAssert.AreEqual(
                new[] { "CurrentDurability", "IsBroken", "ItemId", "MaxDurability", "SelectionOrder" },
                fields);
        }

        [Test]
        public void DuplicatePickupFullyRestoresSingleEntryAndPublishesFeedback()
        {
            Rig rig = CreateRig();
            HandToolDefinition definition = CreateDefinition("TOOL_PICKAXE", 201, 8);
            PickaxeRuntime retained = CreateRuntime("Retained", definition);
            PickaxeRuntime duplicate = CreateRuntime("Duplicate", definition);

            Assert.That(rig.Inventory.ResolvePickup(retained), Is.EqualTo(EquipmentPickupResult.Added));
            retained.ResourceState.ConfigureForTests(ToolResourceMode.Durability, 8, 0);
            ItemDurabilityService.Synchronize(rig.Inventory.Entries[0]);
            int selectionOrder = rig.Inventory.Entries[0].SelectionOrder;
            DurableEquipmentRecoveryResult observed = default;
            rig.Inventory.DurabilityRestored += (_, result) => observed = result;

            Assert.That(rig.Inventory.ResolvePickup(duplicate), Is.EqualTo(EquipmentPickupResult.DuplicateRepaired));
            Assert.That(rig.Inventory.Entries, Has.Count.EqualTo(1));
            Assert.That(rig.Inventory.Entries[0].CurrentDurability, Is.EqualTo(8));
            Assert.That(rig.Inventory.Entries[0].IsBroken, Is.False);
            Assert.That(rig.Inventory.Entries[0].SelectionOrder, Is.EqualTo(selectionOrder));
            Assert.That(retained.CurrentResource, Is.EqualTo(8));
            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(retained));
            Assert.That(observed.Succeeded, Is.True);
            Assert.That(observed.WasBroken, Is.True);
            Assert.That(observed.PreviousDurability, Is.Zero);
            Assert.That(observed.CurrentDurability, Is.EqualTo(observed.MaxDurability));
            Assert.That(observed.Message, Is.EqualTo("내구도 완전 회복"));
            Assert.That(rig.Inventory.LatestFeedbackMessage, Is.EqualTo("내구도 완전 회복"));
            Assert.That(rig.Inventory.FeedbackRevision, Is.EqualTo(1));
        }

        [Test]
        public void ZeroDurabilityMarksEntryBrokenWithoutDeletingOrAutoSwap()
        {
            Rig rig = CreateRig();
            HandToolDefinition definition = CreateDefinition("TOOL_PICKAXE", 201, 8);
            PickaxeRuntime retained = CreateRuntime("Retained", definition);
            rig.Inventory.ResolvePickup(retained);

            retained.ResourceState.ConfigureForTests(ToolResourceMode.Durability, 8, 0);
            ItemDurabilityService.Synchronize(rig.Inventory.Entries[0]);

            Assert.That(rig.Inventory.Entries[0].CurrentDurability, Is.Zero);
            Assert.That(rig.Inventory.Entries[0].IsBroken, Is.True);
            Assert.That(rig.Inventory.Entries, Has.Count.EqualTo(1));
            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(retained));
        }

        [Test]
        public void TabSelectionSkipsBrokenEquipmentInBothDirections()
        {
            Rig rig = CreateRig();
            PickaxeRuntime first = CreateRuntime("First", CreateDefinition("CUSTOM_FIRST", 901, 5));
            PickaxeRuntime broken = CreateRuntime("Broken", CreateDefinition("CUSTOM_BROKEN", 902, 7));
            PickaxeRuntime third = CreateRuntime("Third", CreateDefinition("CUSTOM_THIRD", 903, 9));
            rig.Inventory.ResolvePickup(first);
            rig.Inventory.ResolvePickup(broken);
            rig.Inventory.ResolvePickup(third);
            broken.ResourceState.ConfigureForTests(ToolResourceMode.Durability, 7, 0);
            ItemDurabilityService.Synchronize(rig.Inventory.Entries[1]);

            Assert.That(rig.Inventory.TrySelectNext(1f), Is.True);
            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(third));
            Assert.That(rig.Inventory.TrySelectPrevious(1.2f), Is.True);
            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(first));
        }

        [Test]
        public void SelectionCyclesAndStowedSelectionRestoresToHandSlot()
        {
            Rig rig = CreateRig();
            PickaxeRuntime first = CreateRuntime("First", CreateDefinition("CUSTOM_FIRST", 901, 5));
            PickaxeRuntime second = CreateRuntime("Second", CreateDefinition("CUSTOM_SECOND", 902, 7));
            rig.Inventory.ResolvePickup(first);
            rig.Inventory.ResolvePickup(second);

            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(first));
            Assert.That(rig.Inventory.TrySelectNext(1f), Is.True);
            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(second));
            Assert.That(rig.HandSlot.CurrentItem, Is.SameAs(second));
            Assert.That(rig.Inventory.TrySelectPrevious(1.2f), Is.True);
            Assert.That(rig.Inventory.SelectedRuntime, Is.SameAs(first));

            Assert.That(rig.Inventory.TryStowSelected(), Is.True);
            Assert.That(rig.HandSlot.IsEmpty, Is.True);
            rig.Inventory.TryRestoreSelected();
            Assert.That(rig.HandSlot.CurrentItem, Is.SameAs(first));
        }

        private Rig CreateRig()
        {
            GameObject root = Track(new GameObject("InventoryRig"));
            HandSlotPresenter presenter = root.AddComponent<HandSlotPresenter>();
            presenter.ConfigureForTests(root.transform);
            PlayerHandSlot slot = root.AddComponent<PlayerHandSlot>();
            slot.ConfigureForTests(presenter);
            EquipmentInventory inventory = root.AddComponent<EquipmentInventory>();
            inventory.ConfigureForTests(slot);
            return new Rig(slot, inventory);
        }

        private HandToolDefinition CreateDefinition(string toolId, int itemId, int durability)
        {
            HandToolDefinition definition = Track(ScriptableObject.CreateInstance<HandToolDefinition>());
            definition.Configure(
                toolId,
                toolId,
                ToolTag.Pickaxe,
                ToolResourceMode.Durability,
                durability,
                100,
                new ToolActionProfile(),
                new ToolActionProfile(),
                new[] { Vector2Int.right });
            definition.ConfigureItemContract(itemId, ItemUseCategory.ActiveTool, durability, true, true, true, itemId);
            return definition;
        }

        private PickaxeRuntime CreateRuntime(string name, HandToolDefinition definition)
        {
            GameObject toolObject = Track(new GameObject(name));
            PickaxeRuntime runtime = toolObject.AddComponent<PickaxeRuntime>();
            runtime.Configure(definition);
            return runtime;
        }

        private T Track<T>(T value) where T : Object
        {
            created.Add(value);
            return value;
        }

        private readonly struct Rig
        {
            public Rig(PlayerHandSlot handSlot, EquipmentInventory inventory)
            {
                HandSlot = handSlot;
                Inventory = inventory;
            }

            public PlayerHandSlot HandSlot { get; }
            public EquipmentInventory Inventory { get; }
        }
    }
}

#endif
