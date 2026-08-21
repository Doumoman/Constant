#if LEGACY_DISABLED
using UnityEngine;

namespace StarNight.Population.P7
{
    [DisallowMultipleComponent]
    public sealed class P7ShopInventory2D : MonoBehaviour
    {
        [SerializeField, Min(0)] private int ropeCount;
        [SerializeField, Min(0)] private int bombCount;
        [SerializeField, Min(0)] private int moonCakeCount;
        [SerializeField, Min(0)] private int handToolCount;
        [SerializeField, Min(0)] private int lanternRechargeCount;

        public int RopeCount => ropeCount;
        public int BombCount => bombCount;
        public int MoonCakeCount => moonCakeCount;
        public int HandToolCount => handToolCount;
        public int LanternRechargeCount => lanternRechargeCount;

        public bool CanGrant(P7ShopProductKind product)
        {
            switch (product)
            {
                case P7ShopProductKind.RopeBundle3:
                    return ropeCount <= P7EconomyWallet2D.MaximumSmallNumber - 3;
                case P7ShopProductKind.BombBundle2:
                    return bombCount <= P7EconomyWallet2D.MaximumSmallNumber - 2;
                case P7ShopProductKind.MoonCake:
                    return moonCakeCount < P7EconomyWallet2D.MaximumSmallNumber;
                case P7ShopProductKind.HandTool6:
                case P7ShopProductKind.HandTool7:
                case P7ShopProductKind.HandTool8:
                    return handToolCount < P7EconomyWallet2D.MaximumSmallNumber;
                case P7ShopProductKind.LaniLanternRecharge:
                    return lanternRechargeCount
                        < P7EconomyWallet2D.MaximumSmallNumber;
                default:
                    return false;
            }
        }

        public bool TryGrant(P7ShopProductKind product)
        {
            if (!CanGrant(product))
            {
                return false;
            }

            switch (product)
            {
                case P7ShopProductKind.RopeBundle3:
                    ropeCount += 3;
                    return true;
                case P7ShopProductKind.BombBundle2:
                    bombCount += 2;
                    return true;
                case P7ShopProductKind.MoonCake:
                    moonCakeCount++;
                    return true;
                case P7ShopProductKind.HandTool6:
                case P7ShopProductKind.HandTool7:
                case P7ShopProductKind.HandTool8:
                    handToolCount++;
                    return true;
                case P7ShopProductKind.LaniLanternRecharge:
                    lanternRechargeCount++;
                    return true;
                default:
                    return false;
            }
        }

        public void ResetForTests()
        {
            ropeCount = 0;
            bombCount = 0;
            moonCakeCount = 0;
            handToolCount = 0;
            lanternRechargeCount = 0;
        }
    }
}

#endif
