#if LEGACY_DISABLED
using System;
using System.Collections;
using StarNight.Core.Flow;
using StarNight.Narrative;
using StarNight.Stage.Lab;
using StarNight.Stage.Rooms;
using StarNight.UI.HUD;
using StarNight.UI.Menus;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;

namespace StarNight.Integration
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Core04TwoRoomLab), typeof(GridLabSoakMonitor))]
    public sealed class Core12GridLab : MonoBehaviour
    {
        public const int RequiredRoomCount = 4;
        public const int ExitRoomIndex = 1;
        public const int AcceleratedSoakSeconds = 30 * 60;
        public const long MaximumAcceleratedGrowthBytes = 16L * 1024L * 1024L;

        private static readonly string[] RoomLabels =
        {
            "1. MOVE + JUMP",
            "2. INTERACTION + EXIT",
            "3. YARN + HUD",
            "4. MARU + PAUSE",
        };

        [SerializeField] private string dialogueNode = "STG_MOON_1_1_Intro";

        private Core04TwoRoomLab lab;

        public GridLabStation InteractionStation { get; private set; }
        public GridLabStation NarrativeStation { get; private set; }
        public GridLabSoakMonitor SoakMonitor { get; private set; }
        public long LastAcceleratedManagedGrowthBytes { get; private set; }
        public int LastAcceleratedTransitionCount { get; private set; }
        public bool LastAcceleratedSoakStable { get; private set; }
        public bool IsReady => lab != null && lab.Rooms.Count == RequiredRoomCount &&
            InteractionStation != null && NarrativeStation != null &&
            FindFirstObjectByType<HUDController>() != null &&
            FindFirstObjectByType<NarrativeSystemController>()?.Service != null &&
            FindFirstObjectByType<PauseMenuController>() != null;

        public void Configure(string yarnNode)
        {
            dialogueNode = yarnNode ?? string.Empty;
        }

        private void Awake()
        {
            lab = GetComponent<Core04TwoRoomLab>();
            lab.ConfigurePrototypeLayout(RequiredRoomCount, ExitRoomIndex);
            SoakMonitor = GetComponent<GridLabSoakMonitor>();
            if (GameBootstrap.IsReady && GameBootstrap.Instance.Services.TryGet(out GameFlowController flow))
            {
                flow.BeginStandaloneSession();
            }
        }

        private IEnumerator Start()
        {
            lab.BuildIfNeeded();
            for (int attempt = 0; attempt < 30 && lab.Rooms.Count < RequiredRoomCount; attempt++)
            {
                yield return null;
            }
            BuildRoomFixtures();
        }

        public IEnumerator RunAcceleratedTransitionSoak(int simulatedSeconds = AcceleratedSoakSeconds)
        {
            if (!IsReady || lab.TransitionController == null)
            {
                yield break;
            }

            const int warmupTransitions = 16;
            int direction = 1;
            int currentIndex = IndexOfRoom(lab.TransitionController.CurrentRoom);
            for (int index = 0; index < warmupTransitions; index++)
            {
                CommitNext(ref currentIndex, ref direction);
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            yield return null;
            long before = Profiler.GetMonoUsedSizeLong();

            LastAcceleratedTransitionCount = 0;
            for (int second = 0; second < Mathf.Max(0, simulatedSeconds); second++)
            {
                if (CommitNext(ref currentIndex, ref direction))
                {
                    LastAcceleratedTransitionCount++;
                }
                if ((second + 1) % 120 == 0)
                {
                    yield return null;
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            yield return null;
            LastAcceleratedManagedGrowthBytes = Profiler.GetMonoUsedSizeLong() - before;
            LastAcceleratedSoakStable = LastAcceleratedTransitionCount == Mathf.Max(0, simulatedSeconds) &&
                LastAcceleratedManagedGrowthBytes <= MaximumAcceleratedGrowthBytes;
        }

        private bool CommitNext(ref int currentIndex, ref int direction)
        {
            int nextIndex = currentIndex + direction;
            if (nextIndex < 0 || nextIndex >= lab.Rooms.Count)
            {
                direction *= -1;
                nextIndex = currentIndex + direction;
            }
            RoomRuntime current = lab.Rooms[currentIndex];
            RoomRuntime next = lab.Rooms[nextIndex];
            var portal = lab.GetPortal(current.RoomId, next.RoomId);
            if (portal == null || !lab.TransitionController.CommitImmediate(portal))
            {
                return false;
            }
            currentIndex = nextIndex;
            return true;
        }

        private int IndexOfRoom(RoomRuntime room)
        {
            for (int index = 0; index < lab.Rooms.Count; index++)
            {
                if (lab.Rooms[index] == room)
                {
                    return index;
                }
            }
            return 0;
        }

        private void BuildRoomFixtures()
        {
            for (int index = 0; index < lab.Rooms.Count && index < RoomLabels.Length; index++)
            {
                RoomRuntime room = lab.Rooms[index];
                Transform markerRoot = room.transform.Find("DebugRoot") ?? room.transform;
                Transform existing = markerRoot.Find("GridLabRoleLabel");
                if (existing == null)
                {
                    GameObject labelObject = new("GridLabRoleLabel", typeof(TextMeshPro));
                    labelObject.transform.SetParent(markerRoot, false);
                    labelObject.transform.localPosition = new Vector3(Core04TwoRoomLab.RoomWidth * 0.5f, 6.7f, 0f);
                    TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
                    label.text = RoomLabels[index];
                    label.fontSize = 0.7f;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = new Color(0.92f, 0.87f, 0.65f, 1f);
                    label.sortingOrder = 60;
                }
            }

            InteractionStation = BuildStation(lab.Rooms[1], "InteractionStation", 8f, GridLabStationRole.Interaction);
            NarrativeStation = BuildStation(lab.Rooms[2], "NarrativeStation", 12f, GridLabStationRole.Narrative);
        }

        private GridLabStation BuildStation(RoomRuntime room, string stationName, float localX, GridLabStationRole role)
        {
            Transform parent = room.DynamicRoot != null ? room.DynamicRoot : room.transform;
            Transform existing = parent.Find(stationName);
            GameObject stationObject = existing != null ? existing.gameObject : new GameObject(stationName);
            stationObject.transform.SetParent(parent, false);
            stationObject.transform.localPosition = new Vector3(localX, 1.7f, 0f);
            GridLabStation station = stationObject.GetComponent<GridLabStation>();
            if (station == null)
            {
                station = stationObject.AddComponent<GridLabStation>();
            }
            station.Configure(role, dialogueNode);
            return station;
        }
    }
}

#endif
