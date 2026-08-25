using System.Collections.Generic;
using StarNight.Character.MapIntegration;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 방 준비 소스(불변). 투영에 성공한 생성 방만 준비됨으로 보고하고
    /// 미생성 방은 미등록(게이트가 BlockedMissingRoom 처리)이다.
    /// L02_01 루트/카메라 소비자와 ICharacterRoomReadinessSource로 호환된다.
    /// </summary>
    public sealed class CharacterLiveGeneratedReadinessSource : ICharacterRoomReadinessSource
    {
        private readonly Dictionary<CharacterRoomId, bool> rooms;

        public CharacterLiveGeneratedReadinessSource(
            IReadOnlyList<CharacterRoomId> readyRooms)
        {
            rooms = new Dictionary<CharacterRoomId, bool>();

            if (readyRooms == null)
            {
                return;
            }

            for (int index = 0; index < readyRooms.Count; index++)
            {
                rooms[readyRooms[index]] = true;
            }
        }

        public int ReadyRoomCount
        {
            get { return rooms.Count; }
        }

        public bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady)
        {
            return rooms.TryGetValue(room, out isReady);
        }
    }
}
