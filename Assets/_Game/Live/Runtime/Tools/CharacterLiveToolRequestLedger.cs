using System.Collections.Generic;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 도구 요청 소비 대장(결정적 dedupe 경로). 채널×요청 id 조합은 수락
    /// 시에만 기록되며, 기록된 조합의 재시도는 중복으로 거부된다.
    /// 거부된 요청은 기록되지 않으므로 같은 id로 재시도할 수 있다.
    /// 전역/정적 상태 없음 — 인스턴스를 배선 계층이 소유한다.
    /// </summary>
    public sealed class CharacterLiveToolRequestLedger
    {
        private const int ChannelCount = 5;

        private readonly HashSet<long>[] consumedByChannel;

        public CharacterLiveToolRequestLedger()
        {
            consumedByChannel = new HashSet<long>[ChannelCount];
            for (int index = 0; index < ChannelCount; index++)
            {
                consumedByChannel[index] = new HashSet<long>();
            }
        }

        public bool IsConsumed(CharacterLiveToolChannel channel, long requestId)
        {
            return consumedByChannel[(int)channel].Contains(requestId);
        }

        /// <summary>수락된 요청 기록. 이미 기록된 조합이면 false.</summary>
        public bool TryMarkConsumed(CharacterLiveToolChannel channel, long requestId)
        {
            return consumedByChannel[(int)channel].Add(requestId);
        }

        public int ConsumedCount(CharacterLiveToolChannel channel)
        {
            return consumedByChannel[(int)channel].Count;
        }
    }
}
