namespace StarNight.Character.Traversal
{
    /// <summary>
    /// 로프 등반 판정 입력 스냅샷. 겹침/의도/수직 축을 값으로 받으며
    /// 라이브 입력·물리 배선은 없다.
    /// </summary>
    public readonly struct CharacterRopeClimbInput
    {
        public CharacterRopeClimbInput(
            int actorId,
            bool isOverlappingRope,
            bool hasClimbIntent,
            float verticalAxis,
            float currentWorldY,
            CharacterRopeExtent ropeExtent)
        {
            ActorId = actorId;
            IsOverlappingRope = isOverlappingRope;
            HasClimbIntent = hasClimbIntent;
            VerticalAxis = verticalAxis;
            CurrentWorldY = currentWorldY;
            RopeExtent = ropeExtent;
        }

        public int ActorId { get; }
        public bool IsOverlappingRope { get; }
        public bool HasClimbIntent { get; }

        /// <summary>+1 위, -1 아래, 0 정지(정책에서 [-1,1] clamp).</summary>
        public float VerticalAxis { get; }

        public float CurrentWorldY { get; }
        public CharacterRopeExtent RopeExtent { get; }
    }
}
