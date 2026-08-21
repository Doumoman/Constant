#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Population.P7
{
    public static class P7RegionProfileCatalog
    {
        private static readonly P7ShopProductKind[] StandardShopProducts =
        {
            P7ShopProductKind.RopeBundle3,
            P7ShopProductKind.BombBundle2,
            P7ShopProductKind.MoonCake,
            P7ShopProductKind.HandTool6,
            P7ShopProductKind.HandTool7,
            P7ShopProductKind.HandTool8,
            P7ShopProductKind.LaniLanternRecharge
        };

        public static P7RegionPlacementProfile Create(RoomRegion region)
        {
            switch (region)
            {
                case RoomRegion.MoonPalace:
                    return Profile(
                        region,
                        new Vector3Int(9, 14, 7),
                        new Vector3Int(6, 10, 4),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 5),
                            Enemy(P7EnemyKind.Hopper, 1, 5),
                            Enemy(P7EnemyKind.Charger, 2, 2),
                            Enemy(P7EnemyKind.Carrier, 2, 2)),
                        Traps(
                            Trap(P7TrapKind.RollingRiceCake, 1, 4),
                            Trap(P7TrapKind.FallingPestle, 2, 3),
                            Trap(P7TrapKind.CrumblingMoonFloor, 1, 4)));
                case RoomRegion.MagpieBridge:
                    return Profile(
                        region,
                        new Vector3Int(10, 16, 7),
                        new Vector3Int(6, 10, 5),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 3),
                            Enemy(P7EnemyKind.Hopper, 1, 4),
                            Enemy(P7EnemyKind.Flyer, 2, 5),
                            Enemy(P7EnemyKind.Charger, 2, 2)),
                        Traps(
                            Trap(P7TrapKind.SnappingThread, 1, 4),
                            Trap(P7TrapKind.Crosswind, 2, 4),
                            Trap(P7TrapKind.SwayingPlatform, 1, 4)));
                case RoomRegion.DragonPalace:
                    return Profile(
                        region,
                        new Vector3Int(10, 17, 8),
                        new Vector3Int(7, 11, 5),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 2),
                            Enemy(P7EnemyKind.Spitter, 2, 5),
                            Enemy(P7EnemyKind.Burrower, 2, 4),
                            Enemy(P7EnemyKind.Flyer, 2, 3)),
                        Traps(
                            Trap(P7TrapKind.Floodgate, 2, 4),
                            Trap(P7TrapKind.Riptide, 2, 4),
                            Trap(P7TrapKind.BubbleLauncher, 2, 4)));
                case RoomRegion.StarPostOffice:
                    return Profile(
                        region,
                        new Vector3Int(10, 17, 8),
                        new Vector3Int(7, 11, 5),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 3),
                            Enemy(P7EnemyKind.Spitter, 2, 4),
                            Enemy(P7EnemyKind.Carrier, 2, 5),
                            Enemy(P7EnemyKind.Charger, 2, 2)),
                        Traps(
                            Trap(P7TrapKind.ReturnStamp, 1, 4),
                            Trap(P7TrapKind.ParcelLauncher, 2, 4),
                            Trap(P7TrapKind.SuctionMailTube, 2, 4)));
                case RoomRegion.SunriseGarden:
                    return Profile(
                        region,
                        new Vector3Int(11, 19, 9),
                        new Vector3Int(7, 12, 6),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 3),
                            Enemy(P7EnemyKind.Hopper, 1, 3),
                            Enemy(P7EnemyKind.Burrower, 2, 5),
                            Enemy(P7EnemyKind.Flyer, 2, 3)),
                        Traps(
                            Trap(P7TrapKind.RotatingSunlight, 2, 4),
                            Trap(P7TrapKind.ShadowSeed, 1, 4),
                            Trap(P7TrapKind.OverheatedVine, 2, 4)));
                case RoomRegion.PolarisObservatory:
                    return Profile(
                        region,
                        new Vector3Int(11, 20, 10),
                        new Vector3Int(8, 13, 6),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 2),
                            Enemy(P7EnemyKind.Charger, 2, 4),
                            Enemy(P7EnemyKind.Spitter, 2, 4),
                            Enemy(P7EnemyKind.Flyer, 2, 4),
                            Enemy(P7EnemyKind.Carrier, 2, 2)),
                        Traps(
                            Trap(P7TrapKind.ReturnField, 2, 4),
                            Trap(P7TrapKind.StarWeight, 2, 4),
                            Trap(P7TrapKind.MixedLegacyTrap, 3, 3)));
                case RoomRegion.StarlessSea:
                    return Profile(
                        region,
                        new Vector3Int(19, 25, 18),
                        new Vector3Int(13, 17, 12),
                        Enemies(
                            Enemy(P7EnemyKind.Walker, 1, 2),
                            Enemy(P7EnemyKind.Hopper, 1, 2),
                            Enemy(P7EnemyKind.Charger, 2, 3),
                            Enemy(P7EnemyKind.Spitter, 2, 3),
                            Enemy(P7EnemyKind.Burrower, 2, 3),
                            Enemy(P7EnemyKind.Flyer, 2, 3),
                            Enemy(P7EnemyKind.Carrier, 2, 2)),
                        Traps(
                            Trap(P7TrapKind.MixedLegacyTrap, 3, 5),
                            Trap(P7TrapKind.ReturnField, 2, 2),
                            Trap(P7TrapKind.Crosswind, 2, 2),
                            Trap(P7TrapKind.Riptide, 2, 2)));
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(region),
                        region,
                        "No P7 population profile exists for this region.");
            }
        }

        public static IReadOnlyList<P7RegionPlacementProfile> CreateAll()
        {
            return new[]
            {
                Create(RoomRegion.MoonPalace),
                Create(RoomRegion.MagpieBridge),
                Create(RoomRegion.DragonPalace),
                Create(RoomRegion.StarPostOffice),
                Create(RoomRegion.SunriseGarden),
                Create(RoomRegion.PolarisObservatory),
                Create(RoomRegion.StarlessSea)
            };
        }

        private static P7RegionPlacementProfile Profile(
            RoomRegion region,
            Vector3Int enemyBudgets,
            Vector3Int hazardBudgets,
            IReadOnlyList<P7EnemyWeight> enemies,
            IReadOnlyList<P7TrapWeight> traps)
        {
            return new P7RegionPlacementProfile(
                region,
                enemyBudgets,
                hazardBudgets,
                P7EconomyRules.DefaultMainPathGoldTarget,
                enemies,
                traps,
                StandardShopProducts);
        }

        private static P7EnemyWeight Enemy(
            P7EnemyKind kind,
            int cost,
            int weight)
        {
            return new P7EnemyWeight(kind, cost, weight);
        }

        private static P7TrapWeight Trap(
            P7TrapKind kind,
            int cost,
            int weight)
        {
            return new P7TrapWeight(kind, cost, weight);
        }

        private static P7EnemyWeight[] Enemies(params P7EnemyWeight[] values)
        {
            return values;
        }

        private static P7TrapWeight[] Traps(params P7TrapWeight[] values)
        {
            return values;
        }
    }
}

#endif
