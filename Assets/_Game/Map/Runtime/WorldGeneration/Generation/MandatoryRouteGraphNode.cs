using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Domain;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteGraphNode
    {
        internal MandatoryRouteGraphNode(MandatoryRouteGraphNodeId nodeId, SectorCoord coordinate, string routeMaskId,
            bool openLeft, bool openRight, bool openUp, bool openDown, int shortestDistanceFromStart,
            IEnumerable<string> terminalSourceIds, IEnumerable<string> siteSourceIds, IEnumerable<string> loopSourceIds, IEnumerable<string> gatewaySourceIds)
        {
            NodeId = nodeId;
            Coordinate = coordinate;
            SectorIndex = WorldGridIndex.ToIndex(coordinate);
            RouteMaskId = routeMaskId ?? throw new ArgumentNullException(nameof(routeMaskId));
            OpenLeft = openLeft; OpenRight = openRight; OpenUp = openUp; OpenDown = openDown;
            ShortestDistanceFromStart = shortestDistanceFromStart;
            TerminalSourceIds = Freeze(terminalSourceIds);
            SiteSourceIds = Freeze(siteSourceIds);
            LoopSourceIds = Freeze(loopSourceIds);
            GatewaySourceIds = Freeze(gatewaySourceIds);
        }

        public MandatoryRouteGraphNodeId NodeId { get; }
        public int SectorIndex { get; }
        public SectorCoord Coordinate { get; }
        public string RouteMaskId { get; }
        public bool OpenLeft { get; }
        public bool OpenRight { get; }
        public bool OpenUp { get; }
        public bool OpenDown { get; }
        public int ShortestDistanceFromStart { get; }
        public bool MandatoryGraphNode => true;
        public IReadOnlyList<string> TerminalSourceIds { get; }
        public IReadOnlyList<string> SiteSourceIds { get; }
        public IReadOnlyList<string> LoopSourceIds { get; }
        public IReadOnlyList<string> GatewaySourceIds { get; }

        private static IReadOnlyList<string> Freeze(IEnumerable<string> source)
        {
            var values = new List<string>(source ?? Array.Empty<string>());
            values.Sort(StringComparer.Ordinal);
            return new ReadOnlyCollection<string>(values);
        }
    }
}
