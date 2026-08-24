using System;

namespace StarNight.Character.RunState
{
    /// <summary>
    /// 런 상태 중앙 설정. 시작 폭탄 4·로프 4는 레거시 RunState.CreateNew
    /// (bombs=4, ropes=4) 선례를 따른 기준선이다.
    /// </summary>
    public readonly struct CharacterRunStateSettings
    {
        public CharacterRunStateSettings(
            int startingBombCount,
            int startingRopeCount)
        {
            if (startingBombCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingBombCount));
            }

            if (startingRopeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startingRopeCount));
            }

            StartingBombCount = startingBombCount;
            StartingRopeCount = startingRopeCount;
        }

        public int StartingBombCount { get; }
        public int StartingRopeCount { get; }

        public static CharacterRunStateSettings Default
        {
            get { return new CharacterRunStateSettings(4, 4); }
        }
    }
}
