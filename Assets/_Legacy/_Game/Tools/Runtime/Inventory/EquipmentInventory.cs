#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Linq;
using StarNight.Core.Inventory;
using StarNight.Interaction.HandSlot;
using StarNight.Tools.Core;
using StarNight.Tools.Items;
using UnityEngine;

namespace StarNight.Tools.Inventory
{
    [DisallowMultipleComponent]
    public sealed class EquipmentInventory : MonoBehaviour, IEquipmentInventoryBridge
    {
        public const float SelectionCooldownSeconds = 0.12f;

        [SerializeField] private PlayerHandSlot handSlot;
        [SerializeField] private Transform stowedEquipmentRoot;
        [SerializeField] private List<InventoryEntry> entries = new List<InventoryEntry>();

        private readonly List<EquipmentInventoryHudEntry> hudEntries = new List<EquipmentInventoryHudEntry>();
        private float nextSelectionAllowedAt;
        private bool suppressHandSlotCallback;

        public event Action InventoryChanged;
        public event Action<InventoryEntry> SelectionChanged;
        public event Action<InventoryEntry> DuplicateRepaired;
        public event Action<InventoryEntry, DurableEquipmentRecoveryResult> DurabilityRestored;

        public IReadOnlyList<InventoryEntry> Entries => entries;
        public IReadOnlyList<EquipmentInventoryHudEntry> HudEntries => hudEntries;
        public InventoryEntry SelectedEntry => entries.FirstOrDefault(entry => entry.IsSelected);
        public HandSlotItemRuntime SelectedRuntime => SelectedEntry?.Runtime;
        public DurableEquipmentRecoveryResult LastDurabilityRestore { get; private set; }
        public string LatestFeedbackMessage => LastDurabilityRestore.Message ?? string.Empty;
        public int FeedbackRevision { get; private set; }

        private void Awake()
        {
            ResolveDependencies();
            RebuildHudEntries();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            if (handSlot != null)
            {
                handSlot.ItemChanged += HandleHandSlotChanged;
            }
            TryRestoreSelected();
        }

        private void OnDisable()
        {
            if (handSlot != null)
            {
                handSlot.ItemChanged -= HandleHandSlotChanged;
            }
        }

        private void LateUpdate()
        {
            bool changed = false;
            for (int index = 0; index < entries.Count; index++)
            {
                int previous = entries[index].CurrentDurability;
                ItemDurabilityService.Synchronize(entries[index]);
                changed |= previous != entries[index].CurrentDurability;
            }
            if (changed)
            {
                PublishInventoryChanged();
            }
        }

        public bool IsInventoryItem(HandSlotItemRuntime item)
        {
            return item != null && entries.Any(entry => entry.Runtime == item);
        }

        public bool TryPickupEquipment(HandSlotItemRuntime item)
        {
            return item is HandToolRuntime runtime &&
                   ItemPickupResolver.Resolve(this, runtime) != EquipmentPickupResult.Rejected;
        }

        public EquipmentPickupResult ResolvePickup(HandToolRuntime runtime)
        {
            if (runtime == null || runtime.Definition == null)
            {
                return EquipmentPickupResult.Rejected;
            }

            InventoryEntry duplicate = entries.FirstOrDefault(entry =>
                entry.ItemId == runtime.Definition.ItemId ||
                string.Equals(entry.StableItemId, runtime.StableItemId, StringComparison.Ordinal));
            if (duplicate != null)
            {
                if (!runtime.Definition.AllowDuplicate)
                {
                    return EquipmentPickupResult.Rejected;
                }

                DurableEquipmentRecoveryResult recovery =
                    ItemDurabilityService.ApplyDuplicatePickup(duplicate, duplicate.Runtime);
                if (!recovery.Succeeded)
                {
                    return EquipmentPickupResult.Rejected;
                }

                DestroyPickupRuntime(runtime);
                LastDurabilityRestore = recovery;
                FeedbackRevision++;
                PublishInventoryChanged();
                DuplicateRepaired?.Invoke(duplicate);
                DurabilityRestored?.Invoke(duplicate, recovery);
                return EquipmentPickupResult.DuplicateRepaired;
            }

            var entry = new InventoryEntry
            {
                ItemId = runtime.Definition.ItemId,
                MaxDurability = runtime.Definition.MaxDurability,
                CurrentDurability = runtime.CurrentResource,
                SelectionOrder = runtime.Definition.SelectionOrder,
                Runtime = runtime,
            };
            entry.IsSelected = entries.All(candidate => !candidate.IsSelected)
                && runtime.Definition.TabSelectable
                && !entry.IsBroken;
            entries.Add(entry);
            entries.Sort((left, right) => left.SelectionOrder.CompareTo(right.SelectionOrder));
            runtime.StowForInventory(stowedEquipmentRoot);
            TryRestoreSelected();
            PublishInventoryChanged();
            if (entry.IsSelected)
            {
                SelectionChanged?.Invoke(entry);
            }
            return EquipmentPickupResult.Added;
        }

        public bool TryStowSelected()
        {
            InventoryEntry selected = SelectedEntry;
            if (selected?.Runtime == null)
            {
                return true;
            }

            if (handSlot != null && handSlot.CurrentItem == selected.Runtime)
            {
                suppressHandSlotCallback = true;
                bool released = handSlot.TryReleaseCurrent(selected.Runtime);
                suppressHandSlotCallback = false;
                if (!released)
                {
                    return false;
                }
            }
            selected.Runtime.StowForInventory(stowedEquipmentRoot);
            return true;
        }

