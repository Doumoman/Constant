using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Data
{
    public sealed class CsvParsedValue
    {
        private readonly object payload;

        private CsvParsedValue(CsvSchemaDataType dataType, bool isEmpty, object payload)
        {
            DataType = dataType;
            IsEmpty = isEmpty;
            this.payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public CsvSchemaDataType DataType { get; }

        public bool IsEmpty { get; }

        public string StringValue
        {
            get
            {
                RequireOneOf(CsvSchemaDataType.String, CsvSchemaDataType.Id, CsvSchemaDataType.Enum);
                return (string)payload;
            }
        }

        public string IdValue
        {
            get
            {
                Require(CsvSchemaDataType.Id);
                return (string)payload;
            }
        }

        public string EnumValue
        {
            get
            {
                Require(CsvSchemaDataType.Enum);
                return (string)payload;
            }
        }

        public int IntegerValue
        {
            get
            {
                Require(CsvSchemaDataType.Int);
                return (int)payload;
            }
        }

        public int IntValue => IntegerValue;

        public ulong UnsignedIntegerValue
        {
            get
            {
                Require(CsvSchemaDataType.ULong);
                return (ulong)payload;
            }
        }

        public ulong ULongValue => UnsignedIntegerValue;

        public float FloatValue
        {
            get
            {
                Require(CsvSchemaDataType.Float);
                return (float)payload;
            }
        }

        public bool BooleanValue
        {
            get
            {
                Require(CsvSchemaDataType.Bool);
                return (bool)payload;
            }
        }

        public bool BoolValue => BooleanValue;

        public CsvHexValue HexValue
        {
            get
            {
                Require(CsvSchemaDataType.Hex);
                return (CsvHexValue)payload;
            }
        }

        public DateTimeOffset DateTimeValue
        {
            get
            {
                Require(CsvSchemaDataType.DateTime);
                return (DateTimeOffset)payload;
            }
        }

        public IReadOnlyList<string> StringListValue
        {
            get
            {
                RequireOneOf(CsvSchemaDataType.IdList, CsvSchemaDataType.EnumList);
                return (IReadOnlyList<string>)payload;
            }
        }

        public IReadOnlyList<string> IdListValue
        {
            get
            {
                Require(CsvSchemaDataType.IdList);
                return (IReadOnlyList<string>)payload;
            }
        }

        public IReadOnlyList<string> EnumListValue
        {
            get
            {
                Require(CsvSchemaDataType.EnumList);
                return (IReadOnlyList<string>)payload;
            }
        }

        public IReadOnlyList<int> IntegerListValue
        {
            get
            {
                Require(CsvSchemaDataType.IntList);
                return (IReadOnlyList<int>)payload;
            }
        }

        public IReadOnlyList<int> IntListValue => IntegerListValue;

        internal static CsvParsedValue Empty(CsvSchemaDataType dataType)
        {
            switch (dataType)
            {
                case CsvSchemaDataType.String:
                case CsvSchemaDataType.Id:
                case CsvSchemaDataType.Enum:
                    return new CsvParsedValue(dataType, true, string.Empty);
                case CsvSchemaDataType.Int:
                    return new CsvParsedValue(dataType, true, 0);
                case CsvSchemaDataType.ULong:
                    return new CsvParsedValue(dataType, true, 0UL);
                case CsvSchemaDataType.Float:
                    return new CsvParsedValue(dataType, true, 0f);
                case CsvSchemaDataType.Bool:
                    return new CsvParsedValue(dataType, true, false);
                case CsvSchemaDataType.Hex:
                    return new CsvParsedValue(
                        dataType,
                        true,
                        new CsvHexValue(string.Empty, Array.Empty<byte>()));
                case CsvSchemaDataType.DateTime:
                    return new CsvParsedValue(dataType, true, default(DateTimeOffset));
                case CsvSchemaDataType.IdList:
                case CsvSchemaDataType.EnumList:
                    return FromStringList(dataType, Array.Empty<string>(), true);
                case CsvSchemaDataType.IntList:
                    return FromIntegerList(Array.Empty<int>(), true);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }

        internal static CsvParsedValue FromString(
            CsvSchemaDataType dataType,
            string value)
        {
            return new CsvParsedValue(dataType, false, value);
        }

        internal static CsvParsedValue FromInteger(int value)
        {
            return new CsvParsedValue(CsvSchemaDataType.Int, false, value);
        }

        internal static CsvParsedValue FromUnsignedInteger(ulong value)
        {
            return new CsvParsedValue(CsvSchemaDataType.ULong, false, value);
        }

        internal static CsvParsedValue FromFloat(float value)
        {
            return new CsvParsedValue(CsvSchemaDataType.Float, false, value);
        }

        internal static CsvParsedValue FromBoolean(bool value)
        {
            return new CsvParsedValue(CsvSchemaDataType.Bool, false, value);
        }

        internal static CsvParsedValue FromHex(CsvHexValue value)
        {
            return new CsvParsedValue(CsvSchemaDataType.Hex, false, value);
        }

        internal static CsvParsedValue FromDateTime(DateTimeOffset value)
        {
            return new CsvParsedValue(CsvSchemaDataType.DateTime, false, value);
        }

        internal static CsvParsedValue FromStringList(
            CsvSchemaDataType dataType,
            IEnumerable<string> values,
            bool isEmpty = false)
        {
            var copied = new ReadOnlyCollection<string>(
                new List<string>(values ?? throw new ArgumentNullException(nameof(values))));
            return new CsvParsedValue(dataType, isEmpty, copied);
        }

        internal static CsvParsedValue FromIntegerList(
            IEnumerable<int> values,
            bool isEmpty = false)
        {
            var copied = new ReadOnlyCollection<int>(
                new List<int>(values ?? throw new ArgumentNullException(nameof(values))));
            return new CsvParsedValue(CsvSchemaDataType.IntList, isEmpty, copied);
        }

        private void Require(CsvSchemaDataType expected)
        {
            if (DataType != expected)
            {
                throw new InvalidOperationException(
                    "Parsed CSV value is " + CsvSchemaDataTypes.ToToken(DataType) +
                    ", not " + CsvSchemaDataTypes.ToToken(expected) + ".");
            }
        }

        private void RequireOneOf(
            CsvSchemaDataType first,
            CsvSchemaDataType second,
            CsvSchemaDataType third = (CsvSchemaDataType)(-1))
        {
            if (DataType != first && DataType != second && DataType != third)
            {
                throw new InvalidOperationException(
                    "The requested accessor is not valid for " +
                    CsvSchemaDataTypes.ToToken(DataType) + ".");
            }
        }
    }
}
