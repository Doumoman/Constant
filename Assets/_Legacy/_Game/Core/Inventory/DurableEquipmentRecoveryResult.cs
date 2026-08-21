#if LEGACY_DISABLED
namespace StarNight.Core.Inventory
{
    public readonly struct DurableEquipmentRecoveryResult
    {
        public const string FullRestoreMessage = "내구도 완전 회복";

        public DurableEquipmentRecoveryResult(
            int itemId,
            int previousDurability,
            int currentDurability,
            int maxDurability,
            bool wasBroken,
            int selectionOrder)
        {
            Succeeded = true;
            ItemId = itemId;
            PreviousDurability = previousDurability;
            CurrentDurability = currentDurability;
            MaxDurability = maxDurability;
            WasBroken = wasBroken;
            SelectionOrder = selectionOrder;
            Message = FullRestoreMessage;
        }

        public bool Succeeded { get; }
        public int ItemId { get; }
        public int PreviousDurability { get; }
        public int CurrentDurability { get; }
        public int MaxDurability { get; }
        public bool WasBroken { get; }
        public int SelectionOrder { get; }
        public string Message { get; }
    }
}

#endif
