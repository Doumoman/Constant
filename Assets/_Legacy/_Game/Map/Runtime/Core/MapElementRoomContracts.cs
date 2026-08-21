#if LEGACY_DISABLED
namespace StarNight.Map
{
    public enum MapRoomState
    {
        Dormant,
        NeighborPreview,
        TransitionTarget,
        Active,
        Frozen,
    }

    public interface IMapElementSimulationParticipant
    {
        void SetMapRoomState(MapRoomState state);
    }

    public interface IMapElementPersistentParticipant
    {
        string PersistenceId { get; }
        string CaptureMapElementState();
        void RestoreMapElementState(string payload);
    }
}

#endif
