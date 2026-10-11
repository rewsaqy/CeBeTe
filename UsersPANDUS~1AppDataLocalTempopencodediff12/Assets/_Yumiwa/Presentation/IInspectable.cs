using Yumiwa.Infrastructure;

namespace Yumiwa.Presentation
{
    /// <summary>
    /// Contract for world objects that can enter the inspection system.
    /// Future puzzle hotspots (buttons, locks, hidden compartments) extend this
    /// boundary without changing the base inspection flow.
    /// </summary>
    public interface IInspectable
    {
        ItemData Data { get; }
        bool IsAvailable { get; }
        void Consume();
    }
}
