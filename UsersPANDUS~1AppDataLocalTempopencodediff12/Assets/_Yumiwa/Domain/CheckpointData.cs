using UnityEngine;
using Yumiwa.Domain;

namespace Yumiwa.Domain
{
    [System.Serializable]
    public struct CheckpointData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Health;
        public float Stamina;
        public MovementState MovementState;
        public string ActiveScene;
        public long Timestamp;
    }
}