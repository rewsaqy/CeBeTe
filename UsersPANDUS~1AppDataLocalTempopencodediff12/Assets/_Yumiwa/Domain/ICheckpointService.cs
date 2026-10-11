using Yumiwa.Domain;

namespace Yumiwa.Domain
{
    public interface ICheckpointService
    {
        void SaveCheckpoint(CheckpointData data);
        CheckpointData LoadCheckpoint();
        bool HasCheckpoint { get; }
        void ClearCheckpoint();
    }
}