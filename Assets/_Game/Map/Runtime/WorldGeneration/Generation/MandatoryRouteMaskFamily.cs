using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public sealed class MandatoryRouteMaskFamily
    {
        public sealed class Entry
        {
            internal Entry(string maskId, int routeType, bool openLeft, bool openRight, bool openUp, bool openDown)
            {
                MaskId = maskId;
                RouteType = routeType;
                OpenLeft = openLeft;
                OpenRight = openRight;
                OpenUp = openUp;
                OpenDown = openDown;
            }

            public string MaskId { get; }
            public int RouteType { get; }
            public bool OpenLeft { get; }
            public bool OpenRight { get; }
            public bool OpenUp { get; }
            public bool OpenDown { get; }
        }

        public const string Type1Id = "ROUTE_T1_LR";
        public const string Type2Id = "ROUTE_T2_LRD";
        public const string Type3Id = "ROUTE_T3_LRU";
        public const string Type4UdId = "ROUTE_T4_UD";
        public const string Type4LudId = "ROUTE_T4_LUD";
        public const string Type4RudId = "ROUTE_T4_RUD";
        public const string Type4LrudId = "ROUTE_T4_LRUD";

        private readonly IReadOnlyList<Entry> entries;
        private readonly IReadOnlyDictionary<string, Entry> byId;
        private readonly IReadOnlyDictionary<int, Entry> byBits;

        public MandatoryRouteMaskFamily(MandatoryRouteMaskLookup lookup)
        {
            SourceLookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
            if (lookup.Count != 3 || lookup.Type1.MaskId.Value != Type1Id || lookup.Type2.MaskId.Value != Type2Id || lookup.Type3.MaskId.Value != Type3Id)
                throw new ArgumentException("Type1/2/3 lookup identities do not match the mandatory family.", nameof(lookup));

            var values = new[]
            {
                new Entry(Type1Id, 1, true, true, false, false),
                new Entry(Type2Id, 2, true, true, false, true),
                new Entry(Type3Id, 3, true, true, true, false),
                new Entry(Type4UdId, 4, false, false, true, true),
                new Entry(Type4LudId, 4, true, false, true, true),
                new Entry(Type4RudId, 4, false, true, true, true),
                new Entry(Type4LrudId, 4, true, true, true, true)
            };
            var ids = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var masks = new Dictionary<int, Entry>();
            foreach (var value in values)
            {
                ids.Add(value.MaskId, value);
                masks.Add(Bits(value.OpenLeft, value.OpenRight, value.OpenUp, value.OpenDown), value);
            }
            entries = new ReadOnlyCollection<Entry>(values);
            byId = new ReadOnlyDictionary<string, Entry>(ids);
            byBits = new ReadOnlyDictionary<int, Entry>(masks);
        }

        public MandatoryRouteMaskLookup SourceLookup { get; }
        public IReadOnlyList<Entry> Entries => entries;
        public int Count => entries.Count;
        public bool TryGetById(string maskId, out Entry entry) => byId.TryGetValue(maskId ?? string.Empty, out entry);
        public bool TryResolve(bool openLeft, bool openRight, bool openUp, bool openDown, out Entry entry) =>
            byBits.TryGetValue(Bits(openLeft, openRight, openUp, openDown), out entry);

        private static int Bits(bool left, bool right, bool up, bool down) =>
            (left ? 1 : 0) | (right ? 2 : 0) | (up ? 4 : 0) | (down ? 8 : 0);
    }
}
