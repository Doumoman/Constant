#if LEGACY_DISABLED
using System;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Population.P7;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Campaign.P12
{
    [DisallowMultipleComponent]
    public sealed class P12ReturnCrystal2D : MonoBehaviour
    {
        public const float DriftUnitsPerSecond =
            P12ReturnCrystalRules.DriftCellsPerSecond * GridWorld.CellSize;

        [SerializeField, Min(1)] private int crystalValue =
            P12ReturnCrystalRules.StandardCrystalValue;
        [SerializeField] private Transform driftTarget;
        [SerializeField] private P5RunState2D p5RunState;
        [SerializeField] private P7EconomyWallet2D p7Wallet;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer crystalRenderer;

        public event Action<P12ReturnCrystal2D, int> Collected;

        public int Value => crystalValue;
        public Transform DriftTarget => driftTarget;
        public bool IsCollected { get; private set; }
        public bool CanCollect =>
            !IsCollected && (p5RunState != null || p7Wallet != null);

        public void Configure(
            int value,
            Transform target,
            P5RunState2D targetP5RunState = null,
            P7EconomyWallet2D targetP7Wallet = null)
        {
            crystalValue = Mathf.Max(1, value);
            driftTarget = target;
            p5RunState = targetP5RunState;
            p7Wallet = targetP7Wallet;
            IsCollected = false;
        }

        private void Awake()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }

            if (crystalRenderer == null)
            {
                crystalRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void FixedUpdate()
        {
            Drift(Time.fixedDeltaTime, useBody: true);
        }

        public void DriftForTests(float deltaSeconds)
        {
            Drift(deltaSeconds, useBody: false);
        }

        private void Drift(float deltaSeconds, bool useBody)
        {
            if (IsCollected || driftTarget == null || deltaSeconds <= 0f)
            {
                return;
            }

            Vector2 next = Vector2.MoveTowards(
                transform.position,
                driftTarget.position,
                DriftUnitsPerSecond * deltaSeconds);
            if (useBody && body != null)
            {
                body.MovePosition(next);
            }
            else
            {
                transform.position = next;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other != null
                && other.GetComponentInParent<PlayerInputAdapter>() != null)
            {
                TryCollect();
            }
        }

        public bool TryCollect()
        {
            if (!CanCollect)
            {
                return false;
            }

            int credited = p5RunState != null
                ? p5RunState.AddGold(crystalValue)
                : p7Wallet.AddGold(crystalValue);
            if (credited != crystalValue)
            {
                return false;
            }

            IsCollected = true;
            Collected?.Invoke(this, crystalValue);
            gameObject.SetActive(false);
            return true;
        }

        public bool CollectForTests()
        {
            return TryCollect();
        }
    }
}

#endif
