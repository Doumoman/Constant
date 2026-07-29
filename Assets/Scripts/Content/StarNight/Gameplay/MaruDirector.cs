using System.Collections;
using UnityEngine;

namespace StarFetchingNight
{
    [DisallowMultipleComponent]
    public sealed class MaruDirector : MonoBehaviour
    {
        [SerializeField] private Transform maru;
        [SerializeField] private float chaseSpeed = 4.8f;
        [SerializeField] private float aerialChaseSpeed = 6.2f;
        [SerializeField] private float catchDistance = 1.15f;
        [SerializeField] private float warningDuration = 5f;
        [SerializeField] private float replayInterval = 0.8f;
        [SerializeField] private Vector3 hiddenPosition = new(54f, 9f, 0f);

        private StarNightPlayerAgent player;
        private StarNightChapterState chapter;
        private bool summoned;
        private bool chasing;
        private float chaseStartTime;
        private float blindedUntil;
        private Vector3 spawnPoint;
        private MaruHuntMode huntMode;
        private string currentTargetKind = "None";

        public bool Chasing => chasing;
        public bool Summoned => summoned;
        public MaruHuntMode HuntMode => huntMode;
        public string CurrentTargetKind => currentTargetKind;
        public bool CanTargetPlayer => huntMode == MaruHuntMode.PlayerHunt;
        public float ArrivalProgress => chapter != null && chapter.GateLoopEnabled
            ? (float)chapter.BellPhase / (float)StarBellPhase.Third
            : !summoned ? 0f : Mathf.Clamp01((Time.time - chaseStartTime) / warningDuration);

        public void Configure(Transform maruTransform, Vector3 spawn)
        {
            maru = maruTransform;
            hiddenPosition = spawn;
        }

        private void Start()
        {
            player = FindFirstObjectByType<StarNightPlayerAgent>();
            chapter = StarNightRunState.Ensure().Chapter;
            chapter.ScentChanged += OnScentChanged;
            spawnPoint = maru != null ? maru.position : hiddenPosition;
            if (maru != null)
            {
                maru.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (chapter != null)
            {
                chapter.ScentChanged -= OnScentChanged;
            }
        }

        private void OnScentChanged(float scent, StarScentStage stage)
        {
            if (chapter != null && chapter.GateLoopEnabled)
            {
                return;
            }

            if (stage >= StarScentStage.Bell && !summoned)
            {
                summoned = true;
                chaseStartTime = Time.time;
                StarNightHUD.Instance?.Toast("멀리서 방울이 한 번 울렸다. 마루가 냄새를 찾고 있다.", 4f);
            }
            if (stage == StarScentStage.ReturnTime && summoned)
            {
                BeginChase();
            }
        }

        private void Update()
        {
            if (!summoned || player == null || maru == null)
            {
                return;
            }
            if (Time.time < blindedUntil)
            {
                return;
            }

            if (!chapter.GateLoopEnabled && !chasing && Time.time - chaseStartTime >= warningDuration)
            {
                BeginChase();
            }
            if (!chasing)
            {
                return;
            }

            Transform target = huntMode == MaruHuntMode.StationHunt
                ? ChooseStationTarget()
                : ChoosePlayerHuntTarget();
            if (target == null)
            {
                currentTargetKind = "None";
                return;
            }

            Vector3 delta = target.position - maru.position;
            float speed = IsAirborneTarget(target) ? aerialChaseSpeed : chaseSpeed;
            maru.position += delta.normalized * speed * Time.deltaTime;
            if (target == player.transform && delta.sqrMagnitude <= catchDistance * catchDistance)
            {
                player.ForcedReturn();
                chasing = false;
            }
            else if (target.TryGetComponent(out FableObject objectTarget) && delta.sqrMagnitude < 1f)
            {
                objectTarget.SetStored(true);
                if (objectTarget.HasTrait(FableTraits.LastLetter))
                {
                    StarNightRunState.Instance.SetFlag("CH4_LETTER_LOST_TO_MARU");
                    StarNightRunState.Instance.SetFlag("CH4_LETTER_STATE_LOST_TO_MARU");
                    StarNightRunState.Instance.Actions.Record(new StarActionContext
                    {
                        actionType = StarActionType.ParcelIntercepted,
                        actorId = "Maru",
                        targetId = objectTarget.ObjectId,
                        detail = "마루가 라니에게 보내진 마지막 편지를 물어 갔다",
                        causedAccident = true,
                        witnessed = true
                    });
                }
                chapter.AddScent(-Mathf.Max(3f, objectTarget.ScentWeight * 4f), "마루가 가장 진한 냄새를 물어 갔다", objectTarget.ObjectId);
                StarNightRunState.Instance.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.MaruLured,
                    actorId = "Maru",
                    targetId = objectTarget.ObjectId,
                    detail = $"{objectTarget.DisplayName}이 마루를 잠시 유인했다"
                });
            }
            else if (target.TryGetComponent(out MaruNpcTarget npcTarget) &&
                     delta.sqrMagnitude < catchDistance * catchDistance)
            {
                string npcId = npcTarget.NpcId;
                string displayName = npcTarget.DisplayName;
                if (npcTarget.TryTake())
                {
                    StarNightRunState.Instance.Actions.Record(new StarActionContext
                    {
                        actionType = StarActionType.MaruTookNpc,
                        actorId = "Maru",
                        targetId = npcId,
                        detail = $"두 번째 방울 뒤 마루가 {displayName}의 냄새를 물어 갔다",
                        gateContributions = chapter.GateContributions,
                        gateReady = chapter.GateReady,
                        gateActivated = chapter.GateActivated,
                        bellPhase = (int)chapter.BellPhase,
                        witnessed = true
                    });
                    StarNightHUD.Instance?.Toast($"마루가 {displayName}을 물어 갔다!", 4.5f);
                }
            }
        }

