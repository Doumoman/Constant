using System;

namespace StarNight.Map.WorldGeneration.Boundaries
{
    public sealed class MoonpalaceBoundaryChunkResolver
    {
        private readonly MoonpalaceBoundaryResolvePolicy resolvePolicy;

        public MoonpalaceBoundaryChunkResolver(MoonpalaceBoundaryResolvePolicy resolvePolicy = null)
        {
            this.resolvePolicy = resolvePolicy ?? MoonpalaceBoundaryResolvePolicy.Default;
        }

        public MoonpalaceBoundaryResolveResult Resolve(
            MoonpalaceBoundaryCandidateIndex index,
            MoonpalaceBoundaryResolveRequest request)
        {
            if (index == null)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.MissingIndex);
            }

            if (request == null)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.MissingRequest);
            }

            if (!request.FromBiome.IsDefined)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.InvalidFromBiome);
            }

            if (!request.ToBiome.IsDefined)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.InvalidToBiome);
            }

            if (request.FromBiome == request.ToBiome)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.SelfPair);
            }

            if (!request.Profile.IsDefined)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.InvalidProfile);
            }

            if (request.Orientation != MoonpalaceBoundaryOrientation.Horizontal &&
                request.Orientation != MoonpalaceBoundaryOrientation.Vertical)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.InvalidOrientation);
            }

            if (!request.RouteRole.IsDefined)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.InvalidRouteRole);
            }

            if (!request.EdgeSignature.IsDefined)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.InvalidEdgeSignature);
            }

            var canonicalPair = new MoonpalaceBiomePair(request.FromBiome, request.ToBiome);
            var direction = request.FromBiome == canonicalPair.First
                ? MoonpalaceBoundaryRequestDirection.Forward
                : MoonpalaceBoundaryRequestDirection.Reverse;
            var key = new MoonpalaceBoundaryCandidateKey(
                canonicalPair,
                request.Profile,
                request.Orientation,
                request.RouteRole,
                request.EdgeSignature);
            var candidates = index.GetCandidates(key);
            if (candidates.Count == 0)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.NoCandidates);
            }

            var selected = resolvePolicy.Select(key, candidates, request.SelectionSeed);
            if (selected == null)
            {
                return MoonpalaceBoundaryResolveResult.Failure(
                    MoonpalaceBoundaryResolveIssue.NoCandidates);
            }

            var transformPolicy = MoonpalaceBoundaryTransformPolicy.Create(
                direction,
                request.Orientation);
            var resolved = new MoonpalaceBoundaryResolvedCandidate(
                selected,
                canonicalPair,
                direction,
                transformPolicy,
                key);
            return MoonpalaceBoundaryResolveResult.Success(resolved);
        }
    }
}
