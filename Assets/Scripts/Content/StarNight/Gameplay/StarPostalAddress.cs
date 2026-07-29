using System;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarPostalAddress : MonoBehaviour
    {
        [SerializeField] private string addressId = "LOCAL";
        [SerializeField] private string displayName = "이름 없는 우체통";
        [SerializeField] private Transform arrivalPoint;
        [SerializeField] private bool dangerous;

        public string AddressId => addressId;
        public string DisplayName => displayName;
        public bool Dangerous => dangerous;
        public Vector3 ArrivalPosition => arrivalPoint != null
            ? arrivalPoint.position
            : transform.position + Vector3.up * 1.2f;

        public event Action<FableObject> ParcelReceived;

        public void Configure(string id, string label, Transform arrival = null, bool isDangerous = false)
        {
            addressId = id;
            displayName = label;
            arrivalPoint = arrival;
            dangerous = isDangerous;
        }

        public void Receive(FableObject parcel)
        {
            if (parcel == null)
            {
                return;
            }

            parcel.SetStored(false);
            parcel.transform.position = ArrivalPosition;
            if (parcel.Body != null)
            {
                parcel.Body.position = ArrivalPosition;
                parcel.Body.linearVelocity = Vector2.zero;
                parcel.Body.angularVelocity = 0f;
            }
            ParcelReceived?.Invoke(parcel);
        }

        public void ReceivePlayer(StarNightPlayerAgent player)
        {
            if (player == null)
            {
                return;
            }

            player.transform.position = ArrivalPosition;
            if (player.TryGetComponent(out Rigidbody2D body))
            {
                body.position = ArrivalPosition;
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
