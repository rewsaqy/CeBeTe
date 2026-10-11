using Yumiwa.Domain;

namespace Yumiwa.Application
{
    public sealed class CheckpointService : ICheckpointService
    {
        private CheckpointData? savedData;
        private readonly IEventBus bus;

        public CheckpointService(IEventBus bus)
        {
            this.bus = bus;
        }

        public bool HasCheckpoint => savedData.HasValue;

        public void SaveCheckpoint(CheckpointData data)
        {
            savedData = data;
        }

        public CheckpointData LoadCheckpoint()
        {
            return savedData ?? default;
        }

        public void ClearCheckpoint()
        {
            savedData = null;
        }
    }
}