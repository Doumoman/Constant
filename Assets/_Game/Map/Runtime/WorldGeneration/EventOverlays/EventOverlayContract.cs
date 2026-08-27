using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using StarNight.Map.WorldGeneration.Activities;
using StarNight.Map.WorldGeneration.Pipeline;
using StarNight.Map.WorldGeneration.TerrainClusters;

namespace StarNight.Map.WorldGeneration.EventOverlays
{
    public readonly struct EventOverlayId : IEquatable<EventOverlayId>, IComparable<EventOverlayId>
    {
        private readonly string value;
        public EventOverlayId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(EventOverlayId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(EventOverlayId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EventOverlayId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(EventOverlayId left, EventOverlayId right) => left.Equals(right);
        public static bool operator !=(EventOverlayId left, EventOverlayId right) => !left.Equals(right);
    }

    public readonly struct EventMarkerId : IEquatable<EventMarkerId>, IComparable<EventMarkerId>
    {
        private readonly string value;
        public EventMarkerId(string value) { this.value = value; }
        public string Value => value ?? string.Empty;
        public int CompareTo(EventMarkerId other) => string.Compare(Value, other.Value, StringComparison.Ordinal);
        public bool Equals(EventMarkerId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is EventMarkerId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;
        public static bool operator ==(EventMarkerId left, EventMarkerId right) => left.Equals(right);
        public static bool operator !=(EventMarkerId left, EventMarkerId right) => !left.Equals(right);
    }

    public enum EventOverlayKind
    {
        Npc = 1,
        Reward = 2,
        State = 3,
        Cosmetic = 4,
        Empty = 5,
    }

    public enum EventMarkerOperation
    {
        EnableMarker = 1,
        DisableMarker = 2,
        SpawnNpc = 3,
        SpawnReward = 4,
        SetState = 5,
    }

    public sealed class EventMarkerAssignment
    {
        public EventMarkerAssignment(EventMarkerId targetMarkerId, EventMarkerOperation operation, string payloadId)
        {
            TargetMarkerId = targetMarkerId;
            Operation = operation;
            PayloadId = payloadId ?? string.Empty;
        }

        public EventMarkerId TargetMarkerId { get; }
        public EventMarkerOperation Operation { get; }
        public string PayloadId { get; }
    }

    public sealed class EventOverlayContract
    {
        private readonly ReadOnlyCollection<EventMarkerAssignment> assignments;

        public EventOverlayContract(
            EventOverlayId id,
            EventOverlayKind kind,
            TerrainClusterId terrainClusterId,
            ActivityStructureId? activityStructureId,
            IEnumerable<EventMarkerAssignment> assignments,
            string displayText = null)
        {
            Id = id;
            Kind = kind;
            TerrainClusterId = terrainClusterId;
            ActivityStructureId = activityStructureId;
            var copy = assignments == null ? Array.Empty<EventMarkerAssignment>() : assignments.ToArray();
            Array.Sort(copy, CompareAssignments);
            this.assignments = new ReadOnlyCollection<EventMarkerAssignment>(copy);
            DisplayText = displayText ?? string.Empty;
        }

        public EventOverlayId Id { get; }
        public EventOverlayKind Kind { get; }
        public TerrainClusterId TerrainClusterId { get; }
        public ActivityStructureId? ActivityStructureId { get; }
        public IReadOnlyList<EventMarkerAssignment> Assignments => assignments;
        public string DisplayText { get; }

        public string GetCanonicalDigest(
            TerrainClusterContract staticShell,
            ActivityStructureContract activity,
            IEnumerable<EventMarkerId> knownMarkerIds,
            EventOverlayRemovalEvidence removalEvidence)
        {
            var result = EventOverlayValidator.Validate(this, staticShell, activity, knownMarkerIds, removalEvidence);
            if (!result.IsValid)
                throw new InvalidOperationException("Cannot compute a published digest for an invalid EventOverlay contract.");
            return result.CanonicalDigest;
        }

        private static int CompareAssignments(EventMarkerAssignment left, EventMarkerAssignment right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return 1;
            if (right == null) return -1;
            var comparison = left.TargetMarkerId.CompareTo(right.TargetMarkerId);
            if (comparison != 0) return comparison;
            comparison = ((int)left.Operation).CompareTo((int)right.Operation);
            return comparison != 0 ? comparison : string.Compare(left.PayloadId, right.PayloadId, StringComparison.Ordinal);
        }
    }

    public sealed class EventOverlayRemovalEvidence
    {
        public EventOverlayRemovalEvidence(
            string staticShellDigestBeforeRemoval,
            string staticShellDigestAfterRemoval,
            string mandatoryPathDigestBeforeRemoval,
            string mandatoryPathDigestAfterRemoval,
            AccessClass accessClassBeforeRemoval,
            AccessClass accessClassAfterRemoval,
            string activityRemovalSafetyDigestBeforeRemoval,
            string activityRemovalSafetyDigestAfterRemoval,
            bool declaresNonMarkerMutation = false)
        {
            StaticShellDigestBeforeRemoval = staticShellDigestBeforeRemoval ?? string.Empty;
            StaticShellDigestAfterRemoval = staticShellDigestAfterRemoval ?? string.Empty;
            MandatoryPathDigestBeforeRemoval = mandatoryPathDigestBeforeRemoval ?? string.Empty;
            MandatoryPathDigestAfterRemoval = mandatoryPathDigestAfterRemoval ?? string.Empty;
            AccessClassBeforeRemoval = accessClassBeforeRemoval;
            AccessClassAfterRemoval = accessClassAfterRemoval;
            ActivityRemovalSafetyDigestBeforeRemoval = activityRemovalSafetyDigestBeforeRemoval ?? string.Empty;
            ActivityRemovalSafetyDigestAfterRemoval = activityRemovalSafetyDigestAfterRemoval ?? string.Empty;
            DeclaresNonMarkerMutation = declaresNonMarkerMutation;
        }

        public string StaticShellDigestBeforeRemoval { get; }
        public string StaticShellDigestAfterRemoval { get; }
        public string MandatoryPathDigestBeforeRemoval { get; }
        public string MandatoryPathDigestAfterRemoval { get; }
        public AccessClass AccessClassBeforeRemoval { get; }
        public AccessClass AccessClassAfterRemoval { get; }
        public string ActivityRemovalSafetyDigestBeforeRemoval { get; }
        public string ActivityRemovalSafetyDigestAfterRemoval { get; }
        public bool DeclaresNonMarkerMutation { get; }
    }
}
