using System;

namespace StarNight.Map.WorldGeneration.Microchunks
{
    public sealed class MicrochunkTransformResult
    {
        public MicrochunkDefinition SourceDefinition { get; }
        public MicrochunkDefinition Definition { get; }
        public MicrochunkDefinition TransformedDefinition => Definition;
        public MicrochunkTransform Transform { get; }
        public int TileCellCount => Definition.TileCells.Count;
        public int SocketCount => Definition.Sockets.Count;
        public int ObjectSlotCount => Definition.ObjectSlots.Count;

        internal MicrochunkTransformResult(
            MicrochunkDefinition sourceDefinition,
            MicrochunkDefinition definition,
            MicrochunkTransform transform)
        {
            SourceDefinition = sourceDefinition ?? throw new ArgumentNullException(nameof(sourceDefinition));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            MicrochunkTransformUtility.ValidateTransform(transform);
            Transform = transform;
        }
    }
}
