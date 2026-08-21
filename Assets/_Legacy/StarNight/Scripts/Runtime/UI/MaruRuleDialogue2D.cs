#if LEGACY_DISABLED
using System;
using StarNight.Maru.P8;
using UnityEngine;
using Yarn.Unity;

namespace StarNight.UI
{
    public enum MaruRuleTopic
    {
        FirstBell = 0,
        StatueBroken = 1,
        BiteEscape = 2
    }

    [DefaultExecutionOrder(95)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DialogueRunner))]
    public sealed class MaruRuleDialogue2D : MonoBehaviour
    {
        public const string FirstBellNode = "Maru_FirstBell";
        public const string StatueBrokenNode = "Maru_StatueBroken";
        public const string BiteEscapeNode = "Maru_BiteEscape";

        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private YarnProject narrativeProject;
        [SerializeField] private MaruRuleLinePresenter2D linePresenter;
        [SerializeField] private P8MaruTimeline2D timeline;
        [SerializeField] private P8HomecomingStatue2D statue;
        [SerializeField] private P8MaruBiteController2D biteController;
        [SerializeField] private bool firstBellPlayed;
        [SerializeField] private bool statueBrokenPlayed;
        [SerializeField] private bool biteEscapePlayed;

        private bool subscribed;

        public event Action<MaruRuleTopic, string> TopicRequested;

        public DialogueRunner Runner => dialogueRunner;
        public YarnProject NarrativeProject => narrativeProject;
        public MaruRuleLinePresenter2D LinePresenter => linePresenter;
        public string LastRequestedNode { get; private set; } = string.Empty;
        public int RequestedCount { get; private set; }
        public bool IsProjectBound => ResolveProgramReady();

        public void Configure(
            P8MaruTimeline2D targetTimeline,
            P8HomecomingStatue2D targetStatue,
            P8MaruBiteController2D targetBiteController)
        {
            Unsubscribe();
            timeline = targetTimeline;
            statue = targetStatue;
            biteController = targetBiteController;
            if (Application.isPlaying)
            {
                Subscribe();
            }
        }

        public bool TryBindProject(UnityEngine.Object projectAsset)
        {
            if (!(projectAsset is YarnProject project))
            {
                return false;
            }

            narrativeProject = project;
            return true;
        }

        public bool HasPlayed(MaruRuleTopic topic)
        {
            switch (topic)
            {
                case MaruRuleTopic.StatueBroken:
                    return statueBrokenPlayed;
                case MaruRuleTopic.BiteEscape:
                    return biteEscapePlayed;
                default:
                    return firstBellPlayed;
            }
        }

        public static string NodeFor(MaruRuleTopic topic)
        {
            switch (topic)
            {
                case MaruRuleTopic.StatueBroken:
                    return StatueBrokenNode;
                case MaruRuleTopic.BiteEscape:
                    return BiteEscapeNode;
                default:
                    return FirstBellNode;
            }
        }

        public bool TryPlay(MaruRuleTopic topic)
        {
            if (HasPlayed(topic))
            {
                return false;
            }

            MarkPlayed(topic);
            string node = NodeFor(topic);
            LastRequestedNode = node;
            RequestedCount++;
            TopicRequested?.Invoke(topic, node);
            StartNode(node);
            return true;
        }

        public void ResetForTests()
        {
            firstBellPlayed = false;
            statueBrokenPlayed = false;
            biteEscapePlayed = false;
            LastRequestedNode = string.Empty;
            RequestedCount = 0;
        }

        private void Awake()
        {
            if (dialogueRunner == null)
            {
                dialogueRunner = GetComponent<DialogueRunner>();
            }

            if (linePresenter == null)
            {
                linePresenter =
                    GetComponentInChildren<MaruRuleLinePresenter2D>(true);
            }

            if (timeline == null)
            {
                timeline = FindFirstObjectByType<P8MaruTimeline2D>();
            }

            if (statue == null)
            {
                statue = FindFirstObjectByType<P8HomecomingStatue2D>();
            }

            if (biteController == null)
            {
                biteController =
                    FindFirstObjectByType<P8MaruBiteController2D>();
            }

            BindRunner();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void BindRunner()
        {
            if (dialogueRunner == null)
            {
                return;
            }

            if (linePresenter != null && !HasAnyPresenter())
            {
                dialogueRunner.DialoguePresenters =
                    new DialoguePresenterBase[] { linePresenter };
            }

            if (dialogueRunner.YarnProject == null
                && narrativeProject != null
                && HasProgram(narrativeProject))
            {
                dialogueRunner.SetProject(narrativeProject);
            }
        }

        private bool HasAnyPresenter()
        {
            foreach (DialoguePresenterBase presenter
                in dialogueRunner.DialoguePresenters)
            {
                if (presenter != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void Subscribe()
        {
            if (subscribed)
            {
                return;
            }

            subscribed = true;
            if (timeline != null)
            {
                timeline.PhaseChanged -= HandlePhaseChanged;
                timeline.PhaseChanged += HandlePhaseChanged;
                timeline.BellRang -= HandleBellRang;
                timeline.BellRang += HandleBellRang;
            }

            if (statue != null)
            {
                statue.StateChanged -= HandleStatueStateChanged;
                statue.StateChanged += HandleStatueStateChanged;
            }

            if (biteController != null)
            {
                biteController.BiteStarted -= HandleBiteStarted;
                biteController.BiteStarted += HandleBiteStarted;
            }
        }

        private void Unsubscribe()
        {
            subscribed = false;
            if (timeline != null)
            {
                timeline.PhaseChanged -= HandlePhaseChanged;
                timeline.BellRang -= HandleBellRang;
            }

            if (statue != null)
            {
                statue.StateChanged -= HandleStatueStateChanged;
            }

            if (biteController != null)
            {
                biteController.BiteStarted -= HandleBiteStarted;
            }
        }

        private void HandlePhaseChanged(P8MaruPhase phase)
        {
            if (phase == P8MaruPhase.FirstBell
                || phase == P8MaruPhase.SecondBell
                || phase == P8MaruPhase.Hunting)
            {
                TryPlay(MaruRuleTopic.FirstBell);
            }
        }

        private void HandleBellRang(P8BellEvent bell)
        {
            if (bell.Cause == P8BellCause.StatueDestroyed)
            {
                TryPlay(MaruRuleTopic.StatueBroken);
                return;
            }

            TryPlay(MaruRuleTopic.FirstBell);
        }

        private void HandleStatueStateChanged(P8StatueState state)
        {
            if (state == P8StatueState.Destroyed)
            {
                TryPlay(MaruRuleTopic.StatueBroken);
            }
        }

        private void HandleBiteStarted(int biteCount)
        {
            TryPlay(MaruRuleTopic.BiteEscape);
        }

        private void MarkPlayed(MaruRuleTopic topic)
        {
            switch (topic)
            {
                case MaruRuleTopic.StatueBroken:
                    statueBrokenPlayed = true;
                    break;
                case MaruRuleTopic.BiteEscape:
                    biteEscapePlayed = true;
                    break;
                default:
                    firstBellPlayed = true;
                    break;
            }
        }

        private void StartNode(string node)
        {
            if (!Application.isPlaying
                || dialogueRunner == null
                || dialogueRunner.IsDialogueRunning
                || !ResolveProgramReady()
                || !ContainsNode(node))
            {
                return;
            }

            _ = dialogueRunner.StartDialogue(node);
        }

        private bool ResolveProgramReady()
        {
            YarnProject project = dialogueRunner != null
                && dialogueRunner.YarnProject != null
                    ? dialogueRunner.YarnProject
                    : narrativeProject;
            return HasProgram(project);
        }

        private bool ContainsNode(string node)
        {
            YarnProject project = dialogueRunner != null
                && dialogueRunner.YarnProject != null
                    ? dialogueRunner.YarnProject
                    : narrativeProject;
            if (!HasProgram(project))
            {
                return false;
            }

            string[] names;
            try
            {
                names = project.NodeNames;
            }
            catch (Exception)
            {
                return false;
            }

            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], node, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasProgram(YarnProject project)
        {
            return project != null
                && project.compiledYarnProgram != null
                && project.compiledYarnProgram.Length > 0;
        }
    }
}

#endif