        private void BeginChase()
        {
            if (chasing)
            {
                return;
            }
            chasing = true;
            huntMode = MaruHuntMode.PlayerHunt;
            maru.gameObject.SetActive(true);
            maru.position = spawnPoint;
            StarNightHUD.Instance?.Toast("마루가 왔다. 공중의 가벼운 물건과 진한 냄새를 먼저 노린다!", 5f);
            if (StarNightRunState.Instance?.AccidentReport.Steps.Count > 0)
            {
                StartCoroutine(ReplayAccidentChain());
            }
        }

        public void ApplyBellPhase(StarBellPhase phase)
        {
            switch (phase)
            {
                case StarBellPhase.None:
                    huntMode = MaruHuntMode.Hidden;
                    summoned = false;
                    chasing = false;
                    currentTargetKind = "None";
                    if (maru != null)
                    {
                        maru.gameObject.SetActive(false);
                    }
                    break;
                case StarBellPhase.First:
                    huntMode = MaruHuntMode.TraceOnly;
                    summoned = false;
                    chasing = false;
                    currentTargetKind = "Trace";
                    if (maru != null)
                    {
                        maru.gameObject.SetActive(false);
                    }
                    break;
                case StarBellPhase.Second:
                    huntMode = MaruHuntMode.StationHunt;
                    summoned = true;
                    chasing = true;
                    currentTargetKind = "Station";
                    ActivateAtSpawnIfNeeded();
                    break;
                case StarBellPhase.Third:
                    huntMode = MaruHuntMode.PlayerHunt;
                    summoned = true;
                    chasing = true;
                    currentTargetKind = "Player";
                    ActivateAtSpawnIfNeeded();
                    break;
            }
        }

        private void ActivateAtSpawnIfNeeded()
        {
            if (maru == null)
            {
                return;
            }
            if (!maru.gameObject.activeSelf)
            {
                maru.position = spawnPoint;
                maru.gameObject.SetActive(true);
            }
        }

