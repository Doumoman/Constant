using System;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StarNightInventory))]
    public sealed class StarNightPlayerAgent : MonoBehaviour
    {
        [SerializeField] private float interactionRadius = 2.4f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private int maximumHealth = 4;
        [SerializeField] private FableVerb selectedTool = FableVerb.Resize;
        [SerializeField] private ResizeIntent resizeIntent = ResizeIntent.Enlarge;

        private readonly Collider2D[] nearby = new Collider2D[24];
        private ContactFilter2D targetFilter;
        private StarNightCombinationResolver resolver;
        private StarNightInventory inventory;
        private FableObject currentTarget;
        private int health;
        private float invulnerableUntil;

        public int Health => health;
        public int MaximumHealth => maximumHealth;
        public FableVerb SelectedTool => selectedTool;
        public ResizeIntent ResizeIntent => resizeIntent;
        public FableObject CurrentTarget => currentTarget;
        public StarNightInventory Inventory => inventory;
        public event Action<int, int> HealthChanged;
        public event Action SelectionChanged;

        private void Awake()
        {
            targetFilter = ContactFilter2D.noFilter;
            targetFilter.SetLayerMask(targetMask);
            inventory = GetComponent<StarNightInventory>();
            resolver = FindFirstObjectByType<StarNightCombinationResolver>();
            if (resolver == null)
            {
                resolver = StarNightRunState.Ensure().gameObject.AddComponent<StarNightCombinationResolver>();
            }
            health = maximumHealth;
        }

        private void Update()
        {
            FindTarget();

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (selectedTool == FableVerb.Resize)
                {
                    resizeIntent = resizeIntent == ResizeIntent.Enlarge ? ResizeIntent.Shrink : ResizeIntent.Enlarge;
                }
                SelectionChanged?.Invoke();
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                CycleTool();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                UseTool();
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                Interact();
            }
            if (Input.GetKeyDown(KeyCode.G))
            {
                DropSelected();
            }

            for (int i = 0; i < 8; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
                {
                    inventory.Select(i);
                }
            }
        }

        private void FindTarget()
        {
            int count = Physics2D.OverlapCircle(transform.position, interactionRadius, targetFilter, nearby);
            float nearest = float.MaxValue;
            currentTarget = null;
            for (int i = 0; i < count; i++)
            {
                FableObject candidate = nearby[i] != null ? nearby[i].GetComponentInParent<FableObject>() : null;
                if (candidate == null || candidate.IsStored)
                {
                    continue;
                }

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    currentTarget = candidate;
                }
            }
        }

        private void UseTool()
        {
            FableToolResult result = resolver.Apply(currentTarget, selectedTool, resizeIntent);
            StarNightHUD.Instance?.Toast(result.sentence);
            SelectionChanged?.Invoke();
        }

        private void CycleTool()
        {
            StarNightRunState run = StarNightRunState.Ensure();
            FableVerb[] order = { FableVerb.Resize, FableVerb.Link, FableVerb.Float, FableVerb.Deliver, FableVerb.Awaken };
            int start = Array.IndexOf(order, selectedTool);
            for (int offset = 1; offset <= order.Length; offset++)
            {
                FableVerb candidate = order[(start + offset) % order.Length];
                if (!run.IsToolUnlocked(candidate))
                {
                    continue;
                }

                selectedTool = candidate;
                SelectionChanged?.Invoke();
                string toolMessage = candidate switch
                {
                    FableVerb.Link => "까치의 붉은 실을 꺼냈다.",
                    FableVerb.Float => "구름병의 마개를 열었다.",
                    FableVerb.Deliver => "별 우편 도장에 잉크를 묻혔다.",
                    FableVerb.Awaken => "햇빛 씨앗을 손바닥 위에 올렸다.",
                    _ => "달토끼의 절구를 들었다."
                };
                StarNightHUD.Instance?.Toast(toolMessage);
                return;
            }
        }

        public void SelectTool(FableVerb tool)
        {
            if (StarNightRunState.Ensure().IsToolUnlocked(tool))
            {
                selectedTool = tool;
                SelectionChanged?.Invoke();
            }
        }

        private void Interact()
        {
            IStarNightInteractable interactable = FindNearestInteractable();
            if (interactable != null)
            {
                interactable.Interact(this);
                return;
            }

            if (currentTarget != null && inventory.TryStore(currentTarget))
            {
                StarNightHUD.Instance?.Toast($"{currentTarget.DisplayName}을 챙겼다.");
            }
        }

        private IStarNightInteractable FindNearestInteractable()
        {
            int count = Physics2D.OverlapCircle(transform.position, interactionRadius, targetFilter, nearby);
            IStarNightInteractable nearest = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                MonoBehaviour[] behaviours = nearby[i] != null
                    ? nearby[i].GetComponentsInParent<MonoBehaviour>(true)
                    : Array.Empty<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is not IStarNightInteractable candidate)
                    {
                        continue;
                    }

                    float distance = (behaviour.transform.position - transform.position).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = candidate;
                    }
                }
            }
            return nearest;
        }

        private void DropSelected()
        {
            Vector2 direction = transform.localScale.x < 0f ? Vector2.left : Vector2.right;
            FableObject dropped = inventory.DropSelected(transform.position + (Vector3)direction, direction * 2.5f + Vector2.up);
            if (dropped != null)
            {
                StarNightHUD.Instance?.Toast($"{dropped.DisplayName}을 내려놓았다.");
            }
        }

        public void TakeDamage(int amount, string reason)
        {
            if (amount <= 0 || Time.time < invulnerableUntil)
            {
                return;
            }

            invulnerableUntil = Time.time + 1.2f;
            health = Mathf.Max(0, health - amount);
            HealthChanged?.Invoke(health, maximumHealth);
            StarNightHUD.Instance?.Toast(reason);
            if (health <= 0)
            {
                StarNightRunState.Instance?.EndRun(StarRunEndReason.HealthLost);
                StarNightHUD.Instance?.ShowEnding("별빛이 꺼졌다", StarNightRunState.Instance?.AccidentReport.BuildReport());
            }
        }

        public void ForcedReturn()
        {
            StarNightRunState run = StarNightRunState.Instance;
            StarNightChapterState chapter = run?.Chapter;
            bool thirdBell = chapter != null &&
                             chapter.GateLoopEnabled &&
                             chapter.BellPhase == StarBellPhase.Third;
            run?.Actions.Record(new StarActionContext
            {
                actionType = StarActionType.PlayerCaught,
                actorId = "Maru",
                targetId = "Player",
                detail = thirdBell
                    ? "세 번째 방울 뒤에도 머물러 별문이 닫히는 동안 마루가 아이의 옷깃을 물었다"
                    : "마루가 아이의 옷깃을 물고 집으로 데려갔다",
                gateContributions = chapter?.GateContributions ?? 0,
                gateReady = chapter?.GateReady ?? false,
                gateActivated = chapter?.GateActivated ?? false,
                bellPhase = chapter != null ? (int)chapter.BellPhase : 0,
                witnessed = true
            });
            run?.EndRun(StarRunEndReason.ForcedReturnByMaru);
            string explanation = thirdBell
                ? "세 번째 방울은 마루가 물건 대신 플레이어를 직접 쫓는 단계다. 출항 가능한 순간을 넘겨 붙잡혔다.\n\n"
                : string.Empty;
            StarNightHUD.Instance?.ShowEnding("마루에게 붙잡혔다",
                explanation + run?.Watcher.ResolveRaniSummary());
            enabled = false;
        }

        public string CurrentPrompt()
        {
            IStarNightInteractable interactable = FindNearestInteractable();
            if (interactable != null)
            {
                return $"[X] {interactable.Prompt}";
            }
            if (currentTarget != null && currentTarget.HasTrait(FableTraits.Carryable))
            {
                return $"[X] {currentTarget.DisplayName} 줍기  ·  [E] {resolver.Preview(currentTarget, selectedTool, resizeIntent)}";
            }
            return currentTarget != null ? $"[E] {resolver.Preview(currentTarget, selectedTool, resizeIntent)}" : string.Empty;
        }
    }

    public interface IStarNightInteractable
    {
        string Prompt { get; }
        void Interact(StarNightPlayerAgent player);
    }
}
