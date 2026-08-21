#if LEGACY_DISABLED
using System;
using StarNight.Core.Flow;
using StarNight.Core.State;
using StarNight.Interaction.HandSlot;
using StarNight.Interaction.Input;
using StarNight.Interaction.Targeting;
using StarNight.Tools.Core;
using UnityEngine;

namespace StarNight.Tools.Shop
{
    public enum ToolPurchaseResult
    {
        None,
        Purchased,
        Refilled,
        InvalidProduct,
        NoActiveRun,
        InsufficientFunds,
        NoHandSlot,
        NoSafeExchangeCell,
        SpawnFailed,
    }

    [DisallowMultipleComponent]
    public sealed class ToolShopCounter : MonoBehaviour,
        IContextReceiver,
        IWorldInteractionReceiver,
        IInteractionPromptSource
    {
        [SerializeField] private HandToolDefinition product;
        [SerializeField] private TextMesh priceText;

        private RunManager runManagerOverride;

        public event Action<ToolPurchaseResult> PurchaseResolved;

        public int ContextPriority => 300;
        public HandToolDefinition Product => product;
        public int PriceWon => product != null ? product.ShopPriceWon : 0;
        public string PriceLabel => PriceWon.ToString("N0") + "원";
        public string PromptLabel => "구매  " + PriceLabel;
        public ToolPurchaseResult LastResult { get; private set; }

        private void Awake()
        {
            EnsurePriceText();
        }

        private void OnValidate()
        {
            RefreshPriceText();
        }

        public bool CanReceive(ContextReceiverQuery query)
        {
            return query.Actor != null && product != null;
        }

        public ContextReceiverResult TryReceive(ContextReceiverRequest request)
        {
            bool accepted = TryPurchase(request.Actor, out ToolPurchaseResult result);
            return accepted
                ? new ContextReceiverResult(true, false, FeedbackId(result))
                : ContextReceiverResult.Rejected(FeedbackId(result));
        }

        public bool CanInteract(GameObject actor)
        {
            return actor != null && product != null;
        }

        public bool TryInteract(PlayerActionContext action, GameObject actor)
        {
            return TryPurchase(actor, out _);
        }

        public bool TryPurchase(GameObject actor, out ToolPurchaseResult result)
        {
            RunManager manager = ResolveRunManager();
            RunState run = manager?.Current;
            if (product == null
                || product.RuntimePrefab == null
                || product.ShopPriceWon <= 0
                || product.ShopPriceWon % 10 != 0)
            {
                return Resolve(false, ToolPurchaseResult.InvalidProduct, out result);
            }
            if (run == null || run.phase != RunPhase.Running)
            {
                return Resolve(false, ToolPurchaseResult.NoActiveRun, out result);
            }
            if (run.moneyWon < product.ShopPriceWon)
            {
                return Resolve(false, ToolPurchaseResult.InsufficientFunds, out result);
            }

            PlayerHandSlot slot = actor != null ? actor.GetComponent<PlayerHandSlot>() : null;
            if (slot == null)
            {
                return Resolve(false, ToolPurchaseResult.NoHandSlot, out result);
            }

            if (slot.CurrentItem is HandToolRuntime heldTool
                && string.Equals(heldTool.StableItemId, product.ToolId, StringComparison.Ordinal))
            {
                heldTool.RepairFull();
                run.moneyWon -= product.ShopPriceWon;
                run.handToolId = product.ToolId;
                return Resolve(true, ToolPurchaseResult.Refilled, out result);
            }

            GameObject instance = Instantiate(product.RuntimePrefab, actor.transform.position, Quaternion.identity);
            HandToolRuntime purchasedTool = instance != null
                ? instance.GetComponentInChildren<HandToolRuntime>(true)
                : null;
            if (purchasedTool == null)
            {
                DestroySpawn(instance);
                return Resolve(false, ToolPurchaseResult.SpawnFailed, out result);
            }
            purchasedTool.Configure(product);

            bool transferred;
            if (slot.IsEmpty)
            {
                transferred = slot.TryAttach(purchasedTool);
            }
            else
            {
                HandSlotTransferService transfer = actor.GetComponent<HandSlotTransferService>();
                transferred = transfer != null && transfer.TryExchangeCurrent(purchasedTool);
            }

            if (!transferred)
            {
                DestroySpawn(instance);
                return Resolve(false, ToolPurchaseResult.NoSafeExchangeCell, out result);
            }

            run.moneyWon -= product.ShopPriceWon;
            run.handToolId = product.ToolId;
            return Resolve(true, ToolPurchaseResult.Purchased, out result);
        }

        public void ConfigureForTests(HandToolDefinition configuredProduct, RunManager manager)
        {
            product = configuredProduct;
            runManagerOverride = manager;
            EnsurePriceText();
        }

        public void Configure(HandToolDefinition configuredProduct)
        {
            product = configuredProduct;
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

        private bool Resolve(bool success, ToolPurchaseResult result, out ToolPurchaseResult output)
        {
            LastResult = result;
            output = result;
            PurchaseResolved?.Invoke(result);
            return success;
        }

        private static string FeedbackId(ToolPurchaseResult result)
        {
            return result switch
            {
                ToolPurchaseResult.Purchased => "shop.tool.purchased",
                ToolPurchaseResult.Refilled => "shop.tool.refilled",
                ToolPurchaseResult.InsufficientFunds => "shop.insufficient_funds",
                ToolPurchaseResult.NoSafeExchangeCell => "shop.no_safe_exchange_cell",
                _ => "shop.purchase_failed",
            };
        }

        private static void DestroySpawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(instance);
            }
            else
            {
                DestroyImmediate(instance);
            }
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
            }
            RefreshPriceText();
        }

        private void RefreshPriceText()
        {
            if (priceText != null)
            {
                priceText.text = product != null ? PriceLabel : string.Empty;
            }
        }
    }
}

#endif
