using System;

namespace StarNight.Map.WorldGeneration.Data
{
    public enum CsvSchemaDataType
    {
        String,
        Id,
        Int,
        ULong,
        Float,
        Bool,
        Enum,
        IdList,
        EnumList,
        IntList,
        Hex,
        DateTime,
    }

    public static class CsvSchemaDataTypes
    {
        public static bool TryParse(string token, out CsvSchemaDataType dataType)
        {
            switch (token)
            {
                case "STRING":
                    dataType = CsvSchemaDataType.String;
                    return true;
                case "ID":
                    dataType = CsvSchemaDataType.Id;
                    return true;
                case "INT":
                    dataType = CsvSchemaDataType.Int;
                    return true;
                case "ULONG":
                    dataType = CsvSchemaDataType.ULong;
                    return true;
                case "FLOAT":
                    dataType = CsvSchemaDataType.Float;
                    return true;
                case "BOOL":
                    dataType = CsvSchemaDataType.Bool;
                    return true;
                case "ENUM":
                    dataType = CsvSchemaDataType.Enum;
                    return true;
                case "ID_LIST":
                    dataType = CsvSchemaDataType.IdList;
                    return true;
                case "ENUM_LIST":
                    dataType = CsvSchemaDataType.EnumList;
                    return true;
                case "INT_LIST":
                    dataType = CsvSchemaDataType.IntList;
                    return true;
                case "HEX":
                    dataType = CsvSchemaDataType.Hex;
                    return true;
                case "DATETIME":
                    dataType = CsvSchemaDataType.DateTime;
                    return true;
                default:
                    dataType = default;
                    return false;
            }
        }

        public static string ToToken(CsvSchemaDataType dataType)
        {
            switch (dataType)
            {
                case CsvSchemaDataType.String:
                    return "STRING";
                case CsvSchemaDataType.Id:
                    return "ID";
                case CsvSchemaDataType.Int:
                    return "INT";
                case CsvSchemaDataType.ULong:
                    return "ULONG";
                case CsvSchemaDataType.Float:
                    return "FLOAT";
                case CsvSchemaDataType.Bool:
                    return "BOOL";
                case CsvSchemaDataType.Enum:
                    return "ENUM";
                case CsvSchemaDataType.IdList:
                    return "ID_LIST";
                case CsvSchemaDataType.EnumList:
                    return "ENUM_LIST";
                case CsvSchemaDataType.IntList:
                    return "INT_LIST";
                case CsvSchemaDataType.Hex:
                    return "HEX";
                case CsvSchemaDataType.DateTime:
                    return "DATETIME";
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataType), dataType, null);
            }
        }
    }
}