        private Transform ChoosePlayerHuntTarget()
        {
            FableObject[] objects = FindObjectsByType<FableObject>(FindObjectsSortMode.None);
            FableObject best = null;
            float bestScore = 0f;
            foreach (FableObject candidate in objects)
            {
                if (candidate.IsStored || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float score = CalculateTargetScore(candidate, player.transform.position.y);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            float playerScore = 8f + chapter.Scent * 0.08f;
            if (best != null && bestScore > playerScore)
            {
                currentTargetKind = "Object";
                return best.transform;
            }

            currentTargetKind = "Player";
            return player.transform;
        }

        private Transform ChooseStationTarget()
        {
            Transform best = null;
            float bestScore = float.MinValue;
            foreach (FableObject candidate in FindObjectsByType<FableObject>(FindObjectsSortMode.None))
            {
                float score = CalculateTargetScore(candidate, player.transform.position.y);
                if (score <= float.MinValue)
                {
                    continue;
                }
                score += 10f - Vector3.Distance(maru.position, candidate.transform.position) * 0.08f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate.transform;
                    currentTargetKind = "Object";
                }
            }

            foreach (MaruNpcTarget candidate in FindObjectsByType<MaruNpcTarget>(FindObjectsSortMode.None))
            {
                if (candidate.Taken || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float score = candidate.TargetPriority -
                              Vector3.Distance(maru.position, candidate.transform.position) * 0.08f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate.transform;
                    currentTargetKind = "Npc";
                }
            }
            return best;
        }

        public static float CalculateTargetScore(FableObject candidate, float playerHeight)
        {
            if (candidate == null || candidate.IsStored || !candidate.gameObject.activeInHierarchy)
            {
                return float.MinValue;
            }

            float score = candidate.ScentWeight * (1f + candidate.ModificationCount);
            if (candidate.HasTrait(FableTraits.MoonCake)) score += 6f;
            if (candidate.IsOverloaded) score += 8f;
            if (candidate.HasTrait(FableTraits.RainCloud)) score += 3f;
            if (candidate.HasTrait(FableTraits.LastLetter)) score += 25f;
            if (candidate.HasTrait(FableTraits.SunlightSource)) score += 8f;
            if (candidate.HasTrait(FableTraits.BrightSource)) score += 12f;

            CloudWeightState weight = candidate.GetComponent<CloudWeightState>();
            bool airborne = weight != null
                ? weight.IsAirborne
                : candidate.Body != null &&
                  (candidate.Body.gravityScale <= 0f || candidate.transform.position.y > playerHeight + 2f);
            if (airborne)
            {
                score += 7f;
            }
            return score;
        }

        public void Blind(float duration)
        {
            blindedUntil = Mathf.Max(blindedUntil, Time.time + Mathf.Max(0.5f, duration));
            if (maru != null && maru.gameObject.activeInHierarchy)
            {
                foreach (SpriteRenderer renderer in maru.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.color = new Color(1f, 0.88f, 0.4f);
                }
            }
            StarNightHUD.Instance?.Toast("마루가 강한 빛에 눈을 감았다. 잠시 냄새의 방향을 잃는다.", 4f);
        }

        private static bool IsAirborneTarget(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            CloudWeightState weight = target.GetComponent<CloudWeightState>();
            if (weight != null)
            {
                return weight.IsAirborne;
            }
            Rigidbody2D body = target.GetComponent<Rigidbody2D>();
            return body != null && body.gravityScale <= 0f;
        }

        private IEnumerator ReplayAccidentChain()
        {
            StarNightRunState run = StarNightRunState.Instance;
            if (run == null)
            {
                yield break;
            }

            int replayed = 0;
            for (int i = 0; i < run.AccidentReport.Steps.Count; i++)
            {
                AccidentStep step = run.AccidentReport.Steps[i];
                StarNightHUD.Instance?.Toast(
                    $"마루가 사고 냄새를 되짚는다 · {step.subject} {step.verb} {step.result}",
                    replayInterval + 0.25f);
                replayed++;
                yield return new WaitForSeconds(replayInterval);
            }

            if (replayed > 0)
            {
                run.Actions.Record(new StarActionContext
                {
                    actionType = StarActionType.AccidentReplayed,
                    actorId = "Maru",
                    targetId = replayed.ToString(),
                    detail = $"마루가 최근 사고 {replayed}단계를 순서대로 되짚었다",
                    witnessed = true
                });
            }
        }
    }
}
