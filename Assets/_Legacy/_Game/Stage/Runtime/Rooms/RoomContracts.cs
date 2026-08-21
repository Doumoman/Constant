#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Stage.Rooms
{
    public enum RoomDimension
    {
        Main,
        Secret,
    }

    public enum RoomSimulationState
    {
        Dormant,
        NeighborPreview,
        TransitionTarget,
        Active,
        ResidualSimulation,
        Frozen,
    }

    public enum RoomInstanceState
    {
        Uninstantiated,
        Building,
        WarmLoaded,
        Active,
        FrozenVisited,
    }

    public enum RoomCameraMode
    {
        Fixed = 0,
        BoundedX = 1,
        BoundedY = 2,
        BoundedXY = 3,
        BoundedXAnchors = 4,
        BoundedYAnchors = 5,
        BoundedXYAnchors = 6,
    }

    public enum CardinalDirection
    {
        Left,
        Right,
        Up,
        Down,
    }

    [Serializable]
    public sealed class RoomPersistentState
    {
        [SerializeField] private int revision;

        private readonly Dictionary<string, string> objectStates = new Dictionary<string, string>();
        private readonly HashSet<Vector2Int> destroyedCells = new HashSet<Vector2Int>();
        private readonly HashSet<string> collectedRuntimeIds = new HashSet<string>();

        public int Revision => revision;
        public IReadOnlyDictionary<string, string> ObjectStates => objectStates;
        public IReadOnlyCollection<Vector2Int> DestroyedCells => destroyedCells;
        public IReadOnlyCollection<string> CollectedRuntimeIds => collectedRuntimeIds;

        public void StoreObject(string persistenceId, string payload)
        {
            if (string.IsNullOrWhiteSpace(persistenceId))
            {
                throw new ArgumentException("A room persistence ID is required.", nameof(persistenceId));
            }

            objectStates[persistenceId] = payload ?? string.Empty;
        }

        public bool TryGetObject(string persistenceId, out string payload)
        {
            return objectStates.TryGetValue(persistenceId, out payload);
        }

        public void MarkDestroyed(Vector2Int cell)
        {
            destroyedCells.Add(cell);
        }

        public void MarkCollected(string runtimeId)
        {
            if (!string.IsNullOrWhiteSpace(runtimeId))
            {
                collectedRuntimeIds.Add(runtimeId);
            }
        }

        public void CommitRevision()
        {
            revision++;
        }
    }

    public interface IRoomPersistentParticipant
    {
        string PersistenceId { get; }
        string CaptureRoomState();
        void RestoreRoomState(string payload);
    }

    public interface IRoomSimulationParticipant
    {
        void SetRoomSimulationState(RoomSimulationState state);
    }

    public readonly struct RoomChangedEvent
    {
        public RoomChangedEvent(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; }
        public string To { get; }
    }
}

#endif
