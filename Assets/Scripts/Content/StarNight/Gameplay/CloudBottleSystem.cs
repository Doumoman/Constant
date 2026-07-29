using System;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class CloudBottleSystem : MonoBehaviour
    {
        [SerializeField] private float baseCapacity = 4f;
        [SerializeField] private float collectScent = 5f;
        [SerializeField] private float transferScent = 7f;

        private FableObject source;
        private CloudWeightState sourceState;
        private float heldWeight;

        public FableObject Source => source;
        public float HeldWeight => heldWeight;
        public float Capacity => baseCapacity +
            (StarNightRunState.Instance != null
                ? StarNightRunState.Instance.GetCounter("cloudbottle.capacity_bonus")
                : 0);

        public event Action WeightChanged;

        public FableToolResult Use(FableObject target, string actorId = "Player")
        {
            if (target == null)
            {
                return FableToolResult.Fail("구름병을 기울일 대상을 바라보세요.");
            }
            if (!target.HasTrait(FableTraits.Floatable) || target.IsOverloaded)
            {
                return FableToolResult.Fail($"{target.DisplayName}의 무게는 구름병에 담기지 않는다.");
            }

            CloudWeightState targetState = CloudWeightState.GetOrAdd(target);
            if (targetState == null)
            {
                return FableToolResult.Fail($"{target.DisplayName}에는 옮길 수 있는 물리적 무게가 없다.");
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (heldWeight <= 0f)
            {
                float amount = targetState.Extract(Capacity);
                if (amount <= 0f)
                {
                    return FableToolResult.Fail($"{target.DisplayName}은 더 가벼워질 수 없다.");
                }

                source = target;
                sourceState = targetState;
                heldWeight = amount;
                float scent = run.ConsequenceResolver.ModifyScent(collectScent * Mathf.Max(0.5f, target.ScentWeight));
                run.Chapter.AddScent(scent, "구름병이 무게를 빨아들였다", target.ObjectId);
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.WeightCollected,
                    actorId = actorId,
                    targetId = target.ObjectId,
                    tool = FableVerb.Float,
                    detail = $"{target.DisplayName}에서 무게 {amount:0.0}을 덜어 냈다",
                    scentDelta = scent
                });
                WeightChanged?.Invoke();
                return new FableToolResult
                {
                    success = true,
                    awaitingWeightTarget = true,
                    weightChanged = true,
                    sentence = $"{target.DisplayName}이 가벼워져 떠오른다. 이 무게를 다른 곳에 남겨야 한다.",
                    scentAdded = scent
                };
            }

            if (target == source)
            {
                float returned = heldWeight;
                sourceState?.Deposit(returned);
                ClearBottle();
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.WeightReturned,
                    actorId = actorId,
                    targetId = target.ObjectId,
                    tool = FableVerb.Float,
                    detail = $"{target.DisplayName}에 무게 {returned:0.0}을 되돌렸다"
                });
                return new FableToolResult
                {
                    success = true,
                    weightChanged = true,
                    sentence = $"{target.DisplayName}에 원래 무게를 되돌렸다."
                };
            }

            float moved = heldWeight;
            FableObject previousSource = source;
            targetState.Deposit(moved);
            ClearBottle();
            float transferNoise = run.ConsequenceResolver.ModifyScent(
                transferScent * Mathf.Max(0.5f, (previousSource.ScentWeight + target.ScentWeight) * 0.5f));
            run.Chapter.AddScent(transferNoise, "무게가 다른 대상에 쏟아졌다", target.ObjectId);
            StarActionRecord record = run.Actions.Record(new StarActionContext
            {
                actionType = targetState.IsOverpressured
                    ? StarActionType.CloudBottleOverpressured
                    : StarActionType.WeightTransferred,
                actorId = actorId,
                targetId = $"{previousSource.ObjectId}->{target.ObjectId}",
                tool = FableVerb.Float,
                detail = $"{previousSource.DisplayName}의 무게 {moved:0.0}이 {target.DisplayName}으로 옮겨졌다",
                scentDelta = transferNoise,
                causedAccident = targetState.IsOverpressured,
                witnessed = true
            });

            if (targetState.IsOverpressured)
            {
                float accidentScent = run.ConsequenceResolver.ModifyScent(14f);
                run.Chapter.AddScent(accidentScent, "구름병이 감당할 수 없는 하중을 한곳에 쏟았다", target.ObjectId);
                target.Body.AddForce(UnityEngine.Random.insideUnitCircle.normalized * 7f + Vector2.up * 4f,
                    ForceMode2D.Impulse);
                run.AddCounter("CH3_STORM_DAMAGE");
                run.SetFlag("CH3_STORM_STARTED");
                run.AccidentReport.Add(previousSource.DisplayName, "무게를 잃어 떠오르고",
                    $"{target.DisplayName}이 과압으로 구조물을 흔들었다", record?.sequence ?? 0);
            }

            return new FableToolResult
            {
                success = true,
                overloaded = targetState.IsOverpressured,
                weightChanged = true,
                sentence = targetState.IsOverpressured
                    ? $"{target.DisplayName}에 무게가 몰렸다. 목장 전체가 흔들린다!"
                    : $"{previousSource.DisplayName}은 뜨고, {target.DisplayName}은 무거워져 내려앉는다.",
                scentAdded = transferNoise
            };
        }

        public string Preview(FableObject target)
        {
            if (target == null)
            {
                return heldWeight > 0f ? "무게를 받을 대상을 바라보세요" : "가볍게 만들 대상을 바라보세요";
            }
            if (heldWeight <= 0f)
            {
                return $"{target.DisplayName}의 무게를 병에 담기";
            }
            if (target == source)
            {
                return $"{target.DisplayName}에 무게 되돌리기";
            }
            return $"{source.DisplayName}의 무게 {heldWeight:0.0}을 {target.DisplayName}으로 옮기기";
        }

        public int AddCapacity(int amount = 2)
        {
            return StarNightRunState.Ensure().AddCounter("cloudbottle.capacity_bonus", Mathf.Max(0, amount));
        }

        public void ResetForChapter()
        {
            if (heldWeight > 0f && sourceState != null)
            {
                sourceState.Deposit(heldWeight);
            }
            ClearBottle();
        }

        private void ClearBottle()
        {
            source = null;
            sourceState = null;
            heldWeight = 0f;
            WeightChanged?.Invoke();
        }
    }
}
