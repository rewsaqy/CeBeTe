namespace Yumiwa.Domain
{
    /// <summary>Pure item identity snapshot shared by inspection, inventory, and world items.</summary>
    public readonly struct ItemDefinition
    {
        public readonly string ItemId;
        public readonly string DisplayName;
        public readonly string Description;
        public readonly int Quantity;

        public ItemDefinition(string itemId, string displayName, string description, int quantity = 1)
        {
            ItemId = itemId;
            DisplayName = displayName;
            Description = description;
            Quantity = quantity;
        }

        public bool HasIdentity => !string.IsNullOrEmpty(ItemId) && Quantity > 0;

        public static bool IsValid(ItemDefinition item) => item.HasIdentity;
    }
}
