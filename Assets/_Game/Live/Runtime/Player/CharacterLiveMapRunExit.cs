using UnityEngine;

namespace StarNight.Character.Live.Player
{
    /// <summary>Fixture-local exit observation point; it never changes scene or run state.</summary>
    public sealed class CharacterLiveMapRunExit : MonoBehaviour
    {
        public bool HasBeenReached { get; private set; }
        public CharacterLivePlayerRig ReachedBy { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CharacterLivePlayerRig rig = other == null ? null :
                other.GetComponentInParent<CharacterLivePlayerRig>();
            if (rig == null)
            {
                return;
            }

            HasBeenReached = true;
            ReachedBy = rig;
        }
    }
}
