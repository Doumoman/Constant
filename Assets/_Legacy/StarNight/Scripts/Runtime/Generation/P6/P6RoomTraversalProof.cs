#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Generation.P6
{
    public enum P6SocketLinkKind
    {
        MainRoutePair = 0,
        OptionalEntry = 1,
        OptionalReturn = 2
    }

    public sealed class P6SocketTraversalProof
    {
        internal P6SocketTraversalProof(
            string socketId,
            RoomSocketSide side,
            Vector2Int cellOffset,
            Vector2Int openingSize,
            RoomTraversalType traversalType,
            bool mainRouteAllowed,
            Vector2Int validationAnchor,
            bool anchorStandable,
            bool oneCellOpening,
            bool oneCellBodyClearance)
        {
            SocketId = socketId ?? string.Empty;
            Side = side;
            CellOffset = cellOffset;
            OpeningSize = openingSize;
            TraversalType = traversalType;
            MainRouteAllowed = mainRouteAllowed;
            ValidationAnchor = validationAnchor;
            AnchorStandable = anchorStandable;
            OneCellOpening = oneCellOpening;
            OneCellBodyClearance = oneCellBodyClearance;
        }

        public string SocketId { get; }
        public RoomSocketSide Side { get; }
        public Vector2Int CellOffset { get; }
        public Vector2Int OpeningSize { get; }
        public RoomTraversalType TraversalType { get; }
        public bool MainRouteAllowed { get; }
        public Vector2Int ValidationAnchor { get; }
        public bool AnchorStandable { get; }
        public bool OneCellOpening { get; }
        public bool OneCellBodyClearance { get; }
        public bool IsToolFreeMainRouteSocket =>
            MainRouteAllowed
            && RoomSocketRules.IsMainRouteTraversal(TraversalType);
    }

    public sealed class P6SocketLinkProof
    {
        private readonly ReadOnlyCollection<Vector2Int> path;

        internal P6SocketLinkProof(
            string fromSocketId,
            string toSocketId,
            P6SocketLinkKind kind,
            bool reachable,
            IReadOnlyList<Vector2Int> sourcePath,
            bool oneCellBodyClearance,
            bool jumpEnvelopePassed,
            int maximumHorizontalStep,
            int maximumRise,
            int maximumDrop)
        {
            FromSocketId = fromSocketId ?? string.Empty;
            ToSocketId = toSocketId ?? string.Empty;
            Kind = kind;
            Reachable = reachable;
            Vector2Int[] copiedPath =
                sourcePath != null
                    ? Copy(sourcePath)
                    : Array.Empty<Vector2Int>();
            path = Array.AsReadOnly(copiedPath);
            OneCellBodyClearance = oneCellBodyClearance;
            JumpEnvelopePassed = jumpEnvelopePassed;
            MaximumHorizontalStep = maximumHorizontalStep;
            MaximumRise = maximumRise;
            MaximumDrop = maximumDrop;
        }

        public string FromSocketId { get; }
        public string ToSocketId { get; }
        public P6SocketLinkKind Kind { get; }
        public bool Reachable { get; }
        public IReadOnlyList<Vector2Int> Path => path;
        public bool OneCellBodyClearance { get; }
        public bool JumpEnvelopePassed { get; }
        public int MaximumHorizontalStep { get; }
        public int MaximumRise { get; }
        public int MaximumDrop { get; }
        public bool Passed =>
            Reachable && OneCellBodyClearance && JumpEnvelopePassed;

        private static Vector2Int[] Copy(
            IReadOnlyList<Vector2Int> source)
        {
            Vector2Int[] result = new Vector2Int[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }
    }

    public sealed class P6RoomTraversalProof
    {
        private readonly ReadOnlyCollection<P6SocketTraversalProof> sockets;
        private readonly ReadOnlyCollection<P6SocketLinkProof> links;
        private readonly ReadOnlyCollection<string> validationIssues;

        internal P6RoomTraversalProof(
            string roomId,
            Vector2Int logicalSize,
            bool roomPrefabValidationPassed,
            IReadOnlyList<string> issues,
            bool exitBlockProof,
            IReadOnlyList<P6SocketTraversalProof> socketProofs,
            IReadOnlyList<P6SocketLinkProof> linkProofs)
        {
            RoomId = roomId ?? string.Empty;
            LogicalSize = logicalSize;
            RoomPrefabValidationPassed = roomPrefabValidationPassed;
            ExitBlockProof = exitBlockProof;
            sockets = Array.AsReadOnly(Copy(socketProofs));
            links = Array.AsReadOnly(Copy(linkProofs));
            validationIssues = Array.AsReadOnly(CopyStrings(issues));
        }

        public string RoomId { get; }
        public Vector2Int LogicalSize { get; }
        public bool RoomPrefabValidationPassed { get; }
        public bool ExitBlockProof { get; }
        public IReadOnlyList<P6SocketTraversalProof> Sockets => sockets;
        public IReadOnlyList<P6SocketLinkProof> Links => links;
        public IReadOnlyList<string> ValidationIssues => validationIssues;
        public bool SocketAnchorsPassed => AllSockets(
            socket => socket.AnchorStandable);
        public bool OneCellOpeningsPassed => AllSockets(
            socket => socket.OneCellOpening);
        public bool OneCellBodyClearancePassed =>
            AllSockets(socket => socket.OneCellBodyClearance)
            && AllLinks(link => link.OneCellBodyClearance);
        public bool JumpEnvelopePassed =>
            AllLinks(link => link.JumpEnvelopePassed);
        public bool MainSocketPairsReachable =>
            AllLinks(
                link => link.Kind != P6SocketLinkKind.MainRoutePair
                    || link.Reachable);
        public bool OptionalReturnsReachable =>
            AllLinks(
                link => link.Kind == P6SocketLinkKind.MainRoutePair
                    || link.Reachable);
        public bool Passed =>
            RoomPrefabValidationPassed
            && ExitBlockProof
            && SocketAnchorsPassed
            && OneCellOpeningsPassed
            && OneCellBodyClearancePassed
            && JumpEnvelopePassed
            && MainSocketPairsReachable
            && OptionalReturnsReachable;

        public P6SocketTraversalProof FindSocket(string socketId)
        {
            for (int index = 0; index < sockets.Count; index++)
            {
                if (string.Equals(
                        sockets[index].SocketId,
                        socketId,
                        StringComparison.Ordinal))
                {
                    return sockets[index];
                }
            }

            return null;
        }

        private bool AllSockets(
            Func<P6SocketTraversalProof, bool> predicate)
        {
            for (int index = 0; index < sockets.Count; index++)
            {
                if (!predicate(sockets[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool AllLinks(Func<P6SocketLinkProof, bool> predicate)
        {
            for (int index = 0; index < links.Count; index++)
            {
                if (!predicate(links[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static P6SocketTraversalProof[] Copy(
            IReadOnlyList<P6SocketTraversalProof> source)
        {
            if (source == null)
            {
                return Array.Empty<P6SocketTraversalProof>();
            }

            P6SocketTraversalProof[] result =
                new P6SocketTraversalProof[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }

        private static P6SocketLinkProof[] Copy(
            IReadOnlyList<P6SocketLinkProof> source)
        {
            if (source == null)
            {
                return Array.Empty<P6SocketLinkProof>();
            }

            P6SocketLinkProof[] result =
                new P6SocketLinkProof[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index];
            }

            return result;
        }

        private static string[] CopyStrings(IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            string[] result = new string[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                result[index] = source[index] ?? string.Empty;
            }

            return result;
        }
    }

    public static class P6RoomTraversalProofFactory
    {
        private const int MaximumJumpDistance = 3;
        private const int MaximumJumpRise = 2;
        private const int MaximumDropDistance = 12;

        public static P6RoomTraversalProof Capture(RoomTemplate2D template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            RoomValidationReport validation =
                RoomPrefabValidator.Validate(template);
            RoomTraversalSnapshot snapshot =
                RoomTraversalSnapshot.Capture(template);
            List<P6SocketTraversalProof> sockets =
                CaptureSockets(template, snapshot);
            List<P6SocketLinkProof> links =
                CaptureLinks(template, snapshot);

            return new P6RoomTraversalProof(
                template.RoomId,
                template.LogicalSize,
                validation.Passed,
                validation.Issues,
                template.ExitBlockProof,
                sockets,
                links);
        }

        public static P6RoomPrefabDescriptor CreateDescriptor(
            RoomTemplate2D template,
            int selectionWeight = 1,
            int maxUses = 2)
        {
            P6RoomTraversalProof proof = Capture(template);
            P6SocketSides sides = P6SocketSides.None;
            bool mainRouteAllowed = false;
            for (int index = 0; index < proof.Sockets.Count; index++)
            {
                P6SocketTraversalProof socket = proof.Sockets[index];
                sides |= ToP6Side(socket.Side);
                mainRouteAllowed |= socket.IsToolFreeMainRouteSocket;
            }

            return new P6RoomPrefabDescriptor(
                template.RoomId,
                template.Archetype,
                template.Roles,
                template.Region,
                template.MacroSize,
                sides,
                mainRouteAllowed,
                selectionWeight,
                maxUses,
                proof);
        }

        private static List<P6SocketTraversalProof> CaptureSockets(
            RoomTemplate2D template,
            RoomTraversalSnapshot snapshot)
        {
            List<P6SocketTraversalProof> result =
                new List<P6SocketTraversalProof>(template.Sockets.Count);
            for (int index = 0; index < template.Sockets.Count; index++)
            {
                RoomSocket2D socket = template.Sockets[index];
                if (socket == null)
                {
                    continue;
                }

                bool standable =
                    snapshot != null
                    && snapshot.IsStandable(socket.ValidationAnchor);
                bool oneCellOpening =
                    socket.OpeningSize == Vector2Int.one;
                bool bodyClear =
                    standable
                    && oneCellOpening
                    && HasClearOpening(template, socket, snapshot)
                    && snapshot.IsBodyClearAt(
                        new Vector2(
                            socket.ValidationAnchor.x + 0.5f,
                            socket.ValidationAnchor.y + 0.45f),
                        false);
                result.Add(
                    new P6SocketTraversalProof(
                        socket.SocketId,
                        socket.Side,
                        socket.CellOffset,
                        socket.OpeningSize,
                        socket.TraversalType,
                        socket.MainRouteAllowed,
                        socket.ValidationAnchor,
                        standable,
                        oneCellOpening,
                        bodyClear));
            }

            return result;
        }

        private static List<P6SocketLinkProof> CaptureLinks(
            RoomTemplate2D template,
            RoomTraversalSnapshot snapshot)
        {
            List<RoomSocket2D> mainSockets = new List<RoomSocket2D>();
            List<RoomSocket2D> optionalSockets = new List<RoomSocket2D>();
            for (int index = 0; index < template.Sockets.Count; index++)
            {
                RoomSocket2D socket = template.Sockets[index];
                if (socket == null)
                {
                    continue;
                }

                if (socket.MainRouteAllowed)
                {
                    mainSockets.Add(socket);
                }
                else
                {
                    optionalSockets.Add(socket);
                }
            }

            List<P6SocketLinkProof> result =
                new List<P6SocketLinkProof>();
            for (int fromIndex = 0;
                 fromIndex < mainSockets.Count;
                 fromIndex++)
            {
                for (int toIndex = 0;
                     toIndex < mainSockets.Count;
                     toIndex++)
                {
                    if (fromIndex == toIndex)
                    {
                        continue;
                    }

                    result.Add(
                        CaptureLink(
                            snapshot,
                            mainSockets[fromIndex],
                            mainSockets[toIndex],
                            P6SocketLinkKind.MainRoutePair));
                }
            }

            if (mainSockets.Count == 0)
            {
                return result;
            }

            RoomSocket2D mainAnchor = mainSockets[0];
            for (int index = 0; index < optionalSockets.Count; index++)
            {
                RoomSocket2D optional = optionalSockets[index];
                result.Add(
                    CaptureLink(
                        snapshot,
                        mainAnchor,
                        optional,
                        P6SocketLinkKind.OptionalEntry));
                result.Add(
                    CaptureLink(
                        snapshot,
                        optional,
                        mainAnchor,
                        P6SocketLinkKind.OptionalReturn));
            }

            return result;
        }

        private static P6SocketLinkProof CaptureLink(
            RoomTraversalSnapshot snapshot,
            RoomSocket2D from,
            RoomSocket2D to,
            P6SocketLinkKind kind)
        {
            bool reachable =
                RoomTraversalSolver.TryFindPath(
                    snapshot,
                    from.ValidationAnchor,
                    to.ValidationAnchor,
                    out IReadOnlyList<Vector2Int> path);
            bool bodyClearance =
                reachable && HasPathBodyClearance(snapshot, path);
            int maximumHorizontalStep = 0;
            int maximumRise = 0;
            int maximumDrop = 0;
            bool envelope =
                reachable
                && HasJumpEnvelope(
                    path,
                    out maximumHorizontalStep,
                    out maximumRise,
                    out maximumDrop);
            return new P6SocketLinkProof(
                from.SocketId,
                to.SocketId,
                kind,
                reachable,
                path,
                bodyClearance,
                envelope,
                maximumHorizontalStep,
                maximumRise,
                maximumDrop);
        }

        private static bool HasClearOpening(
            RoomTemplate2D template,
            RoomSocket2D socket,
            RoomTraversalSnapshot snapshot)
        {
            if (socket.OpeningSize.x < 1 || socket.OpeningSize.y < 1)
            {
                return false;
            }

            for (int x = 0; x < socket.OpeningSize.x; x++)
            {
                for (int y = 0; y < socket.OpeningSize.y; y++)
                {
                    Vector2Int cell =
                        socket.CellOffset + new Vector2Int(x, y);
                    if (!template.CellBounds.Contains(cell)
                        || snapshot.IsTerrain(cell)
                        || snapshot.IsHazard(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool HasPathBodyClearance(
            RoomTraversalSnapshot snapshot,
            IReadOnlyList<Vector2Int> path)
        {
            if (snapshot == null || path == null || path.Count == 0)
            {
                return false;
            }

            for (int index = 0; index < path.Count; index++)
            {
                Vector2Int cell = path[index];
                if (!snapshot.IsStandable(cell)
                    || !snapshot.IsBodyClearAt(
                        new Vector2(cell.x + 0.5f, cell.y + 0.45f),
                        false))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HasJumpEnvelope(
            IReadOnlyList<Vector2Int> path,
            out int maximumHorizontalStep,
            out int maximumRise,
            out int maximumDrop)
        {
            maximumHorizontalStep = 0;
            maximumRise = 0;
            maximumDrop = 0;
            if (path == null || path.Count == 0)
            {
                return false;
            }

            for (int index = 1; index < path.Count; index++)
            {
                Vector2Int delta = path[index] - path[index - 1];
                maximumHorizontalStep = Mathf.Max(
                    maximumHorizontalStep,
                    Mathf.Abs(delta.x));
                maximumRise = Mathf.Max(maximumRise, delta.y);
                maximumDrop = Mathf.Max(maximumDrop, -delta.y);
                if (Mathf.Abs(delta.x) > MaximumJumpDistance
                    || delta.y > MaximumJumpRise
                    || -delta.y > MaximumDropDistance)
                {
                    return false;
                }
            }

            return true;
        }

        private static P6SocketSides ToP6Side(RoomSocketSide side)
        {
            switch (side)
            {
                case RoomSocketSide.Left:
                    return P6SocketSides.Left;
                case RoomSocketSide.Right:
                    return P6SocketSides.Right;
                case RoomSocketSide.Bottom:
                    return P6SocketSides.Bottom;
                case RoomSocketSide.Top:
                    return P6SocketSides.Top;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side),
                        side,
                        "Unknown room socket side.");
            }
        }
    }
}

#endif
