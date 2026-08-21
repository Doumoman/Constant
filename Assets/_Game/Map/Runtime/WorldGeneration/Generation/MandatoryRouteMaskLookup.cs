using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteMaskLookup
    {
        private readonly IReadOnlyList<MandatoryRouteMaskRecord> records;
        private readonly IReadOnlyDictionary<MandatoryRouteMaskId, MandatoryRouteMaskRecord> byId;
        private readonly IReadOnlyDictionary<int, MandatoryRouteMaskRecord> byRouteType;
        private readonly IReadOnlyDictionary<MandatoryRouteOpenMask, MandatoryRouteMaskRecord> byOpenMask;
        private readonly IReadOnlyDictionary<MandatoryRouteMaskKind, MandatoryRouteMaskRecord> byKind;

        internal MandatoryRouteMaskLookup(IEnumerable<MandatoryRouteMaskRecord> sourceRecords)
        {
            if (sourceRecords == null) throw new ArgumentNullException(nameof(sourceRecords));
            var values = new List<MandatoryRouteMaskRecord>(sourceRecords);
            values.Sort((left, right) => left.RouteType.CompareTo(right.RouteType));
            if (values.Count != 3) throw new ArgumentException("Exactly three mandatory route masks are required.", nameof(sourceRecords));
            var ids = new Dictionary<MandatoryRouteMaskId, MandatoryRouteMaskRecord>();
            var types = new Dictionary<int, MandatoryRouteMaskRecord>();
            var masks = new Dictionary<MandatoryRouteOpenMask, MandatoryRouteMaskRecord>();
            var kinds = new Dictionary<MandatoryRouteMaskKind, MandatoryRouteMaskRecord>();
            for (var index = 0; index < values.Count; index++)
            {
                var record = values[index];
                if (record == null || record.RouteType != index + 1 || record.Kind != (MandatoryRouteMaskKind)index ||
                    !MandatoryRouteMaskRecord.Matches(record.Kind, record.OpenMask) ||
                    !string.Equals(record.MaskId.Value, ExpectedId(record.Kind), StringComparison.Ordinal))
                    throw new ArgumentException("Mandatory route mask records must be exact Type1, Type2, Type3.", nameof(sourceRecords));
                if (!ids.TryAdd(record.MaskId, record) || !types.TryAdd(record.RouteType, record) ||
                    !masks.TryAdd(record.OpenMask, record) || !kinds.TryAdd(record.Kind, record))
                    throw new ArgumentException("Mandatory route mask identities must be unique.", nameof(sourceRecords));
            }
            records = new ReadOnlyCollection<MandatoryRouteMaskRecord>(values);
            byId = new ReadOnlyDictionary<MandatoryRouteMaskId, MandatoryRouteMaskRecord>(ids);
            byRouteType = new ReadOnlyDictionary<int, MandatoryRouteMaskRecord>(types);
            byOpenMask = new ReadOnlyDictionary<MandatoryRouteOpenMask, MandatoryRouteMaskRecord>(masks);
            byKind = new ReadOnlyDictionary<MandatoryRouteMaskKind, MandatoryRouteMaskRecord>(kinds);
            Type1 = byKind[MandatoryRouteMaskKind.Type1];
            Type2 = byKind[MandatoryRouteMaskKind.Type2];
            Type3 = byKind[MandatoryRouteMaskKind.Type3];
        }

        public IReadOnlyList<MandatoryRouteMaskRecord> Records => records;
        public int Count => records.Count;
        public MandatoryRouteMaskRecord Type1 { get; }
        public MandatoryRouteMaskRecord Type2 { get; }
        public MandatoryRouteMaskRecord Type3 { get; }
        public bool TryGetById(MandatoryRouteMaskId id, out MandatoryRouteMaskRecord record) => byId.TryGetValue(id, out record);
        public bool TryGetByRouteType(int routeType, out MandatoryRouteMaskRecord record) => byRouteType.TryGetValue(routeType, out record);
        public bool TryGetByOpenMask(MandatoryRouteOpenMask openMask, out MandatoryRouteMaskRecord record) => byOpenMask.TryGetValue(openMask, out record);

        public MandatoryRouteMaskRecord GetRequired(MandatoryRouteMaskKind kind)
        {
            if (!byKind.TryGetValue(kind, out var record)) throw new ArgumentOutOfRangeException(nameof(kind));
            return record;
        }

        private static string ExpectedId(MandatoryRouteMaskKind kind)
        {
            switch (kind)
            {
                case MandatoryRouteMaskKind.Type1: return "ROUTE_T1_LR";
                case MandatoryRouteMaskKind.Type2: return "ROUTE_T2_LRD";
                case MandatoryRouteMaskKind.Type3: return "ROUTE_T3_LRU";
                default: return string.Empty;
            }
        }
    }
}
