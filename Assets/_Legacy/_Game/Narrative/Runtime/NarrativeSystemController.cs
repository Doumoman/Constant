#if LEGACY_DISABLED
using StarNight.Narrative.Presenters;
using TMPro;
using UnityEngine;
using Yarn.Unity;

namespace StarNight.Narrative
{
    [DisallowMultipleComponent]
    public sealed class NarrativeSystemController : MonoBehaviour
    {
        [SerializeField] private YarnProject yarnProject;
        [SerializeField] private CharacterDatabase characterDatabase;
        [SerializeField] private TMP_FontAsset dialogueFont;

        public DialogueRunner Runner { get; private set; }
        public NarrativeService Service { get; private set; }
        public NarrativeCommandBridge CommandBridge { get; private set; }
        public DialogueInputRouter InputRouter { get; private set; }
        public NarrativeUIState UIState { get; private set; }
        public NarrativeViewLayout View { get; private set; }

        public void Configure(YarnProject project, CharacterDatabase characters, TMP_FontAsset font)
        {
            yarnProject = project;
            characterDatabase = characters;
            dialogueFont = font;
        }

        private void Awake()
        {
            NarrativeSystemController[] systems = FindObjectsByType<NarrativeSystemController>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            if (systems.Length > 1 && systems[0] != this)
            {
                Destroy(gameObject);
                return;
            }

            BuildSystem();
        }

        private void BuildSystem()
        {
            View = NarrativeViewLayout.Build(transform, dialogueFont);
            GameObject systemObject = new("DialogueSystem");
            systemObject.transform.SetParent(transform, false);

            UIState = systemObject.AddComponent<NarrativeUIState>();
            ConversationPanelPresenter conversation = systemObject.AddComponent<ConversationPanelPresenter>();
            FieldBubblePresenter bubble = systemObject.AddComponent<FieldBubblePresenter>();
            NarrationCardPresenter narration = systemObject.AddComponent<NarrationCardPresenter>();
            GameOptionsPresenter options = systemObject.AddComponent<GameOptionsPresenter>();

            conversation.Configure(View, UIState, characterDatabase);
            bubble.Configure(View, UIState, characterDatabase);
            narration.Configure(View, UIState, characterDatabase);
            options.Configure(View, UIState);

            Runner = systemObject.AddComponent<DialogueRunner>();
            if (yarnProject != null)
            {
                Runner.SetProject(yarnProject);
            }
            Runner.DialoguePresenters = new DialoguePresenterBase[] { conversation, bubble, narration, options };
            Runner.autoStart = false;
            Runner.runSelectedOptionAsLine = false;

            Service = systemObject.AddComponent<NarrativeService>();
            Service.Configure(Runner, UIState);
            CommandBridge = systemObject.AddComponent<NarrativeCommandBridge>();
            CommandBridge.Configure(Runner, Service, UIState);
            InputRouter = systemObject.AddComponent<DialogueInputRouter>();
            InputRouter.Configure(Runner, UIState, options);
        }
    }
}

#endif
