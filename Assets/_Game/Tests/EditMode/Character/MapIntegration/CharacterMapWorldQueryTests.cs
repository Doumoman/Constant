using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using StarNight.Character.MapIntegration;
using StarNight.Map.WorldGeneration.Domain;
using StarNight.Map.WorldGeneration.Microchunks;

namespace StarNight.Character.Tests.MapIntegration
{
    public sealed class CharacterMapWorldQueryTests
    {
        /// <summary>결정적 fake 질의 소스(라이브 생성 맵 소스는 CHAR06 소관).</summary>
        private sealed class FakeMapWorldQuery : ICharacterMapWorldQuery
        {
            private readonly Dictionary<long, CharacterMapCellState> cells =
                new Dictionary<long, CharacterMapCellState>();

            public void SetCell(WorldTileCoord tile, CharacterMapCellState state)
            {
                cells[Key(tile)] = state;
            }

            public bool TryGetCellState(WorldTileCoord tile, out CharacterMapCellState state)
            {
                return cells.TryGetValue(Key(tile), out state);
            }

            private static long Key(WorldTileCoord tile)
            {
                return ((long)tile.Y << 32) | (uint)tile.X;
            }
        }

        private static WorldTileCoord Tile(int x, int y)
        {
            WorldTileCoord tile;
            Assert.That(WorldCoordinateUtility.TryCreateWorldTile(x, y, out tile), Is.True);
            return tile;
        }

        [Test]
        public void MapWorldQuery_ReportsSolidHazardOneWayLiquidBreakableAndEmpty()
        {
            var query = new FakeMapWorldQuery();
            query.SetCell(Tile(0, 0),
                CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.GroundSolid));
            query.SetCell(Tile(1, 0),
                CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.OneWay));
            query.SetCell(Tile(2, 0),
                CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.Hazard));
            query.SetCell(Tile(3, 0),
                CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.Liquid));
            query.SetCell(Tile(4, 0),
                CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.Breakable));
            query.SetCell(Tile(5, 0),
                CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.DecorationFront));

            CharacterMapCellState state;

            Assert.That(query.TryGetCellState(Tile(0, 0), out state), Is.True);
            Assert.That(state.IsSolid, Is.True);
            Assert.That(state.IsEmpty, Is.False);

            Assert.That(query.TryGetCellState(Tile(1, 0), out state), Is.True);
            Assert.That(state.IsOneWay, Is.True);
            Assert.That(state.IsSolid, Is.False);

            Assert.That(query.TryGetCellState(Tile(2, 0), out state), Is.True);
            Assert.That(state.IsHazard, Is.True);

            Assert.That(query.TryGetCellState(Tile(3, 0), out state), Is.True);
            Assert.That(state.IsLiquid, Is.True);

            // Breakable은 파괴 가능한 고체다.
            Assert.That(query.TryGetCellState(Tile(4, 0), out state), Is.True);
            Assert.That(state.IsBreakable, Is.True);
            Assert.That(state.IsSolid, Is.True);

            // Decoration 계열은 비충돌 오버레이 → empty/passable.
            Assert.That(query.TryGetCellState(Tile(5, 0), out state), Is.True);
            Assert.That(state.IsEmpty, Is.True);

            // 데이터가 없는 타일은 false(미생성 영역).
            Assert.That(query.TryGetCellState(Tile(9, 9), out state), Is.False);

            // 레이어 합성: 고체 위 위험 오버레이.
            var combined = CharacterMapCellState
                .FromTileLayer(MicrochunkTileLayer.GroundSolid)
                .Combine(CharacterMapCellState.FromTileLayer(MicrochunkTileLayer.Hazard));

            Assert.That(combined.IsSolid, Is.True);
            Assert.That(combined.IsHazard, Is.True);
            Assert.That(combined.IsEmpty, Is.False);
        }

        [Test]
        public void MapWorldQuery_DoesNotUseTilemapOrMicroChunkInternals()
        {
            // 계약 표면이 노출하는 타입은 캐릭터 관점 값 객체와 MAP 공용 도메인
            // 좌표뿐이다 — Tilemap, 마이크로청크 배치/정의 내부, 생성 pass,
            // CSV authoring 타입이 표면에 등장하지 않는다.
            var forbiddenTypeNames = new[]
            {
                "Tilemap", "MicrochunkDefinition", "MicrochunkTileCell",
                "Generator", "Csv", "Authoring", "Pass"
            };
            var surfaceTypes = new[]
            {
                typeof(ICharacterMapWorldQuery),
                typeof(CharacterMapCellState),
                typeof(CharacterMapCoordinateBridge),
                typeof(CharacterRoomBoundaryGate),
                typeof(CharacterRoomId)
            };

            foreach (var type in surfaceTypes)
            {
                var memberTypeNames = type
                    .GetMembers(System.Reflection.BindingFlags.Public
                        | System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.Static)
                    .SelectMany(member =>
                    {
                        switch (member)
                        {
                            case System.Reflection.MethodInfo method:
                                return method.GetParameters()
                                    .Select(parameter => parameter.ParameterType.Name)
                                    .Concat(new[] { method.ReturnType.Name });
                            case System.Reflection.PropertyInfo property:
                                return new[] { property.PropertyType.Name };
                            case System.Reflection.FieldInfo field:
                                return new[] { field.FieldType.Name };
                            default:
                                return System.Linq.Enumerable.Empty<string>();
                        }
                    })
                    .ToArray();

                foreach (var typeName in memberTypeNames)
                {
                    foreach (var forbidden in forbiddenTypeNames)
                    {
                        Assert.That(typeName, Does.Not.Contain(forbidden),
                            type.Name + " 표면에 내부 타입 노출: " + typeName);
                    }
                }
            }
        }
    }
}
