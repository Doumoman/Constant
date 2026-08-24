using System;
using System.Collections.Generic;

namespace StarNight.Character.GeneratedRunValidation
{
    /// <summary>
    /// 생성 런 검증 결과 — 시드·진단·요청 수와 결정적 다이제스트를 담는
    /// 읽기 전용 데이터.
    /// </summary>
    public sealed class CharacterGeneratedRunValidationResult
    {
        private static readonly CharacterGeneratedRunValidationDiagnostic[] EmptyDiagnostics =
            Array.Empty<CharacterGeneratedRunValidationDiagnostic>();

        public CharacterGeneratedRunValidationResult(
            int runId,
            int seed,
            int spawnRequestCount,
            int routeRequestCount,
            IReadOnlyList<CharacterGeneratedRunValidationDiagnostic> diagnostics,
            string digest)
        {
            RunId = runId;
            Seed = seed;
            SpawnRequestCount = spawnRequestCount;
            RouteRequestCount = routeRequestCount;
            Diagnostics = diagnostics ?? EmptyDiagnostics;
            Digest = digest;
        }

        public int RunId { get; }
        public int Seed { get; }
        public int SpawnRequestCount { get; }
        public int RouteRequestCount { get; }
        public IReadOnlyList<CharacterGeneratedRunValidationDiagnostic> Diagnostics { get; }

        /// <summary>같은 입력이면 항상 같은 값이 되는 결정적 요약 문자열.</summary>
        public string Digest { get; }

        public bool Passed
        {
            get { return Diagnostics.Count == 0 && SpawnRequestCount == 1; }
        }
    }
}
