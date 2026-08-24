using StarNight.Character.RunState;

namespace StarNight.Character.Presentation
{
    /// <summary>
    /// HUD 스냅샷 — 런 상태에서 결정적으로 파생되는 표시용 데이터 값 객체.
    /// Unity UI/Canvas/TextMeshPro/GameObject/SceneManager/AudioSource/
    /// Animator/PlayerPrefs를 일절 참조하지 않는다. 실제 HUD 바인딩은
    /// 이 과제 밖이다.
    /// </summary>
    public readonly struct CharacterHudSnapshot
    {
        public CharacterHudSnapshot(
            int currentHealth,
            int maxHealth,
            bool isInvulnerable,
            int bombCount,
            int ropeCount,
            CharacterRunStatus runStatus,
            string returnDestinationToken)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsInvulnerable = isInvulnerable;
            BombCount = bombCount;
            RopeCount = ropeCount;
            RunStatus = runStatus;
            ReturnDestinationToken = returnDestinationToken;
        }

        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool IsInvulnerable { get; }
        public int BombCount { get; }
        public int RopeCount { get; }
        public CharacterRunStatus RunStatus { get; }
        public string ReturnDestinationToken { get; }

        public bool HasReturnDestination
        {
            get { return !string.IsNullOrEmpty(ReturnDestinationToken); }
        }

        /// <summary>런 상태 → HUD 스냅샷(순수·결정적).</summary>
        public static CharacterHudSnapshot FromRunState(
            in CharacterRunState runState)
        {
            return new CharacterHudSnapshot(
                runState.Health.CurrentHealth,
                runState.Health.MaxHealth,
                runState.Health.IsInvulnerable,
                runState.Inventory.BombCount,
                runState.Inventory.RopeCount,
                runState.Status,
                runState.ReturnDestinationToken);
        }
    }
}
