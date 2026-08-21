#if LEGACY_DISABLED
using System;
using StarNight.Objects;
using StarNight.Population.P7;
using StarNight.Stages.P5;
using UnityEngine;

namespace StarNight.Maru.P8
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CarryableObject2D))]
    public sealed class P8StarTear2D : MonoBehaviour
    {
        public const int GoldValue = 12;
        public static readonly Vector2Int FootprintCells = Vector2Int.one;

        [SerializeField] private CarryableObject2D carryable;
        [SerializeField] private P5StageExit2D stageExit;
        [SerializeField] private P5RunState2D p5RunState;
        [SerializeField] private P7EconomyWallet2D p7Wallet;
        [SerializeField] private bool converted;

        private bool subscribed;

        public event Action<P8StarTear2D, int> ConvertedAtExit;

        public CarryableObject2D Carryable => carryable;
        public int Value => GoldValue;
        public bool Converted => converted;
        public bool CanBeLost => true;

        public void Configure(
            CarryableObject2D targetCarryable,
            P5StageExit2D targetExit = null,
            P5RunState2D targetP5RunState = null,
            P7EconomyWallet2D targetP7Wallet = null)
        {
            Unsubscribe();
            carryable = targetCarryable != null
                ? targetCarryable
                : GetComponent<CarryableObject2D>();
            stageExit = targetExit;
            p5RunState = targetP5RunState;
            p7Wallet = targetP7Wallet;
            converted = false;
            Subscribe();
        }

        private void Awake()
        {
            if (carryable == null)
            {
                carryable = GetComponent<CarryableObject2D>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public bool TryConvertAtExit(bool requireHeld = true)
        {
            if (converted
                || (requireHeld
                    && (carryable == null || !carryable.IsHeld)))
            {
                return false;
            }

            int credited = 0;
            if (p5RunState != null)
            {
                credited = p5RunState.AddGold(GoldValue);
            }
            else if (p7Wallet != null)
            {
                credited = p7Wallet.AddGold(GoldValue);
            }

            if (credited != GoldValue)
            {
                return false;
            }

            converted = true;
            ConvertedAtExit?.Invoke(this, GoldValue);
            gameObject.SetActive(false);
            return true;
        }

        public void ResetForTests()
        {
            converted = false;
        }

        private void HandleExitDeparted()
        {
            TryConvertAtExit();
        }

        private void Subscribe()
        {
            if (subscribed || stageExit == null)
            {
                return;
            }

            stageExit.Departed += HandleExitDeparted;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || stageExit == null)
            {
                return;
            }

            stageExit.Departed -= HandleExitDeparted;
            subscribed = false;
        }
    }
}

#endif
