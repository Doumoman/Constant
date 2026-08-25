using System.Collections.Generic;
using StarNight.Character.MapIntegration;

namespace StarNight.Character.Live.Rooms
{
    /// <summary>
    /// 라이브 방 준비 소스(ICharacterRoomReadinessSource 구현). 등록된 방의
    /// 준비 상태만 보고한다 — 준비 판정 로직 자체는 CHAR03 게이트 소관이며
    /// 여기는 데이터 등록부일 뿐이다. L02_02에서 생성 MAP 어댑터가 같은
    /// 인터페이스로 교체한다.
    /// </summary>
    public sealed class CharacterLiveRoomReadinessSource : ICharacterRoomReadinessSource
    {
        private readonly Dictionary<CharacterRoomId, bool> rooms =
            new Dictionary<CharacterRoomId, bool>();

        public void RegisterRoom(CharacterRoomId room, bool isReady)
        {
            rooms[room] = isReady;
        }

        public bool TryGetRoomReadiness(CharacterRoomId room, out bool isReady)
        {
            return rooms.TryGetValue(room, out isReady);
        }
    }
}
