#if LEGACY_DISABLED
namespace StarNight.Interaction.State
{
    public interface IRuntimeRoomStateParticipant
    {
        string RuntimeRoomStateId { get; }
        string CaptureRuntimeRoomState();
        void RestoreRuntimeRoomState(string payload);
    }

    public interface IResidualSimulationParticipant
    {
        bool HasResidualWork { get; }
        void BeginResidualSimulation();
        void TickResidualSimulation(float deltaSeconds);
        void FreezeResidualSimulation(bool timedOut);
    }
}

#endif
