using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkEdgeSignatureDefinition
    {
        private readonly IReadOnlyList<string> tags;

        public string EdgeSignatureId { get; }
        public MicrochunkEdgeAxis Axis { get; }
        public string AxisToken => MicrochunkSocketBandDefinition.ToAxisToken(Axis);
        public string BandId { get; }
        public MicrochunkTraversalKind TraversalKind { get; }
        public int GroundEntryHeight { get; }
        public int ClearanceWidth { get; }
        public int ClearanceHeight { get; }
        public MicrochunkToolRequirement ToolRequirement { get; }
        public bool MandatoryAllowed { get; }
        public IReadOnlyList<string> Tags => tags;
        public string Notes { get; }

        public MicrochunkEdgeSignatureDefinition(
            string edgeSignatureId,
            string axisToken,
            string bandId,
            MicrochunkTraversalKind traversalKind,
            int groundEntryHeight,
            int clearanceWidth,
            int clearanceHeight,
            MicrochunkToolRequirement toolRequirement,
            bool mandatoryAllowed,
            IEnumerable<string> tags,
            string notes)
            : this(
                edgeSignatureId,
                ParseAxisToken(axisToken),
                bandId,
                traversalKind,
                groundEntryHeight,
                clearanceWidth,
                clearanceHeight,
                toolRequirement,
                mandatoryAllowed,
                tags,
                notes)
        {
        }

        public MicrochunkEdgeSignatureDefinition(
            string edgeSignatureId,
            MicrochunkEdgeAxis axis,
            string bandId,
            MicrochunkTraversalKind traversalKind,
            int groundEntryHeight,
            int clearanceWidth,
            int clearanceHeight,
            MicrochunkToolRequirement toolRequirement,
            bool mandatoryAllowed,
            IEnumerable<string> tags,
            string notes)
        {
            if (string.IsNullOrWhiteSpace(edgeSignatureId))
            {
                throw new ArgumentException("Edge-signature ID is required.", nameof(edgeSignatureId));
            }

            if (!Enum.IsDefined(typeof(MicrochunkEdgeAxis), axis))
            {
                throw new ArgumentOutOfRangeException(nameof(axis));
            }

            if (!Enum.IsDefined(typeof(MicrochunkTraversalKind), traversalKind))
            {
                throw new ArgumentOutOfRangeException(nameof(traversalKind));
            }

            if (!Enum.IsDefined(typeof(MicrochunkToolRequirement), toolRequirement))
            {
                throw new ArgumentOutOfRangeException(nameof(toolRequirement));
            }

            if (groundEntryHeight < 0) throw new ArgumentOutOfRangeException(nameof(groundEntryHeight));
            if (clearanceWidth < 0) throw new ArgumentOutOfRangeException(nameof(clearanceWidth));
            if (clearanceHeight < 0) throw new ArgumentOutOfRangeException(nameof(clearanceHeight));

            EdgeSignatureId = edgeSignatureId;
            Axis = axis;
            BandId = bandId ?? string.Empty;
            TraversalKind = traversalKind;
            GroundEntryHeight = groundEntryHeight;
            ClearanceWidth = clearanceWidth;
            ClearanceHeight = clearanceHeight;
            ToolRequirement = toolRequirement;
            MandatoryAllowed = mandatoryAllowed;
            this.tags = FreezeTags(tags);
            Notes = notes ?? string.Empty;
        }

        private static IReadOnlyList<string> FreezeTags(IEnumerable<string> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var values = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in source)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Tags cannot contain null, empty, or whitespace values.", nameof(source));
                }

                if (!unique.Add(value))
                {
                    throw new ArgumentException("Tags must be unique.", nameof(source));
                }

                values.Add(value);
            }

            values.Sort(StringComparer.Ordinal);
            return new ReadOnlyCollection<string>(values);
        }

        private static MicrochunkEdgeAxis ParseAxisToken(string token)
        {
            if (!MicrochunkSocketBandDefinition.TryParseAxisToken(token, out var axis))
            {
                throw new ArgumentException(
                    "Axis must be exactly HORIZONTAL_EDGE, VERTICAL_EDGE, or SOLID.",
                    nameof(token));
            }

            return axis;
        }
    }
}
