using System.Collections.Generic;
using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Physical inventory per GDD §03:
    /// Left Hand (1) + Right Hand (1) + Pocket (4) = 6 total items max.
    /// Only hand items can be used directly; pocket items need transfer first.
    /// </summary>
    public sealed class PhysicalInventoryService : IInventoryService
    {
        private readonly InventorySlot[] slots = new InventorySlot[6];

        public PhysicalInventoryService()
        {
            slots[0] = new InventorySlot(InventorySlotType.HandLeft, default);
            slots[1] = new InventorySlot(InventorySlotType.HandRight, default);
            slots[2] = new InventorySlot(InventorySlotType.Pocket0, default);
            slots[3] = new InventorySlot(InventorySlotType.Pocket1, default);
            slots[4] = new InventorySlot(InventorySlotType.Pocket2, default);
            slots[5] = new InventorySlot(InventorySlotType.Pocket3, default);
        }

        public bool CanAccept(ItemDefinition item, out string rejectReason)
        {
            if (!ItemDefinition.IsValid(item))
            {
                rejectReason = "Invalid item.";
                return false;
            }
            if (IsFull())
            {
                rejectReason = "Inventory full (6 slots max: 2 hands + 4 pockets).";
                return false;
            }
            rejectReason = null;
            return true;
        }

        public bool TryAdd(ItemDefinition item, out string rejectReason)
        {
            if (!CanAccept(item, out rejectReason))
                return false;

            int pocketIdx = FindFirstEmptyPocket();
            if (pocketIdx >= 0)
            {
                slots[pocketIdx] = new InventorySlot((InventorySlotType)pocketIdx, item);
                rejectReason = null;
                return true;
            }

            rejectReason = "No pocket slot available.";
            return false;
        }

        public int GetCount(string itemId)
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    count += slots[i].Item.Quantity;
            }
            return count;
        }

        public bool IsHand(ItemDefinition item)
        {
            for (int i = 0; i < 2; i++)
            {
                if (slots[i].IsOccupied && slots[i].Item.ItemId == item.ItemId)
                    return true;
            }
            return false;
        }

        public bool IsInPocket(ItemDefinition item)
        {
            for (int i = 2; i < slots.Length; i++)
            {
                if (slots[i].IsOccupied && slots[i].Item.ItemId == item.ItemId)
                    return true;
            }
            return false;
        }

        public bool CanUse(ItemDefinition item) => IsHand(item);

        public bool TransferToHand(ItemDefinition item, HandType hand, out string rejectReason)
        {
            rejectReason = null;
            int sourceIdx = FindItemInPocket(item.ItemId);
            if (sourceIdx < 0)
            {
                rejectReason = "Item not found in pocket.";
                return false;
            }
            int handIdx = hand == HandType.Left ? 0 : 1;
            if (slots[handIdx].IsOccupied)
            {
                rejectReason = $"Hand {hand} is occupied.";
                return false;
            }
            slots[handIdx] = slots[sourceIdx];
            slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, default);
            return true;
        }

        public bool TransferToPocket(ItemDefinition item, out string rejectReason)
        {
            rejectReason = null;
            int sourceIdx = FindItemInHand(item.ItemId);
            if (sourceIdx < 0)
            {
                rejectReason = "Item not found in hand.";
                return false;
            }
            int pocketIdx = FindFirstEmptyPocket();
            if (pocketIdx < 0)
            {
                rejectReason = "No pocket slot available.";
                return false;
            }
            slots[pocketIdx] = slots[sourceIdx];
            slots[sourceIdx] = new InventorySlot((InventorySlotType)sourceIdx, default);
            return true;
        }

        public bool Drop(ItemDefinition item, out string rejectReason)
        {
            rejectReason = null;
            int idx = FindItemIndex(item.ItemId);
            if (idx < 0)
            {
                rejectReason = "Item not found.";
                return false;
            }
            slots[idx] = new InventorySlot((InventorySlotType)idx, default);
            return true;
        }

        public InventorySlot[] GetSlots() => (InventorySlot[])slots.Clone();

        private bool IsFull()
        {
            for (int i = 0; i < slots.Length; i++)
                if (!slots[i].IsOccupied)
                    return false;
            return true;
        }

        private int FindFirstEmptyPocket()
        {
            for (int i = 2; i < slots.Length; i++)
                if (!slots[i].IsOccupied)
                    return i;
            return -1;
        }

        private int FindItemInPocket(string itemId)
        {
            for (int i = 2; i < slots.Length; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return i;
            return -1;
        }

        private int FindItemInHand(string itemId)
        {
            for (int i = 0; i < 2; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return i;
            return -1;
        }

        private int FindItemIndex(string itemId)
        {
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].IsOccupied && slots[i].Item.ItemId == itemId)
                    return i;
            return -1;
        }
    }
}