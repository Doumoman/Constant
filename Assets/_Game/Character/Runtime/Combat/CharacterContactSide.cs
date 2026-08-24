namespace StarNight.Character.Combat
{
    /// <summary>플레이어-적 접촉의 기하학적 방향(플레이어 기준).</summary>
    public enum CharacterContactSide
    {
        /// <summary>겹침 없음 — 전투 이벤트 없음.</summary>
        None,

        /// <summary>플레이어가 적 상단에 접촉.</summary>
        Top,

        /// <summary>측면 접촉.</summary>
        Side,

        /// <summary>플레이어가 적 하단에서 접촉(적이 위).</summary>
        Bottom
    }
}
