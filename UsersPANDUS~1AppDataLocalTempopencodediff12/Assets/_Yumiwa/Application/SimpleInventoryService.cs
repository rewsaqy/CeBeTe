using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Minimal bounded inventory behind IInventoryService: slot-count capacity plus
    /// a per-item stack cap. Swap for a save-backed implementation without touching
    /// the inspection system.
    /// </summary>
    public sealed class SimpleInventoryService : IInventoryService
    {
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>();
        private readonly int maxSlots;
        private readonly int maxStackPerSlot;

        public SimpleInventoryService(int maxSlots = 16, int maxStackPerSlot = 99)
        {
            this.maxSlots = maxSlots < 1 ? 1 : maxSlots;
            this.maxStackPerSlot = maxStackPerSlot < 1 ? 1 : maxStackPerSlot;
        }

        public bool CanAccept(ItemDefinition item, out string rejectReason) =>
            Check(item, out rejectReason);

        public bool TryAdd(ItemDefinition item, out string rejectReason)
        {
            if (!Check(item, out rejectReason))
                return false;
            if (counts.TryGetValue(item.ItemId, out int held))
                counts[item.ItemId] = held + item.Quantity;
            else
                counts[item.ItemId] = item.Quantity;
            rejectReason = null;
            return true;
        }

        public int GetCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;
            return counts.TryGetValue(itemId, out int held) ? held : 0;
        }

        private bool Check(ItemDefinition item, out string rejectReason)
        {
            if (!ItemDefinition.IsValid(item))
            {
                rejectReason = "Invalid item.";
                return false;
            }
            int held = GetCount(item.ItemId);
            if (held + item.Quantity > maxStackPerSlot)
            {
                rejectReason = "Cannot carry more of this item.";
                return false;
            }
            if (held == 0 && counts.Count >= maxSlots)
            {
                rejectReason = "Inventory full.";
                return false;
            }
            rejectReason = null;
            return true;
        }
    }
}
