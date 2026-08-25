using System.Collections.Generic;
using StarNight.Character.Equipment;
using StarNight.Character.Traversal;

namespace StarNight.Character.Live.Tools
{
    /// <summary>
    /// 로프 1건의 라이브 명령 값 객체 — 캐릭터 설치 요청과 결정적 수직
    /// 세그먼트 요청 목록(CHAR05_02 정책 산출)을 그대로 운반한다.
    /// 프리팹 생성·씬 배치는 이후 배선 과제의 소비자 소관이다.
    /// </summary>
    public readonly struct CharacterLiveRopeCommand
    {
        public CharacterLiveRopeCommand(
            CharacterRopePlacementRequest placement,
            IReadOnlyList<CharacterRopeSegmentRequest> segments)
        {
            Placement = placement;
            Segments = segments;
        }

        public CharacterRopePlacementRequest Placement { get; }
        public IReadOnlyList<CharacterRopeSegmentRequest> Segments { get; }
    }
}
