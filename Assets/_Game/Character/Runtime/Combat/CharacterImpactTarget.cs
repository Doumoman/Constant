namespace StarNight.Character.Combat
{
    /// <summary>임팩트 대상 스냅샷 값 객체.</summary>
    public readonly struct CharacterImpactTarget
    {
        public CharacterImpactTarget(
            CharacterImpactTargetKind targetKind,
            int targetId,
            bool isHostile)
        {
            TargetKind = targetKind;
            TargetId = targetId;
            IsHostile = isHostile;
        }

        public CharacterImpactTargetKind TargetKind { get; }
        public int TargetId { get; }

        /// <summary>적대 여부. 기절한 휴대 대상 등은 명시적으로 적대일 때만 피해 대상이다.</summary>
        public bool IsHostile { get; }

        public static CharacterImpactTarget SolidWorld
        {
            get
            {
                return new CharacterImpactTarget(
                    CharacterImpactTargetKind.SolidWorld, 0, false);
            }
        }
    }
}
