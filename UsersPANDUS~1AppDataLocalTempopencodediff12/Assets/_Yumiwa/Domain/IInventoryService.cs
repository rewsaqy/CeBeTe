namespace Yumiwa.Domain
{
    /// <summary>
    /// Inventory boundary for the inspection system. The inspection code never
    /// touches a concrete inventory; any capacity rules live behind this contract.
    /// </summary>
    public interface IInventoryService
    {
        bool CanAccept(ItemDefinition item, out string rejectReason);
        bool TryAdd(ItemDefinition item, out string rejectReason);
        int GetCount(string itemId);
    }
}
