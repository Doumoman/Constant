using System;
using System.Linq;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarDeliverySystem : MonoBehaviour
    {
        [SerializeField] private FableObject pendingParcel;
        [SerializeField] private int deliveryCount;
        [SerializeField] private bool dryFailureConsumed;
        [SerializeField] private bool wetMisrouteConsumed;

        public FableObject PendingParcel => pendingParcel;
        public int DeliveryCount => deliveryCount;

        public event Action<FableObject> PendingChanged;
        public event Action<FableObject, StarPostalAddress> ParcelDelivered;

        public FableToolResult Use(FableObject target, string actorId = "Player")
        {
            if (target == null)
            {
                return FableToolResult.Fail(pendingParcel == null
                    ? "도장을 찍을 소포를 바라보세요."
                    : "목적지 우체통을 바라보세요.");
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (pendingParcel == null)
            {
                if (target.HasTrait(FableTraits.PostalAddress))
                {
                    return FableToolResult.Fail("먼저 보낼 소포에 도장을 찍어야 한다.");
                }
                if (!IsParcel(target))
                {
                    return FableToolResult.Fail($"{target.DisplayName}은 우편으로 보낼 수 없다.");
                }

                pendingParcel = target;
                PendingChanged?.Invoke(target);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.ParcelSelected,
                    actorId = actorId,
                    targetId = target.ObjectId,
                    tool = FableVerb.Deliver,
                    detail = $"{target.DisplayName}에 목적지가 비어 있는 별 도장을 찍었다"
                });
                return new FableToolResult
                {
                    success = true,
                    awaitingDestination = true,
                    sentence = $"{target.DisplayName}을 보낼 주소를 고르자."
                };
            }

            if (target == pendingParcel)
            {
                string label = pendingParcel.DisplayName;
                ClearPending();
                return new FableToolResult
                {
                    success = true,
                    deliveryChanged = true,
                    sentence = $"{label}의 빈 도장을 지웠다."
                };
            }

            StarPostalAddress address = target.GetComponent<StarPostalAddress>();
            if (!target.HasTrait(FableTraits.PostalAddress) || address == null)
            {
                return FableToolResult.Fail($"{target.DisplayName}에는 읽을 수 있는 주소가 없다.");
            }

            if (run.GetFlag("CH4_DRY_INK") && !dryFailureConsumed)
            {
                dryFailureConsumed = true;
                run.SetFlag("CH4_DRY_STAMP_DELAYED");
                float dryScent = run.ConsequenceResolver.ModifyScent(3f);
                run.Chapter.AddScent(dryScent, "마른 도장이 한 번에 찍히지 않았다", pendingParcel.ObjectId);
                return FableToolResult.Fail("잉크가 말라 주소가 찍히지 않았다. 도장을 눌러 다시 보내자.");
            }

            FableObject parcel = pendingParcel;
            ClearPending();
            return DeliverDirect(parcel, address, actorId, true);
        }

        public FableToolResult DeliverDirect(FableObject parcel, StarPostalAddress requestedAddress,
            string actorId = "Player", bool applyRouteHazards = true)
        {
            if (parcel == null || requestedAddress == null)
            {
                return FableToolResult.Fail("소포 또는 주소가 사라졌다.");
            }

            StarNightRunState run = StarNightRunState.Ensure();
            MaruDirector maru = FindFirstObjectByType<MaruDirector>();
            if (applyRouteHazards && maru != null && maru.Chasing &&
                parcel.HasTrait(FableTraits.LastLetter))
            {
                parcel.SetStored(true);
                run.SetFlag("CH4_LETTER_LOST_TO_MARU");
                run.SetFlag("CH4_LETTER_STATE_LOST_TO_MARU");
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.ParcelIntercepted,
                    actorId = "Maru",
                    targetId = parcel.ObjectId,
                    tool = FableVerb.Deliver,
                    detail = "마루가 배송 중인 마지막 편지를 가로챘다",
                    causedAccident = true,
                    witnessed = true
                });
                return new FableToolResult
                {
                    success = true,
                    deliveryChanged = true,
                    sentence = "별빛 경로가 끊겼다. 마루가 마지막 편지를 물어 갔다!"
                };
            }

            StarPostalAddress actualAddress = requestedAddress;
            bool misdelivered = false;
            if (applyRouteHazards && run.GetFlag("CH4_WET_INK") && !wetMisrouteConsumed)
            {
                StarPostalAddress alternate = FindObjectsByType<StarPostalAddress>(FindObjectsSortMode.None)
                    .Where(candidate => candidate != null && candidate != requestedAddress)
                    .OrderBy(candidate => candidate.AddressId)
                    .FirstOrDefault();
                if (alternate != null)
                {
                    wetMisrouteConsumed = true;
                    actualAddress = alternate;
                    misdelivered = true;
                    run.SetFlag("CH4_SPLIT_DELIVERY_OCCURRED");
                }
            }

            actualAddress.Receive(parcel);
            deliveryCount++;
            float discount = run.GetCounter("delivery.scent_discount") > 0 ? 0.72f : 1f;
            float rawScent = (actualAddress.Dangerous ? 11f : 6f) * discount;
            float scent = run.ConsequenceResolver.ModifyScent(rawScent);
            run.Chapter.AddScent(scent,
                misdelivered ? "젖은 주소가 번져 다른 우체통에 나타났다" : "소포가 별빛 경로를 통과했다",
                parcel.ObjectId);
            run.Actions.Record(new StarActionContext
            {
                actionType = misdelivered ? StarActionType.ParcelMisdelivered : StarActionType.ParcelDelivered,
                actorId = actorId,
                targetId = $"{parcel.ObjectId}->{actualAddress.AddressId}",
                tool = FableVerb.Deliver,
                detail = misdelivered
                    ? $"{parcel.DisplayName}이 {requestedAddress.DisplayName} 대신 {actualAddress.DisplayName}에 잘못 도착했다"
                    : $"{parcel.DisplayName}을 {actualAddress.DisplayName}으로 보냈다",
                scentDelta = scent,
                causedAccident = misdelivered,
                witnessed = true
            });

            if (misdelivered)
            {
                run.AccidentReport.Add(parcel.DisplayName, "젖은 주소가 둘로 번져",
                    $"{actualAddress.DisplayName}에 잘못 도착했다", run.Actions.LatestSequence);
            }
            else
            {
                HandleStoryDelivery(run, parcel, actualAddress);
            }

            ParcelDelivered?.Invoke(parcel, actualAddress);
            return new FableToolResult
            {
                success = true,
                deliveryChanged = true,
                overloaded = misdelivered,
                sentence = misdelivered
                    ? $"{parcel.DisplayName}이 엉뚱한 주소에 도착했다. 다시 찾아야 한다!"
                    : $"{parcel.DisplayName}에서 {actualAddress.DisplayName}(으)로 배송 완료.",
                scentAdded = scent
            };
        }

        public FableToolResult DeliverPlayer(StarNightPlayerAgent player, string addressId)
        {
            StarPostalAddress address = FindAddress(addressId);
            if (player == null || address == null)
            {
                return FableToolResult.Fail("몸을 보낼 주소를 찾지 못했다.");
            }

            address.ReceivePlayer(player);
            StarNightRunState run = StarNightRunState.Ensure();
            float scent = run.ConsequenceResolver.ModifyScent(address.Dangerous ? 14f : 8f);
            run.Chapter.AddScent(scent, "사람이 직접 별빛 소포가 되었다", address.AddressId);
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PlayerDelivered,
                actorId = "Player",
                targetId = address.AddressId,
                tool = FableVerb.Deliver,
                detail = $"자신을 {address.DisplayName}으로 배송했다",
                scentDelta = scent,
                witnessed = true
            });
            return new FableToolResult
            {
                success = true,
                deliveryChanged = true,
                sentence = $"수신 완료 · {address.DisplayName}",
                scentAdded = scent
            };
        }

        public string Preview(FableObject target)
        {
            if (target == null)
            {
                return pendingParcel == null ? "보낼 소포를 바라보세요" : "목적지 우체통을 바라보세요";
            }
            if (pendingParcel == null)
            {
                return target.HasTrait(FableTraits.PostalAddress)
                    ? "먼저 소포 선택"
                    : $"{target.DisplayName}에 빈 목적지 도장 찍기";
            }
            if (target == pendingParcel)
            {
                return "빈 도장 지우기";
            }
            StarPostalAddress address = target.GetComponent<StarPostalAddress>();
            return address != null
                ? $"{pendingParcel.DisplayName}에서 {address.DisplayName}(으)로"
                : $"{target.DisplayName}: 주소 없음";
        }

        public StarPostalAddress FindAddress(string addressId)
        {
            return FindObjectsByType<StarPostalAddress>(FindObjectsSortMode.None)
                .FirstOrDefault(address => address.AddressId == addressId);
        }

        public void ResetForChapter()
        {
            pendingParcel = null;
            deliveryCount = 0;
            dryFailureConsumed = false;
            wetMisrouteConsumed = false;
            PendingChanged?.Invoke(null);
        }

        private static bool IsParcel(FableObject target)
        {
            return target != null &&
                   (target.HasTrait(FableTraits.Deliverable) ||
                    target.HasTrait(FableTraits.PostalParcel) ||
                    target.HasTrait(FableTraits.LastLetter));
        }

        private static void HandleStoryDelivery(StarNightRunState run, FableObject parcel,
            StarPostalAddress address)
        {
            if (!parcel.HasTrait(FableTraits.LastLetter) || address.AddressId != "RANI")
            {
                return;
            }

            bool wasOpened = run.GetFlag("CH4_LETTER_STATE_OPENED");
            parcel.SetStored(true);
            run.SetFlag("CH4_LETTER_STATE_DELIVERED");
            run.SetFlag("CH4_RANI_DISCONNECTED");
            run.SetFlag("STARPATH_ROUTE_CLUE");
            run.SetFlag("STARPATH_RANI_CAN_BE_DELIVERED");
            if (!wasOpened)
            {
                run.SetFlag("STARPATH_LAST_LETTER_DELIVERED");
            }
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.LetterDelivered,
                actorId = "Player",
                targetId = "Rani",
                tool = FableVerb.Deliver,
                detail = wasOpened
                    ? "봉인이 열린 마지막 편지를 라니에게 보냈다"
                    : "마지막 편지를 열지 않고 라니에게 보냈다",
                helpedResident = true,
                witnessed = true
            });
            run.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.RaniDisconnected,
                actorId = "Rani",
                targetId = "Communication",
                detail = "라니가 편지를 받은 뒤 처음으로 통신을 끊었다",
                witnessed = true
            });
            StarNightHUD.Instance?.Toast(wasOpened
                ? "수신자: 라니. 뜯긴 봉인이 함께 도착했다.\n…라니의 통신이 끊겼다."
                : "수신자: 라니. 편지는 열리지 않았다.\n…라니의 통신이 끊겼다.", 7f);
        }

        private void ClearPending()
        {
            pendingParcel = null;
            PendingChanged?.Invoke(null);
        }
    }
}
