using Yumiwa.Domain;

namespace Yumiwa.Application
{
    /// <summary>
    /// Pure inspection session: one active item at most, idempotent Take, explicit
    /// Cancel. Publishes struct events; never touches UnityEngine or inventory
    /// internals beyond the IInventoryService boundary.
    /// </summary>
    public sealed class InspectionSessionService
    {
        private readonly IInventoryService inventory;
        private readonly IEventBus bus;

        private bool active;
        private ItemDefinition current;
        private bool takeCompleted;

        public InspectionSessionService(IInventoryService inventory, IEventBus bus)
        {
            this.inventory = inventory;
            this.bus = bus;
        }

        public bool IsActive => active;
        public bool TakeCompleted => takeCompleted;
        public ItemDefinition Current => current;

        /// <summary>Opens a session; returns false when busy or the item is invalid.</summary>
        public bool Begin(ItemDefinition item)
        {
            if (active || !ItemDefinition.IsValid(item))
                return false;
            active = true;
            current = item;
            takeCompleted = false;
            bus.Publish(new InspectionStartedEvent(item.ItemId));
            return true;
        }

        /// <summary>
        /// Confirms Take. Adds to inventory at most once; on rejection the session
        /// stays active so the player can keep inspecting.
        /// </summary>
        public InspectionTakeResult ConfirmTake()
        {
            if (!active)
                return InspectionTakeResult.SessionNotActive;
            if (takeCompleted)
                return InspectionTakeResult.AlreadyCompleted;
            if (!inventory.TryAdd(current, out string reason))
            {
                bus.Publish(new InspectionTakeFailedEvent(current.ItemId, reason));
                return InspectionTakeResult.RejectedByInventory;
            }
            takeCompleted = true;
            bus.Publish(new ItemPickedUpEvent(current.ItemId, current.Quantity));
            return InspectionTakeResult.Success;
        }

        /// <summary>Closes after a successful Take; fails when Take never completed.</summary>
        public bool EndAfterTake()
        {
            if (!active || !takeCompleted)
                return false;
            Close(true);
            return true;
        }

        /// <summary>Cancels without touching inventory; the world item stays.</summary>
        public bool Cancel()
        {
            if (!active)
                return false;
            Close(false);
            return true;
        }

        private void Close(bool taken)
        {
            string itemId = current.ItemId;
            active = false;
            current = default;
            takeCompleted = false;
            bus.Publish(new InspectionEndedEvent(itemId, taken));
        }
    }
}
