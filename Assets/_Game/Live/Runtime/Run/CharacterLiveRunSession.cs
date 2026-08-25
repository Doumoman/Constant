using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;
using StarNight.Character.RunState;
using StarNight.Character.Survival;

namespace StarNight.Character.Live.Run
{
    /// <summary>
    /// 라이브 런 세션 상태(순수 보관). 스폰 요청 1회 소비로 시작되며
    /// 액터 ID·현재 방·런 상태(CharacterRunState)를 소유한다.
    /// L02(카메라/루트)·L03(도구/HUD) 소비자가 이 표면을 읽는다.
    /// </summary>
    public sealed class CharacterLiveRunSession
    {
        public bool IsRunStarted { get; private set; }
        public bool IsSpawnConsumed { get; private set; }
        public int ActorId { get; private set; }
        public CharacterRoomId CurrentRoomId { get; private set; }
        public CharacterPlayerSpawnRequest SpawnRequest { get; private set; }
        public CharacterRunState RunState { get; private set; }

        /// <summary>
        /// 스폰 요청 1회 소비로 런을 시작한다. 이미 시작된 세션이면 false
        /// (스폰 요청은 런 시작당 정확히 한 번만 소비된다).
        /// </summary>
        public bool TryStartRun(in CharacterPlayerSpawnRequest spawnRequest)
        {
            if (IsRunStarted)
            {
                return false;
            }

            ActorId = spawnRequest.ActorId;
            CurrentRoomId = spawnRequest.StartRoomId;
            SpawnRequest = spawnRequest;

            var survivalSettings = CharacterSurvivalSettings.Default;
            var health = CharacterHealthState.CreateFull(
                spawnRequest.ActorId,
                CharacterSurvivalTargetKind.Player,
                survivalSettings.MaxPlayerHealth);
            var inventory = CharacterRunInventoryState.CreateStarting(
                spawnRequest.ActorId, CharacterRunStateSettings.Default);

            RunState = CharacterRunState.CreateActive(
                spawnRequest.ActorId, in health, in inventory);

            IsSpawnConsumed = true;
            IsRunStarted = true;
            return true;
        }

        /// <summary>런 상태 스냅샷 갱신(순수 계약 적용 결과 반영 — L03 소비용).</summary>
        public void UpdateRunState(in CharacterRunState runState)
        {
            RunState = runState;
        }

        /// <summary>현재 방 갱신(방 전환 소비 — L02 소관).</summary>
        public void UpdateCurrentRoom(CharacterRoomId roomId)
        {
            CurrentRoomId = roomId;
        }
    }
}
