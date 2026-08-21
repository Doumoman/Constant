#if LEGACY_DISABLED
using System.Collections.Generic;
using UnityEngine;

namespace StarNight.Map
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapElementInstance))]
    public sealed class ToolReactionReceiver : MonoBehaviour, IToolReactionReceiver
    {
        private const int ActionHistoryLimit = 64;

        [SerializeField] private bool transitionBusy;
        [SerializeField] private int acceptedHitCount;

        private readonly HashSet<int> processedActionIds = new HashSet<int>();
        private readonly Queue<int> actionOrder = new Queue<int>();
        private readonly Dictionary<ToolTag, int> hitCounts = new Dictionary<ToolTag, int>();
        private MapElementInstance element;
        private CommonElementDriver driver;
        private MaruElementDriver maruDriver;
        private MoonElementDriver moonDriver;
        private BridgeElementDriver bridgeDriver;
        private PalaceElementDriver palaceDriver;
        private PostElementDriver postDriver;
        private SunElementDriver sunDriver;
        private PolarisElementDriver polarisDriver;

        public int AcceptedHitCount => acceptedHitCount;
        public bool TransitionBusy => transitionBusy;

        private void Awake()
        {
            CacheComponents();
        }

        public void SetTransitionBusy(bool busy)
        {
            transitionBusy = busy;
        }

        public void ClearActionHistory()
        {
            processedActionIds.Clear();
            actionOrder.Clear();
            hitCounts.Clear();
            acceptedHitCount = 0;
        }

        public ToolReactionResult TryReact(ToolReactionContext context)
        {
            CacheComponents();
            if (!RegisterAction(context.ActionId))
            {
                return ToolReactionResult.Rejected(FeedbackId.DuplicateAction);
            }

            if (transitionBusy)
            {
                return ToolReactionResult.Rejected(FeedbackId.Busy);
            }

            var definition = element != null ? element.Definition : null;
            var common = definition != null ? definition.CommonProfile : null;
            if (common != null && common.Kind == CommonElementKind.UnbreakableBlock)
            {
                return ToolReactionResult.Rejected(FeedbackId.MetalFail);
            }

            if (common != null && common.Kind == CommonElementKind.SoftSoil)
            {
                return ResolveSoftSoilReaction(context);
            }

            var table = definition?.ToolReactions;
            if (table == null || !table.TryResolve(context.Tags, out var entry, out var matchedTool))
            {
                return ToolReactionResult.Rejected(FeedbackId.None);
            }

            if (common != null && common.Kind == CommonElementKind.SoftSoil && driver != null)
            {
                var wetMud = string.Equals(
                    driver.VariantState,
                    "WetMud",
                    System.StringComparison.OrdinalIgnoreCase);
                if (string.Equals(
                        driver.VariantState,
                        "CompressedPlatform",
                        System.StringComparison.OrdinalIgnoreCase) ||
                    matchedTool == ToolTag.Pound && !wetMud)
                {
                    return ToolReactionResult.Rejected(FeedbackId.None);
                }
            }

            acceptedHitCount++;
            var required = common != null && common.Kind == CommonElementKind.SoftSoil &&
                           driver != null &&
                           string.Equals(driver.VariantState, "WetMud",
                               System.StringComparison.OrdinalIgnoreCase) &&
                           matchedTool == ToolTag.Shovel
                ? 2
                : Mathf.Max(1, entry.StrengthRequired);
            hitCounts.TryGetValue(matchedTool, out var hitCount);
            hitCount++;
            hitCounts[matchedTool] = hitCount;
            if (hitCount < required)
            {
                var partialChanged = maruDriver != null &&
                                     maruDriver.ApplyPartialToolReaction(entry, context, hitCount, required);
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = partialChanged,
                    ConsumeToolResource = true,
                    Feedback = FeedbackId.Hit,
                };
            }

            hitCounts[matchedTool] = 0;
            var changed = driver != null && driver.ApplyToolReaction(entry, context);
            if (!changed && maruDriver != null)
            {
                changed = maruDriver.ApplyToolReaction(entry, context);
            }
            if (!changed && moonDriver != null)
            {
                changed = moonDriver.ApplyToolReaction(entry, context);
            }
            if (!changed && bridgeDriver != null)
            {
                changed = bridgeDriver.ApplyToolReaction(entry, context);
            }
            if (!changed && palaceDriver != null)
            {
                changed = palaceDriver.ApplyToolReaction(entry, context);
            }
            if (!changed && postDriver != null)
            {
                changed = postDriver.ApplyToolReaction(entry, context);
            }
            if (!changed && sunDriver != null)
            {
                changed = sunDriver.ApplyToolReaction(entry, context);
            }
            if (!changed && polarisDriver != null)
            {
                changed = polarisDriver.ApplyToolReaction(entry, context);
            }
            return new ToolReactionResult
            {
                Accepted = true,
                ChangedState = changed,
                ConsumeToolResource = true,
                Feedback = ResolveFeedback(entry),
            };
        }

        private ToolReactionResult ResolveSoftSoilReaction(ToolReactionContext context)
        {
            if ((context.Tags & ToolTag.Shovel) != 0)
            {
                acceptedHitCount++;
                bool changed = element != null && element.TrySetState(MapElementState.Broken);
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = changed,
                    ConsumeToolResource = true,
                    Feedback = FeedbackId.Hit,
                };
            }
            if ((context.Tags & ToolTag.Pickaxe) != 0)
            {
                acceptedHitCount++;
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = false,
                    ConsumeToolResource = true,
                    Feedback = FeedbackId.Hit,
                };
            }
            if ((context.Tags & ToolTag.Bomb) != 0)
            {
                acceptedHitCount++;
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = false,
                    ConsumeToolResource = false,
                    Feedback = FeedbackId.Hit,
                };
            }
            if ((context.Tags & (ToolTag.LightImpact | ToolTag.HeavyImpact)) != 0)
            {
                return new ToolReactionResult
                {
                    Accepted = true,
                    ChangedState = false,
                    ConsumeToolResource = false,
                    Feedback = FeedbackId.Hit,
                };
            }
            return ToolReactionResult.Rejected(FeedbackId.None);
        }

        private bool RegisterAction(int actionId)
        {
            if (!processedActionIds.Add(actionId))
            {
                return false;
            }

            actionOrder.Enqueue(actionId);
            while (actionOrder.Count > ActionHistoryLimit)
            {
                processedActionIds.Remove(actionOrder.Dequeue());
            }

            return true;
        }

        private void CacheComponents()
        {
            element = element != null ? element : GetComponent<MapElementInstance>();
            driver = driver != null ? driver : GetComponent<CommonElementDriver>();
            maruDriver = maruDriver != null ? maruDriver : GetComponent<MaruElementDriver>();
            moonDriver = moonDriver != null ? moonDriver : GetComponent<MoonElementDriver>();
            bridgeDriver = bridgeDriver != null ? bridgeDriver : GetComponent<BridgeElementDriver>();
            palaceDriver = palaceDriver != null ? palaceDriver : GetComponent<PalaceElementDriver>();
            postDriver = postDriver != null ? postDriver : GetComponent<PostElementDriver>();
            sunDriver = sunDriver != null ? sunDriver : GetComponent<SunElementDriver>();
            polarisDriver = polarisDriver != null ? polarisDriver : GetComponent<PolarisElementDriver>();
        }

        public static FeedbackId ResolveFeedback(ToolReactionEntry entry)
        {
            if (entry.Reaction == ElementReactionType.Break)
            {
                return FeedbackId.Break;
            }
            if (entry.Reaction == ElementReactionType.Disable)
            {
                return FeedbackId.Disable;
            }
            if (string.Equals(entry.ResultState, "WetMud", System.StringComparison.OrdinalIgnoreCase))
            {
                return FeedbackId.WetMud;
            }
            if (string.Equals(entry.ResultState, "Rotate", System.StringComparison.OrdinalIgnoreCase))
            {
                return FeedbackId.Rotate;
            }
            return FeedbackId.Accepted;
        }
    }
}

#endif
