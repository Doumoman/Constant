using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.MapAuthoring.Microchunks
{
    public static class MicrochunkAuthoringGridLayer
    {
        private static readonly IReadOnlyList<MicrochunkTileLayer> OrderedLayerValues =
            new ReadOnlyCollection<MicrochunkTileLayer>(new[]
            {
                MicrochunkTileLayer.GroundSolid,
                MicrochunkTileLayer.OneWay,
                MicrochunkTileLayer.Breakable,
                MicrochunkTileLayer.Hazard,
                MicrochunkTileLayer.Liquid,
                MicrochunkTileLayer.DecorationBack,
                MicrochunkTileLayer.DecorationFront,
                MicrochunkTileLayer.Marker
            });

        public static IReadOnlyList<MicrochunkTileLayer> OrderedLayers => OrderedLayerValues;
        public static int Count => OrderedLayerValues.Count;

        public static MicrochunkTileLayer At(int index)
        {
            if (index < 0 || index >= OrderedLayerValues.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return OrderedLayerValues[index];
        }

        public static int IndexOf(MicrochunkTileLayer layer)
        {
            if (!Enum.IsDefined(typeof(MicrochunkTileLayer), layer))
            {
                throw new ArgumentOutOfRangeException(nameof(layer));
            }

            var index = (int)layer;
            if (index < 0 || index >= OrderedLayerValues.Count || OrderedLayerValues[index] != layer)
            {
                throw new ArgumentOutOfRangeException(nameof(layer));
            }

            return index;
        }
    }
}
