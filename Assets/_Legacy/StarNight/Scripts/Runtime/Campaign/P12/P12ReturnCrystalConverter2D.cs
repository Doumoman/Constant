#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Maru.P8;
using StarNight.Population.P7;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12ReturnCrystalConverter2D : MonoBehaviour
    {
        public const float ConversionRadius = 0.65f;
        public const int MaxActiveCrystals = 8;
        public const float CrystalTriggerRadius = 0.4f;

        [SerializeField] private P8MaruPursuer2D maru;
        [SerializeField] private Transform driftTarget;
        [SerializeField] private P5RunState2D p5RunState;
        [SerializeField] private P7EconomyWallet2D p7Wallet;

        private readonly List<GoldEntry> entries = new List<GoldEntry>();
        private readonly List<P12ReturnCrystal2D> spawnedCrystals =
            new List<P12ReturnCrystal2D>();

        public event Action<P12ReturnCrystal2D> CrystalSpawned;

        public P8MaruPursuer2D Maru => maru;
        public Transform DriftTarget => driftTarget;
        public int ConvertedCount { get; private set; }
        public int RegisteredCount => entries.Count;

        public int ActiveCrystalCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < spawnedCrystals.Count; index++)
                {
                    P12ReturnCrystal2D crystal = spawnedCrystals[index];
                    if (crystal != null
                        && !crystal.IsCollected
                        && crystal.gameObject.activeSelf)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool CanConvert => ActiveCrystalCount < MaxActiveCrystals;

        public void Configure(
            P8MaruPursuer2D targetMaru,
            Transform target,
            P5RunState2D targetP5RunState = null,
            P7EconomyWallet2D targetP7Wallet = null)
        {
            maru = targetMaru;
            driftTarget = target;
            p5RunState = targetP5RunState;
            p7Wallet = targetP7Wallet;
            entries.Clear();
            spawnedCrystals.Clear();
            ConvertedCount = 0;
        }

        public bool RegisterGoldPickup(Component pickup, int value)
        {
            if (pickup == null || value <= 0)
            {
                return false;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].Pickup == pickup)
                {
                    return false;
                }
            }

            entries.Add(new GoldEntry
            {
                Pickup = pickup,
                Value = value,
                Converted = false
            });
            return true;
        }

        private void FixedUpdate()
        {
            if (maru == null || !maru.IsHunting)
            {
                return;
            }

            TryConvertAt(maru.transform.position);
        }

        public bool TryConvertAt(Vector2 maruPosition)
        {
            bool convertedAny = false;
            for (int index = 0; index < entries.Count; index++)
            {
                GoldEntry entry = entries[index];
                if (entry.Converted || !IsConvertible(entry.Pickup))
                {
                    continue;
                }

                float distanceSquared =
                    ((Vector2)entry.Pickup.transform.position - maruPosition)
                    .sqrMagnitude;
                if (distanceSquared > ConversionRadius * ConversionRadius)
                {
                    continue;
                }

                if (!CanConvert)
                {
                    break;
                }

                entry.Converted = true;
                entries[index] = entry;
                SpawnCrystal(entry.Pickup, entry.Value);
                convertedAny = true;
            }

            return convertedAny;
        }

        private static bool IsConvertible(Component pickup)
        {
            if (pickup == null || !pickup.gameObject.activeInHierarchy)
            {
                return false;
            }

            return !(pickup is P5GoldPickup2D gold && gold.IsCollected);
        }

        private void SpawnCrystal(Component pickup, int goldValue)
        {
            Vector3 position = pickup.transform.position;
            pickup.gameObject.SetActive(false);
            var crystalObject = new GameObject("P12ReturnCrystal");
            crystalObject.transform.SetParent(transform, false);
            crystalObject.transform.position = position;
            Rigidbody2D crystalBody =
                crystalObject.AddComponent<Rigidbody2D>();
            crystalBody.bodyType = RigidbodyType2D.Kinematic;
            CircleCollider2D trigger =
                crystalObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = CrystalTriggerRadius;
            P12ReturnCrystal2D crystal =
                crystalObject.AddComponent<P12ReturnCrystal2D>();
            crystal.Configure(
                P12ReturnCrystalRules.CrystalValueFor(goldValue),
                driftTarget,
                p5RunState,
                p7Wallet);
            spawnedCrystals.Add(crystal);
            ConvertedCount++;
            CrystalSpawned?.Invoke(crystal);
        }

        private struct GoldEntry
        {
            public Component Pickup;
            public int Value;
            public bool Converted;
        }
    }
}

#endif
