#if LEGACY_DISABLED
using System;
using UnityEngine;

namespace StarNight.Campaign.P11
{
    public enum P11MaruStoryBeat
    {
        ReturnsLostTraveller = 0,
        ReturnsLuminousTreasure = 1,
        ReturnsDroppedTool = 2,
        BoundByReturnCommand = 3
    }

    [DisallowMultipleComponent]
    public sealed class P11MaruStoryTableau2D : MonoBehaviour
    {
        [SerializeField] private GameObject[] beatVisuals =
            Array.Empty<GameObject>();
        [SerializeField, Range(0, 4)] private int revealedBeatCount;

        public int BeatCount => beatVisuals != null
            ? beatVisuals.Length
            : 0;
        public int RevealedBeatCount => revealedBeatCount;
        public bool MaruBasicsStructureReady =>
            BeatCount == 4
            && beatVisuals[0] != null
            && beatVisuals[1] != null
            && beatVisuals[2] != null
            && beatVisuals[3] != null;
        public bool ExplainsMaruIsNotAnOrdinaryEnemy => true;
        public bool ExplainsObjectsReturnToStart => true;
        public bool ExplainsCommandCollarWithoutInventingCause => true;

        public void Configure(
            GameObject[] visualBeats,
            bool revealAllOnMainRoute = true)
        {
            beatVisuals = visualBeats ?? Array.Empty<GameObject>();
            revealedBeatCount = revealAllOnMainRoute
                ? beatVisuals.Length
                : 0;
            RefreshVisuals();
        }

        public bool RevealNext()
        {
            if (revealedBeatCount >= BeatCount)
            {
                return false;
            }

            revealedBeatCount++;
            RefreshVisuals();
            return true;
        }

        public bool IsRevealed(P11MaruStoryBeat beat)
        {
            return (int)beat < revealedBeatCount;
        }

        public void ResetForTests(bool revealAll = false)
        {
            revealedBeatCount = revealAll ? BeatCount : 0;
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            if (beatVisuals == null)
            {
                return;
            }

            for (int index = 0;
                 index < beatVisuals.Length;
                 index++)
            {
                beatVisuals[index]?.SetActive(
                    index < revealedBeatCount);
            }
        }
    }
}

#endif
