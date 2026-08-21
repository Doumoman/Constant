using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvHexValue
    {
        private readonly ReadOnlyCollection<byte> bytes;

        internal CsvHexValue(string originalValue, IEnumerable<byte> sourceBytes)
        {
            OriginalValue = originalValue ?? throw new ArgumentNullException(nameof(originalValue));
            bytes = new ReadOnlyCollection<byte>(
                new List<byte>(sourceBytes ?? throw new ArgumentNullException(nameof(sourceBytes))));
        }

        public string OriginalValue { get; }

        public IReadOnlyList<byte> Bytes => bytes;
    }
}
