using System;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarPostDeliveryRouteObjective : MonoBehaviour
    {
        [SerializeField] private GateRouteObjective routeObjective;
        [SerializeField] private string expectedParcelId;
        [SerializeField] private string expectedAddressId;
        [SerializeField] private string completionFlag;

        private StarDeliverySystem delivery;
        private bool completed;

        public bool Completed => completed;

        public void Configure(
            GateRouteObjective objective,
            string parcelId,
            string addressId,
            string flag)
        {
            routeObjective = objective;
            expectedParcelId = parcelId;
            expectedAddressId = addressId;
            completionFlag = flag;
        }

        private void Start()
        {
            BindForCurrentChapter();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void BindForCurrentChapter()
        {
            Unbind();
            delivery = StarNightRunState.Ensure().Delivery;
            delivery.ParcelDelivered += OnParcelDelivered;
        }

        private void OnParcelDelivered(FableObject parcel, StarPostalAddress address)
        {
            if (completed ||
                parcel == null ||
                address == null ||
                !string.Equals(parcel.ObjectId, expectedParcelId, StringComparison.Ordinal) ||
                !string.Equals(address.AddressId, expectedAddressId, StringComparison.Ordinal))
            {
                return;
            }

            if (routeObjective == null || !routeObjective.Complete())
            {
                return;
            }

            completed = true;
            StarNightRunState run = StarNightRunState.Ensure();
            if (!string.IsNullOrWhiteSpace(completionFlag))
            {
                run.SetFlag(completionFlag);
            }

            if (routeObjective.RouteId == "CH4_ROUTE_REGULAR_POST")
            {
                run.AddCounter("postal.shop_discount");
                StarNightHUD.Instance?.Toast(
                    "정규 우편 분류 완료 · 손상된 소포를 올바른 달 우편함으로 보내 정규 주소 조각을 복구했다.",
                    5f);
            }
            else
            {
                run.SetFlag("CH4_RARE_ROUTE_STAMP");
                StarNightHUD.Instance?.Toast(
                    "반송 불가 배달 완료 · 폐기 주소 조각과 희귀 노선 우표를 복구했다.",
                    5f);
            }
        }

        private void Unbind()
        {
            if (delivery != null)
            {
                delivery.ParcelDelivered -= OnParcelDelivered;
            }

            delivery = null;
        }
    }
}
