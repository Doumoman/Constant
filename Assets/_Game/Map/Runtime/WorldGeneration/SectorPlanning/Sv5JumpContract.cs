using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SectorPlanning
{
    public enum Sv5JumpSupportKind
    {
        Solid = 1,
        OneWay = 2
    }

    public enum Sv5JumpValidationState
    {
        Planned = 1,
        StaticScreen = 2,
        PlayerVerified = 3
    }

    public enum Sv5JumpDirection
    {
        LeftToRight = 1,
        RightToLeft = 2
    }

    public enum Sv5JumpMode
    {
        Jump = 1,
        JumpGrab = 2
    }

    public enum Sv5JumpGrabFace
    {
        Left = 1,
        Right = 2
    }

    public readonly struct Sv5JumpPoint : IEquatable<Sv5JumpPoint>, IComparable<Sv5JumpPoint>
    {
        public Sv5JumpPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(Sv5JumpPoint other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Sv5JumpPoint && Equals((Sv5JumpPoint)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public int CompareTo(Sv5JumpPoint other)
        {
            int x = X.CompareTo(other.X);
            return x != 0 ? x : Y.CompareTo(other.Y);
        }

        public override string ToString()
        {
            return X + ":" + Y;
        }
    }

    public sealed class Sv5JumpSupport : IComparable<Sv5JumpSupport>
    {
        public Sv5JumpSupport(
            string supportId,
            Sv5JumpSupportKind kind,
            int x,
            int y,
            int width,
            int height,
            bool activeRoute,
            bool decorativeOnly,
            Sv5JumpValidationState validationState)
        {
            if (string.IsNullOrWhiteSpace(supportId))
            {
                throw new ArgumentException("A stable support ID is required.", nameof(supportId));
            }

            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Support size must be positive.");
            }

            SupportId = supportId;
            Kind = kind;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            ActiveRoute = activeRoute;
            DecorativeOnly = decorativeOnly;
            ValidationState = validationState;
        }

        public string SupportId { get; }
        public Sv5JumpSupportKind Kind { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int TopY { get { return Y + Height; } }
        public int TopXMin { get { return X; } }
        public int TopXMax { get { return X + Width - 1; } }
        public bool ActiveRoute { get; }
        public bool DecorativeOnly { get; }
        public Sv5JumpValidationState ValidationState { get; }
        public string ExportKind { get { return Kind == Sv5JumpSupportKind.Solid ? "SOLID" : "ONE_WAY"; } }

        public bool ContainsCell(Sv5JumpPoint point)
        {
            return point.X >= X && point.X <= TopXMax && point.Y >= Y && point.Y < TopY;
        }

        public bool SupportsFeet(Sv5JumpPoint point)
        {
            return point.Y == TopY && point.X >= TopXMin && point.X <= TopXMax;
        }

        public int CompareTo(Sv5JumpSupport other)
        {
            return other == null ? 1 : string.Compare(SupportId, other.SupportId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get
            {
                return SupportId + "|" + ExportKind + "|" + X + "|" + Y + "|" + Width + "|" + Height +
                    "|" + ActiveRoute + "|" + DecorativeOnly + "|" + Sv5JumpContract.StateName(ValidationState);
            }
        }
    }

    public sealed class Sv5JumpGrabEdge : IComparable<Sv5JumpGrabEdge>
    {
        public Sv5JumpGrabEdge(
            string grabEdgeId,
            string supportId,
            Sv5JumpPoint contact,
            Sv5JumpGrabFace face,
            Sv5JumpDirection approachDirection,
            Sv5JumpPoint hangBody,
            Sv5JumpPoint pullUp,
            bool exposed,
            bool hangBodyClear,
            bool pullUpClear)
        {
            if (string.IsNullOrWhiteSpace(grabEdgeId) || string.IsNullOrWhiteSpace(supportId))
            {
                throw new ArgumentException("Stable grab-edge and support IDs are required.");
            }

            GrabEdgeId = grabEdgeId;
            SupportId = supportId;
            Contact = contact;
            Face = face;
            ApproachDirection = approachDirection;
            HangBody = hangBody;
            PullUp = pullUp;
            Exposed = exposed;
            HangBodyClear = hangBodyClear;
            PullUpClear = pullUpClear;
        }

        public string GrabEdgeId { get; }
        public string SupportId { get; }
        public Sv5JumpPoint Contact { get; }
        public Sv5JumpGrabFace Face { get; }
        public Sv5JumpDirection ApproachDirection { get; }
        public Sv5JumpPoint HangBody { get; }
        public Sv5JumpPoint PullUp { get; }
        public bool Exposed { get; }
        public bool HangBodyClear { get; }
        public bool PullUpClear { get; }
        public bool Safe { get { return Exposed && HangBodyClear && PullUpClear; } }

        public int CompareTo(Sv5JumpGrabEdge other)
        {
            return other == null ? 1 : string.Compare(GrabEdgeId, other.GrabEdgeId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get
            {
                return GrabEdgeId + "|" + SupportId + "|" + Contact + "|" + Face + "|" +
                    ApproachDirection + "|" + HangBody + "|" + PullUp + "|" + Safe;
            }
        }
    }

    public sealed class Sv5JumpValidationEvidence : IComparable<Sv5JumpValidationEvidence>
    {
        public Sv5JumpValidationEvidence(
            string objectId,
            string objectKind,
            Sv5JumpValidationState state,
            string playerRunId = "",
            string playerResult = "")
        {
            if (string.IsNullOrWhiteSpace(objectId) || string.IsNullOrWhiteSpace(objectKind))
            {
                throw new ArgumentException("Validation evidence requires an object ID and kind.");
            }

            if (state == Sv5JumpValidationState.PlayerVerified)
            {
                throw new InvalidOperationException("PLAYER_VERIFIED_REQUIRES_SV5_20_CONCRETE_RUN");
            }

            if (!string.IsNullOrEmpty(playerRunId) || !string.IsNullOrEmpty(playerResult))
            {
                throw new InvalidOperationException("PLAYER_FIELDS_FORBIDDEN_BEFORE_PLAYER_VERIFIED");
            }

            ObjectId = objectId;
            ObjectKind = objectKind;
            State = state;
            PlayerRunId = string.Empty;
            PlayerResult = string.Empty;
        }

        public string ObjectId { get; }
        public string ObjectKind { get; }
        public Sv5JumpValidationState State { get; }
        public string PlayerRunId { get; }
        public string PlayerResult { get; }

        public int CompareTo(Sv5JumpValidationEvidence other)
        {
            return other == null ? 1 : string.Compare(ObjectId, other.ObjectId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get { return ObjectId + "|" + ObjectKind + "|" + Sv5JumpContract.StateName(State); }
        }
    }

    public sealed class Sv5JumpMeasurementProof : IComparable<Sv5JumpMeasurementProof>
    {
        internal Sv5JumpMeasurementProof(
            string linkId,
            int sourceNearFaceX,
            int targetNearFaceX,
            int computedGapAir,
            int sourceTopY,
            int targetTopY,
            int computedRise,
            bool formulaPass)
        {
            LinkId = linkId;
            SourceNearFaceX = sourceNearFaceX;
            TargetNearFaceX = targetNearFaceX;
            ComputedGapAir = computedGapAir;
            SourceTopY = sourceTopY;
            TargetTopY = targetTopY;
            ComputedRise = computedRise;
            FormulaPass = formulaPass;
        }

        public string LinkId { get; }
        public int SourceNearFaceX { get; }
        public int TargetNearFaceX { get; }
        public int ComputedGapAir { get; }
        public int SourceTopY { get; }
        public int TargetTopY { get; }
        public int ComputedRise { get; }
        public bool FormulaPass { get; }

        public int CompareTo(Sv5JumpMeasurementProof other)
        {
            return other == null ? 1 : string.Compare(LinkId, other.LinkId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get
            {
                return LinkId + "|" + SourceNearFaceX + "|" + TargetNearFaceX + "|" + ComputedGapAir +
                    "|" + SourceTopY + "|" + TargetTopY + "|" + ComputedRise + "|" + FormulaPass;
            }
        }
    }

    public sealed class Sv5JumpLink : IComparable<Sv5JumpLink>
    {
        internal Sv5JumpLink(
            string linkId,
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            Sv5JumpPoint takeoff,
            Sv5JumpPoint landing,
            int gapAir,
            int rise,
            Sv5JumpMode mode,
            Sv5JumpGrabEdge grabEdge,
            Sv5JumpValidationState validationState)
        {
            LinkId = linkId;
            Source = source;
            Target = target;
            Direction = direction;
            Takeoff = takeoff;
            Landing = landing;
            GapAir = gapAir;
            Rise = rise;
            Mode = mode;
            GrabEdge = grabEdge;
            ValidationState = validationState;
        }

        public string LinkId { get; }
        public Sv5JumpSupport Source { get; }
        public Sv5JumpSupport Target { get; }
        public Sv5JumpDirection Direction { get; }
        public Sv5JumpPoint Takeoff { get; }
        public Sv5JumpPoint Landing { get; }
        public int GapAir { get; }
        public int Rise { get; }
        public Sv5JumpMode Mode { get; }
        public Sv5JumpGrabEdge GrabEdge { get; }
        public Sv5JumpValidationState ValidationState { get; }

        public int CompareTo(Sv5JumpLink other)
        {
            return other == null ? 1 : string.Compare(LinkId, other.LinkId, StringComparison.Ordinal);
        }

        internal string DigestToken
        {
            get
            {
                return LinkId + "|" + Source.SupportId + "|" + Target.SupportId + "|" + Direction + "|" +
                    Takeoff + "|" + Landing + "|" + GapAir + "|" + Rise + "|" + Mode + "|" +
                    (GrabEdge == null ? string.Empty : GrabEdge.GrabEdgeId) + "|" +
                    Sv5JumpContract.StateName(ValidationState);
            }
        }
    }

    public sealed class Sv5JumpMeasurementResult
    {
        internal Sv5JumpMeasurementResult(
            Sv5JumpLink link,
            Sv5JumpMeasurementProof proof,
            IEnumerable<string> rejectionReasons)
        {
            Link = link;
            Proof = proof;
            RejectionReasons = Array.AsReadOnly((rejectionReasons ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        }

        public Sv5JumpLink Link { get; }
        public Sv5JumpMeasurementProof Proof { get; }
        public IReadOnlyList<string> RejectionReasons { get; }
        public bool Success { get { return Link != null && RejectionReasons.Count == 0; } }
    }

    public static class Sv5JumpRejectionReason
    {
        public const string MissingSupport = "MISSING_SUPPORT";
        public const string DuplicateSupport = "SOURCE_AND_TARGET_MUST_DIFFER";
        public const string InactiveSupport = "ACTIVE_ROUTE_SUPPORT_REQUIRED";
        public const string DecorativeSupport = "DECORATIVE_SUPPORT_REJECTED";
        public const string TakeoffNotSupported = "TAKEOFF_NOT_ON_SOURCE_TOP";
        public const string LandingNotSupported = "LANDING_NOT_ON_TARGET_TOP";
        public const string DeclaredGapMismatch = "DECLARED_GAP_AIR_MISMATCH";
        public const string DeclaredRiseMismatch = "DECLARED_RISE_MISMATCH";
        public const string NormalRiseExceeded = "NORMAL_JUMP_RISE_EXCEEDS_ONE";
        public const string GrabRiseExceeded = "JUMP_GRAB_RISE_EXCEEDS_TWO";
        public const string UnexpectedGrab = "NORMAL_JUMP_MUST_NOT_REFERENCE_GRAB";
        public const string MissingGrab = "JUMP_GRAB_REQUIRES_EDGE";
        public const string GrabTargetMismatch = "GRAB_EDGE_TARGET_MISMATCH";
        public const string GrabRequiresSolid = "GRAB_REQUIRES_SOLID_TARGET";
        public const string GrabFaceNotExposed = "GRAB_FACE_NOT_EXPOSED";
        public const string GrabFaceMismatch = "GRAB_FACE_DIRECTION_MISMATCH";
        public const string GrabContactMismatch = "GRAB_CONTACT_NOT_TARGET_EXPOSED_CORNER";
        public const string MissingHangClearance = "HANG_BODY_CLEARANCE_MISSING";
        public const string MissingPullUpSpace = "PULL_UP_SPACE_MISSING";
        public const string HangBodyCellInvalid = "HANG_BODY_CELL_INVALID";
        public const string PullUpDestinationInvalid = "PULL_UP_DESTINATION_INVALID";
        public const string PrematurePlayerState = "PLAYER_VERIFIED_REQUIRES_SV5_20";
    }

    public sealed class Sv5JumpContractFixture
    {
        internal Sv5JumpContractFixture(
            IEnumerable<Sv5JumpSupport> supports,
            IEnumerable<Sv5JumpGrabEdge> grabEdges,
            IEnumerable<Sv5JumpLink> links,
            IEnumerable<Sv5JumpMeasurementProof> proofs,
            IEnumerable<Sv5JumpValidationEvidence> validationStates)
        {
            Supports = Array.AsReadOnly(supports.OrderBy(value => value).ToArray());
            GrabEdges = Array.AsReadOnly(grabEdges.OrderBy(value => value).ToArray());
            Links = Array.AsReadOnly(links.OrderBy(value => value).ToArray());
            MeasurementProofs = Array.AsReadOnly(proofs.OrderBy(value => value).ToArray());
            ValidationStates = Array.AsReadOnly(validationStates.OrderBy(value => value).ToArray());
            Diagnostics = Array.AsReadOnly(Validate().ToArray());
            Digest = Hash(string.Join("\n", Supports.Select(value => value.DigestToken)
                .Concat(GrabEdges.Select(value => value.DigestToken))
                .Concat(Links.Select(value => value.DigestToken))
                .Concat(MeasurementProofs.Select(value => value.DigestToken))
                .Concat(ValidationStates.Select(value => value.DigestToken))));
        }

        public IReadOnlyList<Sv5JumpSupport> Supports { get; }
        public IReadOnlyList<Sv5JumpGrabEdge> GrabEdges { get; }
        public IReadOnlyList<Sv5JumpLink> Links { get; }
        public IReadOnlyList<Sv5JumpMeasurementProof> MeasurementProofs { get; }
        public IReadOnlyList<Sv5JumpValidationEvidence> ValidationStates { get; }
        public IReadOnlyList<string> Diagnostics { get; }
        public string Digest { get; }
        public int WholeWorldBuildsInNewTargetedTests { get { return 0; } }
        public bool ReverseCompletionRequired { get { return false; } }
        public bool JumpContractReady { get { return Diagnostics.Count == 0; } }
        public bool JumpSolidGeometryReady { get { return false; } }
        public bool JumpGrabGeometryReady { get { return false; } }
        public bool ComposedGeometryReady { get { return false; } }
        public bool PlayerVerified { get { return false; } }

        public Sv5JumpLink Link(string id)
        {
            return Links.Single(value => string.Equals(value.LinkId, id, StringComparison.Ordinal));
        }

        private IEnumerable<string> Validate()
        {
            if (Supports.Select(value => value.SupportId).Distinct(StringComparer.Ordinal).Count() != Supports.Count)
            {
                yield return "DUPLICATE_SUPPORT_ID";
            }

            if (Links.Select(value => value.LinkId).Distinct(StringComparer.Ordinal).Count() != Links.Count)
            {
                yield return "DUPLICATE_LINK_ID";
            }

            if (!Supports.Any(value => value.Kind == Sv5JumpSupportKind.Solid && value.ActiveRoute && !value.DecorativeOnly))
            {
                yield return "ACTIVE_SOLID_REQUIRED";
            }

            if (!Supports.Any(value => value.Kind == Sv5JumpSupportKind.OneWay && value.ActiveRoute && !value.DecorativeOnly))
            {
                yield return "ACTIVE_ONE_WAY_REQUIRED";
            }

            if (Links.Count != 3 || !Links.Any(value => value.LinkId == "J1" && value.GapAir == 3 && value.Rise == 1 && value.Mode == Sv5JumpMode.Jump) ||
                !Links.Any(value => value.LinkId == "J2" && value.GapAir == 4 && value.Rise == 1 && value.Mode == Sv5JumpMode.Jump) ||
                !Links.Any(value => value.LinkId == "J3" && value.GapAir == 2 && value.Rise == 2 && value.Mode == Sv5JumpMode.JumpGrab))
            {
                yield return "CANONICAL_LINK_SET_MISMATCH";
            }

            Sv5JumpLink j3 = Links.SingleOrDefault(value => value.LinkId == "J3");
            if (j3 == null || j3.Target.Kind != Sv5JumpSupportKind.Solid || j3.GrabEdge == null || !j3.GrabEdge.Safe)
            {
                yield return "J3_REAL_SOLID_GRAB_REQUIRED";
            }

            if (MeasurementProofs.Count != Links.Count || MeasurementProofs.Any(value => !value.FormulaPass))
            {
                yield return "MEASUREMENT_PROOF_MISMATCH";
            }

            if (ValidationStates.Any(value => value.State == Sv5JumpValidationState.PlayerVerified))
            {
                yield return Sv5JumpRejectionReason.PrematurePlayerState;
            }
        }

        private static string Hash(string value)
        {
            using (SHA256 algorithm = SHA256.Create())
            {
                return BitConverter.ToString(algorithm.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }

    public static class Sv5JumpContract
    {
        public const int MaximumNormalRiseCells = 1;
        public const int MaximumJumpGrabRiseCells = 2;

        public static Sv5JumpContractFixture CreateCanonicalLocalFixture()
        {
            var supports = new[]
            {
                Support("J1_SOURCE_SOLID", Sv5JumpSupportKind.Solid, 0, 0, 2, 1),
                Support("J1_TARGET_ONE_WAY", Sv5JumpSupportKind.OneWay, 5, 1, 2, 1),
                Support("J2_TARGET_SOLID", Sv5JumpSupportKind.Solid, 8, 1, 2, 1),
                Support("J2_SOURCE_ONE_WAY", Sv5JumpSupportKind.OneWay, 14, 0, 2, 1),
                Support("J3_SOURCE_ONE_WAY", Sv5JumpSupportKind.OneWay, 20, 0, 2, 1),
                Support("J3_TARGET_SOLID", Sv5JumpSupportKind.Solid, 24, 1, 2, 2)
            };

            Sv5JumpGrabEdge j3Grab = new Sv5JumpGrabEdge(
                "J3_TARGET_LEFT_EXPOSED",
                "J3_TARGET_SOLID",
                new Sv5JumpPoint(24, 2),
                Sv5JumpGrabFace.Left,
                Sv5JumpDirection.LeftToRight,
                new Sv5JumpPoint(23, 2),
                new Sv5JumpPoint(24, 3),
                true,
                true,
                true);

            var attempts = new[]
            {
                Measure("J1", supports[0], supports[1], Sv5JumpDirection.LeftToRight,
                    new Sv5JumpPoint(1, 1), new Sv5JumpPoint(5, 2), Sv5JumpMode.Jump,
                    null, Sv5JumpValidationState.Planned, 3, 1),
                Measure("J2", supports[3], supports[2], Sv5JumpDirection.RightToLeft,
                    new Sv5JumpPoint(14, 1), new Sv5JumpPoint(9, 2), Sv5JumpMode.Jump,
                    null, Sv5JumpValidationState.Planned, 4, 1),
                Measure("J3", supports[4], supports[5], Sv5JumpDirection.LeftToRight,
                    new Sv5JumpPoint(21, 1), new Sv5JumpPoint(24, 3), Sv5JumpMode.JumpGrab,
                    j3Grab, Sv5JumpValidationState.Planned, 2, 2)
            };

            if (attempts.Any(value => !value.Success))
            {
                throw new InvalidOperationException("Canonical jump fixture rejected: " +
                    string.Join(",", attempts.SelectMany(value => value.RejectionReasons)));
            }

            var states = supports.Select(value => new Sv5JumpValidationEvidence(
                    value.SupportId, "SUPPORT", Sv5JumpValidationState.StaticScreen))
                .Concat(new[]
                {
                    new Sv5JumpValidationEvidence(j3Grab.GrabEdgeId, "GRAB_EDGE", Sv5JumpValidationState.StaticScreen)
                })
                .Concat(attempts.Select(value => new Sv5JumpValidationEvidence(
                    value.Link.LinkId, "LINK", Sv5JumpValidationState.Planned)));

            return new Sv5JumpContractFixture(
                supports,
                new[] { j3Grab },
                attempts.Select(value => value.Link),
                attempts.Select(value => value.Proof),
                states);
        }

        public static Sv5JumpMeasurementResult Measure(
            string linkId,
            Sv5JumpSupport source,
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            Sv5JumpPoint takeoff,
            Sv5JumpPoint landing,
            Sv5JumpMode mode,
            Sv5JumpGrabEdge grabEdge,
            Sv5JumpValidationState validationState,
            int? declaredGapAir = null,
            int? declaredRise = null)
        {
            var reasons = new List<string>();
            if (source == null || target == null)
            {
                reasons.Add(Sv5JumpRejectionReason.MissingSupport);
                return new Sv5JumpMeasurementResult(null, null, reasons);
            }

            int sourceNearFace = direction == Sv5JumpDirection.LeftToRight ? source.TopXMax : source.TopXMin;
            int targetNearFace = direction == Sv5JumpDirection.LeftToRight ? target.TopXMin : target.TopXMax;
            int gapAir = direction == Sv5JumpDirection.LeftToRight
                ? Math.Max(0, targetNearFace - sourceNearFace - 1)
                : Math.Max(0, sourceNearFace - targetNearFace - 1);
            int rise = target.TopY - source.TopY;
            bool formulaPass = (!declaredGapAir.HasValue || declaredGapAir.Value == gapAir) &&
                (!declaredRise.HasValue || declaredRise.Value == rise);
            var proof = new Sv5JumpMeasurementProof(linkId, sourceNearFace, targetNearFace, gapAir,
                source.TopY, target.TopY, rise, formulaPass);

            Add(reasons, source.SupportId == target.SupportId, Sv5JumpRejectionReason.DuplicateSupport);
            Add(reasons, !source.ActiveRoute || !target.ActiveRoute, Sv5JumpRejectionReason.InactiveSupport);
            Add(reasons, source.DecorativeOnly || target.DecorativeOnly, Sv5JumpRejectionReason.DecorativeSupport);
            Add(reasons, !source.SupportsFeet(takeoff), Sv5JumpRejectionReason.TakeoffNotSupported);
            Add(reasons, !target.SupportsFeet(landing), Sv5JumpRejectionReason.LandingNotSupported);
            Add(reasons, declaredGapAir.HasValue && declaredGapAir.Value != gapAir,
                Sv5JumpRejectionReason.DeclaredGapMismatch);
            Add(reasons, declaredRise.HasValue && declaredRise.Value != rise,
                Sv5JumpRejectionReason.DeclaredRiseMismatch);
            Add(reasons, validationState == Sv5JumpValidationState.PlayerVerified,
                Sv5JumpRejectionReason.PrematurePlayerState);

            if (mode == Sv5JumpMode.Jump)
            {
                Add(reasons, rise > MaximumNormalRiseCells, Sv5JumpRejectionReason.NormalRiseExceeded);
                Add(reasons, grabEdge != null, Sv5JumpRejectionReason.UnexpectedGrab);
            }
            else
            {
                Add(reasons, rise > MaximumJumpGrabRiseCells, Sv5JumpRejectionReason.GrabRiseExceeded);
                if (grabEdge == null)
                {
                    reasons.Add(Sv5JumpRejectionReason.MissingGrab);
                }
                else
                {
                    ValidateGrab(target, direction, grabEdge, reasons);
                }
            }

            Sv5JumpLink link = reasons.Count == 0
                ? new Sv5JumpLink(linkId, source, target, direction, takeoff, landing, gapAir, rise,
                    mode, grabEdge, validationState)
                : null;
            return new Sv5JumpMeasurementResult(link, proof, reasons);
        }

        public static string StateName(Sv5JumpValidationState state)
        {
            if (state == Sv5JumpValidationState.Planned)
            {
                return "PLANNED";
            }

            if (state == Sv5JumpValidationState.StaticScreen)
            {
                return "STATIC_SCREEN";
            }

            return "PLAYER_VERIFIED";
        }

        public static string DirectionName(Sv5JumpDirection direction)
        {
            return direction == Sv5JumpDirection.LeftToRight ? "LEFT_TO_RIGHT" : "RIGHT_TO_LEFT";
        }

        public static string ModeName(Sv5JumpMode mode)
        {
            return mode == Sv5JumpMode.Jump ? "JUMP" : "JUMP_GRAB";
        }

        public static string FaceName(Sv5JumpGrabFace face)
        {
            return face == Sv5JumpGrabFace.Left ? "LEFT" : "RIGHT";
        }

        private static Sv5JumpSupport Support(
            string id,
            Sv5JumpSupportKind kind,
            int x,
            int y,
            int width,
            int height)
        {
            return new Sv5JumpSupport(id, kind, x, y, width, height, true, false,
                Sv5JumpValidationState.StaticScreen);
        }

        private static void ValidateGrab(
            Sv5JumpSupport target,
            Sv5JumpDirection direction,
            Sv5JumpGrabEdge grab,
            ICollection<string> reasons)
        {
            Add(reasons, !string.Equals(grab.SupportId, target.SupportId, StringComparison.Ordinal),
                Sv5JumpRejectionReason.GrabTargetMismatch);
            Add(reasons, target.Kind != Sv5JumpSupportKind.Solid,
                Sv5JumpRejectionReason.GrabRequiresSolid);
            Add(reasons, !grab.Exposed, Sv5JumpRejectionReason.GrabFaceNotExposed);
            Add(reasons, !grab.HangBodyClear, Sv5JumpRejectionReason.MissingHangClearance);
            Add(reasons, !grab.PullUpClear, Sv5JumpRejectionReason.MissingPullUpSpace);

            Sv5JumpGrabFace expectedFace = direction == Sv5JumpDirection.LeftToRight
                ? Sv5JumpGrabFace.Left
                : Sv5JumpGrabFace.Right;
            Add(reasons, grab.Face != expectedFace || grab.ApproachDirection != direction,
                Sv5JumpRejectionReason.GrabFaceMismatch);

            int expectedContactX = grab.Face == Sv5JumpGrabFace.Left ? target.TopXMin : target.TopXMax;
            Add(reasons, grab.Contact.X != expectedContactX || grab.Contact.Y != target.TopY - 1 ||
                !target.ContainsCell(grab.Contact), Sv5JumpRejectionReason.GrabContactMismatch);

            int expectedHangX = grab.Face == Sv5JumpGrabFace.Left ? target.TopXMin - 1 : target.TopXMax + 1;
            Add(reasons, grab.HangBody.X != expectedHangX || grab.HangBody.Y != grab.Contact.Y ||
                target.ContainsCell(grab.HangBody), Sv5JumpRejectionReason.HangBodyCellInvalid);
            Add(reasons, grab.PullUp.Y != target.TopY || grab.PullUp.X < target.TopXMin ||
                grab.PullUp.X > target.TopXMax, Sv5JumpRejectionReason.PullUpDestinationInvalid);
        }

        private static void Add(ICollection<string> reasons, bool condition, string reason)
        {
            if (condition)
            {
                reasons.Add(reason);
            }
        }
    }
}
