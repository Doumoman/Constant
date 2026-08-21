#if LEGACY_DISABLED
using System;
using System.Collections.Generic;
using StarNight.Campaign.P10;
using StarNight.Generation.P6;
using StarNight.MapHarness.P11;
using StarNight.Rooms;
using UnityEngine;

namespace StarNight.Debugging
{
    [DisallowMultipleComponent]
    public sealed class P10MapDirectionalityAudit2D : MonoBehaviour
    {
        [SerializeField] private P10StageEnvironment2D[] environments =
            Array.Empty<P10StageEnvironment2D>();
        [SerializeField] private string lastAudit;
        [SerializeField] private bool directionCueRetrofitComplete;
        [SerializeField] private bool variableRoomRebuildComplete;
        [SerializeField] private bool physicalCorridorRebuildComplete;

        public IReadOnlyList<P10StageEnvironment2D> Environments =>
            environments;
        public string LastAudit => lastAudit;
        public bool DirectionCueRetrofitComplete =>
            directionCueRetrofitComplete;
        public bool VariableRoomRebuildComplete =>
            variableRoomRebuildComplete;
        public bool PhysicalCorridorRebuildComplete =>
            physicalCorridorRebuildComplete;
        public bool VariableRoomRebuildPending =>
            !variableRoomRebuildComplete;
        public bool CorridorReassemblyPending =>
            !physicalCorridorRebuildComplete;
        public bool KnownLegacyRectangularStructure =>
            VariableRoomRebuildPending
            || CorridorReassemblyPending;
        public bool AuditHonestAboutRemainingMapWork =>
            directionCueRetrofitComplete
            && KnownLegacyRectangularStructure;

        public void Configure(P10StageEnvironment2D[] stageEnvironments)
        {
            environments = stageEnvironments
                ?? Array.Empty<P10StageEnvironment2D>();
            RefreshAudit();
        }

        public bool RefreshAudit()
        {
            int directionalStages = 0;
            int variableRoomStages = 0;
            int corridorStages = 0;
            for (int index = 0; index < environments.Length; index++)
            {
                P10StageEnvironment2D environment =
                    environments[index];
                if (environment == null
                    || environment.EnvironmentRoot == null)
                {
                    continue;
                }

                P11MapDirectionCue2D[] cues =
                    environment.EnvironmentRoot.GetComponentsInChildren<
                        P11MapDirectionCue2D>(true);
                bool hasMain = HasCue(cues, P11MapCueKind.MainExit);
                bool hasEdge = HasCue(cues, P11MapCueKind.ExitEdge);
                bool hasLandmark =
                    HasCue(cues, P11MapCueKind.Landmark);
                if (hasMain && hasEdge && hasLandmark)
                {
                    directionalStages++;
                }

                RoomTemplate2D[] rooms =
                    environment.EnvironmentRoot
                        .GetComponentsInChildren<RoomTemplate2D>(true);
                var sizes = new HashSet<Vector2Int>();
                for (int roomIndex = 0;
                     roomIndex < rooms.Length;
                     roomIndex++)
                {
                    if (rooms[roomIndex] != null)
                    {
                        sizes.Add(rooms[roomIndex].MacroSize);
                    }
                }

                if (rooms.Length >= 7 && sizes.Count >= 2)
                {
                    variableRoomStages++;
                }

                if (environment.EnvironmentRoot
                        .GetComponentsInChildren<
                            P6PhysicalCorridorModule2D>(true).Length > 0)
                {
                    corridorStages++;
                }
            }

            directionCueRetrofitComplete =
                environments.Length == 9
                && directionalStages == environments.Length;
            variableRoomRebuildComplete =
                environments.Length == 9
                && variableRoomStages == environments.Length;
            physicalCorridorRebuildComplete =
                environments.Length == 9
                && corridorStages == environments.Length;
            lastAudit =
                $"P10 direction cues {directionalStages}/"
                + $"{environments.Length}; variable-room stages "
                + $"{variableRoomStages}/{environments.Length}; "
                + $"physical-corridor stages {corridorStages}/"
                + $"{environments.Length}. "
                + (KnownLegacyRectangularStructure
                    ? "Variable room/corridor production rebuild remains."
                    : "Variable room and corridor rebuild complete.");
            return directionCueRetrofitComplete
                && variableRoomRebuildComplete
                && physicalCorridorRebuildComplete;
        }

        private static bool HasCue(
            IReadOnlyList<P11MapDirectionCue2D> cues,
            P11MapCueKind kind)
        {
            for (int index = 0; index < cues.Count; index++)
            {
                P11MapDirectionCue2D cue = cues[index];
                if (cue != null
                    && cue.CueKind == kind
                    && cue.Route == P11MapRouteKind.Main
                    && cue.VisibleAtEntry
                    && cue.RouteVisible
                    && cue.TextFreeVisualReady
                    && cue.Target != null
                    && (kind != P11MapCueKind.Landmark
                        || cue.Target.GetComponentInChildren<
                            SpriteRenderer>(true) != null))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
