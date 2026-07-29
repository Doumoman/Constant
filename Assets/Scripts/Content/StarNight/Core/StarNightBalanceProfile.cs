using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StarFetchingNight
{
    public enum RunPaceBand
    {
        Unknown,
        TooFast,
        Target,
        TooSlow
    }

    [Serializable]
    public sealed class ChapterBalanceTarget
    {
        public StarChapterId chapter;
        public string displayName;
        public float minimumMinutes;
        public float maximumMinutes;
        public bool gateLoopChapter;
        public List<string> routeIds = new();
    }

    [Serializable]
    public sealed class RouteSelectionStat
    {
        public StarChapterId chapter;
        public string routeId;
        public int selectedCount;
    }

    [Serializable]
    public sealed class EndingSelectionStat
    {
        public PolarisEndingType ending;
        public int selectedCount;
    }

    [Serializable]
    public sealed class ChapterBalanceSample
    {
        public StarChapterId chapter;
        public float durationSeconds;
        public int highestBell;
        public float exitAlert;
        public bool temptationEntered;
        public List<string> contributedRoutes = new();

        public string RouteCombination => contributedRoutes == null || contributedRoutes.Count == 0
            ? "None"
            : string.Join("+", contributedRoutes.OrderBy(route => route));
    }

    [Serializable]
    public sealed class RunBalanceSnapshot
    {
        public int seed;
        public float durationSeconds;
        public StarRunEndReason endReason;
        public PolarisEndingType ending;
        public int informationUnits;
        public int accidentCount;
        public int contextualAccidentCount;
        public List<ChapterBalanceSample> chapters = new();

        public bool JourneyCompleted => endReason == StarRunEndReason.JourneyComplete;
        public bool StarRoad => ending == PolarisEndingType.StarRoad;
        public RunPaceBand PaceBand => StarNightBalanceProfile.EvaluateRunPace(durationSeconds, StarRoad);
        public int EligibleTemptationChapters => chapters?.Count(sample =>
            StarNightBalanceProfile.GetTarget(sample.chapter)?.gateLoopChapter == true) ?? 0;
        public int TemptationEntries => chapters?.Count(sample => sample.temptationEntered) ?? 0;
        public float TemptationRate => EligibleTemptationChapters <= 0
            ? 0f
            : (float)TemptationEntries / EligibleTemptationChapters;

        public string BuildTechnicalReport()
        {
            float minutes = durationSeconds / 60f;
            string chapterReport = chapters == null
                ? string.Empty
                : string.Join(" | ", chapters.Select(sample =>
                    $"{sample.chapter}:{sample.durationSeconds / 60f:0.0}m," +
                    $"{sample.RouteCombination},bell{sample.highestBell}," +
                    $"temptation={(sample.temptationEntered ? "Y" : "N")}"));
            return $"seed={seed}; end={endReason}; ending={ending}; time={minutes:0.0}m; " +
                   $"pace={PaceBand}; temptation={TemptationEntries}/{EligibleTemptationChapters}; " +
                   $"info={informationUnits}; accidents={contextualAccidentCount}/{accidentCount}; " +
                   $"chapters=[{chapterReport}]";
        }
    }

    [Serializable]
    public sealed class StarNightBalanceAggregate
    {
        public int totalRuns;
        public int completedJourneys;
        public int generalEndingRuns;
        public int starRoadRuns;
        public float generalEndingSeconds;
        public float starRoadSeconds;
        public int generalInformationUnits;
        public int starRoadInformationUnits;
        public int eligibleTemptationChapters;
        public int temptationEntries;
        public int accidentCount;
        public int contextualAccidentCount;
        public List<RouteSelectionStat> routeSelections = new();
        public List<EndingSelectionStat> endingSelections = new();

        public float TemptationRate => eligibleTemptationChapters <= 0
            ? 0f
            : (float)temptationEntries / eligibleTemptationChapters;
        public float GeneralAverageMinutes => generalEndingRuns <= 0
            ? 0f
            : generalEndingSeconds / generalEndingRuns / 60f;
        public float StarRoadAverageMinutes => starRoadRuns <= 0
            ? 0f
            : starRoadSeconds / starRoadRuns / 60f;
        public float GeneralAverageInformation => generalEndingRuns <= 0
            ? 0f
            : (float)generalInformationUnits / generalEndingRuns;
        public float StarRoadAverageInformation => starRoadRuns <= 0
            ? 0f
            : (float)starRoadInformationUnits / starRoadRuns;
        public float ContextualAccidentRate => accidentCount <= 0
            ? 1f
            : (float)contextualAccidentCount / accidentCount;

        public void Add(RunBalanceSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            totalRuns++;
            if (snapshot.JourneyCompleted)
            {
                completedJourneys++;
            }

            if (snapshot.ending != PolarisEndingType.None)
            {
                EndingSelectionStat ending = endingSelections.Find(stat => stat.ending == snapshot.ending);
                if (ending == null)
                {
                    ending = new EndingSelectionStat { ending = snapshot.ending };
                    endingSelections.Add(ending);
                }
                ending.selectedCount++;

                if (snapshot.StarRoad)
                {
                    starRoadRuns++;
                    starRoadSeconds += snapshot.durationSeconds;
                    starRoadInformationUnits += snapshot.informationUnits;
                }
                else
                {
                    generalEndingRuns++;
                    generalEndingSeconds += snapshot.durationSeconds;
                    generalInformationUnits += snapshot.informationUnits;
                }
            }

            eligibleTemptationChapters += snapshot.EligibleTemptationChapters;
            temptationEntries += snapshot.TemptationEntries;
            accidentCount += snapshot.accidentCount;
            contextualAccidentCount += snapshot.contextualAccidentCount;

            if (snapshot.chapters == null)
            {
                return;
            }
            foreach (ChapterBalanceSample sample in snapshot.chapters)
            {
                if (sample?.contributedRoutes == null)
                {
                    continue;
                }
                foreach (string routeId in sample.contributedRoutes.Distinct())
                {
                    RouteSelectionStat route = routeSelections.Find(stat =>
                        stat.chapter == sample.chapter && stat.routeId == routeId);
                    if (route == null)
                    {
                        route = new RouteSelectionStat { chapter = sample.chapter, routeId = routeId };
                        routeSelections.Add(route);
                    }
                    route.selectedCount++;
                }
            }
        }

        public float GetRouteShare(StarChapterId chapter, string routeId)
        {
            int total = routeSelections
                .Where(stat => stat.chapter == chapter)
                .Sum(stat => stat.selectedCount);
            if (total <= 0)
            {
                return 0f;
            }
            int selected = routeSelections
                .Where(stat => stat.chapter == chapter && stat.routeId == routeId)
                .Sum(stat => stat.selectedCount);
            return (float)selected / total;
        }

        public string BuildTechnicalReport()
        {
            string temptationStatus = eligibleTemptationChapters == 0
                ? "표본 대기"
                : StarNightBalanceProfile.IsTemptationRateOnTarget(TemptationRate)
                    ? "목표"
                    : "조정 필요";
            return $"runs={totalRuns}; completed={completedJourneys}; " +
                   $"general={generalEndingRuns}@{GeneralAverageMinutes:0.0}m; " +
                   $"starRoad={starRoadRuns}@{StarRoadAverageMinutes:0.0}m; " +
                   $"temptation={TemptationRate:P0}({temptationStatus}); " +
                   $"info={GeneralAverageInformation:0.0}/{StarRoadAverageInformation:0.0}; " +
                   $"accidentContext={ContextualAccidentRate:P0}";
        }
    }

    public static class StarNightBalanceProfile
    {
        public const float GeneralRunMinimumMinutes = 45f;
        public const float GeneralRunMaximumMinutes = 60f;
        public const float StarRoadMinimumMinutes = 60f;
        public const float StarRoadMaximumMinutes = 80f;
        public const float TemptationRateMinimum = 0.55f;
        public const float TemptationRateMaximum = 0.70f;
        public const int GeneralEndingInformationUnits = 3;
        public const int StarRoadAdditionalInformationUnits = 4;

        private static readonly List<ChapterBalanceTarget> Targets = new()
        {
            Target(StarChapterId.Prologue, "프롤로그", 5f, 7f, false),
            Target(StarChapterId.MoonRabbitMill, "CH1 달토끼 방앗간", 8f, 10f, true,
                "CH1_ROUTE_MILL", "CH1_ROUTE_MINE", "CH1_ROUTE_STORAGE"),
            Target(StarChapterId.MagpieBridge, "CH2 까치다리 정거장", 8f, 10f, true,
                "CH2_ROUTE_NEW_ANCHOR", "CH2_ROUTE_STORM_ANCHOR", "CH2_ROUTE_OLD_BRIDGE"),
            Target(StarChapterId.CloudWhaleRanch, "CH3 구름고래 목장", 8f, 10f, true,
                "CH3_ROUTE_RANCH_WHEEL", "CH3_ROUTE_STORM_RIDGE", "CH3_ROUTE_GURU_BREATH"),
            Target(StarChapterId.StarPostOffice, "CH4 별 우체국", 10f, 12f, true,
                "CH4_ROUTE_REGULAR_POST", "CH4_ROUTE_DEAD_LETTER", "CH4_ROUTE_SEALED_LETTER"),
            Target(StarChapterId.SleepingSunGarden, "CH5 잠든 해님의 정원", 8f, 10f, true,
                "CH5_ROUTE_STORED_SUNLIGHT", "CH5_ROUTE_GREENHOUSE_TOP", "CH5_ROUTE_HAOREUM_WAKE"),
            Target(StarChapterId.PolarisObservatory, "CH6 북극성 관측소", 12f, 18f, false)
        };

        public static IReadOnlyList<ChapterBalanceTarget> ChapterTargets => Targets;
        public static int StarRoadInformationUnits =>
            GeneralEndingInformationUnits + StarRoadAdditionalInformationUnits;

        public static ChapterBalanceTarget GetTarget(StarChapterId chapter) =>
            Targets.Find(target => target.chapter == chapter);

        public static RunPaceBand EvaluateRunPace(float seconds, bool starRoad)
        {
            if (seconds <= 0f)
            {
                return RunPaceBand.Unknown;
            }
            float minutes = seconds / 60f;
            float minimum = starRoad ? StarRoadMinimumMinutes : GeneralRunMinimumMinutes;
            float maximum = starRoad ? StarRoadMaximumMinutes : GeneralRunMaximumMinutes;
            if (minutes < minimum) return RunPaceBand.TooFast;
            if (minutes > maximum) return RunPaceBand.TooSlow;
            return RunPaceBand.Target;
        }

        public static RunPaceBand EvaluateChapterPace(StarChapterId chapter, float seconds)
        {
            ChapterBalanceTarget target = GetTarget(chapter);
            if (target == null || seconds <= 0f)
            {
                return RunPaceBand.Unknown;
            }
            float minutes = seconds / 60f;
            if (minutes < target.minimumMinutes) return RunPaceBand.TooFast;
            if (minutes > target.maximumMinutes) return RunPaceBand.TooSlow;
            return RunPaceBand.Target;
        }

        public static bool IsTemptationRateOnTarget(float rate) =>
            rate >= TemptationRateMinimum && rate <= TemptationRateMaximum;

        public static int CountInformationUnits(StarNightRunState run)
        {
            if (run == null)
            {
                return 0;
            }

            int units = 0;
            if (run.GetFlag("TICKET_MAP_UNLOCKED")) units++;
            if (run.GetFlag("POLARIS_ALL_RECORDS_SEEN")) units++;
            if (run.GetFlag("POLARIS_TRUTH_SEEN")) units++;
            if (PolarisFinaleState.HasStarRoadMemory(run)) units++;
            if (PolarisFinaleState.HasStarRoadConnection(run)) units++;
            if (PolarisFinaleState.HasStarRoadDelivery(run)) units++;
            if (PolarisFinaleState.HasStarRoadLight(run)) units++;
            return units;
        }

        private static ChapterBalanceTarget Target(StarChapterId chapter, string name,
            float minimumMinutes, float maximumMinutes, bool gateLoop, params string[] routeIds)
        {
            return new ChapterBalanceTarget
            {
                chapter = chapter,
                displayName = name,
                minimumMinutes = minimumMinutes,
                maximumMinutes = maximumMinutes,
                gateLoopChapter = gateLoop,
                routeIds = routeIds?.ToList() ?? new List<string>()
            };
        }
    }

    public static class StarNightTelemetryStore
    {
        public const string PlayerPrefsKey = "StarNight.M6.BalanceAggregate.v1";

        public static StarNightBalanceAggregate Load()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new StarNightBalanceAggregate();
            }
            StarNightBalanceAggregate aggregate = JsonUtility.FromJson<StarNightBalanceAggregate>(json);
            return aggregate ?? new StarNightBalanceAggregate();
        }

        public static void Save(StarNightBalanceAggregate aggregate)
        {
            PlayerPrefs.SetString(PlayerPrefsKey,
                JsonUtility.ToJson(aggregate ?? new StarNightBalanceAggregate()));
            PlayerPrefs.Save();
        }

        public static void Record(RunBalanceSnapshot snapshot)
        {
            StarNightBalanceAggregate aggregate = Load();
            aggregate.Add(snapshot);
            Save(aggregate);
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsKey);
            PlayerPrefs.Save();
        }
    }
}
