#if LEGACY_DISABLED
using System.Collections.Generic;
using StarNight.Grid;
using StarNight.Player;
using StarNight.Tools.Water;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(OverheatedDevice2D))]
    public sealed class P11OverheatedPlatform2D : MonoBehaviour
    {
        public const int StandingDamage = 1;

        [SerializeField] private OverheatedDevice2D waterReactive;
        [SerializeField] private Collider2D supportCollider;
        [SerializeField] private Collider2D heatTrigger;
        [SerializeField] private SpriteRenderer platformVisual;
        [SerializeField] private int damageApplicationCount;
        [SerializeField] private PlayerRecovery lastDamagedPlayer;
        private readonly HashSet<PlayerRecovery> occupants =
            new HashSet<PlayerRecovery>();
        private bool subscribed;

        public OverheatedDevice2D WaterReactive => waterReactive;
        public bool IsCooled =>
            waterReactive != null && waterReactive.IsCooled;
        public bool SafeToStand => IsCooled;
        public int DamageApplicationCount => damageApplicationCount;
        public PlayerRecovery LastDamagedPlayer => lastDamagedPlayer;
        public bool IsConfigured =>
            waterReactive != null
            && supportCollider != null
            && heatTrigger != null
            && supportCollider != heatTrigger;

        public void Configure(
            WaterInteractionRegistry2D registry,
            GridWorld world,
            GridPos cell,
            Collider2D targetSupportCollider,
            Collider2D targetHeatTrigger,
            SpriteRenderer visual,
            bool startsCooled = false)
        {
            waterReactive = GetComponent<OverheatedDevice2D>();
            supportCollider = targetSupportCollider != null
                ? targetSupportCollider
                : GetComponent<BoxCollider2D>();
            heatTrigger = targetHeatTrigger;
            platformVisual = visual;
            damageApplicationCount = 0;
            lastDamagedPlayer = null;
            occupants.Clear();
            if (supportCollider != null)
            {
                supportCollider.isTrigger = false;
                supportCollider.enabled = true;
            }

            if (heatTrigger != null)
            {
                heatTrigger.isTrigger = true;
            }

            Subscribe();
            waterReactive.Configure(
                registry,
                world,
                cell,
                heatTrigger,
                platformVisual,
                startsCooled);
            RefreshPhysicalState();
        }

        public bool TryApplyStandingDamage(PlayerRecovery player)
        {
            if (IsCooled || player == null)
            {
                return false;
            }

            int applied = player.ApplyDamage(StandingDamage);
            if (applied <= 0)
            {
                return false;
            }

            damageApplicationCount++;
            lastDamagedPlayer = player;
            return true;
        }

        public bool HandleTriggerEnter(Collider2D other)
        {
            PlayerRecovery player = other != null
                ? other.GetComponentInParent<PlayerRecovery>()
                : null;
            if (player == null || !occupants.Add(player))
            {
                return false;
            }

            return TryApplyStandingDamage(player);
        }

        public bool HandleTriggerExit(Collider2D other)
        {
            PlayerRecovery player = other != null
                ? other.GetComponentInParent<PlayerRecovery>()
                : null;
            return player != null && occupants.Remove(player);
        }

        public void ReheatForTests()
        {
            waterReactive?.ReheatForTests();
            occupants.Clear();
            RefreshPhysicalState();
        }

        private void Awake()
        {
            waterReactive = GetComponent<OverheatedDevice2D>();
            if (supportCollider == null)
            {
                supportCollider = GetComponent<BoxCollider2D>();
            }

            Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            occupants.Clear();
            Unsubscribe();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleTriggerEnter(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            HandleTriggerExit(other);
        }

        private void Subscribe()
        {
            if (subscribed || waterReactive == null)
            {
                return;
            }

            waterReactive.Cooled += HandleCooled;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || waterReactive == null)
            {
                return;
            }

            waterReactive.Cooled -= HandleCooled;
            subscribed = false;
        }

        private void HandleCooled()
        {
            occupants.Clear();
            RefreshPhysicalState();
        }

        private void RefreshPhysicalState()
        {
            if (supportCollider != null)
            {
                supportCollider.enabled = true;
            }

            if (heatTrigger != null)
            {
                heatTrigger.enabled = !IsCooled;
            }
        }
    }
}

#endif
