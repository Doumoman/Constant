using System;
using System.Collections.Generic;
using StarNight.Character.Integration;
using StarNight.Character.MapIntegration;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 루트 소스(불변). 투영된 선언 엣지와 생성 준비 소스를
    /// L02_01 CharacterLiveRouteTransitionConsumer가 그대로 소비할 수 있는
    /// 형태로 노출한다(수동 소스와 동일 표면).
    /// </summary>
    public sealed class CharacterLiveGeneratedRouteSource
    {
        private static readonly CharacterGeneratedRouteEdgeSnapshot[] EmptyRoutes =
            Array.Empty<CharacterGeneratedRouteEdgeSnapshot>();

        public CharacterLiveGeneratedRouteSource(
            IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> declaredEdges,
            CharacterLiveGeneratedReadinessSource readinessSource)
        {
            DeclaredEdges = declaredEdges ?? EmptyRoutes;
            ReadinessSource = readinessSource;
        }

        public IReadOnlyList<CharacterGeneratedRouteEdgeSnapshot> DeclaredEdges { get; }

        public CharacterLiveGeneratedReadinessSource ReadinessSource { get; }

        public ICharacterRoomReadinessSource Readiness
        {
            get { return ReadinessSource; }
        }
    }
}
