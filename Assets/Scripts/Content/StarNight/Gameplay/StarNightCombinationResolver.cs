using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class StarNightCombinationResolver : MonoBehaviour
    {
        public FableToolResult Apply(FableObject target, FableVerb verb, ResizeIntent resizeIntent, string actorId = "Player")
        {
            if (target == null)
            {
                return FableToolResult.Fail("도구가 닿을 만한 물건이 없다.");
            }

            StarNightRunState run = StarNightRunState.Ensure();
            if (!run.IsToolUnlocked(verb))
            {
                return FableToolResult.Fail("아직 이 생활 도구의 말을 배우지 못했다.");
            }
            if (verb == FableVerb.Link)
            {
                return run.RedThread.Use(target, actorId);
            }
            if (verb == FableVerb.Float)
            {
                return run.CloudBottle.Use(target, actorId);
            }
            if (verb == FableVerb.Deliver)
            {
                return run.Delivery.Use(target, actorId);
            }
            if (verb == FableVerb.Awaken)
            {
                return run.SunSeeds.Use(target, actorId);
            }

            FableToolResult result = target.Apply(verb, resizeIntent);
            if (!result.success)
            {
                return result;
            }

            float scent = run.ConsequenceResolver.ModifyScent(result.scentAdded);
            run.Chapter.AddScent(scent, result.sentence, target.ObjectId);
            StarActionRecord record = run.Actions.Record(new StarActionContext
            {
                actionType = result.overloaded ? StarActionType.ToolOverloaded : StarActionType.ToolApplied,
                actorId = actorId,
                targetId = target.ObjectId,
                tool = verb,
                detail = result.sentence,
                scentDelta = scent,
                causedAccident = result.overloaded
            });

            if (result.overloaded)
            {
                run.AccidentReport.Add(target.DisplayName, "네 번 바뀌어", "주변 물건을 튕겨 냈다", record?.sequence ?? 0);
            }
            else if (target.HasTrait(FableTraits.Explosive) && target.ModificationCount >= 2)
            {
                run.AccidentReport.Add(target.DisplayName, "커진 채 흔들려", "방앗간을 위험하게 만들었다", record?.sequence ?? 0);
            }

            return result;
        }

        public string Preview(FableObject target, FableVerb verb, ResizeIntent resizeIntent)
        {
            if (target == null)
            {
                return "대상을 바라보세요";
            }
            if (!target.Accepts(verb))
            {
                return $"{target.DisplayName}: 반응 없음";
            }

            if (verb == FableVerb.Link)
            {
                return StarNightRunState.Ensure().RedThread.Preview(target);
            }
            if (verb == FableVerb.Float)
            {
                return StarNightRunState.Ensure().CloudBottle.Preview(target);
            }
            if (verb == FableVerb.Deliver)
            {
                return StarNightRunState.Ensure().Delivery.Preview(target);
            }
            if (verb == FableVerb.Awaken)
            {
                return StarNightRunState.Ensure().SunSeeds.Preview(target);
            }

            string verbLabel = verb == FableVerb.Resize
                ? (resizeIntent == ResizeIntent.Enlarge ? "크게" : "작게")
                : verb switch
                {
                    FableVerb.Link => "잇기",
                    FableVerb.Float => "띄우기",
                    FableVerb.Deliver => "보내기",
                    FableVerb.Awaken => "깨우기",
                    _ => verb.ToString()
                };
            return $"{target.DisplayName}을(를) {verbLabel}";
        }
    }
}
