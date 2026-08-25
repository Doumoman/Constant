namespace StarNight.Character.Live.Hud
{
    /// <summary>
    /// 라이브 HUD 뷰 모델 값 객체(표시용 데이터 전용 — 권위 상태 아님).
    /// 소스 데이터 부재 시 안정 빈 값을 노출한다(HasRunData=false,
    /// 수치 0, 라벨 "NO RUN"/"-", 피드백 빈 문자열).
    /// </summary>
    public readonly struct CharacterLiveHudSnapshot
    {
        public CharacterLiveHudSnapshot(
            bool hasRunData,
            int currentHealth,
            int maxHealth,
            bool isInvulnerable,
            int bombCount,
            int ropeCount,
            string runStatusLabel,
            string roomLabel,
            string latestFeedback)
        {
            HasRunData = hasRunData;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            IsInvulnerable = isInvulnerable;
            BombCount = bombCount;
            RopeCount = ropeCount;
            RunStatusLabel = runStatusLabel ?? string.Empty;
            RoomLabel = roomLabel ?? string.Empty;
            LatestFeedback = latestFeedback ?? string.Empty;
        }

        public bool HasRunData { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public bool IsInvulnerable { get; }
        public int BombCount { get; }
        public int RopeCount { get; }
        public string RunStatusLabel { get; }
        public string RoomLabel { get; }
        public string LatestFeedback { get; }

        public static CharacterLiveHudSnapshot Empty
        {
            get
            {
                return new CharacterLiveHudSnapshot(
                    false, 0, 0, false, 0, 0, "NO RUN", "-", string.Empty);
            }
        }
    }
}
