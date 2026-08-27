using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StarNight.Map.WorldGeneration.SpecialRegions
{
    public static class SpecialRegionCanonicalDigest
    {
        public static string Compute(SpecialRegionContract contract)
        {
            if (contract == null) throw new ArgumentNullException(nameof(contract));
            var material = new StringBuilder();
            Append(material, "id", contract.Id.Value);
            Append(material, "kind", Number((int)contract.Kind));
            Append(material, "reservation", contract.ReservationId.Value);

            foreach (var offset in contract.Footprint.Offsets.OrderBy(value => value))
                Append(material, "footprint", Coordinate(offset));
            foreach (var cell in contract.FixedShell)
                Append(material, "shell", Coordinate(cell.SectorOffset) + "/" + Coordinate(cell.Tile.X, cell.Tile.Y) + "/" + cell.ShellId);
            foreach (var slot in contract.Slots)
                Append(material, "slot", string.Join("/", new[]
                {
                    slot.Id.Value,
                    Number((int)slot.Kind),
                    Coordinate(slot.SectorOffset),
                    Coordinate(slot.Tile.X, slot.Tile.Y),
                    slot.Required ? "1" : "0",
                    Number((int)slot.PersistenceScope),
                    slot.PersistenceKey.Value,
                }));
            foreach (var port in contract.Ports)
                Append(material, "port", string.Join("/", new[]
                {
                    port.PortId,
                    port.SlotId.Value,
                    Number((int)port.Kind),
                    Coordinate(port.SectorOffset),
                    Coordinate(port.Tile.X, port.Tile.Y),
                    Number((int)port.Side),
                    Number((int)port.AccessClass),
                }));
            foreach (var binding in contract.Persistence)
                Append(material, "persistence", string.Join("/", new[]
                {
                    binding.Key.Value,
                    Number((int)binding.Scope),
                    binding.SlotId.Value,
                    binding.InitialMeaning,
                }));
            return Sha256(material.ToString());
        }

        private static void Append(StringBuilder material, string name, string value)
            => material.Append(name).Append('=').Append(value ?? string.Empty).Append('\n');

        private static string Coordinate(SpecialRegionSectorOffset value) => Coordinate(value.X, value.Y);
        private static string Coordinate(int x, int y) => Number(x) + "," + Number(y);
        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Sha256(string material)
        {
            using (var sha = SHA256.Create())
            {
                return string.Concat(sha.ComputeHash(new UTF8Encoding(false).GetBytes(material))
                    .Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }
    }
}
