using StarNight.Character.Survival;

namespace StarNight.Character.RunState
{
    /// <summary>
    /// 불변 런 상태 값 객체 — Survival 체력 상태 + 런 인벤토리 + 런 진행
    /// 상태 + 복귀 토큰(불투명 데이터)의 스냅샷. 갱신은 항상 새 상태를
    /// 반환하며 씬 리로드·세이브 변조·UI 어떤 것도 수행하지 않는다.
    /// </summary>
    public readonly struct CharacterRunState
    {
        public CharacterRunState(
            int actorId,
            CharacterHealthState health,
            CharacterRunInventoryState inventory,
            CharacterRunStatus status,
            string returnDestinationToken)
        {
            ActorId = actorId;
            Health = health;
            Inventory = inventory;
            Status = status;
            ReturnDestinationToken = returnDestinationToken;
        }

        public int ActorId { get; }
        public CharacterHealthState Health { get; }
        public CharacterRunInventoryState Inventory { get; }
        public CharacterRunStatus Status { get; }

        /// <summary>선택적 복귀 목적지 토큰 — 데이터 전용.</summary>
        public string ReturnDestinationToken { get; }

        public bool HasReturnDestination
        {
            get { return !string.IsNullOrEmpty(ReturnDestinationToken); }
        }

        /// <summary>활성 런 시작 상태.</summary>
        public static CharacterRunState CreateActive(
            int actorId,
            in CharacterHealthState health,
            in CharacterRunInventoryState inventory)
        {
            return new CharacterRunState(
                actorId, health, inventory, CharacterRunStatus.Active, null);
        }

        /// <summary>Survival 체력 스냅샷 갱신 — 새 상태 반환.</summary>
        public CharacterRunState WithHealth(in CharacterHealthState health)
        {
            return new CharacterRunState(
                ActorId, health, Inventory, Status, ReturnDestinationToken);
        }

        /// <summary>인벤토리 스냅샷 갱신 — 새 상태 반환.</summary>
        public CharacterRunState WithInventory(
            in CharacterRunInventoryState inventory)
        {
            return new CharacterRunState(
                ActorId, Health, inventory, Status, ReturnDestinationToken);
        }

        /// <summary>
        /// 런 실패 요청 적용 — 본인 대상 요청만 Failed로 표시하고 복귀
        /// 토큰을 기록한다. 대상 불일치 요청은 무시한다(적/비플레이어
        /// 사망은 CHAR05_03 정책상 애초에 런 실패 요청을 만들지 못한다).
        /// </summary>
        public CharacterRunState ApplyRunFailure(
            in CharacterRunFailureRequest request)
        {
            if (request.ActorId != ActorId)
            {
                return this;
            }

            return new CharacterRunState(
                ActorId,
                Health,
                Inventory,
                CharacterRunStatus.Failed,
                request.ReturnDestinationToken);
        }
    }
}
