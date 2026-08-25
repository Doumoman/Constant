using System;
using System.Collections.Generic;
using StarNight.Character.GeneratedRunValidation;

namespace StarNight.Character.Live.Adapters
{
    /// <summary>
    /// 생성 MAP 투영 결과(불변): 캐릭터 생성 런 스냅샷 + 검증 결과 +
    /// 준비/루트 소스 + 월드 질의 + 어댑터 진단.
    /// </summary>
    public sealed class CharacterLiveGeneratedMapProjection
    {
        private static readonly CharacterLiveGeneratedMapDiagnostic[] EmptyDiagnostics =
            Array.Empty<CharacterLiveGeneratedMapDiagnostic>();

        public CharacterLiveGeneratedMapProjection(
            CharacterGeneratedRunSnapshot snapshot,
            CharacterGeneratedRunValidationResult validationResult,
            CharacterLiveGeneratedReadinessSource readinessSource,
            CharacterLiveGeneratedRouteSource routeSource,
            CharacterLiveMapWorldQueryAdapter worldQuery,
            IReadOnlyList<CharacterLiveGeneratedMapDiagnostic> adapterDiagnostics)
        {
            Snapshot = snapshot;
            ValidationResult = validationResult;
            ReadinessSource = readinessSource;
            RouteSource = routeSource;
            WorldQuery = worldQuery;
            AdapterDiagnostics = adapterDiagnostics ?? EmptyDiagnostics;
        }

        public CharacterGeneratedRunSnapshot Snapshot { get; }
        public CharacterGeneratedRunValidationResult ValidationResult { get; }
        public CharacterLiveGeneratedReadinessSource ReadinessSource { get; }
        public CharacterLiveGeneratedRouteSource RouteSource { get; }
        public CharacterLiveMapWorldQueryAdapter WorldQuery { get; }
        public IReadOnlyList<CharacterLiveGeneratedMapDiagnostic> AdapterDiagnostics { get; }

        /// <summary>어댑터 결함 0 + 캐릭터 검증 통과.</summary>
        public bool IsUsable
        {
            get
            {
                return AdapterDiagnostics.Count == 0 && ValidationResult.Passed;
            }
        }
    }
}
