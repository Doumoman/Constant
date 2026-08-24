namespace StarNight.Character.RunState
{
    /// <summary>
    /// 소모 요청 적용 결과. 입력 상태는 그대로이고 새 상태가 여기 담긴다.
    /// AppliedAmount 0이면 아무 변화가 없었다는 뜻이다.
    /// </summary>
    public readonly struct CharacterRunInventoryApplyResult
    {
        public CharacterRunInventoryApplyResult(
            CharacterRunInventoryState newState,
            int appliedAmount)
        {
            NewState = newState;
            AppliedAmount = appliedAmount;
        }

        public CharacterRunInventoryState NewState { get; }
        public int AppliedAmount { get; }

        public bool Changed
        {
            get { return AppliedAmount > 0; }
        }
    }
}
