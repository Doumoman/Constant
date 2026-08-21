#if LEGACY_DISABLED
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Tools.Bomb;
using StarNight.Tools.Rope;
using UnityEngine;

namespace StarNight.Tools.Shop
{
    public enum ConsumableBundleKind
    {
        RopeBundle,
        BombBundle,
    }

    [DisallowMultipleComponent]
    public sealed class ConsumableShopCounter : MonoBehaviour,
        IContextReceiver,
        IWorldInteractionReceiver,
        IInteractionPromptSource
    {
        public const int RopeBundleQuantity = 3;
        public const int RopeBundlePriceWon = 100;
        public const int BombBundleQuantity = 2;
        public const int BombBundlePriceWon = 150;

        [SerializeField] private ConsumableBundleKind bundleKind;
        [SerializeField] private TextMesh priceText;

        private RunManager runManagerOverride;

        public int ContextPriority => 300;
        public int Quantity => bundleKind == ConsumableBundleKind.RopeBundle
            ? RopeBundleQuantity
            : BombBundleQuantity;
        public int PriceWon => bundleKind == ConsumableBundleKind.RopeBundle
            ? RopeBundlePriceWon
            : BombBundlePriceWon;
        public string PriceLabel => PriceWon.ToString("N0") + "원";
        public string PromptLabel => "구매  " + PriceLabel;
        public ToolPurchaseResult LastResult { get; private set; }

        private void Awake()
        {
            EnsurePriceText();
        }

        private void OnValidate()
        {
            if (priceText != null)
            {
                priceText.text = PriceLabel;
            }
        }

        public bool CanReceive(ContextReceiverQuery query) => query.Actor != null;

        public ContextReceiverResult TryReceive(ContextReceiverRequest request)
        {
            bool accepted = TryPurchase(request.Actor, out ToolPurchaseResult result);
            return accepted
                ? new ContextReceiverResult(true, false, "shop.consumable.purchased")
                : ContextReceiverResult.Rejected(result == ToolPurchaseResult.InsufficientFunds
                    ? "shop.insufficient_funds"
                    : "shop.purchase_failed");
        }

        public bool CanInteract(GameObject actor) => actor != null;

        public bool TryInteract(PlayerActionContext action, GameObject actor)
        {
            return TryPurchase(actor, out _);
        }

        public bool TryPurchase(GameObject actor, out ToolPurchaseResult result)
        {
            RunManager manager = ResolveRunManager();
            RunState run = manager?.Current;
            if (run == null || run.phase != RunPhase.Running)
            {
                LastResult = result = ToolPurchaseResult.NoActiveRun;
                return false;
            }
            if (run.moneyWon < PriceWon)
            {
                LastResult = result = ToolPurchaseResult.InsufficientFunds;
                return false;
            }

            if (bundleKind == ConsumableBundleKind.RopeBundle)
            {
                RopeInventoryState inventory = actor != null ? actor.GetComponent<RopeInventoryState>() : null;
                if (inventory == null)
                {
                    LastResult = result = ToolPurchaseResult.NoHandSlot;
                    return false;
                }
                inventory.Restore(inventory.Remaining + Quantity);
                run.ropes = inventory.Remaining;
            }
            else
            {
                BombInventoryState inventory = actor != null ? actor.GetComponent<BombInventoryState>() : null;
                if (inventory == null)
                {
                    LastResult = result = ToolPurchaseResult.NoHandSlot;
                    return false;
                }
                inventory.Restore(inventory.Remaining + Quantity);
                run.bombs = inventory.Remaining;
            }

            run.moneyWon -= PriceWon;
            LastResult = result = ToolPurchaseResult.Purchased;
            return true;
        }

        public void ConfigureForTests(ConsumableBundleKind kind, RunManager manager)
        {
            bundleKind = kind;
            runManagerOverride = manager;
            EnsurePriceText();
        }

        private RunManager ResolveRunManager()
        {
            if (runManagerOverride != null)
            {
                return runManagerOverride;
            }
            if (GameBootstrap.IsReady
                && GameBootstrap.Instance.Services.TryGet(out RunManager manager))
            {
                return manager;
            }
            return null;
        }

        private void EnsurePriceText()
        {
            if (priceText == null)
            {
                Transform existing = transform.Find("PriceLabel");
                GameObject label = existing != null
                    ? existing.gameObject
                    : new GameObject("PriceLabel", typeof(TextMesh));
                if (existing == null)
                {
                    label.transform.SetParent(transform, false);
                    label.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                }
                priceText = label.GetComponent<TextMesh>();
            }
            if (priceText != null)
            {
                priceText.anchor = TextAnchor.MiddleCenter;
                priceText.alignment = TextAlignment.Center;
                priceText.fontSize = 48;
                priceText.characterSize = 0.08f;
                priceText.color = new Color32(239, 205, 118, 255);
                priceText.text = PriceLabel;
            }
        }
    }
}

#endif
