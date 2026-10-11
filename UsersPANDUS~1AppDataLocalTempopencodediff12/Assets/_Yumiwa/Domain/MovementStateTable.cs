using System.Collections.Generic;

namespace Yumiwa.Domain
{
    /// <summary>O(1) data table for movement-state costs and vital points; extend via data, never switches.</summary>
    public static class MovementStateTable
    {
        private static readonly Dictionary<MovementState, MovementStateData> Table =
            new Dictionary<MovementState, MovementStateData>
            {
                { MovementState.Idle, new MovementStateData(0f, new[] { VitalPoint.Torso, VitalPoint.Chest }) },
                { MovementState.Walk, new MovementStateData(0f, new[] { VitalPoint.Torso, VitalPoint.Chest }) },
                { MovementState.Sprint, new MovementStateData(3f, new[] { VitalPoint.Torso, VitalPoint.Chest }) },
                { MovementState.Crouch, new MovementStateData(1f, new[] { VitalPoint.Torso, VitalPoint.Chest }) },
                { MovementState.Jump, new MovementStateData(5f, new[] { VitalPoint.Neck, VitalPoint.UpperSpine }) },
            };

        public static MovementStateData Get(MovementState state) => Table[state];

        public static bool TryGet(MovementState state, out MovementStateData data) =>
            Table.TryGetValue(state, out data);
    }
}
