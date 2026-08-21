#if LEGACY_DISABLED
using System;
using System.Linq;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Population.P7
{
    [CreateAssetMenu(
        fileName = "P7_RegionPopulationProfile",
        menuName = "StarNight/P7/Region Population Profile")]
    public sealed class P7RegionPlacementProfileAsset : ScriptableObject
    {
        [SerializeField] private RoomRegion region = RoomRegion.MoonPalace;
        [SerializeField] private Vector3Int enemyBudgets =
            new Vector3Int(7, 10, 13);
        [SerializeField] private Vector3Int hazardBudgets =
            new Vector3Int(3, 6, 9);
        // 0은 "코드 기본 파생"(상한의 60%)을 뜻한다. 손 저작 시 양수를 넣는다.
        [SerializeField] private Vector3Int enemyBudgetFloors = Vector3Int.zero;
        [SerializeField] private Vector3Int hazardBudgetFloors = Vector3Int.zero;
        [SerializeField, Min(P7EconomyRules.BigGoldValue)]
        private int mainPathGoldTarget =
            P7EconomyRules.DefaultMainPathGoldTarget;
        [SerializeField] private P7EnemyWeight[] enemyWeights =
            Array.Empty<P7EnemyWeight>();
        [SerializeField] private P7TrapWeight[] trapWeights =
            Array.Empty<P7TrapWeight>();
        [SerializeField] private P7ShopProductKind[] shopProducts =
            Array.Empty<P7ShopProductKind>();

        public RoomRegion Region => region;
        public Vector3Int EnemyBudgets => enemyBudgets;
        public Vector3Int HazardBudgets => hazardBudgets;
        public Vector3Int EnemyBudgetFloors => enemyBudgetFloors;
        public Vector3Int HazardBudgetFloors => hazardBudgetFloors;
        public int MainPathGoldTarget => mainPathGoldTarget;
        public P7EnemyWeight[] EnemyWeights => enemyWeights;
        public P7TrapWeight[] TrapWeights => trapWeights;
        public P7ShopProductKind[] ShopProducts => shopProducts;

        public void Configure(P7RegionPlacementProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            profile.Validate();
            region = profile.Region;
            enemyBudgets = profile.EnemyBudgets;
            hazardBudgets = profile.HazardBudgets;
            enemyBudgetFloors = profile.EnemyBudgetFloors;
            hazardBudgetFloors = profile.HazardBudgetFloors;
            mainPathGoldTarget = profile.MainPathGoldTarget;
            enemyWeights = profile.EnemyWeights.ToArray();
            trapWeights = profile.TrapWeights.ToArray();
            shopProducts = profile.ShopProducts.ToArray();
        }

        public P7RegionPlacementProfile ToRuntimeProfile()
        {
            return new P7RegionPlacementProfile(
                region,
                enemyBudgets,
                hazardBudgets,
                mainPathGoldTarget,
                enemyWeights,
                trapWeights,
                shopProducts,
                enemyBudgetFloors,
                hazardBudgetFloors);
        }

        private void OnValidate()
        {
            enemyBudgets = new Vector3Int(
                Mathf.Max(0, enemyBudgets.x),
                Mathf.Max(0, enemyBudgets.y),
                Mathf.Max(0, enemyBudgets.z));
            hazardBudgets = new Vector3Int(
                Mathf.Max(0, hazardBudgets.x),
                Mathf.Max(0, hazardBudgets.y),
                Mathf.Max(0, hazardBudgets.z));
            enemyBudgetFloors = ClampFloors(enemyBudgetFloors, enemyBudgets);
            hazardBudgetFloors = ClampFloors(hazardBudgetFloors, hazardBudgets);
            mainPathGoldTarget = Mathf.Max(
                P7EconomyRules.BigGoldValue,
                mainPathGoldTarget);
            enemyWeights = enemyWeights ?? Array.Empty<P7EnemyWeight>();
            trapWeights = trapWeights ?? Array.Empty<P7TrapWeight>();
            shopProducts = shopProducts ?? Array.Empty<P7ShopProductKind>();
        }

        private static Vector3Int ClampFloors(
            Vector3Int floors,
            Vector3Int ceilings)
        {
            return new Vector3Int(
                Mathf.Clamp(floors.x, 0, ceilings.x),
                Mathf.Clamp(floors.y, 0, ceilings.y),
                Mathf.Clamp(floors.z, 0, ceilings.z));
        }
    }
}

#endif