        public bool TryDropSelected(Vector2 worldPosition)
        {
            InventoryEntry selected = SelectedEntry;
            if (selected?.Runtime == null || selected.Runtime.Definition == null || !selected.Runtime.Definition.CanDrop)
            {
                return false;
            }

            suppressHandSlotCallback = true;
            if (handSlot != null && handSlot.CurrentItem == selected.Runtime)
            {
                handSlot.TryReleaseCurrent(selected.Runtime);
            }
            suppressHandSlotCallback = false;
            entries.Remove(selected);
            selected.Runtime.gameObject.SetActive(true);
            selected.Runtime.ExitHandSlot(worldPosition, true);
            SelectFallbackAfterRemoval();
            TryRestoreSelected();
            PublishInventoryChanged();
            return true;
        }

        public bool TrySelectNext(float now) => TrySelect(1, now);
        public bool TrySelectPrevious(float now) => TrySelect(-1, now);

        public void TryRestoreSelected()
        {
            InventoryEntry selected = SelectedEntry;
            if (selected?.Runtime == null || handSlot == null || !handSlot.IsEmpty)
            {
                return;
            }

            selected.Runtime.gameObject.SetActive(true);
            if (!handSlot.TryAttach(selected.Runtime))
            {
                selected.Runtime.StowForInventory(stowedEquipmentRoot);
            }
        }

        public void ConfigureForTests(PlayerHandSlot configuredHandSlot, Transform configuredStowedRoot = null)
        {
            if (handSlot != null)
            {
                handSlot.ItemChanged -= HandleHandSlotChanged;
            }
            handSlot = configuredHandSlot;
            stowedEquipmentRoot = configuredStowedRoot;
            ResolveDependencies();
            if (isActiveAndEnabled && handSlot != null)
            {
                handSlot.ItemChanged -= HandleHandSlotChanged;
                handSlot.ItemChanged += HandleHandSlotChanged;
            }
        }

        private bool TrySelect(int direction, float now)
        {
            List<InventoryEntry> selectable = entries
                .Where(entry => entry.Runtime != null
                    && entry.Runtime.Definition.TabSelectable
                    && !entry.IsBroken)
                .OrderBy(entry => entry.SelectionOrder)
                .ToList();
            if (selectable.Count == 0 || now < nextSelectionAllowedAt)
            {
                return false;
            }

            InventoryEntry current = SelectedEntry;
            int currentIndex = selectable.IndexOf(current);
            int nextIndex;
            if (currentIndex < 0)
            {
                nextIndex = direction >= 0 ? 0 : selectable.Count - 1;
            }
            else
            {
                if (selectable.Count <= 1)
                {
                    return false;
                }
                nextIndex = (currentIndex + direction + selectable.Count) % selectable.Count;
            }
            if (!TryStowSelected())
            {
                return false;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                entries[index].IsSelected = false;
            }
            selectable[nextIndex].IsSelected = true;
            nextSelectionAllowedAt = now + SelectionCooldownSeconds;
            TryRestoreSelected();
            PublishInventoryChanged();
            SelectionChanged?.Invoke(selectable[nextIndex]);
            return true;
        }

        private void HandleHandSlotChanged(HandSlotItemRuntime previous, HandSlotItemRuntime current)
        {
            if (suppressHandSlotCallback)
            {
                return;
            }

            if (current == null)
            {
                TryRestoreSelected();
            }
        }

        private void ResolveDependencies()
        {
            handSlot ??= GetComponent<PlayerHandSlot>();
            if (stowedEquipmentRoot == null)
            {
                Transform existing = transform.Find("StowedEquipmentRoot");
                if (existing != null)
                {
                    stowedEquipmentRoot = existing;
                }
                else
                {
                    var root = new GameObject("StowedEquipmentRoot");
                    root.transform.SetParent(transform, false);
                    stowedEquipmentRoot = root.transform;
                }
            }
        }

        private void SelectFallbackAfterRemoval()
        {
            InventoryEntry fallback = entries
                .Where(entry => entry.Runtime != null
                    && entry.Runtime.Definition.TabSelectable
                    && !entry.IsBroken)
                .OrderBy(entry => entry.SelectionOrder)
                .FirstOrDefault();
            if (fallback != null)
            {
                fallback.IsSelected = true;
                SelectionChanged?.Invoke(fallback);
            }
        }

        private void PublishInventoryChanged()
        {
            RebuildHudEntries();
            InventoryChanged?.Invoke();
        }

        private void RebuildHudEntries()
        {
            hudEntries.Clear();
            for (int index = 0; index < entries.Count; index++)
            {
                InventoryEntry entry = entries[index];
                HandToolRuntime runtime = entry.Runtime;
                if (runtime == null)
                {
                    continue;
                }
                hudEntries.Add(new EquipmentInventoryHudEntry(
                    runtime.StableItemId,
                    runtime.DisplayName,
                    runtime.HudIcon,
                    entry.CurrentDurability,
                    entry.MaxDurability,
                    entry.IsSelected,
                    ResolveUseKind(runtime.Definition.UseCategory)));
            }
        }

        private static EquipmentInventoryUseKind ResolveUseKind(ItemUseCategory category)
        {
            return category switch
            {
                ItemUseCategory.JumpModifier => EquipmentInventoryUseKind.Jump,
                ItemUseCategory.PassiveDetector => EquipmentInventoryUseKind.Primary,
                _ => EquipmentInventoryUseKind.Primary,
            };
        }

        private static void DestroyPickupRuntime(HandToolRuntime runtime)
        {
            if (runtime == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(runtime.gameObject);
            }
            else
            {
                DestroyImmediate(runtime.gameObject);
            }
        }
    }
}

#endif
