namespace StarNight.Character.Integration
{
    /// <summary>
    /// 루트 통과 요구 역량 분류. 잠금 이동 문법(2셀 점프/2셀 틈)과 장비
    /// 지원(폭탄/로프)만 수용 가능하며, 잠금 밖 고급 이동·공격류 요구는
    /// Unsupported 분류로 들어와 항상 거부된다(금지 기능 명칭을 계약
    /// 표면에 들이지 않기 위한 의도적 추상화).
    /// </summary>
    public enum CharacterRouteRequirement
    {
        BasicMovement,
        BombSupport,
        RopeSupport,
        UnsupportedAdvancedMovement,
        UnsupportedCombatAction
    }
}
